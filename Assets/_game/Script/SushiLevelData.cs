using System;
using System.Collections.Generic;
using UnityEngine;

public enum TypeLevel
{
    Normal,
    Hard,
    SuperHard
}

[CreateAssetMenu(menuName = "SushiKhai/Level", fileName = "SushiLevel")]
public class SushiLevelData : ScriptableObject
{
    public TypeLevel TypeLevel;
    [Min(1)] public int number = 1;
    public string levelName;
    [TextArea] public string hint;
    [Min(6)] public int beltSlots = 14;
    [Range(0.02f, 0.3f)] public float loopsPerSecond = 0.08f;
    [Range(1, 6)] public int numberColumns = 4;
    public int boardSeed = 1;
    public bool shuffleFoods = true;
    [Tooltip("Sinh board từ order: gộp tối đa bao nhiêu nguyên liệu cùng loại vào một ô. Level cũ mặc định ×1.")]
    [Range(1, 3)] public int maxIngredientsPerTile = 1;
    public List<CustomerOrderData> customers = new List<CustomerOrderData>();
    [Tooltip("Để trống để sinh từ orders. Khi nhập trực tiếp, tổng nguyên liệu phải khớp orders. Phần tử 0 là hàng 1.")]
    public List<FoodQueueData> boardColumns = new List<FoodQueueData>();
    public List<FoodModifierData> modifiers = new List<FoodModifierData>();
    public int CustomerCount => customers == null ? 0 : customers.Count;

    public int GetTotalFoodCount()
    {
        int total = 0;
        if (customers != null)
            foreach (var customer in customers)
                if (customer != null)
                    foreach (var order in customer.CreateIngredientOrders()) if (order != null) total += order.amount;
        return total;
    }

    public List<FoodQueueData> CreateBoardData()
    {
        string error = ValidateData();
        if (error != null) throw new InvalidOperationException(error);
        return BuildBoard();
    }

    private List<FoodQueueData> BuildBoard(bool applyModifiers = true)
    {
        var board = new List<FoodQueueData>();
        if (boardColumns != null && boardColumns.Count > 0)
        {
            foreach (var column in boardColumns)
            {
                var copy = new FoodQueueData();
                foreach (var food in column.foods) copy.foods.Add(food.Copy());
                board.Add(copy);
            }
        }
        else
        {
            var amounts = new Dictionary<PoolType, int>();
            foreach (var customer in customers)
                foreach (var order in customer.CreateIngredientOrders())
                {
                    amounts.TryGetValue(order.typeFood, out int count);
                    amounts[order.typeFood] = count + order.amount;
                }
            var foods = new List<FoodData>();
            if (maxIngredientsPerTile == 1)
            {
                // Giữ nguyên thứ tự sinh/seed của level ×1 đã có.
                foreach (var customer in customers)
                    foreach (var order in customer.CreateIngredientOrders())
                        for (int i = 0; i < order.amount; i++) foods.Add(new FoodData { typeFood = order.typeFood });
            }
            // Duyệt enum theo thứ tự cố định để seed không phụ thuộc Dictionary.
            foreach (PoolType type in Enum.GetValues(typeof(PoolType)))
            {
                if (maxIngredientsPerTile == 1) break;
                amounts.TryGetValue(type, out int remaining);
                while (remaining > 0)
                {
                    int amount = Mathf.Min(maxIngredientsPerTile, remaining);
                    foods.Add(new FoodData { typeFood = type, amount = amount });
                    remaining -= amount;
                }
            }
            if (shuffleFoods)
            {
                var random = new System.Random(boardSeed);
                for (int i = foods.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    FoodData temp = foods[i]; foods[i] = foods[j]; foods[j] = temp;
                }
            }
            for (int i = 0; i < numberColumns; i++) board.Add(new FoodQueueData());
            for (int i = 0; i < foods.Count; i++) board[i % numberColumns].foods.Add(foods[i]);
        }
        if (applyModifiers && modifiers != null)
            foreach (var modifier in modifiers)
            {
                FoodData food = board[modifier.column].foods[modifier.row];
                food.foodState = modifier.foodState;
                food.hidden = modifier.hidden || modifier.foodState == FoodState.HoiCham;
                food.iceLayers = modifier.foodState == FoodState.Ice ? modifier.iceLayers : 0;
                food.linkId = modifier.linkId > 0 ? modifier.linkId : modifier.lockId;
                food.lockId = 0;
                food.trayId = modifier.trayId;
            }
        return board;
    }

    public string ValidateData()
    {
        if (number < 1) return "Số level phải >= 1.";
        if (numberColumns < 1 || numberColumns > 6) return "Board cần 1–6 cột.";
        if (beltSlots < 6) return "Băng chuyền cần ít nhất 6 slot.";
        if (float.IsNaN(loopsPerSecond) || float.IsInfinity(loopsPerSecond) || loopsPerSecond < 0.02f || loopsPerSecond > 0.3f)
            return "Tốc độ phải trong khoảng 0.02–0.3 vòng/giây.";
        if (maxIngredientsPerTile < 1 || maxIngredientsPerTile > 3) return "Mỗi ô chứa ×1–×3.";
        if (customers == null || customers.Count == 0) return "Level chưa có khách.";
        int rushCount = 0;
        foreach (var customer in customers)
        {
            if (customer == null) return "Khách không được rỗng.";
            string orderError = customer.ValidateOrders();
            if (orderError != null) return orderError;
            int total = 0;
            var types = new HashSet<PoolType>();
            foreach (var order in customer.CreateIngredientOrders())
            {
                if (order == null || !IsFoodType(order.typeFood) || order.amount < 1 || (!customer.UsesRecipes && order.amount > 6))
                    return "Order không hợp lệ: loại nguyên liệu và số lượng 1–6 là bắt buộc.";
                if (!types.Add(order.typeFood)) return "Gộp loại nguyên liệu trùng vào amount.";
                total += order.amount;
            }
            if (!customer.UsesRecipes && (types.Count > 4 || total > 6)) return "Mỗi khách tối đa 4 loại và tổng 6 nguyên liệu.";
            if (customer.rushCustomer)
            {
                rushCount++;
                if (number < 20) return "rush_customer mở ở level 20.";
                if (float.IsNaN(customer.rushSeconds) || float.IsInfinity(customer.rushSeconds) || customer.rushSeconds < 0)
                    return "rushSeconds phải >= 0, 0 dùng công thức mặc định.";
            }
        }
        if (number == 20 && rushCount > 1) return "Level 20 giới thiệu một khách vội.";
        if (number < 30 && customers.Count > 1 && customers[0].rushCustomer && customers[1].rushCustomer)
            return "Trước level 30 hai khách đầu tiên không được cùng là khách vội.";
        if (boardColumns != null && boardColumns.Count > 0)
        {
            if (boardColumns.Count != numberColumns) return "boardColumns phải khớp numberColumns.";
            foreach (var column in boardColumns)
            {
                if (column?.foods == null) return "Dữ liệu cột không hợp lệ.";
                foreach (var food in column.foods) if (food == null) return "Dữ liệu ô không được rỗng.";
            }
        }
        // Xác định số hàng trước khi áp dụng modifiers để không truy cập ngoài mảng.
        List<FoodQueueData> board = BuildBoard(false);
        var positions = new HashSet<Vector2Int>();
        if (modifiers != null)
            foreach (var modifier in modifiers)
            {
                if (modifier == null || modifier.column < 0 || modifier.column >= board.Count || modifier.row < 0 ||
                    modifier.row >= board[modifier.column].foods.Count) return "Modifier trỏ vào ô không tồn tại.";
                if (!positions.Add(new Vector2Int(modifier.column, modifier.row))) return "Một ô có nhiều modifier; cấu hình hidden + Ice trong cùng modifier.";
            }
        board = BuildBoard();
        return ValidateBoard(board);
    }

    private string ValidateBoard(List<FoodQueueData> board)
    {
        var expected = new Dictionary<PoolType, int>();
        foreach (var customer in customers)
            foreach (var order in customer.CreateIngredientOrders())
            {
                expected.TryGetValue(order.typeFood, out int value);
                expected[order.typeFood] = value + order.amount;
            }
        var links = new Dictionary<int, List<Vector2Int>>();
        var trays = new Dictionary<int, List<Vector2Int>>();
        bool hasIce = false;
        int unfrozenHeads = 0;
        int fiveLayerTiles = 0;
        int maxIce = number == 39 || number == 49 ? 5 : number <= 32 ? 2 : number <= 39 ? 3 : 4;
        for (int c = 0; c < board.Count; c++)
        {
            var foods = board[c].foods;
            if (foods.Count > 0 && foods[0].foodState != FoodState.Ice && foods[0].iceLayers == 0) unfrozenHeads++;
            for (int r = 0; r < foods.Count; r++)
            {
                FoodData food = foods[r];
                if (!IsFoodType(food.typeFood) || food.amount < 1 || food.amount > 3) return "Mỗi ô cần loại nguyên liệu hợp lệ và amount 1–3.";
                expected.TryGetValue(food.typeFood, out int balance);
                expected[food.typeFood] = balance - food.amount;
                if (!Enum.IsDefined(typeof(FoodState), food.foodState)) return "FoodState không hợp lệ.";
                bool hidden = food.hidden || food.foodState == FoodState.HoiCham;
                int ice = food.iceLayers;
                int link = food.linkId > 0 ? food.linkId : food.lockId;
                if (food.linkId < 0 || food.lockId < 0 || food.trayId < 0 || ice < 0) return "ID và số lớp băng không được âm.";
                if (food.foodState == FoodState.Lock && link == 0) return "Ô nối cần linkId > 0.";
                if (food.foodState == FoodState.RigidTray && food.trayId == 0) return "Khay cứng cần trayId > 0.";
                if (hidden && (number < 10 || r == 0)) return "Ô ẩn mở ở level 10 và không được bắt đầu ở hàng 1.";
                if (hidden && ice > 0 && number < 40) return "Ô ẩn + băng chỉ được đi chung từ level 40.";
                if (food.foodState == FoodState.Ice && ice == 0) return "Ô băng cần ít nhất một lớp.";
                if (ice > 0)
                {
                    hasIce = true;
                    if (number < 30 || ice > maxIce) return "Số lớp băng vượt giới hạn của level.";
                    if (ice == 5) fiveLayerTiles++;
                    int availableMoves = (c > 0 ? Mathf.Max(0, board[c - 1].foods.Count - r) : 0) +
                        (c + 1 < board.Count ? Mathf.Max(0, board[c + 1].foods.Count - r) : 0);
                    if (ice > availableMoves) return "Ô băng không đủ lượt di chuyển ở cột sát bên để phá.";
                }
                if ((link > 0 && (hidden || ice > 0 || food.trayId > 0)) ||
                    (food.trayId > 0 && (hidden || ice > 0))) return "Ô nối và khay cứng không kết hợp element khác.";
                if (link > 0)
                {
                    if (number < 40) return "linked_tile mở ở level 40.";
                    AddGroup(links, link, new Vector2Int(c, r));
                }
                if (food.trayId > 0)
                {
                    if (number < 46) return "rigid_tray mở ở level 46.";
                    AddGroup(trays, food.trayId, new Vector2Int(c, r));
                }
            }
        }
        foreach (int balance in expected.Values) if (balance != 0) return "Tổng nguyên liệu trên board phải khớp chính xác orders, kể cả ×n.";
        if (hasIce && unfrozenHeads < 2) return "Board có băng phải bắt đầu với ít nhất 2 cột không đóng băng ở hàng 1.";
        if (fiveLayerTiles > 2) return "Super Hard chỉ có tối đa 2 ô băng 5 lớp.";
        if (links.Count > (number == 40 ? 1 : 3)) return "Level 40 tối đa 1 cặp nối; level sau tối đa 3 cặp.";
        foreach (var group in links.Values)
        {
            if (group.Count != 2 || Mathf.Abs(group[0].x - group[1].x) != 1) return "Mỗi linkId phải có đúng 2 ô ở 2 cột sát nhau.";
            if (group[0].y == 0 && group[1].y == 0) return "Không đặt cả cặp nối ở hàng 1 lúc bắt đầu.";
        }
        foreach (var group in trays.Values)
        {
            if (group.Count < 2 || group.Count > 3) return "Khay cứng dài 2–3 ngăn.";
            int total = 0;
            for (int i = 0; i < group.Count; i++)
            {
                if (group[i].x != group[0].x || group[i].y != group[0].y + i) return "Các ngăn cùng trayId phải liên tiếp trong một cột.";
                total += board[group[i].x].foods[group[i].y].amount;
            }
            if (total > 6) return "Khay cứng tối đa 6 nguyên liệu.";
        }
        return ValidateLinkPaths(board);
    }

    private static string ValidateLinkPaths(List<FoodQueueData> board)
    {
        // Mô phỏng bỏ ô thường/băng và lấy cặp nối đồng thời để phát hiện dây khóa chéo.
        var heads = new int[board.Count];
        bool moved;
        do
        {
            moved = false;
            for (int c = 0; c < board.Count; c++)
            {
                if (heads[c] >= board[c].foods.Count) continue;
                var food = board[c].foods[heads[c]];
                int link = food.linkId > 0 ? food.linkId : food.lockId;
                if (link == 0) { heads[c]++; moved = true; continue; }
                for (int n = 0; n < board.Count; n++)
                {
                    if (n == c || heads[n] >= board[n].foods.Count) continue;
                    var peer = board[n].foods[heads[n]];
                    if ((peer.linkId > 0 ? peer.linkId : peer.lockId) != link) continue;
                    heads[c]++; heads[n]++; moved = true; break;
                }
            }
        } while (moved);
        for (int c = 0; c < board.Count; c++)
            if (heads[c] < board[c].foods.Count) return "Cặp nối bị chặn đường lên hàng 1 bởi cặp nối khác.";
        return null;
    }

    private static void AddGroup(Dictionary<int, List<Vector2Int>> groups, int id, Vector2Int position)
    {
        if (!groups.TryGetValue(id, out var group)) groups[id] = group = new List<Vector2Int>();
        group.Add(position);
    }

    private static bool IsFoodType(PoolType type) => type != PoolType.None && Enum.IsDefined(typeof(PoolType), type);
}
