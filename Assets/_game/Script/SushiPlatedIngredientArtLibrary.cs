using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Kiểu đĩa hoàn thành: mỗi kiểu có đĩa riêng và bộ lớp ảnh riêng. Chỉ thêm ở cuối để giữ dữ liệu đã lưu.
public enum SushiPlateStyle { None, Nigiri, Maki, Gunkan, SashimiTwo, SashimiThree }

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

    // ───── Bộ ảnh ghép lớp (nguyenLieu_monAn/fix): mỗi kiểu món có đĩa riêng và các lớp ảnh vẽ trên cùng một khung gốc ─────

    [Serializable]
    public sealed class StyleEntry
    {
        public SushiPlateStyle style;
        [Tooltip("Tâm khung ảnh gốc so với tâm ô đĩa trên bàn (đơn vị world).")]
        public Vector2 position;
        [Tooltip("Cạnh khung ảnh gốc (258 px) theo đơn vị world: chỉnh để phóng to/thu nhỏ cả món lẫn đĩa.")]
        [Min(0.05f)] public float size = 1f;
        [Tooltip("Đĩa hoàn thành của kiểu món này.")]
        public Sprite plate;
        [Tooltip("Tâm đĩa trong khung gốc, tính theo tỉ lệ khung (0,0 = giữa khung).")]
        public Vector2 platePosition;
        [Tooltip("Kích thước đĩa theo tỉ lệ khung gốc.")]
        public Vector2 plateSize = Vector2.one;
        [Tooltip("Chỗ đặt nguyên liệu gốc (bát cơm, miếng cá...) trước khi ghép thành món, theo tỉ lệ khung gốc. Đặt lệch để không che đôi đũa trên đĩa.")]
        public Vector2 rawPosition;
        [Tooltip("Cạnh lớn nhất của nguyên liệu nền khi chưa ghép, theo tỉ lệ khung gốc. Topping thu nhỏ theo cùng tỉ lệ.")]
        [Min(0.05f)] public float rawSize = 0.5f;
        [Tooltip("Phóng to/thu nhỏ phần món ăn (mọi lớp trừ đĩa) quanh tâm khung gốc. 1 = đúng tỉ lệ ảnh gốc so với đĩa.")]
        [Min(0.1f)] public float foodScale = 1f;
        [Tooltip("Dời phần món ăn so với đĩa, theo tỉ lệ khung gốc (y âm = xuống dưới).")]
        public Vector2 foodOffset;
    }

    [Serializable]
    public sealed class LayerEntry
    {
        public SushiPlateStyle style;
        [Tooltip("Nguyên liệu làm lớp này hiện ra. None = lớp trang trí đi kèm topping ở vị trí slot (vd. củ cải bào dưới lát cá).")]
        public PoolType type;
        [Tooltip("0 = không phụ thuộc vị trí. 1–3 = chỉ dùng khi nguyên liệu nằm ở vị trí topping này của công thức.")]
        [Min(0)] public int slot;
        [Tooltip("Chỉ hiện khi nguyên liệu này đã có trên đĩa (None = không điều kiện).")]
        public PoolType requires;
        [Tooltip("Ẩn khi nguyên liệu này đã có trên đĩa (None = không điều kiện).")]
        public PoolType excludes;
        public Sprite sprite;
        [Tooltip("Tâm lớp ảnh trong khung gốc, tính theo tỉ lệ khung (0,0 = giữa khung).")]
        public Vector2 position;
        [Tooltip("Kích thước lớp ảnh theo tỉ lệ khung gốc.")]
        public Vector2 size = Vector2.one;
        [Tooltip("Thứ tự vẽ trong món: số lớn nằm trên.")]
        public int sortingOrder;
    }

    // Một lớp ảnh đã quy đổi: đơn vị world (trên đĩa) hoặc tỉ lệ khung gốc (UI).
    public struct Layer
    {
        public Sprite sprite;
        public Vector2 position;
        public Vector2 size;
        public int sortingOrder;
    }

    [Header("Món ghép lớp theo kiểu đĩa")]
    public List<StyleEntry> styles = new List<StyleEntry>();
    public List<LayerEntry> layers = new List<LayerEntry>();

    // Kiểu đĩa của công thức: nigiri/maki/gunkan theo nhóm món, sashimi theo số lát cá.
    public static SushiPlateStyle StyleOf(SushiRecipe recipe)
    {
        if (recipe == null) return SushiPlateStyle.None;
        switch (recipe.Family)
        {
            case SushiDishFamily.Nigiri: return SushiPlateStyle.Nigiri;
            case SushiDishFamily.Maki: return SushiPlateStyle.Maki;
            case SushiDishFamily.Gunkan: return SushiPlateStyle.Gunkan;
            case SushiDishFamily.Sashimi:
                return recipe.CoreCount == 2 ? SushiPlateStyle.SashimiTwo :
                    recipe.CoreCount == 3 ? SushiPlateStyle.SashimiThree : SushiPlateStyle.None;
            default: return SushiPlateStyle.None;
        }
    }

    // Cấu hình kiểu đĩa của công thức; null khi món không dùng bộ ảnh ghép lớp (vd. trà).
    public StyleEntry FindStyle(SushiRecipe recipe)
    {
        SushiPlateStyle style = StyleOf(recipe);
        if (style == SushiPlateStyle.None) return null;
        foreach (StyleEntry entry in styles)
            if (entry != null && entry.style == style && entry.plate != null) return entry;
        return null;
    }

    // Món được dựng từ các lớp ảnh trên đĩa riêng thay cho một ảnh món hoàn chỉnh.
    public bool IsLayered(SushiRecipe recipe) => FindStyle(recipe) != null;

    private static bool Matches(LayerEntry entry, SushiPlateStyle style, PoolType type, int slot) =>
        entry != null && entry.style == style && entry.sprite != null &&
        (entry.slot == 0 ? entry.type == type : slot == entry.slot && (entry.type == type || entry.type == PoolType.None));

    // Kiểu đĩa có lớp ảnh cho nguyên liệu này (chưa xét điều kiện requires/excludes).
    public bool HasLayers(SushiRecipe recipe, PoolType type)
    {
        StyleEntry style = FindStyle(recipe);
        if (style == null) return false;
        int slot = recipe.ToppingSlot(type);
        // Lớp trang trí theo vị trí (type None) không đủ để coi nguyên liệu có ảnh riêng.
        foreach (LayerEntry entry in layers)
            if (entry != null && entry.type == type && Matches(entry, style.style, type, slot)) return true;
        return false;
    }

    // Các lớp ảnh của một nguyên liệu trên món. present cho biết nguyên liệu nào đã nằm trên đĩa
    // (null = coi như đủ cả công thức). normalized = true trả về tỉ lệ khung gốc để dựng trên UI.
    public void GetLayers(SushiRecipe recipe, PoolType type, Predicate<PoolType> present, List<Layer> result, bool normalized = false)
    {
        StyleEntry style = FindStyle(recipe);
        if (style == null) return;
        int slot = recipe.ToppingSlot(type);
        int first = result.Count;
        foreach (LayerEntry entry in layers)
        {
            if (!Matches(entry, style.style, type, slot)) continue;
            if (entry.requires != PoolType.None && !IsPresent(recipe, entry.requires, present)) continue;
            if (entry.excludes != PoolType.None && IsPresent(recipe, entry.excludes, present)) continue;
            float food = style.foodScale > 0f ? style.foodScale : 1f;
            result.Add(ToLayer(style, entry.sprite, entry.position * food + style.foodOffset, entry.size * food,
                entry.sortingOrder, normalized));
        }
        // Lớp dưới cùng đứng trước: đĩa/UI vẽ theo đúng thứ tự này.
        result.Sort(first, result.Count - first, LayerOrder);
    }

    // Lớp đĩa của món ghép lớp; false khi món không dùng bộ ảnh này.
    public bool TryGetPlateLayer(SushiRecipe recipe, out Layer plate, bool normalized = false)
    {
        StyleEntry style = FindStyle(recipe);
        plate = style != null ? ToLayer(style, style.plate, style.platePosition, style.plateSize, 0, normalized) : default;
        return style != null;
    }

    private static bool IsPresent(SushiRecipe recipe, PoolType type, Predicate<PoolType> present) =>
        present != null ? present(type) : recipe.Ingredients.Contains(type);

    private static Layer ToLayer(StyleEntry style, Sprite sprite, Vector2 position, Vector2 size, int order, bool normalized) =>
        new Layer
        {
            sprite = sprite,
            position = normalized ? position : style.position + position * style.size,
            size = normalized ? size : size * style.size,
            sortingOrder = order
        };

    private static readonly Comparer<Layer> LayerOrder =
        Comparer<Layer>.Create((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

    // Lấy cấu hình đĩa riêng của món: đĩa theo kiểu món ghép lớp, hoặc đĩa sashimi cũ khi đã có sprite phù hợp.
    public PlateEntry GetPlate(SushiRecipe recipe)
    {
        if (TryGetPlateLayer(recipe, out Layer plate))
            return new PlateEntry { sprite = plate.sprite, position = plate.position, size = plate.size };
        if (recipe.Family != SushiDishFamily.Sashimi) return null;
        PlateEntry entry = recipe.CoreCount == 2 ? sashimiTwoIngredientPlate :
            recipe.CoreCount == 3 ? sashimiThreeIngredientPlate : null;
        return entry != null && entry.sprite != null ? entry : null;
    }

#if UNITY_EDITOR
    // Lên lịch cập nhật hình ảnh gameplay sau khi chỉnh thư viện trong Inspector.
    private void OnValidate()
    {
        // Inspector validation can run off the main thread; refresh scene objects later.
        UnityEditor.EditorApplication.delayCall -= RefreshEditorArtwork;
        UnityEditor.EditorApplication.delayCall += RefreshEditorArtwork;
    }

    // Hủy callback làm mới hình ảnh trong Editor khi thư viện bị tắt.
    private void OnDisable() => UnityEditor.EditorApplication.delayCall -= RefreshEditorArtwork;

    // Áp dụng thay đổi thư viện lên các đĩa cùng thẻ món đang chạy trong Editor.
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

    // Tìm hình nguyên liệu theo món cụ thể, kiểu công thức rồi đến nhóm món và mặc định.
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
    // Tính vị trí nguyên liệu trên đĩa theo cấu hình riêng và cách bố trí của loại công thức.
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

    // Lấy lớp vẽ nguyên liệu từ cấu hình hoặc suy ra thứ tự lớp theo công thức.
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

    // Tìm cấu hình nguyên liệu theo nhóm món, dùng cấu hình chung nếu không có ghi đè.
    public Entry Find(PoolType type, SushiDishFamily family)
    {
        foreach (FamilyEntry entry in familyOverrides)
            if (entry != null && entry.type == type && entry.family == family && entry.sprite != null) return entry;
        foreach (Entry entry in ingredients)
            if (entry != null && entry.type == type && entry.sprite != null) return entry;
        return null;
    }
}
