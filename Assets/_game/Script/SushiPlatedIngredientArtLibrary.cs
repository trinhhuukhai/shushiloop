using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SushiKhai/Plated Ingredient Art", fileName = "SushiPlatedIngredientArt")]
public sealed class SushiPlatedIngredientArtLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public PoolType type;
        public Sprite sprite;
        public Vector2 position;
        public Vector2 size = new Vector2(0.76f, 0.6f);
        public float rotation;
        public int sortingOrder = 2;
    }

    [Serializable]
    public sealed class FamilyEntry : Entry
    {
        public SushiDishFamily family;
    }

    [Serializable]
    public sealed class RecipeEntry : Entry
    {
        public SushiRecipeKind recipeKind = SushiRecipeKind.ThreeWithRice;
    }

    public List<Entry> ingredients = new List<Entry>();
    public List<FamilyEntry> familyOverrides = new List<FamilyEntry>();
    [Tooltip("Ảnh khi lên đĩa, ưu tiên theo kiểu công thức trước ảnh của family.")]
    public List<RecipeEntry> recipeOverrides = new List<RecipeEntry>();

    public Entry Find(PoolType type, SushiRecipe recipe)
    {
        foreach (RecipeEntry entry in recipeOverrides)
            if (entry != null && entry.type == type && entry.recipeKind == recipe.Kind && entry.sprite != null) return entry;
        return Find(type, recipe.Family);
    }

    // Shared by serving plates and order previews, so the recipe looks the same in both places.
    public Vector2 GetPosition(PoolType type, SushiRecipe recipe)
    {
        if (recipe.Kind == SushiRecipeKind.TwoWithRice || recipe.Kind == SushiRecipeKind.Catalog) return Vector2.zero;
        Vector2 position = Find(type, recipe)?.position ?? Vector2.zero;
        if (recipe.Kind == SushiRecipeKind.TwoWithoutRice)
            return position + new Vector2(recipe.Ingredients[0] == type ? -0.2f : 0.2f, 0);
        if (type == PoolType.SushiRice || type == PoolType.Seaweed) return position;
        int toppingCount = 0, toppingIndex = 0;
        foreach (PoolType ingredient in recipe.Ingredients)
        {
            if (ingredient == PoolType.SushiRice || ingredient == PoolType.Seaweed) continue;
            if (ingredient == type) toppingIndex = toppingCount;
            toppingCount++;
        }
        return position + new Vector2(toppingCount > 1 ? (toppingIndex == 0 ? -0.17f : 0.17f) : 0, 0.1f);
    }

    public int GetSortingOrder(PoolType type, SushiRecipe recipe)
    {
        if (recipe.Kind == SushiRecipeKind.ThreeWithRice)
        {
            if (type == PoolType.SushiRice) return 0;
            if (type == PoolType.Seaweed) return -1;
            return recipe.Ingredients[1] == type ? 2 : 3;
        }
        return Find(type, recipe)?.sortingOrder ?? 2;
    }

    public Entry Find(PoolType type, SushiDishFamily family)
    {
        foreach (FamilyEntry entry in familyOverrides)
            if (entry != null && entry.type == type && entry.family == family && entry.sprite != null) return entry;
        foreach (Entry entry in ingredients)
            if (entry != null && entry.type == type && entry.sprite != null) return entry;
        return null;
    }
}
