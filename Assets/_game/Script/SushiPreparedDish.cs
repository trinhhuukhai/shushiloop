using System.Collections.Generic;
using UnityEngine;

// One authored dish assembled on a customer serving plate, then flown to its order.
public sealed class SushiPreparedDish : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plate;
    [SerializeField] private SpriteRenderer dishImage;
    [SerializeField] private Vector2 imageSize = new Vector2(0.86f, 0.7f);
    [SerializeField] private SushiPlatedIngredientArtLibrary ingredientArt;
    [SerializeField] private Transform ingredientRoot;
    private readonly List<FoodShushi> assemblyIngredients = new List<FoodShushi>();
    private readonly Dictionary<PoolType, Transform> ingredientTargets = new Dictionary<PoolType, Transform>();
    public IReadOnlyList<FoodShushi> AssemblyIngredients => assemblyIngredients;
    public Transform IngredientRoot => ingredientRoot;
    public Customer Owner { get; private set; }
    public SushiDishOrder Order { get; private set; }
    public bool IsReady { get; private set; }

    public void Bind(Customer customer, SushiDishOrder order)
    {
        Owner = customer; Order = order; IsReady = false;
        if (ingredientArt == null && SushiArtCatalog.Default != null)
            ingredientArt = SushiArtCatalog.Default.platedIngredientImages;
        if (ingredientRoot == null)
        {
            ingredientRoot = new GameObject("Ingredients").transform;
            ingredientRoot.SetParent(transform, false);
        }
        for (int i = 0; i < order.Ingredients.Count; i++)
        {
            PoolType type = order.Ingredients[i].typeFood;
            var target = new GameObject(type.ToString()).transform;
            target.SetParent(ingredientRoot, false);
            var artwork = ingredientArt != null ? ingredientArt.Find(type, order.Recipe.Family) : null;
            Vector2 position = artwork != null ? artwork.position : Vector2.zero;
            if (order.Recipe.Family == SushiDishFamily.Sashimi)
                position += new Vector2((i - (order.Ingredients.Count - 1) * 0.5f) * 0.42f, 0);
            target.localPosition = new Vector3(position.x, position.y, -0.1f);
            target.localRotation = Quaternion.Euler(0, 0, artwork != null ? artwork.rotation : 0);
            ingredientTargets.Add(type, target);
        }
        dishImage.enabled = false;
        plate.enabled = true;
    }

    public void BeginAssembly()
    {
        plate.enabled = true;
        dishImage.enabled = false;
    }

    public Transform IngredientTarget(PoolType type) => ingredientTargets[type];

    public void ApplyIngredientArtwork(FoodShushi food, bool flying)
    {
        var artwork = ingredientArt != null ? ingredientArt.Find(food.Type, Order.Recipe.Family) : null;
        Sprite sprite = artwork != null ? artwork.sprite : SushiArtCatalog.Default?.GetIngredient(food.Type);
        Vector2 size = artwork != null ? artwork.size : imageSize;
        int order = (flying ? 31 : 18) + (artwork != null ? artwork.sortingOrder : 0);
        food.SetPlatedArtwork(sprite, size, order);
    }

    public void AddAssemblyIngredient(FoodShushi food)
    {
        if (food == null) return;
        food.transform.SetParent(IngredientTarget(food.Type), false);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;
        food.transform.localScale = Vector3.one;
        ApplyIngredientArtwork(food, false);
        assemblyIngredients.Add(food);
    }

    public void ClearAssemblyIngredients()
    {
        foreach (FoodShushi food in assemblyIngredients)
        {
            if (food == null) continue;
            food.transform.SetParent(null, true);
            SimplePool.Despawn(food);
        }
        assemblyIngredients.Clear();
    }

    public void FinishAssembly()
    {
        // Keep the assembled layers together: the parent is the finished dish.
        IsReady = true;
        dishImage.enabled = false;
    }
    public void SetFlying()
    {
        plate.sortingOrder = 30;
        foreach (FoodShushi food in assemblyIngredients) ApplyIngredientArtwork(food, true);
    }
    public void LeaveTable() { plate.enabled = false; }
    private void OnDestroy() => ClearAssemblyIngredients();
}
