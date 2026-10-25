using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A discrete solver proves a normal-tap solution, then the batch entrypoint replays it
// through the real board, belt and customer code. It never forces victory or uses boosters.
[InitializeOnLoad]
public static class SushiCampaignRegression
{
    private const string PendingKey = "SushiCampaignRegression.Pending";
    private const string StartedKey = "SushiCampaignRegression.Started";
    private const string ReportPath = "CampaignRegressionResults.txt";
    private static IEnumerator runner;
    private static readonly List<string> report = new List<string>();
    private static readonly List<string> runtimeErrors = new List<string>();

    static SushiCampaignRegression()
    {
        EditorApplication.update += Update;
        Application.logMessageReceived += OnLog;
    }

    public static bool TrySolve(SushiLevelData level, out List<int> taps, out string detail)
    {
        taps = new List<int>();
        string error = level != null ? level.ValidateData() : "Missing level.";
        if (error != null) { detail = error; return false; }
        var solver = new Planner(level);
        bool solved = solver.Solve(taps);
        detail = solved
            ? taps.Count + " taps; peak reserved " + solver.SolutionPeak + "/" + level.beltSlots +
              "; searched " + solver.Visited + " states."
            : "No normal-tap solution within " + solver.Visited + " searched states.";
        return solved;
    }

    [MenuItem("Sushi/Validate 20 Level Campaign")]
    public static void ValidateCampaign()
    {
        var campaign = FindCampaign();
        Check(campaign.listAllLevel != null && campaign.listAllLevel.Count == 20,
            "The active campaign must contain exactly 20 levels.");
        var unique = new HashSet<SushiLevelData>();
        int previousFood = 0, previousCustomers = 0;
        int firstHidden = 0, firstIce = 0, firstLink = 0, firstTray = 0;
        for (int index = 0; index < campaign.listAllLevel.Count; index++)
        {
            SushiLevelData level = campaign.listAllLevel[index];
            Check(level != null && unique.Add(level), "Campaign has a missing or duplicate level.");
            Check(level.number == index + 1, "Level numbering and active campaign order disagree.");
            Check(level.numberColumns >= 2 && level.numberColumns <= 5,
                "Level " + level.number + " must use at most five columns.");
            Check(level.boardColumns != null && level.boardColumns.Count == level.numberColumns,
                "Campaign boards must be authored explicitly for predictable progression.");
            Check(!string.IsNullOrWhiteSpace(level.levelName) && !string.IsNullOrWhiteSpace(level.hint),
                "Every campaign level needs a name and a play hint.");
            string error = level.ValidateData();
            Check(error == null, "Level " + level.number + ": " + error);
            var board = level.CreateBoardData();
            int hidden = 0, ice = 0, linkedCells = 0, trayCells = 0;
            foreach (FoodQueueData column in board)
                foreach (FoodData food in column.foods)
                {
                    if (food.hidden || food.foodState == FoodState.HoiCham) hidden++;
                    if (food.iceLayers > 0) ice++;
                    if (food.linkId > 0 || food.lockId > 0) linkedCells++;
                    if (food.trayId > 0) trayCells++;
                }
            if (hidden > 0 && firstHidden == 0) firstHidden = level.number;
            if (ice > 0 && firstIce == 0) firstIce = level.number;
            if (linkedCells > 0 && firstLink == 0) firstLink = level.number;
            if (trayCells > 0 && firstTray == 0) firstTray = level.number;
            Check(level.number >= 5 || hidden == 0, "Hidden tiles appeared before level 5.");
            Check(level.number >= 7 || ice == 0, "Ice appeared before level 7.");
            Check(level.number >= 9 || linkedCells == 0, "Links appeared before level 9.");
            Check(level.number >= 11 || trayCells == 0, "Trays appeared before level 11.");
            foreach (CustomerOrderData customer in level.customers)
                Check(!customer.rushCustomer, "This campaign does not introduce an extra rush mechanic.");
            int foodCount = level.GetTotalFoodCount();
            Check(foodCount >= previousFood && level.CustomerCount >= previousCustomers,
                "Campaign workload must not decrease between consecutive levels.");
            previousFood = foodCount;
            previousCustomers = level.CustomerCount;
            Check(TrySolve(level, out var taps, out string detail),
                "Level " + level.number + " cannot be completed normally: " + detail);
            Pass("Level " + level.number.ToString("00") + ": " + level.CustomerCount +
                " customers, " + foodCount + " ingredients, " + level.numberColumns + " columns; hidden " +
                hidden + ", ice " + ice + ", linked cells " + linkedCells + ", tray cells " + trayCells +
                "; " + detail);
        }
        Check(firstHidden == 5 && firstIce == 7 && firstLink == 9 && firstTray == 11,
            "Mechanics must debut exactly at levels 5 / 7 / 9 / 11.");
        var firstBoard = campaign.listAllLevel[0].CreateBoardData();
        FoodData tutorial = firstBoard[1].foods[0];
        Check(tutorial.typeFood == PoolType.Shrimp && tutorial.amount == 1 &&
            tutorial.iceLayers == 0 && !tutorial.hidden && tutorial.foodState == FoodState.Normal &&
            tutorial.linkId == 0 && tutorial.lockId == 0 && tutorial.trayId == 0,
            "Level 1 must preserve the configured single shrimp tutorial target in column 2.");
        CustomerOrderData tutorialCustomer = campaign.listAllLevel[0].customers[0];
        Check(tutorialCustomer.UsesRecipes && tutorialCustomer.dishes.Count > 0 &&
            SushiRecipeCatalog.Get(tutorialCustomer.dishes[0].dish).Ingredients.Contains(PoolType.Shrimp),
            "The first customer's first dish must include the configured tutorial shrimp.");
        Pass("All 20 assets validate, have exact ingredient balances, and have a legal normal-tap solution.");
    }

    public static void RunBatch()
    {
        Check(Application.isBatchMode && PlayerSettings.productName == "Sushi UI Verification" &&
            Application.persistentDataPath.Contains("Sushi UI Verification"),
            "Run only in the isolated Sushi UI Verification project.");
        report.Clear();
        runtimeErrors.Clear();
        ValidateCampaign();
        File.WriteAllText(ReportPath, "RUNNING\n" + string.Join("\n", report) + "\n");
        GameDataFragment data = AssetDatabase.LoadAssetAtPath<GameDataFragment>(
            "Assets/_game/DataSO/GameDataFragment.asset");
        Check(data != null, "Missing validation profile asset.");
        data.dataPlayer = new GameDataFragment.DataPlayer
        {
            cur_level = 0, showTutorialLevel1 = true,
            money = 1000, boosterClick = 0, boosterSuffle = 0, boosterHut = 0
        };
        data.Save();
        EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(PendingKey, true);
        runner = null;
        EditorApplication.isPlaying = true;
    }

    private static AllLevelData FindCampaign()
    {
        string[] assets = AssetDatabase.FindAssets("t:AllLevelData", new[] { "Assets/_game" });
        foreach (string guid in assets)
        {
            AllLevelData candidate = AssetDatabase.LoadAssetAtPath<AllLevelData>(AssetDatabase.GUIDToAssetPath(guid));
            if (candidate != null && candidate.name == "_AllLevelData") return candidate;
        }
        throw new InvalidOperationException("Missing active _AllLevelData asset.");
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        // Unity 6's editor search database can throw while entering batch Play Mode.
        // Exclude that editor indexing failure; gameplay errors still fail this regression.
        if (!string.IsNullOrEmpty(stack) && stack.Contains("UnityEditor.Search")) return;
        if (SessionState.GetBool(PendingKey, false) && (type == LogType.Error || type == LogType.Exception))
            runtimeErrors.Add(message + "\n" + stack);
    }

    private static void Update()
    {
        if (!SessionState.GetBool(PendingKey, false) || EditorApplication.isCompiling) return;
        try
        {
            Check(EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0) < 900,
                "Campaign integration timed out.");
            Check(runtimeErrors.Count == 0, "Runtime emitted an error: " + string.Join("\n", runtimeErrors));
            if (!Application.isPlaying || GameManager.ins == null || GameManager.ins.IsLoadingLevel ||
                LevelManager.Ins == null || DataManager.ins == null || !DataManager.ins.isLoaded) return;
            if (runner == null) runner = PlayCampaign();
            if (runner.MoveNext()) return;
            (runner as IDisposable)?.Dispose();
            runner = null;
            SessionState.SetBool(PendingKey, false);
            File.AppendAllText(ReportPath, "\nPASS\n" + string.Join("\n", report) + "\n");
            Debug.Log("CAMPAIGN_REGRESSION_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            SessionState.SetBool(PendingKey, false);
            (runner as IDisposable)?.Dispose();
            runner = null;
            Time.timeScale = 1;
            File.AppendAllText(ReportPath, "\nFAIL\n" + string.Join("\n", report) + "\n" + error);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static IEnumerator PlayCampaign()
    {
        LevelManager level = LevelManager.Ins;
        AllLevelData campaign = FindCampaign();
        float previousMaximumDelta = Time.maximumDeltaTime;
        try
        {
            Time.maximumDeltaTime = 1;
            for (int index = 0; index < campaign.listAllLevel.Count; index++)
            {
                SushiLevelData data = campaign.listAllLevel[index];
                Check(TrySolve(data, out List<int> taps, out string detail), "Solver failed at runtime: " + detail);
                level.LoadLevel(index);
                Check(level.CurrentLevel == data && level.IsPlaying && !SushiLevelOneTutorial.IsInputLocked,
                    "Cannot enter campaign level " + (index + 1) + ".");
                Time.timeScale = 30; // Test clock only; use the authored conveyor speed and normal taps.
                int peak = 0;
                double deadline = EditorApplication.timeSinceStartup + 120;
                yield return null;
                for (int move = 0; move < taps.Count; move++)
                {
                    while (!IsSettled(level))
                    {
                        CheckStillPlaying(level, index, deadline);
                        yield return null;
                    }
                    Check(!level.HasWon && level.CanInteract, "Level ended before the solution's final tap.");
                    FoodQueue queue = level.Board.FoodQueues[taps[move]];
                    Check(level.Conveyor.TryTakeFromQueue(queue),
                        "Level " + (index + 1) + " rejected normal tap " + (move + 1) + " at column " +
                        (taps[move] + 1) + ". " + DescribeBoard(level));
                    peak = Mathf.Max(peak, level.Conveyor.OccupiedSlotCount);
                    Check(peak <= data.beltSlots, "Atomic pickup exceeded the belt capacity.");
                    yield return null;
                }
                while (!level.HasWon)
                {
                    CheckStillPlaying(level, index, deadline);
                    yield return null;
                }
                Check(level.Board.IsEmpty() && level.Conveyor.OccupiedSlotCount == 0 &&
                    level.Conveyor.DeliveringCount == 0 && level.Conveyor.Customers.AllCustomersCompleted &&
                    UIManager.Ins.IsOpened<CanvasVictory>(),
                    "Victory did not follow actual delivery of every requested ingredient.");
                var player = GameDataFragment.cur.dataPlayer;
                Check(player.boosterClick == 0 && player.boosterSuffle == 0 && player.boosterHut == 0,
                    "Campaign solution used or changed booster inventory.");
                Pass("Runtime level " + (index + 1).ToString("00") + " WON with " + taps.Count +
                    " normal taps; peak " + peak + "/" + data.beltSlots + "; no boosts or revive.");
                File.AppendAllText(ReportPath, report[report.Count - 1] + "\n");
            }
        }
        finally
        {
            Time.timeScale = 1;
            Time.maximumDeltaTime = previousMaximumDelta;
        }
    }

    private static bool IsSettled(LevelManager level)
    {
        if (level.HasWon) return true;
        foreach (FoodQueue column in level.Board.FoodQueues)
        {
            if (column.IsPickupPending) return false;
            FoodShushi head = column.GetTopFood();
            if (head != null && (head.IsHidden || head.IsRevealing)) return false;
        }
        ConveyorManager belt = level.Conveyor;
        if (belt.DeliveringCount > 0) return false;
        CustomerManager customers = belt.Customers;
        int seated = 0;
        foreach (Customer customer in customers.ActiveCustomers)
        {
            if (customer == null) continue;
            if (!customer.IsSeated || customer.IsCompleted) return false;
            seated++;
        }
        if (seated < Mathf.Min(2, customers.TotalCustomers - customers.CompletedCustomers)) return false;
        int settled = 0;
        foreach (Transform slot in belt.Slots)
        {
            FoodShushi food = slot != null ? slot.GetComponentInChildren<FoodShushi>() : null;
            if (food == null) continue;
            if (food.Location != FoodLocation.Conveyor || customers.FirstCustomerFor(food.Type) != null) return false;
            settled++;
        }
        return settled == belt.OccupiedSlotCount;
    }

    private static void CheckStillPlaying(LevelManager level, int index, double deadline)
    {
        Check(EditorApplication.timeSinceStartup < deadline,
            "Level " + (index + 1) + " stalled: " + DescribeBoard(level));
        Check(level.PendingRevive == SushiReviveReason.None && (level.IsPlaying || level.HasWon),
            "Level " + (index + 1) + " lost or needed a revive: " + DescribeBoard(level));
    }

    private static string DescribeBoard(LevelManager level)
    {
        var text = new StringBuilder("Belt " + level.Conveyor.OccupiedSlotCount + "/" + level.Conveyor.Capacity + "; ");
        foreach (FoodQueue column in level.Board.FoodQueues)
        {
            FoodShushi top = column.GetTopFood();
            text.Append("c").Append(column.columnIndex + 1).Append('=');
            if (top == null) text.Append("empty");
            else text.Append(top.Type).Append(" ice").Append(top.IceLayers).Append(" link").Append(top.LinkId)
                .Append(" tray").Append(top.TrayId).Append(" rows").Append(column.Count);
            text.Append("; ");
        }
        return text.ToString();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Pass(string message) { report.Add(message); Debug.Log("CAMPAIGN_CHECK: " + message); }

    private sealed class Planner
    {
        private readonly FoodData[][] columns;
        private readonly int[] offsets;
        private readonly int capacity;
        private readonly bool startsWithTutorial;
        private readonly int typeCount;
        private readonly int[][] orders;
        private readonly HashSet<string> visited = new HashSet<string>();
        private const int SearchLimit = 200000;
        public int Visited => visited.Count;
        public int SolutionPeak { get; private set; }

        private sealed class State
        {
            public int[] heads, ice, belt;
            public int[][] remaining;
            public int peak;
            public State Clone()
            {
                var copy = new State
                {
                    heads = (int[])heads.Clone(), ice = (int[])ice.Clone(),
                    belt = (int[])belt.Clone(), remaining = new int[remaining.Length][], peak = peak
                };
                for (int i = 0; i < remaining.Length; i++) copy.remaining[i] = (int[])remaining[i].Clone();
                return copy;
            }
        }
        private sealed class Move
        {
            public int column, score;
            public State state;
        }

        public Planner(SushiLevelData level)
        {
            capacity = level.beltSlots;
            startsWithTutorial = level.number == 1;
            foreach (PoolType type in Enum.GetValues(typeof(PoolType))) typeCount = Math.Max(typeCount, (int)type + 1);
            List<FoodQueueData> source = level.CreateBoardData();
            columns = new FoodData[source.Count][];
            offsets = new int[source.Count + 1];
            for (int c = 0; c < source.Count; c++)
            {
                columns[c] = source[c].foods.ToArray();
                offsets[c + 1] = offsets[c] + columns[c].Length;
            }
            orders = new int[level.customers.Count][];
            for (int i = 0; i < orders.Length; i++)
            {
                orders[i] = new int[typeCount];
                foreach (var order in level.customers[i].CreateIngredientOrders())
                    orders[i][(int)order.typeFood] += order.amount;
            }
        }

        public bool Solve(List<int> path)
        {
            var state = new State
            {
                heads = new int[columns.Length], ice = new int[offsets[columns.Length]],
                belt = new int[typeCount], remaining = new int[orders.Length][]
            };
            for (int c = 0; c < columns.Length; c++)
                for (int row = 0; row < columns[c].Length; row++)
                    state.ice[offsets[c] + row] = columns[c][row].iceLayers;
            for (int i = 0; i < orders.Length; i++) state.remaining[i] = (int[])orders[i].Clone();
            if (startsWithTutorial)
            {
                // Preserve a valid route from the configured first pickup if the tutorial is re-enabled.
                State afterTutorial = columns.Length > 1 ? Tap(state, 1) : null;
                if (afterTutorial == null) return false;
                path.Add(1);
                if (Search(afterTutorial, path)) return true;
                path.Clear();
                return false;
            }
            return Search(state, path);
        }

        private bool Search(State state, List<int> path)
        {
            bool empty = true;
            for (int c = 0; c < columns.Length; c++) empty &= state.heads[c] == columns[c].Length;
            if (empty)
            {
                if (Sum(state.belt) != 0) return false;
                foreach (int[] customer in state.remaining) if (Sum(customer) != 0) return false;
                SolutionPeak = state.peak;
                return true;
            }
            if (visited.Count >= SearchLimit || !visited.Add(Key(state))) return false;
            var moves = new List<Move>();
            for (int c = 0; c < columns.Length; c++)
            {
                State next = Tap(state, c);
                if (next == null) continue;
                int consumed = 0;
                for (int i = 0; i < orders.Length; i++)
                    consumed += (Sum(state.remaining[i]) - Sum(next.remaining[i])) * (orders.Length - i);
                int iceBroken = Sum(state.ice) - Sum(next.ice);
                moves.Add(new Move
                {
                    column = c, state = next,
                    score = consumed * 20 + iceBroken * 5 - Sum(next.belt) * 3 - next.heads[c]
                });
            }
            moves.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.column.CompareTo(b.column));
            foreach (Move move in moves)
            {
                path.Add(move.column);
                if (Search(move.state, path)) return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        private State Tap(State state, int column)
        {
            int row = state.heads[column];
            if (row == columns[column].Length || state.ice[offsets[column] + row] > 0) return null;
            FoodData top = columns[column][row];
            var removed = new int[columns.Length];
            removed[column] = 1;
            int link = top.linkId > 0 ? top.linkId : top.lockId;
            if (link > 0)
            {
                int partner = -1;
                for (int c = 0; c < columns.Length; c++)
                {
                    if (c == column || state.heads[c] >= columns[c].Length) continue;
                    FoodData head = columns[c][state.heads[c]];
                    if ((head.linkId > 0 ? head.linkId : head.lockId) == link &&
                        state.ice[offsets[c] + state.heads[c]] == 0) { partner = c; break; }
                }
                if (partner < 0) return null;
                removed[partner] = 1;
            }
            else if (top.trayId > 0)
            {
                for (int r = row + 1; r < columns[column].Length && columns[column][r].trayId == top.trayId; r++)
                    removed[column]++;
            }
            int amount = 0;
            for (int c = 0; c < columns.Length; c++)
                for (int r = 0; r < removed[c]; r++) amount += columns[c][state.heads[c] + r].amount;
            int occupied = Sum(state.belt) + amount;
            if (occupied > capacity) return null;
            State next = state.Clone();
            next.peak = Math.Max(next.peak, occupied);
            // Damage is counted once per shifted adjacent column, against its pre-move visible rows.
            for (int source = 0; source < columns.Length; source++)
            {
                if (removed[source] == 0) continue;
                int visible = Math.Min(4, columns[source].Length - state.heads[source]);
                for (int neighbor = Math.Max(0, source - 1); neighbor <= Math.Min(columns.Length - 1, source + 1); neighbor++)
                {
                    if (neighbor == source) continue;
                    for (int r = 0; r < visible && state.heads[neighbor] + r < columns[neighbor].Length; r++)
                    {
                        int cell = offsets[neighbor] + state.heads[neighbor] + r;
                        next.ice[cell] = Math.Max(0, next.ice[cell] - 1);
                    }
                }
            }
            for (int c = 0; c < columns.Length; c++)
                for (int r = 0; r < removed[c]; r++)
                {
                    FoodData food = columns[c][next.heads[c]++];
                    next.belt[(int)food.typeFood] += food.amount;
                }
            Settle(next);
            return next;
        }

        private static void Settle(State state)
        {
            bool moved;
            do
            {
                moved = false;
                int seated = 0;
                for (int customer = 0; customer < state.remaining.Length && seated < 2; customer++)
                {
                    int[] needs = state.remaining[customer];
                    if (Sum(needs) == 0) continue;
                    seated++;
                    for (int type = 0; type < needs.Length; type++)
                    {
                        int count = Math.Min(needs[type], state.belt[type]);
                        if (count == 0) continue;
                        needs[type] -= count;
                        state.belt[type] -= count;
                        moved = true;
                    }
                }
            } while (moved);
        }

        private static int Sum(int[] values) { int result = 0; foreach (int value in values) result += value; return result; }
        private static string Key(State state)
        {
            var text = new StringBuilder();
            Append(text, state.heads); Append(text, state.ice); Append(text, state.belt);
            foreach (int[] customer in state.remaining) Append(text, customer);
            return text.ToString();
        }
        private static void Append(StringBuilder text, int[] values)
        {
            foreach (int value in values) text.Append(value).Append(',');
            text.Append('|');
        }
    }
}