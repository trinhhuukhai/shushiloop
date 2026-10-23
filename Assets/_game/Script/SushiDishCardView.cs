using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Only binds data. RectTransforms, layout, fonts and image fitting belong to the prefab.
public sealed class SushiDishCardView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text dishName;
    [SerializeField] private Image finished;
    [SerializeField] private GameObject completedMark;
    [SerializeField] private SushiOrderIngredientView[] ingredients;
    [SerializeField] private Color pendingColor;
    [SerializeField] private Color completedColor;
    [SerializeField] private Color targetingColor;

    public RectTransform Rect => (RectTransform)transform;
    public Image DishImage => finished;
    public SushiOrderIngredientView IngredientAt(int index) => ingredients[index];
    public Transform DeliverySlot(int index) => ingredients[index].DeliverySlot;

    public void Bind(Customer customer, SushiDishOrder dish, SushiDishArtLibrary art)
    {
        if (dish.Ingredients.Count > ingredients.Length)
            throw new InvalidOperationException("DishCard.prefab needs more ingredient slots for " + dish.Recipe.Name);
        dishName.text = dish.Recipe.Name;
        for (int i = 0; i < ingredients.Length; i++)
            ingredients[i].gameObject.SetActive(i < dish.Ingredients.Count);
        RefreshArtwork(customer, dish, art);
        Refresh(dish, false);
    }

    public void RefreshArtwork(Customer customer, SushiDishOrder dish, SushiDishArtLibrary art)
    {
        finished.sprite = art != null ? art.GetSprite(dish.Recipe.Id) : null;
        finished.enabled = finished.sprite != null;
        for (int i = 0; i < dish.Ingredients.Count; i++)
            ingredients[i].SetSprite(customer.GetIngredientSprite(dish.Ingredients[i].typeFood));
    }

    public void Refresh(SushiDishOrder dish, bool targeting)
    {
        background.color = dish.IsComplete ? completedColor : targeting ? targetingColor : pendingColor;
        completedMark.SetActive(dish.IsComplete);
        for (int i = 0; i < dish.Ingredients.Count; i++) ingredients[i].SetProgress(dish.Ingredients[i]);
    }

    public bool Contains(Vector3 worldPosition) => Rect.rect.Contains(Rect.InverseTransformPoint(worldPosition));
}
