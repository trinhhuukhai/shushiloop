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
    private bool isFlying;
    private bool hasCachedPlate;
    private Sprite defaultPlateSprite;
    private Vector3 defaultPlatePosition;
    private Vector3 defaultPlateScale;
    public IReadOnlyList<FoodShushi> AssemblyIngredients => assemblyIngredients;
    public Transform IngredientRoot => ingredientRoot;
    public Customer Owner { get; private set; }
    public SushiDishOrder Order { get; private set; }
    public bool IsReady { get; private set; }

    public void Bind(Customer customer, SushiDishOrder order)
    {
        Owner = customer; Order = order; IsReady = false;
        isFlying = false;
        if (ingredientArt == null && SushiArtCatalog.Default != null)
            ingredientArt = SushiArtCatalog.Default.platedIngredientImages;
        RefreshPlateArtwork();
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
            UpdateIngredientTarget(type, target);
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

    private void UpdateIngredientTarget(PoolType type, Transform target)
    {
        var artwork = ingredientArt != null ? ingredientArt.Find(type, Order.Recipe) : null;
        Vector2 position = ingredientArt != null ? ingredientArt.GetPosition(type, Order.Recipe) : Vector2.zero;
        target.localPosition = new Vector3(position.x, position.y, -0.1f);
        target.localRotation = Quaternion.Euler(0, 0, artwork != null ? artwork.rotation : 0);
    }

    public void RefreshArtwork(SushiPlatedIngredientArtLibrary changedArt)
    {
        if (Order == null || ingredientArt != changedArt) return;
        RefreshPlateArtwork();
        foreach (var target in ingredientTargets) UpdateIngredientTarget(target.Key, target.Value);
        foreach (FoodShushi food in assemblyIngredients)
            if (food != null) ApplyIngredientArtwork(food, isFlying);
    }

    private void RefreshPlateArtwork()
    {
        if (!hasCachedPlate)
        {
            defaultPlateSprite = plate.sprite;
            defaultPlatePosition = plate.transform.localPosition;
            defaultPlateScale = plate.transform.localScale;
            hasCachedPlate = true;
        }
        var artwork = ingredientArt != null ? ingredientArt.GetPlate(Order.Recipe) : null;
        if (artwork == null)
        {
            plate.sprite = defaultPlateSprite;
            plate.transform.localPosition = defaultPlatePosition;
            plate.transform.localScale = defaultPlateScale;
            return;
        }
        plate.sprite = artwork.sprite;
        plate.transform.localPosition = new Vector3(artwork.position.x, artwork.position.y, defaultPlatePosition.z);
        plate.transform.localScale = new Vector3(artwork.size.x / Mathf.Max(0.01f, artwork.sprite.bounds.size.x),
            artwork.size.y / Mathf.Max(0.01f, artwork.sprite.bounds.size.y), defaultPlateScale.z);
    }

    public void ApplyIngredientArtwork(FoodShushi food, bool flying)
    {
        var artwork = ingredientArt != null ? ingredientArt.Find(food.Type, Order.Recipe) : null;
        Sprite sprite = artwork != null ? artwork.sprite : SushiArtCatalog.Default?.GetIngredient(food.Type);
        Vector2 size = artwork != null ? artwork.size : imageSize;
        int order = (flying ? 31 : 18) + (ingredientArt != null ? ingredientArt.GetSortingOrder(food.Type, Order.Recipe) : 0);
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
        isFlying = true;
        plate.sortingOrder = 30;
        foreach (FoodShushi food in assemblyIngredients) ApplyIngredientArtwork(food, true);
    }
    public void LeaveTable()
    {
        // The decorated sashimi plate is part of the dish delivered to its order.
        plate.enabled = ingredientArt != null && ingredientArt.GetPlate(Order.Recipe) != null;
    }
    private void OnDestroy() => ClearAssemblyIngredients();
}
