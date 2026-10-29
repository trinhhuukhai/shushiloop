using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public enum SushiDishId
{
    None = 0,
    NigiriTuna = 1, NigiriSalmon = 2, NigiriFlounder = 3, NigiriMackerel = 4,
    NigiriShrimp = 5, NigiriOctopus = 6, NigiriAvocado = 7, NigiriEgg = 8,
    NigiriMayonnaise = 9, NigiriOrangeRoe = 10, NigiriBlackRoe = 11, NigiriGreenRoe = 12,
    MakiTuna = 13, MakiSalmon = 14, MakiFlounder = 15, MakiMackerel = 16,
    MakiShrimp = 17, MakiOctopus = 18, MakiAvocado = 19, MakiEgg = 20,
    GunkanTuna = 21, GunkanSalmon = 22, GunkanFlounder = 23, GunkanMackerel = 24,
    GunkanShrimp = 25, GunkanOctopus = 26, GunkanAvocado = 27, GunkanEgg = 28,
    GunkanOrangeRoe = 29, GunkanBlackRoe = 30, GunkanGreenRoe = 31,
    SashimiTunaSalmon = 32, SashimiTunaFlounder = 33, SashimiTunaMackerel = 34,
    SashimiTunaShrimp = 35, SashimiTunaOctopus = 36,
    SashimiSalmonFlounder = 37, SashimiSalmonMackerel = 38,
    SashimiSalmonShrimp = 39, SashimiSalmonOctopus = 40,
    SashimiFlounderMackerel = 41, SashimiFlounderShrimp = 42, SashimiFlounderOctopus = 43,
    SashimiMackerelShrimp = 44, SashimiMackerelOctopus = 45, SashimiShrimpOctopus = 46,
    GreenTea = 47, IcedGreenTea = 48
}

public enum SushiDishFamily { Nigiri, Maki, Gunkan, Sashimi, Tea }

// Zero keeps existing serialized levels on their original catalog recipes.
public enum SushiRecipeKind { Catalog, TwoWithRice, TwoWithoutRice, ThreeWithRice }

public sealed class SushiRecipe
{
    public SushiDishId Id { get; }
    public SushiDishFamily Family { get; }
    public string Name { get; }
    public IReadOnlyList<PoolType> Ingredients { get; }
    public string BaseName { get; }
    public string ToppingStyle { get; }
    public bool HasRice => Ingredients.Contains(PoolType.SushiRice);
    public bool IsCustom => Id == SushiDishId.None;
    public SushiRecipeKind Kind => Family == SushiDishFamily.Tea ? SushiRecipeKind.Catalog :
        Ingredients.Count == 3 && HasRice ? SushiRecipeKind.ThreeWithRice :
        Ingredients.Count == 2 ? (HasRice ? SushiRecipeKind.TwoWithRice : SushiRecipeKind.TwoWithoutRice) : SushiRecipeKind.Catalog;
    public string Key { get; }

    internal SushiRecipe(SushiDishId id, SushiDishFamily family, string name, string baseName,
        string toppingStyle, params PoolType[] ingredients)
    {
        Id = id; Family = family; Name = name; BaseName = baseName; ToppingStyle = toppingStyle;
        Ingredients = Array.AsReadOnly((PoolType[])ingredients.Clone());
        var sorted = (PoolType[])ingredients.Clone();
        Array.Sort(sorted);
        Key = ((int)family) + ":" + string.Join(",", Array.ConvertAll(sorted, type => ((int)type).ToString()));
    }
}

// ID 1–48 trùng bảng công thức do game design cung cấp. Chỉ chứa dữ liệu bất biến.
public static class SushiRecipeCatalog
{
    private static readonly ReadOnlyCollection<SushiRecipe> recipes = CreateRecipes().AsReadOnly();
    public static IReadOnlyList<SushiRecipe> All => recipes;

    public static bool IsFoodTopping(PoolType type) => type >= PoolType.Shrimp && type <= PoolType.Seaweed;

    public static bool TryCreate(SushiRecipeKind kind, PoolType first, PoolType second,
        out SushiRecipe recipe, out string error)
    {
        recipe = null;
        error = null;
        if (kind != SushiRecipeKind.TwoWithRice && kind != SushiRecipeKind.TwoWithoutRice && kind != SushiRecipeKind.ThreeWithRice)
            error = "Chọn loại công thức 2 hoặc 3 nguyên liệu.";
        else if (!IsFoodTopping(first) || (kind != SushiRecipeKind.TwoWithRice && !IsFoodTopping(second)))
            error = "Chọn nguyên liệu món ăn; cơm được thêm tự động, trà/cốc/đá dành cho món nước.";
        else if (kind != SushiRecipeKind.TwoWithRice && first == second)
            error = "Chọn hai loại nguyên liệu khác nhau.";
        if (error != null) return false;

        // Canonical topping order also makes A+B and B+A share one order card.
        if (kind != SushiRecipeKind.TwoWithRice && first > second)
        { PoolType swap = first; first = second; second = swap; }
        PoolType[] items = kind == SushiRecipeKind.TwoWithRice ? new[] { PoolType.SushiRice, first } :
            kind == SushiRecipeKind.ThreeWithRice ? new[] { PoolType.SushiRice, first, second } : new[] { first, second };
        SushiDishFamily family = kind == SushiRecipeKind.TwoWithoutRice ? SushiDishFamily.Sashimi :
            kind == SushiRecipeKind.ThreeWithRice ? SushiDishFamily.Maki : SushiDishFamily.Nigiri;
        recipe = new SushiRecipe(SushiDishId.None, family, string.Join(" + ", Array.ConvertAll(items, IngredientName)),
            kind == SushiRecipeKind.TwoWithoutRice ? "Đĩa đôi" : "Đế cơm", "", items);
        // Mixed old/new orders share both their card and its ingredient-slot order.
        foreach (SushiRecipe preset in recipes)
            if (preset.Key == recipe.Key) { recipe = preset; break; }
        return true;
    }

    public static bool TryGet(SushiDishId id, out SushiRecipe recipe)
    {
        int index = (int)id - 1;
        recipe = index >= 0 && index < recipes.Count ? recipes[index] : null;
        return recipe != null;
    }

    public static SushiRecipe Get(SushiDishId id)
    {
        if (!TryGet(id, out var recipe)) throw new ArgumentOutOfRangeException(nameof(id), id, "Mã món ăn phải từ 1 đến 48.");
        return recipe;
    }

    public static string IngredientName(PoolType type)
    {
        switch (type)
        {
            case PoolType.SushiRice: return "Cơm";
            case PoolType.Tuna: return "Cá ngừ";
            case PoolType.Salmon: return "Cá hồi";
            case PoolType.Flounder: return "Cá bơn";
            case PoolType.Mackerel: return "Cá thu";
            case PoolType.Shrimp: return "Tôm";
            case PoolType.Octopus: return "Bạch tuộc";
            case PoolType.Avocado: return "Bơ";
            case PoolType.Egg: return "Trứng cuộn";
            case PoolType.Mayonnaise: return "Mayonnaise";
            case PoolType.FishRoe: return "Trứng cá cam";
            case PoolType.BlackFishRoe: return "Trứng cá đen";
            case PoolType.GreenFishRoe: return "Trứng cá xanh";
            case PoolType.Seaweed: return "Rong biển";
            case PoolType.TeaPot: return "Ấm trà";
            case PoolType.EmptyCup: return "Cốc rỗng";
            case PoolType.IceCube: return "Đá";
            case PoolType.Sesame: return "Hạt mè";
            case PoolType.CrabStick: return "Thanh cua";
            case PoolType.Cucumber: return "Dưa leo";
            case PoolType.GrilledEel: return "Lươn nướng";
            default: return type.ToString();
        }
    }

    private static List<SushiRecipe> CreateRecipes()
    {
        var result = new List<SushiRecipe>();
        PoolType[] toppings = { PoolType.Tuna, PoolType.Salmon, PoolType.Flounder, PoolType.Mackerel,
            PoolType.Shrimp, PoolType.Octopus, PoolType.Avocado, PoolType.Egg };
        PoolType[] roe = { PoolType.FishRoe, PoolType.BlackFishRoe, PoolType.GreenFishRoe };
        foreach (PoolType topping in toppings)
            Add(result, SushiDishFamily.Nigiri, "Nigiri " + IngredientName(topping).ToLowerInvariant(),
                "Đế nigiri", "Lớp lát", PoolType.SushiRice, topping);
        Add(result, SushiDishFamily.Nigiri, "Nigiri mayonnaise", "Đế nigiri", "Lớp phủ", PoolType.SushiRice, PoolType.Mayonnaise);
        foreach (PoolType topping in roe)
            Add(result, SushiDishFamily.Nigiri, "Nigiri " + IngredientName(topping).ToLowerInvariant(),
                "Đế nigiri", "Lớp phủ", PoolType.SushiRice, topping);
        foreach (PoolType topping in toppings)
            Add(result, SushiDishFamily.Maki, "Maki " + IngredientName(topping).ToLowerInvariant(),
                "Đế maki", "Hạt lựu", PoolType.SushiRice, PoolType.Seaweed, topping);
        foreach (PoolType topping in toppings)
            Add(result, SushiDishFamily.Gunkan, "Gunkan " + IngredientName(topping).ToLowerInvariant(),
                "Đế gunkan", "Hạt lựu", PoolType.SushiRice, PoolType.Seaweed, topping);
        foreach (PoolType topping in roe)
            Add(result, SushiDishFamily.Gunkan, "Gunkan " + IngredientName(topping).ToLowerInvariant(),
                "Đế gunkan", "Trứng cá", PoolType.SushiRice, PoolType.Seaweed, topping);
        // 6 nguyên liệu hải sản tạo đúng 15 cặp Sashimi theo thứ tự trong bảng.
        for (int first = 0; first < 6; first++)
            for (int second = first + 1; second < 6; second++)
                Add(result, SushiDishFamily.Sashimi,
                    "Sashimi " + IngredientName(toppings[first]).ToLowerInvariant() + " + " + IngredientName(toppings[second]).ToLowerInvariant(),
                    "Đĩa sashimi", "Lát", toppings[first], toppings[second]);
        Add(result, SushiDishFamily.Tea, "Trà xanh", "Cốc trà (nóng)", "", PoolType.TeaPot, PoolType.EmptyCup);
        Add(result, SushiDishFamily.Tea, "Trà xanh đá", "Cốc trà (có đá)", "", PoolType.TeaPot, PoolType.EmptyCup, PoolType.IceCube);
        return result;
    }

    private static void Add(List<SushiRecipe> result, SushiDishFamily family, string name, string baseName,
        string toppingStyle, params PoolType[] ingredients)
    {
        result.Add(new SushiRecipe((SushiDishId)(result.Count + 1), family, name, baseName, toppingStyle, ingredients));
    }
}
