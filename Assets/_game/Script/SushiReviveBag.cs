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

    // Ghi danh sách nguyên liệu sẽ đóng túi và số chuyến bay còn chờ hoàn tất.
    internal void Pack(IReadOnlyList<FoodShushi> contents)
    {
        foods.AddRange(contents);
        packingCount = foods.Count;
        RefreshQuantity();
    }

    // Ẩn nguyên liệu đã bay vào túi, gắn vào túi và giảm số lượt đang đóng gói.
    internal void Store(FoodShushi food)
    {
        food.transform.SetParent(transform, false);
        food.transform.localPosition = Vector3.zero;
        food.gameObject.SetActive(false);
        packingCount--;
    }

    // Bỏ nguyên liệu được lấy khỏi túi và cập nhật nhãn số lượng.
    internal void Remove(FoodShushi food)
    {
        foods.Remove(food);
        RefreshQuantity();
    }

    // Hiển thị tổng lượng nguyên liệu hiện còn trong túi.
    private void RefreshQuantity()
    {
        if (quantity != null) quantity.text = "×" + foods.Count;
    }
}
