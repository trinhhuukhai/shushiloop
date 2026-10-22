using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FoodOrderData
{
    public PoolType typeFood;

    [Min(1)]
    public int amount = 1;
}

[Serializable]
public class DishOrderData
{
    public SushiDishId dish = SushiDishId.NigiriShrimp;
    [Min(1)] public int amount = 1;
}

[Serializable]
public class CustomerOrderData
{
    public bool rushCustomer;

    [Tooltip("0 = 20 + 5 giây × tổng nguyên liệu của order.")]
    [Min(0)] public float rushSeconds;

    [Tooltip("Các món thành phẩm trong đơn. Mỗi amount là số phần của công thức đó.")]
    public List<DishOrderData> dishes = new List<DishOrderData>();
    public bool UsesRecipes => dishes != null && dishes.Count > 0;

    public List<FoodOrderData> orders =
        new List<FoodOrderData>();

    // Chuyển thành danh sách mà Customer.Setup() đang sử dụng.
    public List<PoolType> CreateFoodList()
    {
        List<PoolType> result = new List<PoolType>();

        foreach (FoodOrderData order in CreateIngredientOrders())
        {
            for (int i = 0; i < order.amount; i++)
                result.Add(order.typeFood);
        }

        return result;
    }

    public List<FoodOrderData> CreateIngredientOrders()
    {
        var result = new List<FoodOrderData>();
        if (!UsesRecipes)
        {
            if (orders != null)
                foreach (var order in orders)
                    if (order != null) result.Add(new FoodOrderData { typeFood = order.typeFood, amount = order.amount });
            return result;
        }
        foreach (DishOrderData order in dishes)
        {
            if (order == null || order.amount < 1) throw new InvalidOperationException("Order món không hợp lệ.");
            SushiRecipe recipe = SushiRecipeCatalog.Get(order.dish);
            foreach (PoolType type in recipe.Ingredients)
            {
                FoodOrderData ingredient = result.Find(item => item.typeFood == type);
                if (ingredient == null) result.Add(ingredient = new FoodOrderData { typeFood = type, amount = 0 });
                ingredient.amount = checked(ingredient.amount + order.amount);
            }
        }
        return result;
    }

    public string ValidateOrders()
    {
        if (UsesRecipes)
        {
            if (orders != null && orders.Count > 0) return "Chỉ cấu hình dishes (món) hoặc orders cũ (nguyên liệu) trong cùng một khách.";
            long totalIngredients = 0;
            foreach (var dish in dishes)
            {
                if (dish == null || dish.amount < 1 || !SushiRecipeCatalog.TryGet(dish.dish, out var recipe))
                    return "Món cần ID công thức 1–48 và số phần >= 1.";
                totalIngredients += (long)dish.amount * recipe.Ingredients.Count;
                if (totalIngredients > int.MaxValue) return "Tổng nguyên liệu trong đơn vượt giới hạn dữ liệu.";
            }
            return null;
        }
        if (orders == null || orders.Count == 0) return "Khách chưa có món/order.";
        var types = new HashSet<PoolType>();
        foreach (var order in orders)
        {
            if (order == null || order.typeFood == PoolType.None || !Enum.IsDefined(typeof(PoolType), order.typeFood) || order.amount < 1)
                return "Order nguyên liệu không hợp lệ.";
            if (!types.Add(order.typeFood)) return "Gộp nguyên liệu trùng vào amount.";
        }
        return null;
    }
}

[Serializable]
public class FoodData
{
    public PoolType typeFood;
    [Range(1, 3)] public int amount = 1;
    public FoodState foodState = FoodState.Normal;

    public bool hidden;
    public int iceLayers;
    [Min(0)] public int linkId;
    [Min(0)] public int trayId;
    // Giữ dữ liệu cũ: Lock/lockId được đọc như linked_tile/linkId.
    public int lockId;

    public FoodData Copy()
    {
        return (FoodData)MemberwiseClone();
    }
}

[Serializable]
public class FoodQueueData
{
    // Phần tử 0 là Food đầu cột.
    public List<FoodData> foods = new List<FoodData>();
}

[Serializable]
public class FoodModifierData
{
    [Tooltip("Cột bắt đầu từ 0.")]
    [Min(0)]
    public int column;

    [Tooltip("Hàng bắt đầu từ 0, hàng 0 là đầu cột.")]
    [Min(0)]
    public int row;

    public FoodState foodState;

    public bool hidden;

    [Min(1)]
    public int iceLayers = 1;

    [Min(0)]
    public int lockId;

    [Min(0)] public int linkId;
    [Min(0)] public int trayId;
}
