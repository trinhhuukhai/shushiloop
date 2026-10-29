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
    [Tooltip("Catalog giữ món cũ. Các kiểu còn lại cho phép tự ghép nguyên liệu.")]
    public SushiRecipeKind recipeKind;
    public SushiDishId dish = SushiDishId.NigiriShrimp;
    public PoolType firstIngredient = PoolType.Shrimp;
    public PoolType secondIngredient = PoolType.Avocado;
    [Min(1)] public int amount = 1;

    public bool TryGetRecipe(out SushiRecipe recipe, out string error)
    {
        if (recipeKind != SushiRecipeKind.Catalog)
            return SushiRecipeCatalog.TryCreate(recipeKind, firstIngredient, secondIngredient, out recipe, out error);
        bool valid = SushiRecipeCatalog.TryGet(dish, out recipe);
        error = valid ? null : "Mã món trong catalog không hợp lệ.";
        return valid;
    }

    public SushiRecipe GetRecipe()
    {
        if (!TryGetRecipe(out var recipe, out string error)) throw new InvalidOperationException(error);
        return recipe;
    }

    public DishOrderData Copy() => (DishOrderData)MemberwiseClone();
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
        if (!UsesRecipes) return result;
        foreach (DishOrderData order in dishes)
        {
            if (order == null || order.amount < 1) throw new InvalidOperationException("Order món không hợp lệ.");
            SushiRecipe recipe = order.GetRecipe();
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
        if (!UsesRecipes) return "Khách chưa có món trong dishes.";
        long totalIngredients = 0;
        foreach (var dish in dishes)
        {
            if (dish == null || dish.amount < 1) return "Món cần số phần >= 1.";
            if (!dish.TryGetRecipe(out var recipe, out string error)) return error;
            totalIngredients += (long)dish.amount * recipe.Ingredients.Count;
            if (totalIngredients > int.MaxValue) return "Tổng nguyên liệu trong đơn vượt giới hạn dữ liệu.";
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
