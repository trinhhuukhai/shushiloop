using System.Collections.Generic;
using UnityEngine;

// One authored dish assembled on a customer serving plate, then flown to its order.
public sealed class SushiPreparedDish : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plate;
    [SerializeField] private SpriteRenderer dishImage;
    [SerializeField] private Vector2 imageSize = new Vector2(0.86f, 0.7f);
    private readonly List<FoodShushi> assemblyIngredients = new List<FoodShushi>();
    public Customer Owner { get; private set; }
    public SushiDishOrder Order { get; private set; }
    public bool IsReady { get; private set; }

    public void Bind(Customer customer, SushiDishOrder order)
    {
        Owner = customer; Order = order; IsReady = false;
        dishImage.sprite = SushiArtCatalog.Default.GetDish(order.Recipe.Id);
        if (dishImage.sprite != null)
        {
            Vector2 size = dishImage.sprite.bounds.size;
            dishImage.transform.localScale = Vector3.one * Mathf.Min(imageSize.x / size.x, imageSize.y / size.y);
        }
        dishImage.enabled = false;
        plate.enabled = true;
    }

    public void BeginAssembly()
    {
        plate.enabled = true;
        dishImage.enabled = false;
    }

    public void AddAssemblyIngredient(FoodShushi food)
    {
        if (food == null) return;
        food.transform.SetParent(transform, false);
        food.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        food.transform.localRotation = Quaternion.identity;
        food.transform.localScale = Vector3.one * 0.78f;
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
        ClearAssemblyIngredients();
        IsReady = true;
        dishImage.enabled = true;
    }
    public void SetFlying() { plate.sortingOrder = 30; dishImage.sortingOrder = 31; }
    public void LeaveTable() { plate.enabled = false; }
}
