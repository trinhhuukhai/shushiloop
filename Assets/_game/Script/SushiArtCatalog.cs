using UnityEngine;

// Hai danh sách tra ảnh theo type và bộ ảnh element dùng chung cho toàn bộ game.
[CreateAssetMenu(menuName = "SushiKhai/Art Catalog", fileName = "SushiArtCatalog")]
public sealed class SushiArtCatalog : ScriptableObject
{
    public SushiIngredientArtLibrary ingredientImages;
    public SushiDishArtLibrary dishImages;
    public Sprite hiddenTile;
    public Sprite frozenTile;
    public Sprite linkedTile;
    public Sprite rigidTray;

    private static SushiArtCatalog instance;
    public static SushiArtCatalog Default
    {
        get
        {
            if (instance == null) instance = Resources.Load<SushiArtCatalog>("SushiArtCatalog");
            return instance;
        }
    }

    public Sprite GetIngredient(PoolType type) => ingredientImages != null ? ingredientImages.GetSprite(type) : null;
    public Sprite GetDish(SushiDishId type) => dishImages != null ? dishImages.GetSprite(type) : null;
}
