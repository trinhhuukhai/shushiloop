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
        [Tooltip("Exact plated width (X) and height (Y), in local units. Each dimension is applied independently.")]
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

    [Serializable]
    public sealed class DishEntry : Entry
    {
        public SushiDishId dish;
    }

    [Serializable]
    public sealed class PlateEntry
    {
        public Sprite sprite;
        public Vector2 position;
        public Vector2 size = new Vector2(0.86f, 0.7f);
    }

    public List<Entry> ingredients = new List<Entry>();
    public List<FamilyEntry> familyOverrides = new List<FamilyEntry>();
    [Tooltip("Ảnh khi lên đĩa, ưu tiên theo kiểu công thức trước ảnh của family.")]
    public List<RecipeEntry> recipeOverrides = new List<RecipeEntry>();
    [Tooltip("Lớp nguyên liệu của món trong catalog; ưu tiên trước ảnh theo kiểu công thức.")]
    public List<DishEntry> dishOverrides = new List<DishEntry>();

    [Header("Sashimi plates")]
    public PlateEntry sashimiTwoIngredientPlate = new PlateEntry();
    public PlateEntry sashimiThreeIngredientPlate = new PlateEntry();

    public PlateEntry GetPlate(SushiRecipe recipe)
    {
        if (recipe.Family != SushiDishFamily.Sashimi) return null;
        PlateEntry entry = recipe.Ingredients.Count == 2 ? sashimiTwoIngredientPlate :
            recipe.Ingredients.Count == 3 ? sashimiThreeIngredientPlate : null;
        return entry != null && entry.sprite != null ? entry : null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Inspector validation can run off the main thread; refresh scene objects later.
        UnityEditor.EditorApplication.delayCall -= RefreshEditorArtwork;
        UnityEditor.EditorApplication.delayCall += RefreshEditorArtwork;
    }

    private void OnDisable() => UnityEditor.EditorApplication.delayCall -= RefreshEditorArtwork;

    private void RefreshEditorArtwork()
    {
        if (this == null || !UnityEditor.EditorApplication.isPlaying) return;
        foreach (SushiPreparedDish dish in FindObjectsByType<SushiPreparedDish>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            dish.RefreshArtwork(this);
        if (SushiArtCatalog.Default != null && SushiArtCatalog.Default.platedIngredientImages == this)
            foreach (CustomerRecipeView view in FindObjectsByType<CustomerRecipeView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                view.RefreshArtwork();
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    public Entry Find(PoolType type, SushiRecipe recipe)
    {
        if (!recipe.IsCustom)
            foreach (DishEntry entry in dishOverrides)
                if (entry != null && entry.dish == recipe.Id && entry.type == type && entry.sprite != null) return entry;
        foreach (RecipeEntry entry in recipeOverrides)
            if (entry != null && entry.type == type && entry.recipeKind == recipe.Kind && entry.sprite != null) return entry;
        return Find(type, recipe.Family);
    }

    // Shared by serving plates and order previews, so the recipe looks the same in both places.
    public Vector2 GetPosition(PoolType type, SushiRecipe recipe)
    {
        Entry entry = Find(type, recipe);
        if (entry != null && (recipe.Kind != SushiRecipeKind.ThreeWithoutRice || entry is RecipeEntry || entry is DishEntry))
            return entry.position;
        if (recipe.Kind == SushiRecipeKind.TwoWithRice || recipe.Kind == SushiRecipeKind.Catalog) return Vector2.zero;
        Vector2 position = entry != null ? entry.position : Vector2.zero;
        if (recipe.Kind == SushiRecipeKind.TwoWithoutRice)
            return position + new Vector2(recipe.Ingredients[0] == type ? -0.2f : 0.2f, 0);
        if (recipe.Kind == SushiRecipeKind.ThreeWithoutRice)
        {
            int index = 0;
            while (index < recipe.Ingredients.Count && recipe.Ingredients[index] != type) index++;
            return position + new Vector2((index - 1) * 0.22f, 0);
        }
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
        // Keep the authored order with the selected recipe/family/ingredient artwork.
        Entry entry = Find(type, recipe);
        if (entry != null) return entry.sortingOrder;
        if (recipe.Kind == SushiRecipeKind.ThreeWithRice)
        {
            if (type == PoolType.SushiRice) return 0;
            if (type == PoolType.Seaweed) return -1;
            return recipe.Ingredients[1] == type ? 2 : 3;
        }
        return 2;
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
