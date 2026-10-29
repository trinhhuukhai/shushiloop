using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SushiKhai/Dish Art", fileName = "SushiDishArt")]
public class SushiDishArtLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SushiDishId dish;
        public Sprite sprite;
    }
    public List<Entry> dishes = new List<Entry>();

    [Serializable]
    public sealed class CustomEntry
    {
        public SushiRecipeKind recipeKind = SushiRecipeKind.ThreeWithRice;
        public PoolType firstIngredient = PoolType.Shrimp;
        public PoolType secondIngredient = PoolType.Avocado;
        public Sprite sprite;
    }

    [Tooltip("Ảnh order riêng cho từng tổ hợp. Để trống sẽ ghép ảnh nguyên liệu trên đĩa.")]
    public List<CustomEntry> customRecipes = new List<CustomEntry>();

    public Sprite GetSprite(SushiRecipe recipe)
    {
        foreach (CustomEntry entry in customRecipes)
            if (entry != null && entry.sprite != null &&
                SushiRecipeCatalog.TryCreate(entry.recipeKind, entry.firstIngredient, entry.secondIngredient, out var custom, out _) &&
                custom.Key == recipe.Key) return entry.sprite;
        if (!recipe.IsCustom) return GetSprite(recipe.Id);
        foreach (SushiRecipe preset in SushiRecipeCatalog.All)
            if (preset.Key == recipe.Key) return GetSprite(preset.Id);
        return null;
    }

    public Sprite GetSprite(SushiDishId id)
    {
        foreach (var entry in dishes) if (entry != null && entry.dish == id) return entry.sprite;
        return null;
    }
}
