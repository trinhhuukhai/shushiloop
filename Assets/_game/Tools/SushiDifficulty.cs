#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Đặt trong thư mục Editor (cùng SushiLevelEditorWindow.cs).
///
/// MÔ HÌNH MÔ PHỎNG (chỉnh nếu luật game khác):
///  - Chỉ pick được ô ĐẦU CỘT (hàng 1). Mỗi lần pick, từng đơn vị nguyên liệu (×amount) được giao cho
///    khách đang phục vụ nếu họ cần; nếu không thì nằm lại băng chuyền (tối đa beltSlots).
///  - Khách phục vụ theo thứ tự; cùng lúc có `seats` khách đang chờ (mặc định 3).
///    Khi khách xong, khách kế tiếp vào và nhận ngay nguyên liệu đang nằm trên băng chuyền.
///  - Không còn nước đi hợp lệ (băng chuyền đầy / kẹt element) trước khi hết khách => thua.
///  - Ice: ô băng ở đầu cột không pick được; mỗi lần pick ở cột sát bên trừ 1 lớp.
///  - Link: 2 ô cùng linkId chỉ pick được khi cả hai cùng ở đầu cột, và pick cùng lúc.
///  - Hidden/Tray được coi như ô thường khi mô phỏng (nhưng giữ nguyên vị trí khi sắp xếp).
///
/// THUẬT TOÁN KHÓ THEO K (1..10):
///  1. Xếp dễ nhất: ô thường sắp theo thứ tự khách cần, đặt theo hàng (ngay đầu cột = cần sớm nhất).
///  2. Hệ số K -> mục tiêu "tỉ lệ thua" của một người chơi biết thứ tự khách nhưng thỉnh thoảng
///     pick nhầm (PlayerNoise). K=1 ≈ 2%, K=10 ≈ 90%.
///  3. Simulated annealing: hoán đổi 2 ô thường, giữ nếu tỉ lệ thua ước lượng tiến gần mục tiêu
///     VÀ level vẫn giải được (người chơi hoàn hảo: greedy rồi DFS có giới hạn).
///  Các ô element (Ice/Hidden/Link/Tray) giữ nguyên vị trí.
/// </summary>
public static class SushiDifficulty
{
    public const float PlayerNoise = 0.25f;
    private const int ExploreBudget = 15000;
    private const int PlayoutsPerEval = 32;
    private const int PlayoutsFinal = 200;

    public sealed class Result
    {
        public bool ok;
        public string message;
        public float targetLoss;
        public float estimatedLoss;
        public bool solvable;
        public int minBelt = -1;
        public int iterations;
    }

    private struct Tile
    {
        public int type, amount, ice, link;
        public bool free;
        public FoodData src;
    }

    private sealed class Model
    {
        public int T, C, cols, belt, seats;
        public int[] needInit;
        public Tile[][] tiles;
    }

    private sealed class State
    {
        public int[] head, ice, belt, need, left;
        public int beltCount, first;

        public State Clone()
        {
            return new State
            {
                head = (int[])head.Clone(),
                ice = (int[])ice.Clone(),
                belt = (int[])belt.Clone(),
                need = (int[])need.Clone(),
                left = (int[])left.Clone(),
                beltCount = beltCount,
                first = first
            };
        }
    }

    public static float TargetLoss(int k)
    {
        return Mathf.Lerp(0.02f, 0.90f, (Mathf.Clamp(k, 1, 10) - 1) / 9f);
    }

    // ───────────────────────── Public API ─────────────────────────

    /// Chấm độ khó board hiện tại (không sửa gì).
    public static Result Evaluate(SushiLevelData level, int k, int seats)
    {
        var r = new Result { targetLoss = TargetLoss(k) };
        var m = Build(level, seats, out string err);
        if (m == null) { r.message = err; return r; }
        Fill(r, m, level.boardSeed + k * 131);
        r.ok = true;
        r.message = "Đã chấm điểm board hiện tại.";
        return r;
    }

    /// Sắp xếp lại các ô thường trên board theo hệ số K. Trả về kết quả; board chỉ bị sửa khi r.ok.
    public static Result Arrange(SushiLevelData level, int k, int seats, int iterations, Func<float, bool> cancelWithProgress)
    {
        var r = new Result { targetLoss = TargetLoss(k) };
        var m = Build(level, seats, out string err);
        if (m == null) { r.message = err; return r; }

        var slots = new List<Vector2Int>();
        for (int c = 0; c < m.cols; c++)
            for (int row = 0; row < m.tiles[c].Length; row++)
                if (m.tiles[c][row].free) slots.Add(new Vector2Int(c, row));
        if (slots.Count < 2) { r.message = "Quá ít ô thường để sắp xếp."; return r; }

        // Thứ tự khách cần từng đơn vị nguyên liệu.
        var dueUnits = new List<int>[m.T];
        for (int t = 0; t < m.T; t++) dueUnits[t] = new List<int>();
        for (int c = 0; c < m.C; c++)
            for (int t = 0; t < m.T; t++)
                for (int u = 0; u < m.needInit[c * m.T + t]; u++) dueUnits[t].Add(c);

        var ptr = new int[m.T];
        var freeTiles = new List<(Tile tile, int due)>();
        foreach (var s in slots)
        {
            Tile t = m.tiles[s.x][s.y];
            int due = ptr[t.type] < dueUnits[t.type].Count ? dueUnits[t.type][ptr[t.type]] : m.C;
            ptr[t.type] += t.amount;
            freeTiles.Add((t, due));
        }
        var sortedTiles = freeTiles.OrderBy(x => x.due).Select(x => x.tile).ToList();
        var sortedSlots = slots.OrderBy(s => s.y).ThenBy(s => s.x).ToList();

        // 1) Xếp dễ nhất.
        for (int i = 0; i < sortedSlots.Count; i++)
            m.tiles[sortedSlots[i].x][sortedSlots[i].y] = sortedTiles[i];

        if (!Solvable(m))
        {
            r.message = "Mô phỏng không giải được ngay cả khi xếp dễ nhất (kiểm tra beltSlots, số khách phục vụ cùng lúc, hoặc element Ice/Link).";
            return r;
        }

        // 2) Annealing.
        int seed = level.boardSeed + k * 131;
        var rnd = new System.Random(seed);
        float target = r.targetLoss;
        float cur = LossRate(m, seed, PlayoutsPerEval);
        float score = Mathf.Abs(cur - target);
        float bestScore = score;
        var best = sortedSlots.Select(s => m.tiles[s.x][s.y]).ToArray();
        int it = 0;

        for (; it < iterations && bestScore > 0.03f; it++)
        {
            if (it % 40 == 0 && cancelWithProgress != null && cancelWithProgress((float)it / iterations)) break;

            int a = rnd.Next(sortedSlots.Count), b = rnd.Next(sortedSlots.Count);
            if (a == b) continue;
            var sa = sortedSlots[a]; var sb = sortedSlots[b];
            Tile ta = m.tiles[sa.x][sa.y], tb = m.tiles[sb.x][sb.y];
            if (ta.type == tb.type && ta.amount == tb.amount) continue;

            m.tiles[sa.x][sa.y] = tb; m.tiles[sb.x][sb.y] = ta;
            float d = LossRate(m, seed, PlayoutsPerEval);
            float sc = Mathf.Abs(d - target);
            float temp = Mathf.Lerp(0.08f, 0.004f, (float)it / iterations);
            bool accept = sc <= score || rnd.NextDouble() < Math.Exp(-(sc - score) / temp);

            if (accept && Solvable(m))
            {
                score = sc;
                if (sc < bestScore)
                {
                    bestScore = sc;
                    for (int i = 0; i < sortedSlots.Count; i++) best[i] = m.tiles[sortedSlots[i].x][sortedSlots[i].y];
                }
            }
            else
            {
                m.tiles[sa.x][sa.y] = ta; m.tiles[sb.x][sb.y] = tb;
            }
        }
        r.iterations = it;

        // 3) Ghi kết quả tốt nhất.
        for (int i = 0; i < sortedSlots.Count; i++)
        {
            m.tiles[sortedSlots[i].x][sortedSlots[i].y] = best[i];
            level.boardColumns[sortedSlots[i].x].foods[sortedSlots[i].y] = best[i].src;
        }

        Fill(r, m, seed);
        r.ok = true;
        r.message = Mathf.Abs(r.estimatedLoss - target) <= 0.08f
            ? "Đã sắp xếp theo K."
            : "Đã sắp xếp, nhưng chưa chạm mục tiêu (có thể đã chạm trần độ khó với beltSlots/seats hiện tại).";
        return r;
    }

    private static void Fill(Result r, Model m, int seed)
    {
        r.estimatedLoss = LossRate(m, seed, PlayoutsFinal);
        r.solvable = Solvable(m);
        int orig = m.belt;
        for (int cap = 0; cap <= orig; cap++)
        {
            m.belt = cap;
            if (Solvable(m)) { r.minBelt = cap; break; }
        }
        m.belt = orig;
    }

    // ───────────────────────── Model ─────────────────────────

    private static Model Build(SushiLevelData level, int seats, out string error)
    {
        error = null;
        if (level.customers == null || level.customers.Count == 0) { error = "Level chưa có khách."; return null; }
        if (level.boardColumns == null || level.boardColumns.Count == 0) { error = "Board chưa khởi tạo."; return null; }

        var typeIndex = new Dictionary<PoolType, int>();
        int Idx(PoolType t)
        {
            if (!typeIndex.TryGetValue(t, out int i)) { i = typeIndex.Count; typeIndex[t] = i; }
            return i;
        }

        var perCustomer = new List<Dictionary<int, int>>();
        foreach (var cu in level.customers)
        {
            var d = new Dictionary<int, int>();
            if (cu == null || cu.dishes == null) { error = "Có khách rỗng."; return null; }
            foreach (var o in cu.dishes)
            {
                if (o == null || o.amount < 1 || !o.TryGetRecipe(out var recipe, out _)) { error = "Có món không hợp lệ."; return null; }
                foreach (PoolType t in recipe.Ingredients)
                {
                    int i = Idx(t);
                    d.TryGetValue(i, out int v);
                    d[i] = v + o.amount;
                }
            }
            perCustomer.Add(d);
        }

        foreach (var col in level.boardColumns)
        {
            if (col == null || col.foods == null) { error = "Board có cột rỗng."; return null; }
            foreach (var f in col.foods) { if (f == null) { error = "Board có ô rỗng."; return null; } Idx(f.typeFood); }
        }

        var m = new Model
        {
            T = typeIndex.Count,
            C = level.customers.Count,
            cols = level.boardColumns.Count,
            belt = Mathf.Max(0, level.beltSlots),
            seats = Mathf.Max(1, seats)
        };
        m.needInit = new int[m.C * m.T];
        for (int c = 0; c < m.C; c++)
            foreach (var kv in perCustomer[c]) m.needInit[c * m.T + kv.Key] = kv.Value;

        m.tiles = new Tile[m.cols][];
        for (int c = 0; c < m.cols; c++)
        {
            var foods = level.boardColumns[c].foods;
            m.tiles[c] = new Tile[foods.Count];
            for (int r = 0; r < foods.Count; r++)
            {
                FoodData f = foods[r];
                int link = f.linkId > 0 ? f.linkId : f.lockId;
                m.tiles[c][r] = new Tile
                {
                    type = Idx(f.typeFood),
                    amount = Mathf.Max(1, f.amount),
                    ice = f.iceLayers > 0 ? f.iceLayers : (f.foodState == FoodState.Ice ? 1 : 0),
                    link = link,
                    free = f.foodState == FoodState.Normal && !f.hidden && f.iceLayers == 0 && link == 0 && f.trayId == 0,
                    src = f
                };
            }
        }
        return m;
    }

    private static State NewState(Model m)
    {
        var s = new State
        {
            head = new int[m.cols],
            ice = new int[m.cols],
            belt = new int[m.T],
            need = (int[])m.needInit.Clone(),
            left = new int[m.C]
        };
        for (int c = 0; c < m.cols; c++) s.ice[c] = m.tiles[c].Length > 0 ? m.tiles[c][0].ice : 0;
        for (int c = 0; c < m.C; c++)
            for (int t = 0; t < m.T; t++) s.left[c] += s.need[c * m.T + t];
        AdvanceFirst(m, s);
        return s;
    }

    // ───────────────────────── Simulation core ─────────────────────────

    private static void AdvanceFirst(Model m, State s)
    {
        while (s.first < m.C && s.left[s.first] == 0) s.first++;
    }

    private static bool Deliver(Model m, State s, int type)
    {
        int seen = 0;
        for (int c = s.first; c < m.C && seen < m.seats; c++)
        {
            if (s.left[c] == 0) continue;
            seen++;
            int idx = c * m.T + type;
            if (s.need[idx] > 0)
            {
                s.need[idx]--; s.left[c]--;
                if (c == s.first) AdvanceFirst(m, s);
                return true;
            }
        }
        return false;
    }

    private static void Rebalance(Model m, State s)
    {
        bool changed;
        do
        {
            changed = false;
            for (int t = 0; t < m.T; t++)
                while (s.belt[t] > 0 && Deliver(m, s, t)) { s.belt[t]--; s.beltCount--; changed = true; }
        } while (changed);
    }

    private static void Consume(Model m, State s, int col)
    {
        Tile t = m.tiles[col][s.head[col]];
        s.head[col]++;
        s.ice[col] = s.head[col] < m.tiles[col].Length ? m.tiles[col][s.head[col]].ice : 0;
        if (col > 0 && s.ice[col - 1] > 0) s.ice[col - 1]--;
        if (col + 1 < m.cols && s.ice[col + 1] > 0) s.ice[col + 1]--;
        for (int u = 0; u < t.amount; u++)
            if (!Deliver(m, s, t.type)) { s.belt[t.type]++; s.beltCount++; }
    }

    private static bool CanPick(Model m, State s, int col, out int partner)
    {
        partner = -1;
        if (s.head[col] >= m.tiles[col].Length || s.ice[col] > 0) return false;
        int link = m.tiles[col][s.head[col]].link;
        if (link <= 0) return true;
        for (int p = 0; p < m.cols; p++)
        {
            if (p == col || s.head[p] >= m.tiles[p].Length || s.ice[p] > 0) continue;
            if (m.tiles[p][s.head[p]].link == link) { partner = p; return true; }
        }
        return false;
    }

    private static bool Pick(Model m, State s, int col)
    {
        if (!CanPick(m, s, col, out int p)) return false;
        Consume(m, s, col);
        if (p >= 0) Consume(m, s, p);
        Rebalance(m, s);
        return s.beltCount <= m.belt;
    }

    private static int ActiveNeed(Model m, State s, int type)
    {
        int seen = 0, sum = 0;
        for (int c = s.first; c < m.C && seen < m.seats; c++)
        {
            if (s.left[c] == 0) continue;
            seen++;
            sum += s.need[c * m.T + type];
        }
        return sum;
    }

    private static int NextNeed(Model m, State s, int type)
    {
        for (int c = s.first; c < m.C; c++)
            if (s.need[c * m.T + type] > 0) return c;
        return m.C + 1;
    }

    private static int Unconsumed(Model m, State s, int col)
    {
        Tile t = m.tiles[col][s.head[col]];
        return Math.Max(0, t.amount - ActiveNeed(m, s, t.type));
    }

    private static bool Legal(Model m, State s, int col, int partner)
    {
        int extra = Unconsumed(m, s, col);
        if (partner >= 0) extra += Unconsumed(m, s, partner);
        return s.beltCount + extra <= m.belt;
    }

    private static int BestChoice(Model m, State s, int[] cand, int n)
    {
        int best = cand[0];
        long bestKey = long.MaxValue;
        for (int i = 0; i < n; i++)
        {
            int col = cand[i];
            Tile t = m.tiles[col][s.head[col]];
            long key = (Unconsumed(m, s, col) == 0 ? 0L : 1000000L) + NextNeed(m, s, t.type) * 10L + col;
            if (key < bestKey) { bestKey = key; best = col; }
        }
        return best;
    }

    /// Một ván chơi tự động. eps = xác suất pick ngẫu nhiên (người chơi nhầm). eps = 0 => người chơi greedy hoàn hảo.
    private static bool Playout(Model m, System.Random rnd, float eps)
    {
        State s = NewState(m);
        var cand = new int[m.cols];
        while (s.first < m.C)
        {
            int n = 0;
            for (int c = 0; c < m.cols; c++)
                if (CanPick(m, s, c, out int p) && Legal(m, s, c, p)) cand[n++] = c;
            if (n == 0) return false;

            int choice = (rnd != null && eps > 0f && rnd.NextDouble() < eps)
                ? cand[rnd.Next(n)]
                : BestChoice(m, s, cand, n);
            if (!Pick(m, s, choice)) return false;
        }
        return true;
    }

    private static float LossRate(Model m, int seed, int playouts)
    {
        int loss = 0;
        for (int p = 0; p < playouts; p++)
            if (!Playout(m, new System.Random(seed * 7919 + p), PlayerNoise)) loss++;
        return (float)loss / playouts;
    }

    // ───────────────────────── Solvability ─────────────────────────

    private static bool Solvable(Model m)
    {
        if (Playout(m, null, 0f)) return true;
        int budget = ExploreBudget;
        return Dfs(m, NewState(m), new HashSet<ulong>(), ref budget);
    }

    private static void Mix(ref ulong h, int v)
    {
        h ^= (uint)v;
        h *= 1099511628211UL;
    }

    private static ulong Hash(State s)
    {
        ulong h = 1469598103934665603UL;
        foreach (int v in s.head) Mix(ref h, v);
        foreach (int v in s.ice) Mix(ref h, v);
        foreach (int v in s.belt) Mix(ref h, v);
        foreach (int v in s.need) Mix(ref h, v);
        return h;
    }

    private static bool Dfs(Model m, State s, HashSet<ulong> seen, ref int budget)
    {
        if (s.first >= m.C) return true;
        if (budget <= 0) return false;
        if (!seen.Add(Hash(s))) return false;
        budget--;

        var moves = new List<KeyValuePair<long, State>>();
        for (int col = 0; col < m.cols; col++)
        {
            if (!CanPick(m, s, col, out int _)) continue;
            Tile t = m.tiles[col][s.head[col]];
            State ns = s.Clone();
            if (!Pick(m, ns, col)) continue;
            long key = (long)(ns.beltCount - s.beltCount) * 100000L + NextNeed(m, s, t.type) * 10L + col;
            moves.Add(new KeyValuePair<long, State>(key, ns));
        }
        if (moves.Count == 0) return false;
        moves.Sort((a, b) => a.Key.CompareTo(b.Key));

        // Pick không làm băng chuyền đầy thêm thì luôn không tệ hơn => chỉ thử nước đó.
        if (moves[0].Value.beltCount <= s.beltCount)
            return Dfs(m, moves[0].Value, seen, ref budget);

        foreach (var mv in moves)
        {
            if (Dfs(m, mv.Value, seen, ref budget)) return true;
            if (budget <= 0) return false;
        }
        return false;
    }
}
#endif
