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
    public const int LevelCount = 50;
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
        "Chừa chỗ trên băng chuyền", "Giờ cao điểm", "Bếp trưởng khéo tay", "Thử thách nhà hàng",
        "Ca bếp mới", "Thực đơn phong phú", "Cuộn sushi đủ vị", "Sắp xếp khéo léo", "Chuyến tàu giờ trưa",
        "Quầy sashimi", "Trà xanh giải nhiệt", "Hai bàn liên tục", "Giữ nhịp phục vụ", "Nhà hàng đông khách",
        "Khay nguyên liệu lớn", "Ba món một bàn", "Cuộn rong biển đầy đặn", "Chuẩn bị trước", "Bữa tối gấp gáp",
        "Băng sâu trong bếp", "Món nóng trà mát", "Kết nối nhịp nhàng", "Bàn ăn nối tiếp", "Đêm hội sushi",
        "Tay nghề bếp trưởng", "Thực đơn đặc biệt", "Căn từng chỗ trống", "Phối hợp năm cột", "Khách hẹn giờ",
        "Nhịp bếp chuyên nghiệp", "Khay lớn và băng dày", "Đại tiệc hải sản", "Trước giờ khai tiệc", "Bậc thầy sushi"
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
            SushiCampaignSceneSetup.Setup();
            Debug.Log("CAMPAIGN_SETUP_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("SushiKhai/Levels/Extend Campaign To 50 Levels")]
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
            // Preserve authored boards/orders, including edits made in the Inspector.
            // Only missing assets are generated; checkpoint timers are added if absent.
            for (int number = 1; number <= LevelCount; number++)
            {
                var existing = AssetDatabase.LoadAssetAtPath<SushiLevelData>(Folder + "/Level" + number.ToString("00") + ".asset");
                if (existing != null)
                {
                    var preserved = Object.Instantiate(existing);
                    ApplyCheckpointRush(preserved);
                    string error = preserved.ValidateData();
                    if (error != null) { Object.DestroyImmediate(preserved); throw new InvalidOperationException("Level " + number + ": " + error); }
                    authored.Add(preserved);
                    solutions.Add(null);
                    details.Add("Giữ nguyên board và order đã lưu.");
                    continue;
                }
                SushiLevelData candidate = null;
                string detail = "";
                List<int> taps = null;
                for (int attempt = 0; attempt < 128; attempt++)
                {
                    if (candidate != null) Object.DestroyImmediate(candidate);
                    candidate = Create(number, attempt);
                    ApplyCheckpointRush(candidate);
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
        level.TypeLevel = number > 20 ? (number >= 35 ? TypeLevel.SuperHard : TypeLevel.Hard) :
            number >= 18 ? TypeLevel.SuperHard : number >= 13 ? TypeLevel.Hard : TypeLevel.Normal;
        level.numberColumns = number <= 3 ? 3 : number <= 8 ? 4 : 5;
        level.beltSlots = number <= 4 ? 8 : number <= 6 ? 9 : number <= 13 ? 10 : number <= 16 ? 9 : 8;
        level.loopsPerSecond = number <= 4 ? 0.075f : number <= 10 ? 0.08f : 0.085f;
        level.maxIngredientsPerTile = number < 4 ? 1 : number < 31 ? 2 : 3;
        level.shuffleFoods = false;
        level.boardSeed = number * 1000 + attempt;
        int customerCount = number <= 20 ? Customers[index] : 12 + (number - 20) / 5;
        int dishCount = number <= 20 ? Dishes[index] : 24 + (number - 20) * 2 / 3;
        int rollCount = number <= 20 ? ThreeIngredientDishes[index] :
            Mathf.RoundToInt(dishCount * Mathf.Lerp(.7f, .85f, (number - 21) / 29f));
        int dishIndex = 0, normalIndex = 0, rollIndex = 0;
        for (int c = 0; c < customerCount; c++)
        {
            var customer = new CustomerOrderData();
            int portions = dishCount / customerCount + (c >= customerCount - dishCount % customerCount ? 1 : 0);
            for (int p = 0; p < portions; p++, dishIndex++)
            {
                bool roll = (dishIndex + 1) * rollCount / dishCount > dishIndex * rollCount / dishCount;
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
                // Add different foundations and toppings without increasing the recipe size.
                if (number >= 26 && !roll && normalIndex % 3 == 0)
                    dish = (SushiDishId)((int)SushiDishId.SashimiTunaSalmon + (number + normalIndex) % 15);
                if (number >= 27 && !roll && normalIndex % 4 == 0) dish = SushiDishId.GreenTea;
                if (number >= 32 && roll && rollIndex % 9 == 0) dish = SushiDishId.IcedGreenTea;
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
        int window = number < 8 ? 0 : number < 13 ? 4 : number < 17 ? 6 : number < 31 ? 8 : number < 41 ? 9 : 10;
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
        int trayCount = number < 11 ? 0 : number < 14 ? 1 : number < 18 ? 2 : number < 36 ? 3 : 4;
        for (int id = 1; id <= trayCount; id++)
        {
            int length = number >= 15 && id % 2 == 1 ? 3 : 2;
            var starts = new List<Vector2Int>();
            for (int c = 0; c < board.Count; c++)
                for (int r = 1; r <= board[c].foods.Count - length; r++)
                {
                    bool valid = true;
                    int amount = 0;
                    for (int k = 0; k < length; k++)
                    { valid &= Plain(board[c].foods[r + k]); amount += board[c].foods[r + k].amount; }
                    if (valid && amount <= 6) starts.Add(new Vector2Int(c, r));
                }
            if (starts.Count == 0) return false;
            Vector2Int start = number == 11 ? starts.OrderBy(p => p.y).First() : starts[random.Next(starts.Count)];
            for (int k = 0; k < length; k++)
            { var food = board[start.x].foods[start.y + k]; food.trayId = id; food.foodState = FoodState.RigidTray; }
        }
        int frozenCount = number <= 20 ? Frozen[number - 1] : 8 + (number - 21) / 7;
        for (int i = 0; i < frozenCount; i++)
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
            // Difficulty choices belong to this authoring tool, not runtime unlock gates.
            int max = number < 9 ? 1 : number < 14 ? 2 : number < 18 ? 3 : number < 36 ? 4 : 5;
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
        int hiddenCount = number <= 20 ? Hidden[number - 1] : 10 + (number - 21) / 4;
        if (hiddenCandidates.Count < hiddenCount) return false;
        for (int i = 0; i < hiddenCount; i++)
        {
            int pick = random.Next(hiddenCandidates.Count);
            Vector2Int cell = hiddenCandidates[pick];
            hiddenCandidates.RemoveAt(pick);
            var food = board[cell.x].foods[cell.y];
            food.foodState = FoodState.HoiCham; food.hidden = true;
        }
        return true;
    }

    private static void ApplyCheckpointRush(SushiLevelData level)
    {
        // Default campaign design only. Any level may author rush guests in the Inspector.
        if (level.number % 5 != 0 || level.customers.Any(c => c.rushCustomer)) return;
        int count = level.number <= 10 ? 1 : level.number <= 25 ? 2 : level.number <= 40 ? 3 : 4;
        var indices = new List<int> { 0 };
        if (count >= 3) indices.Add(1);
        if (count >= 2) indices.Add(level.CustomerCount / 2);
        if (count >= 4) indices.Add(level.CustomerCount - 2);
        foreach (int index in indices.Distinct())
        {
            var customer = level.customers[index];
            customer.rushCustomer = true;
            int ingredients = customer.CreateIngredientOrders().Sum(order => order.amount);
            customer.rushSeconds = Mathf.Max(55, 95 - level.number) + Mathf.Max(0, ingredients - 6) * 4;
        }
        level.TypeLevel = level.number % 10 == 0 ? TypeLevel.SuperHard : TypeLevel.Hard;
        level.hint += " Ưu tiên khách có đồng hồ; thời gian chỉ chạy khi khách đã ngồi.";
    }

    private static bool Plain(FoodData food) => food.foodState == FoodState.Normal &&
        !food.hidden && food.iceLayers == 0 && food.linkId == 0 && food.trayId == 0;

    private static string Hint(int number)
    {
        if (number >= 36) return "Giữ chỗ cho khay lớn, làm tan băng dày bằng các cột bên cạnh và phục vụ món đang chờ.";
        if (number >= 31) return "Ô ×3 chiếm ba slot. Đọc đủ công thức trước khi lấy cả khay.";
        if (number >= 26) return "Phân biệt sushi, sashimi và trà; ưu tiên nguyên liệu của hai bàn đang ngồi.";
        if (number >= 21) return "Hoàn thành món đang phục vụ trước khi lấy nguyên liệu cho khách trong hàng chờ.";
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
        var text = new StringBuilder("# Bộ 50 level\n\n");
        text.AppendLine("Giữ board/order của 20 màn gốc, bổ sung 21–50 với độ phức tạp tăng dần: 12–18 khách, 24–44 món, thêm sashimi/trà, ô ×3 và băng dày. Board lưu cố định, replay giữ bố cục.");
        text.AppendLine("Checkpoint 5/10/15/…/50 có 1–4 khách vội. Đồng hồ chỉ chạy khi khách ngồi: các mốc đầu cho nhiều thời gian hơn; đơn lớn được cộng thời gian. Các mốc từ 30 có thể có hai khách vội ngồi cùng lúc.");
        text.AppendLine("Đây là thiết kế mặc định, không phải điều kiện mở khóa. Có thể đặt khách vội hoặc bất kỳ element nào ở bất kỳ số level nào trong Inspector.");
        text.AppendLine("\n| LV | Tên | Khách | Món | Nguyên liệu | Cột | Slot | ? | Ô băng (lớp tối đa) | Cặp link | Khay | Khách vội / giây |");
        text.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var level in levels)
        {
            var foods = level.boardColumns.SelectMany(c => c.foods).ToList();
            text.AppendLine("| " + level.number + " | " + level.levelName + " | " + level.CustomerCount +
                " | " + level.customers.Sum(c => c.dishes.Sum(d => d.amount)) + " | " + level.GetTotalFoodCount() +
                " | " + level.numberColumns + " | " + level.beltSlots + " | " + foods.Count(f => f.hidden) +
                " | " + foods.Count(f => f.iceLayers > 0) + " (" + foods.Max(f => f.iceLayers) + ")" +
                " | " + foods.Where(f => f.linkId > 0).Select(f => f.linkId).Distinct().Count() +
                " | " + foods.Where(f => f.trayId > 0).Select(f => f.trayId).Distinct().Count() +
                " | " + string.Join(", ", level.customers.Select((c, i) => new { customer = c, index = i })
                    .Where(c => c.customer.rushCustomer).Select(c => "#" + (c.index + 1) + ": " + c.customer.rushSeconds + "s")) + " |");
        }
        text.AppendLine("\n## Chỉnh sửa\n\nAssets/_game/LevelData/Campaign/Level01.asset … Level50.asset; _AllLevelData chứa đúng thứ tự chơi 1–50. Màn thử Level03_5Khach4Mon được giữ riêng ngoài danh sách chính.");
        text.AppendLine("Menu SushiKhai > Levels > Extend Campaign To 50 Levels bổ sung asset thiếu và giữ board/order hiện có. Chỉnh customers.rushCustomer/rushSeconds, boardColumns hoặc modifiers trực tiếp trong Inspector.");
        text.AppendLine("Validator chỉ kiểm tra dữ liệu hợp lệ và khả năng hoạt động của board, không kiểm tra mốc xuất hiện khách vội/item. Không chạy lại toàn bộ gameplay; các board mới được tìm đường lấy nguyên liệu khi tạo, chưa đo thời gian chơi của người thật.");
        text.AppendLine("\n## Đường lấy nguyên liệu tham khảo của màn mới\n\nChỉ số cột bắt đầu từ 1. Bộ tìm đường không mô phỏng timer khách vội hay thời gian animation; các chuỗi này không phải bằng chứng hoàn thành trong giới hạn giờ.\n");
        for (int i = 0; i < levels.Count; i++)
        {
            if (solutions[i] == null) continue;
            text.AppendLine("- Level " + (i + 1) + " (seed " + levels[i].boardSeed + "): " +
                string.Join(", ", solutions[i].Select(c => (c + 1).ToString())) + ". " + details[i]);
        }
        File.WriteAllText("Documentation/Campaign50.md", text.ToString());
    }
}
