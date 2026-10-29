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
    [SerializeField] private Sprite oneColumnTwoRowsBoard;
    [SerializeField] private Sprite twoColumnTwoRowsBoard;
    [SerializeField] private Sprite twoColumnThreeRowsBoard;
    private Customer customer;
    private SushiDishArtLibrary art;
    private readonly List<SushiDishCardView> cards = new List<SushiDishCardView>();
    private readonly List<SushiDishCardView> dishCards = new List<SushiDishCardView>();
    private readonly List<List<SushiDishOrder>> groups = new List<List<SushiDishOrder>>();
    private bool magnetTargeting;
    private float collapsedOrderHeight;
    private bool hasCachedOrderHeight;
    private Vector2 defaultOrderSize;
    private Vector2 defaultOrderPosition;
    private Sprite defaultOrderBoard;
    private Image.Type defaultOrderBoardType;
    private Image orderBoardImage;
    private bool hasCachedOrderBoard;

    public SushiDishCardView CardAt(int index) => dishCards[index];

    public void Clear()
    {
        if (customer != null) customer.OnOrderChanged -= Refresh;
        RestoreOrderBoard();
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
            CacheDefaultOrderBoard();
        }
        foreach (var dish in customer.Dishes)
        {
            var group = groups.Find(g => g[0].Recipe.Key == dish.Recipe.Key);
            if (group == null) { group = new List<SushiDishOrder>(); groups.Add(group); }
            group.Add(dish);
        }
        ConfigureOrderBoard(groups.Count);
        bool twoColumns = groups.Count > 1;
        rightColumn.gameObject.SetActive(twoColumns);
        for (int i = 0; i < groups.Count; i++)
        {
            Transform column = twoColumns && i % 2 == 1 ? rightColumn : leftColumn;
            SushiDishCardView card = Instantiate(cardTemplate, column, false);
            card.name = groups[i][0].Recipe.Name;
            card.BindGroup(owner, groups[i], art);
            card.gameObject.SetActive(true);
            cards.Add(card);
        }
        foreach (var dish in customer.Dishes)
            dishCards.Add(cards[groups.FindIndex(g => g[0].Recipe.Key == dish.Recipe.Key)]);
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
            if (card.IsOrderComplete) return false;
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
        bool layoutChanged = false;
        for (int i = 0; i < cards.Count && i < owner.Dishes.Count; i++)
        {
            bool wasExpanded = cards[i].IsRecipeExpanded;
            cards[i].Refresh(groups[i][0], magnetTargeting);
            layoutChanged |= wasExpanded && !cards[i].IsRecipeExpanded;
        }
        if (layoutChanged) RefreshOrderLayout();
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

    private void CacheDefaultOrderBoard()
    {
        if (orderRoot == null)
        {
            Debug.LogError("Customer recipe view requires an order panel RectTransform.", this);
            return;
        }
        orderBoardImage = orderRoot.GetComponent<Image>();
        if (orderBoardImage == null)
        {
            Debug.LogError("Customer order panel requires an Image for its board artwork.", this);
            return;
        }
        defaultOrderSize = orderRoot.sizeDelta;
        defaultOrderPosition = orderRoot.anchoredPosition;
        defaultOrderBoard = orderBoardImage.sprite;
        defaultOrderBoardType = orderBoardImage.type;
        hasCachedOrderBoard = true;
    }

    private void ConfigureOrderBoard(int orderCount)
    {
        if (!hasCachedOrderBoard)
        {
            CacheDefaultOrderBoard();
            if (!hasCachedOrderBoard) return;
        }

        Sprite board;
        float height;
        switch (orderCount)
        {
            case 1:
                board = oneColumnTwoRowsBoard;
                height = 184f;
                break;
            case 2:
            case 3:
            case 4:
                board = twoColumnTwoRowsBoard;
                height = 184f;
                break;
            default:
                board = twoColumnThreeRowsBoard;
                height = 248f;
                break;
        }

        if (board == null)
        {
            Debug.LogError("Customer order board artwork is not assigned for " + orderCount + " orders.", this);
            return;
        }
        orderBoardImage.sprite = board;
        orderBoardImage.type = Image.Type.Simple;
        float width = height * board.bounds.size.x / board.bounds.size.y;
        orderRoot.sizeDelta = new Vector2(width, height);
        orderRoot.anchoredPosition = defaultOrderPosition;
        collapsedOrderHeight = height;
        hasCachedOrderHeight = true;
    }

    private void RestoreOrderBoard()
    {
        if (!hasCachedOrderBoard || orderRoot == null) return;
        orderRoot.sizeDelta = defaultOrderSize;
        orderRoot.anchoredPosition = defaultOrderPosition;
        orderBoardImage.sprite = defaultOrderBoard;
        orderBoardImage.type = defaultOrderBoardType;
        collapsedOrderHeight = defaultOrderSize.y;
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
    }

    private void OnDestroy() { if (customer != null) customer.OnOrderChanged -= Refresh; }
}
