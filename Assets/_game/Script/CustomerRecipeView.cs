using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerRecipeView : MonoBehaviour
{
    [Tooltip("Inactive nested DishCard prefab, editable on Customer.prefab.")]
    [SerializeField] private SushiDishCardView cardTemplate;
    [SerializeField] private RectTransform orderRoot;
    [SerializeField] private Transform leftColumn;
    [SerializeField] private Transform rightColumn;
    private Customer customer;
    private SushiDishArtLibrary art;
    private readonly List<SushiDishCardView> cards = new List<SushiDishCardView>();
    private bool magnetTargeting;

    public SushiDishCardView CardAt(int index) => cards[index];

    public void Clear()
    {
        if (customer != null) customer.OnOrderChanged -= Refresh;
        customer = null;
        magnetTargeting = false;
        foreach (var card in cards)
            if (card != null) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
        cards.Clear();
    }

    public void Setup(Customer owner, SushiDishArtLibrary library)
    {
        Clear();
        customer = owner;
        art = library != null ? library : SushiArtCatalog.Default != null ? SushiArtCatalog.Default.dishImages : null;
        rightColumn.gameObject.SetActive(customer.Dishes.Count > 1);
        foreach (var dish in customer.Dishes)
        {
            SushiDishCardView card = Instantiate(cardTemplate, dish.Index % 2 == 0 ? leftColumn : rightColumn, false);
            card.name = dish.Recipe.Name;
            card.Bind(owner, dish, art);
            card.gameObject.SetActive(true);
            cards.Add(card);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
        customer.OnOrderChanged += Refresh;
        Refresh(customer);
    }

    public Transform DeliverySlot(int dishIndex, int ingredientIndex) => cards[dishIndex].DeliverySlot(ingredientIndex);

    public void SetSeated(bool seated)
    {
        bool changed = false;
        foreach (var card in cards)
            if (card.gameObject.activeSelf != seated) { card.gameObject.SetActive(seated); changed = true; }
        if (seated && changed) LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
    }

    public SushiDishOrder DishAtWorldPosition(Vector3 position)
    {
        if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) return null;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].gameObject.activeInHierarchy && !customer.Dishes[i].IsComplete && cards[i].Contains(position))
                return customer.Dishes[i];
        }
        return null;
    }

    public void SetMagnetTargeting(bool active)
    {
        if (magnetTargeting == active) return;
        magnetTargeting = active;
        if (customer != null) Refresh(customer);
    }

    public void RefreshArtwork()
    {
        if (customer == null) return;
        if (art == null && SushiArtCatalog.Default != null) art = SushiArtCatalog.Default.dishImages;
        for (int i = 0; i < cards.Count && i < customer.Dishes.Count; i++)
            cards[i].RefreshArtwork(customer, customer.Dishes[i], art);
        Refresh(customer);
    }

    private void Refresh(Customer owner)
    {
        SetSeated(owner.IsSeated);
        for (int i = 0; i < cards.Count && i < owner.Dishes.Count; i++)
            cards[i].Refresh(owner.Dishes[i], magnetTargeting);
    }

    private void OnDestroy() { if (customer != null) customer.OnOrderChanged -= Refresh; }
}
