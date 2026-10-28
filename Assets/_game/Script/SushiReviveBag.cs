using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The pouch, opening and quantity label are authored in ReviveBag.prefab.
public sealed class SushiReviveBag : MonoBehaviour
{
    [SerializeField] private Transform opening;
    [SerializeField] private TMP_Text quantity;
    private readonly List<FoodShushi> foods = new List<FoodShushi>();
    private int packingCount;

    public Transform Opening => opening != null ? opening : transform;
    public IReadOnlyList<FoodShushi> Foods => foods;
    public bool IsPacking => packingCount > 0;

    internal void Pack(IReadOnlyList<FoodShushi> contents)
    {
        foods.AddRange(contents);
        packingCount = foods.Count;
        RefreshQuantity();
    }

    internal void Store(FoodShushi food)
    {
        food.transform.SetParent(transform, false);
        food.transform.localPosition = Vector3.zero;
        food.gameObject.SetActive(false);
        packingCount--;
    }

    internal void Remove(FoodShushi food)
    {
        foods.Remove(food);
        RefreshQuantity();
    }

    private void RefreshQuantity()
    {
        if (quantity != null) quantity.text = "×" + foods.Count;
    }
}
