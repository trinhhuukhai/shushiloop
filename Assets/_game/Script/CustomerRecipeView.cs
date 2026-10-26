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
    private readonly List<SushiDishCardView> dishCards = new List<SushiDishCardView>();
    private readonly List<List<SushiDishOrder>> groups = new List<List<SushiDishOrder>>();
    private bool magnetTargeting;
    private float collapsedOrderHeight;
    private bool hasCachedOrderHeight;

    public SushiDishCardView CardAt(int index) => dishCards[index];

    public void Clear()
    {
        if (customer != null) customer.OnOrderChanged -= Refresh;
        RestoreOrderHeight();
        customer = null;
        magnetTargeting = false;
        foreach (var card in cards)
            if (card != null) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
        cards.Clear();
        dishCards.Clear(); groups.Clear();
    }

    public void Setup(Customer owner, SushiDishArtLibrary library)
    {
        Clear();
        customer = owner;
        art = library != null ? library : SushiArtCatalog.Default != null ? SushiArtCatalog.Default.dishImages : null;
        if (!hasCachedOrderHeight && orderRoot != null)
        {
            collapsedOrderHeight = orderRoot.rect.height;
            hasCachedOrderHeight = true;
        }
        foreach (var dish in customer.Dishes)
        {
            var group = groups.Find(g => g[0].Recipe.Id == dish.Recipe.Id);
            if (group == null) { group = new List<SushiDishOrder>(); groups.Add(group); }
            group.Add(dish);
        }
        rightColumn.gameObject.SetActive(groups.Count > 1);
        for (int i = 0; i < groups.Count; i++)
        {
            SushiDishCardView card = Instantiate(cardTemplate, i % 2 == 0 ? leftColumn : rightColumn, false);
            card.name = groups[i][0].Recipe.Name;
            card.BindGroup(owner, groups[i], art);
            card.gameObject.SetActive(true);
            cards.Add(card);
        }
        foreach (var dish in customer.Dishes)
            dishCards.Add(cards[groups.FindIndex(g => g[0].Recipe.Id == dish.Recipe.Id)]);
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
        RefreshOrderLayout();
        customer.OnOrderChanged += Refresh;
        Refresh(customer);
    }

    public Transform DeliverySlot(int dishIndex, int ingredientIndex) => dishCards[dishIndex].DeliverySlot(ingredientIndex);

    public void SetSeated(bool seated)
    {
        if (orderRoot != null) orderRoot.gameObject.SetActive(seated);
        bool changed = false;
        foreach (var card in cards)
            if (card.gameObject.activeSelf != seated) { card.gameObject.SetActive(seated); changed = true; }
        if (!seated)
            foreach (var card in cards)
                if (card.IsRecipeExpanded) card.SetRecipeExpanded(false);
        if (seated && changed) LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
        if (!seated) RefreshOrderLayout();
    }

    public SushiDishOrder DishAtWorldPosition(Vector3 position)
    {
        if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) return null;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].gameObject.activeInHierarchy && cards[i].Contains(position))
            {
                var available = groups[i].Find(d => !d.IsComplete && !d.IsPrepared);
                return available ?? groups[i].Find(d => !d.IsComplete);
            }
        }
        return null;
    }

    public bool ToggleRecipeDetailsAtWorldPosition(Vector3 position)
    {
        if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) return false;
        foreach (var card in cards)
        {
            if (!card.gameObject.activeInHierarchy || !card.Contains(position)) continue;
            bool expand = !card.IsRecipeExpanded;
            if (expand)
                foreach (var otherCard in cards)
                    if (otherCard != card) otherCard.SetRecipeExpanded(false);
            card.SetRecipeExpanded(expand);
            RefreshOrderLayout();
            return true;
        }
        return false;
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
            cards[i].RefreshArtwork(customer, groups[i][0], art);
        Refresh(customer);
    }

    private void Refresh(Customer owner)
    {
        SetSeated(owner.IsSeated);
        for (int i = 0; i < cards.Count && i < owner.Dishes.Count; i++)
            cards[i].Refresh(groups[i][0], magnetTargeting);
    }

    private void RefreshOrderLayout()
    {
        if (orderRoot == null) return;
        float requiredHeight = Mathf.Max(GetColumnHeight(leftColumn), GetColumnHeight(rightColumn));
        HorizontalLayoutGroup layout = orderRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) requiredHeight += layout.padding.top + layout.padding.bottom;
        float targetHeight = Mathf.Max(collapsedOrderHeight, requiredHeight);
        orderRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
    }

    private float GetColumnHeight(Transform column)
    {
        if (column == null) return 0;
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        float height = layout != null ? layout.padding.top + layout.padding.bottom : 0;
        int cardCount = 0;
        foreach (Transform child in column)
        {
            if (!child.gameObject.activeSelf) continue;
            LayoutElement element = child.GetComponent<LayoutElement>();
            height += element != null && element.preferredHeight > 0
                ? element.preferredHeight : ((RectTransform)child).rect.height;
            if (cardCount++ > 0 && layout != null) height += layout.spacing;
        }
        return height;
    }

    private void RestoreOrderHeight()
    {
        if (!hasCachedOrderHeight || orderRoot == null) return;
        orderRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, collapsedOrderHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
    }

    private void OnDestroy() { if (customer != null) customer.OnOrderChanged -= Refresh; }
}
