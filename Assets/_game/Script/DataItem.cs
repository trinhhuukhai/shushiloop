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

public enum TypeTutFood
{
    Normal,
    ClickTut
}

[Serializable]
public class DishOrderData
{
    [Tooltip("Catalog giữ món cũ. Các kiểu còn lại cho phép tự ghép nguyên liệu.")]
    public SushiRecipeKind recipeKind;
    public SushiDishId dish = SushiDishId.NigiriShrimp;
    public PoolType firstIngredient = PoolType.Shrimp;
    public PoolType secondIngredient = PoolType.Avocado;
    public PoolType thirdIngredient = PoolType.Tuna;
    [Min(1)] public int amount = 1;

    // Tra món có sẵn hoặc tạo công thức tùy chỉnh, trả thông báo nếu dữ liệu không hợp lệ.
    public bool TryGetRecipe(out SushiRecipe recipe, out string error)
    {
        if (recipeKind != SushiRecipeKind.Catalog)
            return SushiRecipeCatalog.TryCreate(recipeKind, firstIngredient, secondIngredient, thirdIngredient, out recipe, out error);
        bool valid = SushiRecipeCatalog.TryGet(dish, out recipe);
        error = valid ? null : "Mã món trong catalog không hợp lệ.";
        return valid;
    }

    // Lấy công thức món và phát sinh lỗi nếu cấu hình đơn món không hợp lệ.
    public SushiRecipe GetRecipe()
    {
        if (!TryGetRecipe(out var recipe, out string error)) throw new InvalidOperationException(error);
        return recipe;
    }

    // Sao chép dữ liệu một mục đơn món để chỉnh sửa độc lập.
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
    // Trải đơn nguyên liệu thành danh sách từng phần thực phẩm riêng lẻ.
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

    // Quy đổi số phần của các món thành tổng số lượng cần cho từng loại nguyên liệu.
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

    // Kiểm tra danh sách món, số phần, công thức và giới hạn tổng lượng nguyên liệu.
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
    public TypeTutFood tutFood;
    public PoolType typeFood;
    [Range(1, 3)] public int amount = 1;
    public FoodState foodState = FoodState.Normal;

    public bool hidden;
    public int iceLayers;
    [Min(0)] public int linkId;
    [Min(0)] public int trayId;
    // Giữ dữ liệu cũ: Lock/lockId được đọc như linked_tile/linkId.
    public int lockId;

    // Sao chép dữ liệu một ô nguyên liệu để trạng thái runtime không sửa dữ liệu gốc.
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
