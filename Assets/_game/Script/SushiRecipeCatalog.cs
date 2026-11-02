using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public enum SushiDishId
{
    None = 0,
    [InspectorName("nigiri_ca_ngu")] NigiriTuna = 1,
    [InspectorName("nigiri_ca_hoi")] NigiriSalmon = 2,
    [InspectorName("nigiri_ca_bon")] NigiriFlounder = 3,
    [InspectorName("nigiri_ca_thu")] NigiriMackerel = 4,
    [InspectorName("nigiri_tom")] NigiriShrimp = 5,
    [InspectorName("nigiri_muc")] NigiriOctopus = 6,
    [InspectorName("nigiri_bo")] NigiriAvocado = 7,
    [InspectorName("nigiri_trung")] NigiriEgg = 8,
    [InspectorName("maiki_ca_ngu")] MakiTuna = 13,
    [InspectorName("maiki_ca_hoi")] MakiSalmon = 14,
    [InspectorName("maiki_ca_bon")] MakiFlounder = 15,
    [InspectorName("maiki_ca_thu")] MakiMackerel = 16,
    [InspectorName("maiki_tom")] MakiShrimp = 17,
    [InspectorName("maiki_bach_tuoc")] MakiOctopus = 18,
    [InspectorName("miaki_bo")] MakiAvocado = 19,
    [InspectorName("maiki_trung")] MakiEgg = 20,
    [InspectorName("gunkan_ca_ngu")] GunkanTuna = 21,
    [InspectorName("gunkan_ca_hoi")] GunkanSalmon = 22,
    [InspectorName("gunkan_ca_bon")] GunkanFlounder = 23,
    [InspectorName("gunkan_ca_thu")] GunkanMackerel = 24,
    [InspectorName("gunkan_tom")] GunkanShrimp = 25,
    [InspectorName("gunkan_bach_tuoc")] GunkanOctopus = 26,
    [InspectorName("gunkan_bo")] GunkanAvocado = 27,
    [InspectorName("gunkan_trung")] GunkanEgg = 28,
    [InspectorName("gunkan_roe_vang")] GunkanOrangeRoe = 29,
    [InspectorName("gunkan_roe_den")] GunkanBlackRoe = 30,
    [InspectorName("gunkan_roe_xanh")] GunkanGreenRoe = 31,
    [InspectorName("sashimi_x2/32")] SashimiTunaSalmon = 32,
    [InspectorName("sashimi_x2/33")] SashimiTunaFlounder = 33,
    [InspectorName("sashimi_x2/34")] SashimiTunaMackerel = 34,
    [InspectorName("sashimi_x2/35")] SashimiTunaShrimp = 35,
    [InspectorName("sashimi_x2/36")] SashimiTunaOctopus = 36,
    [InspectorName("sashimi_x2/37")] SashimiSalmonFlounder = 37,
    [InspectorName("sashimi_x2/38")] SashimiSalmonMackerel = 38,
    [InspectorName("sashimi_x2/39")] SashimiSalmonShrimp = 39,
    [InspectorName("sashimi_x2/40")] SashimiSalmonOctopus = 40,
    [InspectorName("sashimi_x2/41")] SashimiFlounderMackerel = 41,
    [InspectorName("sashimi_x2/42")] SashimiFlounderShrimp = 42,
    [InspectorName("sashimi_x2/43")] SashimiFlounderOctopus = 43,
    [InspectorName("sashimi_x2/44")] SashimiMackerelShrimp = 44,
    [InspectorName("sashimi_x2/45")] SashimiMackerelOctopus = 45,
    [InspectorName("sashimi_x2/46")] SashimiShrimpOctopus = 46,
    [InspectorName("tra_nong")] GreenTea = 47,
    [InspectorName("tra_da")] IcedGreenTea = 48,
    [InspectorName("sashimi_x3/49")] SashimiTunaSalmonMackerel = 49,
    [InspectorName("sashimi_x3/50")] SashimiTunaSalmonShrimp = 50,
    [InspectorName("sashimi_x3/51")] SashimiTunaMackerelShrimp = 51,
    [InspectorName("sashimi_x3/52")] SashimiSalmonMackerelShrimp = 52,
    [InspectorName("sashimi_x3/53")] SashimiTunaSalmonFlounder = 53,
    [InspectorName("sashimi_x3/54")] SashimiTunaFlounderMackerel = 54,
    [InspectorName("sashimi_x3/55")] SashimiTunaFlounderShrimp = 55,
    [InspectorName("sashimi_x3/56")] SashimiSalmonFlounderMackerel = 56,
    [InspectorName("sashimi_x3/57")] SashimiSalmonFlounderShrimp = 57,
    [InspectorName("sashimi_x3/58")] SashimiFlounderMackerelShrimp = 58,
    [InspectorName("sashimi_x3/59")] SashimiTunaSalmonOctopus = 59,
    [InspectorName("sashimi_x3/60")] SashimiTunaFlounderOctopus = 60,
    [InspectorName("sashimi_x3/61")] SashimiTunaMackerelOctopus = 61,
    [InspectorName("sashimi_x3/62")] SashimiTunaShrimpOctopus = 62,
    [InspectorName("sashimi_x3/63")] SashimiSalmonFlounderOctopus = 63,
    [InspectorName("sashimi_x3/64")] SashimiSalmonMackerelOctopus = 64,
    [InspectorName("sashimi_x3/65")] SashimiSalmonShrimpOctopus = 65,
    [InspectorName("sashimi_x3/66")] SashimiFlounderMackerelOctopus = 66,
    [InspectorName("sashimi_x3/67")] SashimiFlounderShrimpOctopus = 67,
    [InspectorName("sashimi_x3/68")] SashimiMackerelShrimpOctopus = 68
}

public enum SushiDishFamily { Nigiri, Maki, Gunkan, Sashimi, Tea }

// Zero keeps existing serialized levels on their original catalog recipes.
public enum SushiRecipeKind { Catalog, TwoWithRice, TwoWithoutRice, ThreeWithRice, ThreeWithoutRice }

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
        Ingredients.Count == 3 ? (HasRice ? SushiRecipeKind.ThreeWithRice : SushiRecipeKind.ThreeWithoutRice) :
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

// IDs 9–12 are retired. Keep every remaining ID aligned with existing assets and source filenames.
public static class SushiRecipeCatalog
{
    private static readonly ReadOnlyCollection<SushiRecipe> recipes = CreateRecipes().AsReadOnly();
    private static readonly IReadOnlyDictionary<SushiDishId, SushiRecipe> recipesById = recipes.ToDictionary(recipe => recipe.Id);
    public static IReadOnlyList<SushiRecipe> All => recipes;

    // Use the same source-file labels in built-in enum controls and custom editors.
    public static string AssetName(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var label = field != null ? Attribute.GetCustomAttribute(field, typeof(InspectorNameAttribute)) as InspectorNameAttribute : null;
        return label != null ? label.displayName : value.ToString();
    }

    public static bool IsFoodTopping(PoolType type) => type >= PoolType.Shrimp && type <= PoolType.Seaweed;

    public static bool IsNigiriTopping(PoolType type) => IsFoodTopping(type) && type != PoolType.Mayonnaise &&
        type != PoolType.FishRoe && type != PoolType.BlackFishRoe && type != PoolType.GreenFishRoe;

    public static bool TryCreate(SushiRecipeKind kind, PoolType first, PoolType second,
        out SushiRecipe recipe, out string error)
        => TryCreate(kind, first, second, PoolType.None, out recipe, out error);

    public static bool TryCreate(SushiRecipeKind kind, PoolType first, PoolType second, PoolType third,
        out SushiRecipe recipe, out string error)
    {
        recipe = null;
        error = null;
        bool threeWithoutRice = kind == SushiRecipeKind.ThreeWithoutRice;
        bool withoutRice = kind == SushiRecipeKind.TwoWithoutRice || threeWithoutRice;
        if (kind != SushiRecipeKind.TwoWithRice && kind != SushiRecipeKind.TwoWithoutRice &&
            kind != SushiRecipeKind.ThreeWithRice && !threeWithoutRice)
            error = "Chọn loại công thức 2 hoặc 3 nguyên liệu.";
        else if (!IsFoodTopping(first) || (kind != SushiRecipeKind.TwoWithRice && !IsFoodTopping(second)) ||
            (threeWithoutRice && !IsFoodTopping(third)))
            error = "Chọn nguyên liệu món ăn; cơm được thêm tự động, trà/cốc/đá dành cho món nước.";
        else if (kind == SushiRecipeKind.TwoWithRice && !IsNigiriTopping(first))
            error = "Nguyên liệu này không dùng cho Nigiri. Chọn món hoặc công thức khác phù hợp.";
        else if ((kind != SushiRecipeKind.TwoWithRice && first == second) ||
            (threeWithoutRice && (third == first || third == second)))
            error = "Chọn các loại nguyên liệu khác nhau.";
        if (error != null) return false;

        // Canonical topping order also makes A+B and B+A share one order card.
        PoolType[] items = kind == SushiRecipeKind.TwoWithRice ? new[] { PoolType.SushiRice, first } :
            kind == SushiRecipeKind.ThreeWithRice ? new[] { PoolType.SushiRice, first, second } :
            threeWithoutRice ? new[] { first, second, third } : new[] { first, second };
        Array.Sort(items, withoutRice ? 0 : 1, items.Length - (withoutRice ? 0 : 1));
        SushiDishFamily family = withoutRice ? SushiDishFamily.Sashimi :
            kind == SushiRecipeKind.ThreeWithRice ? SushiDishFamily.Maki : SushiDishFamily.Nigiri;
        recipe = new SushiRecipe(SushiDishId.None, family,
            family + " " + string.Join(" + ", items.Where(type => type != PoolType.SushiRice)
                .Select(type => IngredientName(type).ToLowerInvariant())),
            withoutRice ? "Đĩa sashimi" : "Đế cơm", "", items);
        // Mixed old/new orders share both their card and its ingredient-slot order.
        foreach (SushiRecipe preset in recipes)
            if (preset.Key == recipe.Key) { recipe = preset; break; }
        return true;
    }

    public static bool TryGet(SushiDishId id, out SushiRecipe recipe)
    {
        return recipesById.TryGetValue(id, out recipe);
    }

    public static SushiRecipe Get(SushiDishId id)
    {
        if (!TryGet(id, out var recipe)) throw new ArgumentOutOfRangeException(nameof(id), id, "Mã món ăn không có trong catalog.");
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
            case PoolType.Egg: return "Trứng";
            case PoolType.Mayonnaise: return "Mayonnaise";
            case PoolType.FishRoe: return "Trứng cá vàng";
            case PoolType.BlackFishRoe: return "Trứng cá đen";
            case PoolType.GreenFishRoe: return "Trứng cá xanh";
            case PoolType.Seaweed: return "Rong biển";
            case PoolType.TeaPot: return "Ấm trà";
            case PoolType.EmptyCup: return "Tách trà";
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
            Add(result, SushiDishFamily.Nigiri, "Nigiri " + (topping == PoolType.Octopus ? "mực" : IngredientName(topping).ToLowerInvariant()),
                "Đế nigiri", "Lớp lát", PoolType.SushiRice, topping);
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
        Add(result, SushiDishFamily.Tea, "Trà nóng", "Tách trà", "", PoolType.TeaPot, PoolType.EmptyCup);
        Add(result, SushiDishFamily.Tea, "Trà đá", "Tách trà", "", PoolType.TeaPot, PoolType.EmptyCup, PoolType.IceCube);
        // Explicit source-image order: x3 is not a lexicographic list of combinations.
        AddSashimi(result, PoolType.Tuna, PoolType.Salmon, PoolType.Mackerel);       // 49.png
        AddSashimi(result, PoolType.Tuna, PoolType.Salmon, PoolType.Shrimp);         // 50.png
        AddSashimi(result, PoolType.Tuna, PoolType.Mackerel, PoolType.Shrimp);       // 51.png
        AddSashimi(result, PoolType.Salmon, PoolType.Mackerel, PoolType.Shrimp);     // 52.png
        AddSashimi(result, PoolType.Tuna, PoolType.Salmon, PoolType.Flounder);       // 53.png
        AddSashimi(result, PoolType.Tuna, PoolType.Flounder, PoolType.Mackerel);     // 54.png
        AddSashimi(result, PoolType.Tuna, PoolType.Flounder, PoolType.Shrimp);       // 55.png
        AddSashimi(result, PoolType.Salmon, PoolType.Flounder, PoolType.Mackerel);   // 56.png
        AddSashimi(result, PoolType.Salmon, PoolType.Flounder, PoolType.Shrimp);     // 57.png
        AddSashimi(result, PoolType.Flounder, PoolType.Mackerel, PoolType.Shrimp);   // 58.png
        AddSashimi(result, PoolType.Tuna, PoolType.Salmon, PoolType.Octopus);        // 59.png
        AddSashimi(result, PoolType.Tuna, PoolType.Flounder, PoolType.Octopus);      // 60.png
        AddSashimi(result, PoolType.Tuna, PoolType.Mackerel, PoolType.Octopus);      // 61.png
        AddSashimi(result, PoolType.Tuna, PoolType.Shrimp, PoolType.Octopus);        // 62.png
        AddSashimi(result, PoolType.Salmon, PoolType.Flounder, PoolType.Octopus);    // 63.png
        AddSashimi(result, PoolType.Salmon, PoolType.Mackerel, PoolType.Octopus);    // 64.png
        AddSashimi(result, PoolType.Salmon, PoolType.Shrimp, PoolType.Octopus);      // 65.png
        AddSashimi(result, PoolType.Flounder, PoolType.Mackerel, PoolType.Octopus);  // 66.png
        AddSashimi(result, PoolType.Flounder, PoolType.Shrimp, PoolType.Octopus);    // 67.png
        AddSashimi(result, PoolType.Mackerel, PoolType.Shrimp, PoolType.Octopus);    // 68.png
        return result;
    }

    private static void AddSashimi(List<SushiRecipe> result, params PoolType[] ingredients)
    {
        Add(result, SushiDishFamily.Sashimi,
            "Sashimi " + string.Join(" + ", Array.ConvertAll(ingredients, type => IngredientName(type).ToLowerInvariant())),
            "Đĩa sashimi", "Lát", ingredients);
    }

    private static void Add(List<SushiRecipe> result, SushiDishFamily family, string name, string baseName,
        string toppingStyle, params PoolType[] ingredients)
    {
        int id = result.Count < 8 ? result.Count + 1 : result.Count + 5;
        result.Add(new SushiRecipe((SushiDishId)id, family, name, baseName, toppingStyle, ingredients));
    }
}
