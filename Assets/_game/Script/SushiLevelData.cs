using System;
using System.Collections.Generic;
using UnityEngine;

    [CreateAssetMenu(
        menuName = "SushiKhai/Level",
        fileName = "SushiLevel"
    )]
    public class SushiLevelData : ScriptableObject
    {
        [Header("Thông tin level")]
        [Min(1)]
        public int number = 1;

        public string levelName;

        [TextArea]
        public string hint;

        [Header("Băng chuyền")]
        [Min(6)]
        public int beltSlots = 14;

        [Range(0.02f, 0.3f)]
        public float loopsPerSecond = 0.08f;

        [Header("Board")]
        [Range(1, 6)]
        public int numberColumns = 4;

        [Tooltip("Cùng seed và dữ liệu sẽ tạo cùng thứ tự Food.")]
        public int boardSeed = 1;

        public bool shuffleFoods = true;

        [Header("Khách hàng - mỗi phần tử là một khách")]
        public List<CustomerOrderData> customers =
            new List<CustomerOrderData>();

        [Header("Trạng thái đặc biệt tại từng vị trí")]
        public List<FoodModifierData> modifiers = new List<FoodModifierData>();

        public int CustomerCount =>
            customers == null ? 0 : customers.Count;

        public int GetTotalFoodCount()
        {
            int total = 0;

            if (customers == null)
                return total;

            foreach (CustomerOrderData customer in customers)
            {
                if (customer == null || customer.orders == null)
                    continue;

                foreach (FoodOrderData order in customer.orders)
                {
                    if (order != null)
                        total += order.amount;
                }
            }

            return total;
        }

        // Mỗi lần gọi tạo dữ liệu mới.
        // Không thay đổi dữ liệu gốc trong ScriptableObject.
        public List<FoodQueueData> CreateBoardData()
        {
            string error = ValidateData();

            if (error != null)
                throw new InvalidOperationException(error);

            List<FoodData> allFoods = CreateAllFoods();

            if (shuffleFoods)
                Shuffle(allFoods);

            List<FoodQueueData> board =
                new List<FoodQueueData>();

            for (int i = 0; i < numberColumns; i++)
                board.Add(new FoodQueueData());

            // Chia lần lượt vào từng cột để số món cân bằng.
            for (int i = 0; i < allFoods.Count; i++)
            {
                int columnIndex = i % numberColumns;
                board[columnIndex].foods.Add(allFoods[i]);
            }

            ApplyModifiers(board);

            return board;
        }

        private List<FoodData> CreateAllFoods()
        {
            List<FoodData> foods = new List<FoodData>();

            foreach (CustomerOrderData customer in customers)
            {
                foreach (FoodOrderData order in customer.orders)
                {
                    for (int i = 0; i < order.amount; i++)
                    {
                        foods.Add(new FoodData
                        {
                            typeFood = order.typeFood,
                            foodState = FoodState.Normal,
                            iceLayers = 0,
                            lockId = 0
                        });
                    }
                }
            }

            return foods;
        }

        private void Shuffle(List<FoodData> foods)
        {
            // Không ảnh hưởng UnityEngine.Random của các hệ thống khác.
            System.Random random = new System.Random(boardSeed);

            for (int i = foods.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);

                FoodData temp = foods[i];
                foods[i] = foods[randomIndex];
                foods[randomIndex] = temp;
            }
        }

        private void ApplyModifiers(List<FoodQueueData> board)
        {
            if (modifiers == null)
                return;

            foreach (FoodModifierData modifier in modifiers)
            {
                FoodData food =
                    board[modifier.column].foods[modifier.row];

                food.foodState = modifier.foodState;

                food.iceLayers = modifier.foodState == FoodState.Ice
                    ? modifier.iceLayers
                    : 0;

                food.lockId = modifier.foodState == FoodState.Lock
                    ? modifier.lockId
                    : 0;
            }
        }

        public string ValidateData()
        {
            if (number < 1)
                return "Số level phải lớn hơn hoặc bằng 1.";

            if (numberColumns < 1 || numberColumns > 6)
                return "Board cần từ 1 đến 6 cột.";

            if (beltSlots < 6)
                return "Băng chuyền cần ít nhất 6 slot.";

            if (float.IsNaN(loopsPerSecond) ||
                float.IsInfinity(loopsPerSecond) ||
                loopsPerSecond < 0.02f ||
                loopsPerSecond > 0.3f)
            {
                return "Tốc độ cần nằm trong khoảng 0.02–0.3 vòng/giây.";
            }

            if (customers == null || customers.Count == 0)
                return "Level chưa có khách.";

            for (int i = 0; i < customers.Count; i++)
            {
                CustomerOrderData customer = customers[i];

                if (customer == null ||
                    customer.orders == null ||
                    customer.orders.Count == 0)
                {
                    return "Khách " + (i + 1) + " chưa có order.";
                }

                int total = 0;
                HashSet<PoolType> types = new HashSet<PoolType>();

                foreach (FoodOrderData order in customer.orders)
                {
                    if (order == null)
                        return "Khách " + (i + 1) + " có order rỗng.";

                    if (!Enum.IsDefined(typeof(PoolType), order.typeFood))
                        return "Khách " + (i + 1) + " có món không hợp lệ.";

                    if (order.amount < 1 || order.amount > 6)
                        return "Số lượng mỗi order phải từ 1 đến 6.";

                    if (!types.Add(order.typeFood))
                    {
                        return "Khách " + (i + 1)
                            + " bị trùng loại món. Hãy gộp vào amount.";
                    }

                    total += order.amount;
                }

                // Theo layout Customer hiện tại.
                if (types.Count > 4 || total > 6)
                {
                    return "Khách " + (i + 1)
                        + " chỉ được tối đa 4 loại và tổng 6 món.";
                }
            }

            return ValidateModifiers();
        }

        private string ValidateModifiers()
        {
            if (modifiers == null)
                return null;

            int totalFoods = GetTotalFoodCount();
            HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

            foreach (FoodModifierData modifier in modifiers)
            {
                if (modifier == null)
                    return "Có dữ liệu trạng thái đặc biệt bị rỗng.";

                if (modifier.column < 0 ||
                    modifier.column >= numberColumns ||
                    modifier.row < 0)
                {
                    return "Vị trí trạng thái đặc biệt không hợp lệ.";
                }

                int columnFoodCount = totalFoods / numberColumns;

                if (modifier.column < totalFoods % numberColumns)
                    columnFoodCount++;

                if (modifier.row >= columnFoodCount)
                    return "Trạng thái đặc biệt đang trỏ vào hàng không có Food.";

                Vector2Int position =
                    new Vector2Int(modifier.column, modifier.row);

                if (!usedPositions.Add(position))
                    return "Một vị trí Food đang được cấu hình trạng thái hai lần.";

                if (!Enum.IsDefined(typeof(FoodState), modifier.foodState))
                    return "FoodState không hợp lệ.";

                if (modifier.foodState == FoodState.Ice &&
                    modifier.iceLayers < 1)
                {
                    return "Food đóng băng cần ít nhất một lớp băng.";
                }

                if (modifier.foodState == FoodState.Lock &&
                    modifier.lockId < 0)
                {
                    return "Mã khóa không được âm.";
                }
            }

            return null;
        }
    }