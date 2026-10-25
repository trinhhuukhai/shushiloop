using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit, repeatable authoring. Runtime reads the saved boards; no random difficulty changes on replay.
public static class SushiCampaignSetup
{
    private const string Folder = "Assets/_game/LevelData/Campaign";
    private const string ListPath = "Assets/_game/LevelData/_AllLevelData.asset";
    private static readonly int[] Customers = { 2,3,4,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12 };
    private static readonly int[] Dishes = { 2,3,4,6,8,10,10,12,12,14,14,16,16,18,18,20,20,22,22,24 };
    private static readonly int[] ThreeIngredientDishes = { 0,0,0,0,0,2,2,4,4,6,6,8,8,10,10,12,12,14,14,16 };
    private static readonly int[] Hidden = { 0,0,0,0,2,3,3,4,4,5,5,6,6,7,7,8,8,9,10,10 };
    private static readonly int[] Frozen = { 0,0,0,0,0,0,1,2,2,3,3,4,4,5,5,6,6,7,7,8 };
    private static readonly string[] Names = {
        "Mẻ sushi đầu tiên", "Thêm một vị khách", "Nhớ món khách gọi", "Phục vụ hai món",
        "Điều bí mật", "Cuộn maki đầu tiên", "Làm tan băng", "Nhịp bếp đều tay",
        "Cặp nguyên liệu", "Chọn đúng thứ tự", "Khay phục vụ", "Chuẩn bị chỗ trống",
        "Hai cặp kết nối", "Bếp bắt đầu đông", "Khay ba ngăn", "Phối hợp cả bếp",
        "Chừa chỗ trên băng chuyền", "Giờ cao điểm", "Bếp trưởng khéo tay", "Thử thách nhà hàng"
    };
    private static readonly SushiDishId[] Nigiri = {
        SushiDishId.NigiriShrimp, SushiDishId.NigiriSalmon, SushiDishId.NigiriEgg,
        SushiDishId.NigiriAvocado, SushiDishId.NigiriTuna, SushiDishId.NigiriOctopus,
        SushiDishId.NigiriFlounder, SushiDishId.NigiriMackerel, SushiDishId.NigiriOrangeRoe,
        SushiDishId.NigiriBlackRoe, SushiDishId.NigiriGreenRoe, SushiDishId.NigiriMayonnaise
    };
    private static readonly SushiDishId[] Rolls = {
        SushiDishId.MakiShrimp, SushiDishId.MakiSalmon, SushiDishId.MakiAvocado, SushiDishId.MakiEgg,
        SushiDishId.MakiTuna, SushiDishId.GunkanShrimp, SushiDishId.GunkanSalmon,
        SushiDishId.GunkanOrangeRoe, SushiDishId.GunkanBlackRoe, SushiDishId.GunkanGreenRoe,
        SushiDishId.MakiOctopus, SushiDishId.GunkanAvocado
    };

    public static void RunBatch()
    {
        try
        {
            Setup();
            SushiConveyorCounterSetup.Setup();
            Debug.Log("CAMPAIGN_SETUP_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("SushiKhai/Levels/Build 20 Level Campaign")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring levels.");
        var list = AssetDatabase.LoadAssetAtPath<AllLevelData>(ListPath);
        if (list == null) throw new InvalidOperationException("Missing shared _AllLevelData.");
        var authored = new List<SushiLevelData>();
        var solutions = new List<List<int>>();
        var details = new List<string>();
        try
        {
            // Validate the whole proposed campaign before replacing the active level list.
            for (int number = 1; number <= 20; number++)
            {
                SushiLevelData candidate = null;
                string detail = "";
                List<int> taps = null;
                for (int attempt = 0; attempt < 128; attempt++)
                {
                    if (candidate != null) Object.DestroyImmediate(candidate);
                    candidate = Create(number, attempt);
                    if (!ApplyElements(candidate, new System.Random(candidate.boardSeed))) continue;
                    if (SushiCampaignRegression.TrySolve(candidate, out taps, out detail)) break;
                    taps = null;
                }
                if (taps == null)
                {
                    if (candidate != null) Object.DestroyImmediate(candidate);
                    throw new InvalidOperationException("Cannot author level " + number + ": " + detail);
                }
                authored.Add(candidate);
                solutions.Add(taps);
                details.Add(detail);
                Debug.Log("CAMPAIGN_AUTHOR Level " + number + ": " + detail);
            }
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_game/LevelData", "Campaign");
            var saved = new List<SushiLevelData>();
            for (int i = 0; i < authored.Count; i++)
            {
                string path = Folder + "/Level" + (i + 1).ToString("00") + ".asset";
                SushiLevelData destination = AssetDatabase.LoadAssetAtPath<SushiLevelData>(path);
                if (destination == null)
                {
                    destination = Object.Instantiate(authored[i]);
                    destination.name = "Level" + (i + 1).ToString("00");
                    AssetDatabase.CreateAsset(destination, path);
                }
                else
                {
                    EditorUtility.CopySerialized(authored[i], destination);
                    destination.name = "Level" + (i + 1).ToString("00");
                    EditorUtility.SetDirty(destination);
                }
                saved.Add(destination);
            }
            list.listAllLevel = saved;
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
            SushiCampaignRegression.ValidateCampaign();
            WriteDesign(saved, solutions, details);
        }
        finally
        {
            foreach (var level in authored) if (level != null) Object.DestroyImmediate(level);
        }
    }

    private static SushiLevelData Create(int number, int attempt)
    {
        int index = number - 1;
        var level = ScriptableObject.CreateInstance<SushiLevelData>();
        level.number = number;
        level.name = "Level" + number.ToString("00");
        level.levelName = Names[index];
        level.hint = Hint(number);
        level.TypeLevel = number >= 18 ? TypeLevel.SuperHard : number >= 13 ? TypeLevel.Hard : TypeLevel.Normal;
        level.numberColumns = number <= 3 ? 3 : number <= 8 ? 4 : 5;
        level.beltSlots = number <= 4 ? 8 : number <= 6 ? 9 : number <= 13 ? 10 : number <= 16 ? 9 : 8;
        level.loopsPerSecond = number <= 4 ? 0.075f : number <= 10 ? 0.08f : 0.085f;
        level.maxIngredientsPerTile = number < 4 ? 1 : 2;
        level.shuffleFoods = false;
        level.boardSeed = number * 1000 + attempt;
        int dishIndex = 0, normalIndex = 0, rollIndex = 0;
        for (int c = 0; c < Customers[index]; c++)
        {
            var customer = new CustomerOrderData();
            int portions = Dishes[index] / Customers[index] + (c >= Customers[index] - Dishes[index] % Customers[index] ? 1 : 0);
            for (int p = 0; p < portions; p++, dishIndex++)
            {
                bool roll = (dishIndex + 1) * ThreeIngredientDishes[index] / Dishes[index] >
                    dishIndex * ThreeIngredientDishes[index] / Dishes[index];
                SushiDishId dish;
                if (roll)
                {
                    int choices = Mathf.Clamp(number - 4, 2, Rolls.Length);
                    dish = Rolls[(rollIndex++ + number - 6) % choices];
                }
                else
                {
                    int choices = Mathf.Clamp(number + 1, 2, Nigiri.Length);
                    dish = Nigiri[(normalIndex++ + (number <= 3 ? 0 : number - 4)) % choices];
                }
                customer.dishes.Add(new DishOrderData { dish = dish, amount = 1 });
            }
            level.customers.Add(customer);
        }
        var sequence = new List<FoodData>();
        var random = new System.Random(level.boardSeed);
        foreach (CustomerOrderData customer in level.customers)
        {
            var batch = new List<FoodData>();
            foreach (FoodOrderData ingredient in customer.CreateIngredientOrders())
            {
                int remaining = ingredient.amount;
                while (remaining > 0)
                {
                    int amount = Math.Min(remaining, level.maxIngredientsPerTile);
                    batch.Add(new FoodData { typeFood = ingredient.typeFood, amount = amount });
                    remaining -= amount;
                }
            }
            // Keep a recognizable early opening; later orders require reading the recipe cards.
            if (number >= 3)
                for (int i = batch.Count - 1; i > 0; i--)
                { int next = random.Next(i + 1); var swap = batch[i]; batch[i] = batch[next]; batch[next] = swap; }
            sequence.AddRange(batch);
        }
        // Small windows overlap adjacent customer orders without burying every opening ingredient.
        int window = number < 8 ? 0 : number < 13 ? 4 : number < 17 ? 6 : 8;
        if (window > 0)
            for (int start = level.numberColumns; start < sequence.Count; start += window)
            {
                int end = Math.Min(sequence.Count, start + window);
                for (int i = end - 1; i > start; i--)
                { int next = random.Next(start, i + 1); var swap = sequence[i]; sequence[i] = sequence[next]; sequence[next] = swap; }
            }
        for (int c = 0; c < level.numberColumns; c++) level.boardColumns.Add(new FoodQueueData());
        for (int i = 0; i < sequence.Count; i++) level.boardColumns[i % level.numberColumns].foods.Add(sequence[i]);
        return level;
    }

    private static bool ApplyElements(SushiLevelData level, System.Random random)
    {
        int number = level.number;
        var board = level.boardColumns;
        int linkCount = number < 9 ? 0 : number < 13 ? 1 : number < 18 ? 2 : 3;
        for (int id = 1; id <= linkCount; id++)
        {
            var pairs = new List<Vector2Int>();
            for (int c = 0; c + 1 < board.Count; c++)
                for (int r = 1; r < Math.Min(board[c].foods.Count, board[c + 1].foods.Count); r++)
                    if (Plain(board[c].foods[r]) && Plain(board[c + 1].foods[r]))
                        pairs.Add(new Vector2Int(c, r));
            if (pairs.Count == 0) return false;
            var pair = number == 9 ? pairs.First(p => p.y == 1) : pairs[random.Next(pairs.Count)];
            foreach (int c in new[] { pair.x, pair.x + 1 })
            { board[c].foods[pair.y].linkId = id; board[c].foods[pair.y].foodState = FoodState.Lock; }
        }
        int trayCount = number < 11 ? 0 : number < 14 ? 1 : number < 18 ? 2 : 3;
        for (int id = 1; id <= trayCount; id++)
        {
            int length = number >= 15 && id % 2 == 1 ? 3 : 2;
            var starts = new List<Vector2Int>();
            for (int c = 0; c < board.Count; c++)
                for (int r = 1; r <= board[c].foods.Count - length; r++)
                {
                    bool valid = true;
                    for (int k = 0; k < length; k++) valid &= Plain(board[c].foods[r + k]);
                    if (valid) starts.Add(new Vector2Int(c, r));
                }
            if (starts.Count == 0) return false;
            Vector2Int start = number == 11 ? starts.OrderBy(p => p.y).First() : starts[random.Next(starts.Count)];
            for (int k = 0; k < length; k++)
            { var food = board[start.x].foods[start.y + k]; food.trayId = id; food.foodState = FoodState.RigidTray; }
        }
        for (int i = 0; i < Frozen[number - 1]; i++)
        {
            var cells = new List<Vector2Int>();
            for (int c = 0; c < board.Count; c++)
                for (int r = 0; r < board[c].foods.Count; r++)
                {
                    if (r == 0 && (i != 0 || c != 1)) continue;
                    if (Plain(board[c].foods[r])) cells.Add(new Vector2Int(c, r));
                }
            if (cells.Count == 0) return false;
            Vector2Int cell = i == 0 ? new Vector2Int(1, 0) : cells[random.Next(cells.Count)];
            if (!Plain(board[cell.x].foods[cell.y])) return false;
            int max = SushiLevelData.GetMaximumIceLayers(number);
            int layers = 1 + i % max;
            int available = (cell.x > 0 ? Math.Max(0, board[cell.x - 1].foods.Count - cell.y) : 0) +
                (cell.x + 1 < board.Count ? Math.Max(0, board[cell.x + 1].foods.Count - cell.y) : 0);
            if (available < layers) return false;
            var food = board[cell.x].foods[cell.y];
            food.foodState = FoodState.Ice; food.iceLayers = layers;
        }
        var hiddenCandidates = new List<Vector2Int>();
        for (int c = 0; c < board.Count; c++)
            for (int r = 1; r < board[c].foods.Count; r++)
                if (Plain(board[c].foods[r])) hiddenCandidates.Add(new Vector2Int(c, r));
        if (hiddenCandidates.Count < Hidden[number - 1]) return false;
        for (int i = 0; i < Hidden[number - 1]; i++)
        {
            int pick = random.Next(hiddenCandidates.Count);
            Vector2Int cell = hiddenCandidates[pick];
            hiddenCandidates.RemoveAt(pick);
            var food = board[cell.x].foods[cell.y];
            food.foodState = FoodState.HoiCham; food.hidden = true;
        }
        return true;
    }

    private static bool Plain(FoodData food) => food.foodState == FoodState.Normal &&
        !food.hidden && food.iceLayers == 0 && food.linkId == 0 && food.trayId == 0;

    private static string Hint(int number)
    {
        if (number <= 3) return "Xem món khách gọi và chọn nguyên liệu ở đầu cột.";
        if (number == 4) return "Ô ×2 cần hai chỗ trống trên băng chuyền. Ưu tiên món khách đang chờ.";
        if (number == 5) return "Ô dấu ? tự lật khi lên đầu cột. Lấy ô phía trước để khám phá nguyên liệu.";
        if (number == 6) return "Maki cần rong biển, cơm và topping. Quan sát ô ? vừa được mở.";
        if (number == 7) return "Lấy nguyên liệu ở cột sát bên để làm tan một lớp băng.";
        if (number == 8) return "Dọn cột sát ô băng trước, rồi hoàn thành các món đang phục vụ.";
        if (number == 9) return "Hai ô nối phải cùng lên đầu cột; chạm một ô sẽ lấy cả cặp.";
        if (number == 10) return "Căn hai đầu cặp nối và để đủ chỗ cho toàn bộ nguyên liệu.";
        if (number == 11) return "Khay được lấy trọn trong một lần chạm. Chừa chỗ cho cả khay.";
        if (number <= 14) return "Ưu tiên khách đang ngồi, làm tan băng và căn cặp nối trước khi lấy khay.";
        if (number <= 16) return "Khay ba ngăn cần nhiều slot. Dọn các món trên băng chuyền trước.";
        return "Băng chuyền chỉ có 8 slot. Tính cả ô ×2, cặp nối và khay trước mỗi lượt.";
    }

    private static void WriteDesign(List<SushiLevelData> levels, List<List<int>> solutions, List<string> details)
    {
        Directory.CreateDirectory("Documentation");
        var text = new StringBuilder("# Bộ 20 level\n\n");
        text.AppendLine("Board tối đa 5 cột; dữ liệu được lưu cố định, replay giữ nguyên bố cục. Không thêm khách có timer.");
        text.AppendLine("Tutorial tạm tắt. Mốc xuất hiện: dấu ? ở 5, băng ở 7, link ở 9, tray ở 11.");
        text.AppendLine("Các màn giới thiệu cơ chế giữ khối lượng gần màn trước; các màn sau tăng số khách, món 3 nguyên liệu và số nhóm đặc biệt, rồi giảm slot còn 8.");
        text.AppendLine("\n| LV | Tên | Khách | Món | Nguyên liệu | Cột | Slot | ? | Ô băng (lớp tối đa) | Cặp link | Khay |");
        text.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var level in levels)
        {
            var foods = level.boardColumns.SelectMany(c => c.foods).ToList();
            text.AppendLine("| " + level.number + " | " + level.levelName + " | " + level.CustomerCount +
                " | " + level.customers.Sum(c => c.dishes.Sum(d => d.amount)) + " | " + level.GetTotalFoodCount() +
                " | " + level.numberColumns + " | " + level.beltSlots + " | " + foods.Count(f => f.hidden) +
                " | " + foods.Count(f => f.iceLayers > 0) + " (" + foods.Max(f => f.iceLayers) + ")" +
                " | " + foods.Where(f => f.linkId > 0).Select(f => f.linkId).Distinct().Count() +
                " | " + foods.Where(f => f.trayId > 0).Select(f => f.trayId).Distinct().Count() + " |");
        }
        text.AppendLine("\n## Chỉnh sửa\n\nCác asset nằm ở Assets/_game/LevelData/Campaign/Level01.asset … Level20.asset; _AllLevelData là thứ tự chơi chính.");
        text.AppendLine("Có thể chỉnh orders/board trong Inspector rồi chạy Sushi > Validate 20 Level Campaign để kiểm tra. Build 20 Level Campaign tái tạo bộ mặc định.");
        text.AppendLine("Độ khó thực tế cần tiếp tục cân bằng bằng playtest người chơi; kiểm tra tự động xác nhận có lời giải, không thay thế đánh giá cảm giác chơi.");
        text.AppendLine("\n## Lời giải kiểm chứng\n\nChỉ số cột bên dưới bắt đầu từ 1; đợi món giao xong trước lượt tiếp theo. Đây là một lời giải, không nhất thiết ít lượt nhất.\n");
        for (int i = 0; i < levels.Count; i++)
            text.AppendLine("- Level " + (i + 1) + " (seed " + levels[i].boardSeed + "): " +
                string.Join(", ", solutions[i].Select(c => (c + 1).ToString())) + ". " + details[i]);
        File.WriteAllText("Documentation/Campaign20.md", text.ToString());
    }
}