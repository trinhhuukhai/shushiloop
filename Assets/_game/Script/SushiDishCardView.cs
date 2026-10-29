using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The prefab owns the default card layout; expanded recipes switch the ingredient row to a column.
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
    [SerializeField] private TMP_Text portionCount;
    [SerializeField] private Transform dishDeliverySlot;
    private IReadOnlyList<SushiDishOrder> portions;
    private GameObject ingredientDetails;
    private GameObject completedTick;
    private RectTransform ingredientDetailsRect;
    private HorizontalLayoutGroup ingredientsRowLayout;
    private VerticalLayoutGroup ingredientsColumnLayout;
    private LayoutElement ingredientDetailsLayout;
    private LayoutElement cardLayout;
    private Vector2 ingredientAnchorsMin;
    private Vector2 ingredientAnchorsMax;
    private Vector2 ingredientOffsetMin;
    private Vector2 ingredientOffsetMax;
    private float collapsedCardHeight;
    private float collapsedIngredientHeight;
    private int recipeIngredientCount;
    private bool originalDishNameActive;
    private bool orderComplete;
    private bool hasCachedRecipeLayout;
    private bool recipeExpanded;
    private readonly List<Image> recipeLayers = new List<Image>();

    public RectTransform Rect => (RectTransform)transform;
    public Image DishImage => finished;
    public string PortionProgress => portionCount != null ? portionCount.text : "";
    public Transform DishDeliverySlot => dishDeliverySlot != null ? dishDeliverySlot : finished.transform;
    public SushiOrderIngredientView IngredientAt(int index) => ingredients[index];
    public Transform DeliverySlot(int index) => ingredients[index].DeliverySlot;
    public bool IsRecipeExpanded => recipeExpanded;
    public bool IsOrderComplete => orderComplete;

    public void Bind(Customer customer, SushiDishOrder dish, SushiDishArtLibrary art)
    {
        if (dish.Ingredients.Count > ingredients.Length)
            throw new InvalidOperationException("DishCard.prefab needs more ingredient slots for " + dish.Recipe.Name);
        ingredientDetails = ingredients.Length > 0 ? ingredients[0].transform.parent.gameObject : null;
        Transform tick = completedMark != null ? completedMark.transform.Find("Tick art") : null;
        completedTick = tick != null ? tick.gameObject : null;
        if (completedTick == null)
            Debug.LogError("DishCard prefab is missing the Completed mark/Tick art visual.", this);
        ingredientDetailsRect = ingredientDetails != null ? ingredientDetails.GetComponent<RectTransform>() : null;
        ingredientsRowLayout = ingredientDetails != null ? ingredientDetails.GetComponent<HorizontalLayoutGroup>() : null;
        ingredientsColumnLayout = ingredientDetails != null ? ingredientDetails.GetComponent<VerticalLayoutGroup>() : null;
        ingredientDetailsLayout = ingredientDetails != null ? ingredientDetails.GetComponent<LayoutElement>() : null;
        cardLayout = GetComponent<LayoutElement>();
        recipeIngredientCount = dish.Ingredients.Count;
        if (!hasCachedRecipeLayout)
        {
            collapsedCardHeight = cardLayout != null && cardLayout.preferredHeight > 0
                ? cardLayout.preferredHeight : Rect.rect.height;
            collapsedIngredientHeight = ingredientDetailsLayout != null
                ? ingredientDetailsLayout.preferredHeight : 0;
            originalDishNameActive = dishName.gameObject.activeSelf;
            if (ingredientDetailsRect != null)
            {
                ingredientAnchorsMin = ingredientDetailsRect.anchorMin;
                ingredientAnchorsMax = ingredientDetailsRect.anchorMax;
                ingredientOffsetMin = ingredientDetailsRect.offsetMin;
                ingredientOffsetMax = ingredientDetailsRect.offsetMax;
            }
            hasCachedRecipeLayout = true;
        }
        dishName.text = dish.Recipe.Name;
        for (int i = 0; i < ingredients.Length; i++)
            ingredients[i].gameObject.SetActive(i < dish.Ingredients.Count);
        SetRecipeExpanded(false);
        RefreshArtwork(customer, dish, art);
        Refresh(dish, false);
    }

    public void BindGroup(Customer customer, IReadOnlyList<SushiDishOrder> group, SushiDishArtLibrary art)
    {
        portions = group;
        Bind(customer, group[0], art);
    }

    public void RefreshArtwork(Customer customer, SushiDishOrder dish, SushiDishArtLibrary art)
    {
        finished.sprite = art != null ? art.GetSprite(dish.Recipe) : null;
        finished.enabled = !recipeExpanded && finished.sprite != null;
        RefreshRecipeLayers(customer, dish.Recipe);
        for (int i = 0; i < dish.Ingredients.Count; i++)
            ingredients[i].SetSprite(customer.GetIngredientSprite(dish.Ingredients[i].typeFood));
    }

    private void RefreshRecipeLayers(Customer customer, SushiRecipe recipe)
    {
        foreach (Image layer in recipeLayers) layer.gameObject.SetActive(false);
        if (finished.sprite != null) return;
        SushiPlatedIngredientArtLibrary plated = SushiArtCatalog.Default?.platedIngredientImages;
        var types = new List<PoolType>(recipe.Ingredients);
        if (plated != null) types.Sort((a, b) => plated.GetSortingOrder(a, recipe).CompareTo(plated.GetSortingOrder(b, recipe)));
        for (int i = 0; i < types.Count; i++)
        {
            if (recipeLayers.Count <= i)
            {
                var layerObject = new GameObject("Recipe layer " + i, typeof(RectTransform), typeof(Image));
                layerObject.layer = finished.gameObject.layer;
                layerObject.transform.SetParent(finished.transform, false);
                var newLayer = layerObject.GetComponent<Image>();
                newLayer.raycastTarget = false;
                newLayer.preserveAspect = true;
                recipeLayers.Add(newLayer);
            }
            Image layer = recipeLayers[i];
            var entry = plated != null ? plated.Find(types[i], recipe) : null;
            layer.sprite = entry != null ? entry.sprite : customer.GetIngredientSprite(types[i]);
            Vector2 position = plated != null ? plated.GetPosition(types[i], recipe) : new Vector2((i - 1) * 0.25f, 0);
            Vector2 size = entry != null ? entry.size : new Vector2(0.45f, 0.45f);
            // Relative anchors keep the composed art responsive to the authored image frame.
            layer.rectTransform.anchorMin = Vector2.one * 0.5f + position - size * 0.5f;
            layer.rectTransform.anchorMax = Vector2.one * 0.5f + position + size * 0.5f;
            layer.rectTransform.offsetMin = layer.rectTransform.offsetMax = Vector2.zero;
            layer.rectTransform.localRotation = Quaternion.Euler(0, 0, entry != null ? entry.rotation : 0);
            layer.gameObject.SetActive(layer.sprite != null);
        }
    }

    public void Refresh(SushiDishOrder dish, bool targeting)
    {
        int received = 0, total = portions != null ? portions.Count : 1;
        if (portions != null) { foreach (var serving in portions) if (serving.IsComplete) received++; }
        else if (dish.IsComplete) received = 1;
        bool complete = received == total;
        orderComplete = complete;
        if (complete && recipeExpanded) SetRecipeExpanded(false);
        background.color = complete ? completedColor : targeting ? targetingColor : pendingColor;
        completedMark.SetActive(complete);
        if (completedTick != null) completedTick.SetActive(complete);
        if (portionCount != null)
        {
            portionCount.text = received + "/" + total;
            portionCount.gameObject.SetActive(!complete && !recipeExpanded);
        }
        for (int i = 0; i < dish.Ingredients.Count; i++) ingredients[i].SetProgress(dish.Ingredients[i]);
    }

    public void SetRecipeExpanded(bool expanded)
    {
        if (expanded && orderComplete) return;
        recipeExpanded = expanded;
        dishName.gameObject.SetActive(!expanded && originalDishNameActive);
        finished.gameObject.SetActive(!expanded);
        finished.enabled = !expanded && finished.sprite != null;
        if (portionCount != null)
            portionCount.gameObject.SetActive(!expanded && !orderComplete);
        foreach (SushiOrderIngredientView ingredient in ingredients)
            ingredient.SetRecipeDisplay(expanded);
        if (ingredientDetails == null) return;

        if (ingredientsRowLayout != null) ingredientsRowLayout.enabled = !expanded;
        if (ingredientsColumnLayout != null)
        {
            ingredientsColumnLayout.padding = new RectOffset(0, 0, 0, 0);
            ingredientsColumnLayout.spacing = 8;
            ingredientsColumnLayout.childAlignment = TextAnchor.MiddleCenter;
            ingredientsColumnLayout.childControlWidth = true;
            ingredientsColumnLayout.childControlHeight = true;
            ingredientsColumnLayout.childForceExpandWidth = true;
            ingredientsColumnLayout.childForceExpandHeight = false;
            ingredientsColumnLayout.enabled = expanded;
        }

        if (expanded)
        {
            if (ingredientDetailsRect != null)
            {
                ingredientDetailsRect.anchorMin = Vector2.zero;
                ingredientDetailsRect.anchorMax = Vector2.one;
                ingredientDetailsRect.offsetMin = new Vector2(6, 4);
                ingredientDetailsRect.offsetMax = new Vector2(-6, -4);
            }
            float ingredientHeight = 0;
            for (int i = 0; i < recipeIngredientCount; i++)
            {
                LayoutElement itemLayout = ingredients[i].GetComponent<LayoutElement>();
                ingredientHeight += itemLayout != null && itemLayout.preferredHeight > 0
                    ? itemLayout.preferredHeight : ingredients[i].GetComponent<RectTransform>().rect.height;
            }
            if (ingredientsColumnLayout != null)
                ingredientHeight += Mathf.Max(0, recipeIngredientCount - 1) * ingredientsColumnLayout.spacing;
            if (ingredientDetailsLayout != null)
                ingredientDetailsLayout.preferredHeight = ingredientHeight;
            float expandedCardHeight = Mathf.Max(collapsedCardHeight, ingredientHeight + 8);
            if (cardLayout != null)
                cardLayout.preferredHeight = expandedCardHeight;
        }
        else if (ingredientDetailsRect != null)
        {
            ingredientDetailsRect.anchorMin = ingredientAnchorsMin;
            ingredientDetailsRect.anchorMax = ingredientAnchorsMax;
            ingredientDetailsRect.offsetMin = ingredientOffsetMin;
            ingredientDetailsRect.offsetMax = ingredientOffsetMax;
            if (ingredientDetailsLayout != null)
                ingredientDetailsLayout.preferredHeight = collapsedIngredientHeight;
        }
        if (!expanded && cardLayout != null)
            cardLayout.preferredHeight = collapsedCardHeight;
        ingredientDetails.SetActive(expanded);
        if (expanded && ingredientDetailsRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(ingredientDetailsRect);
    }

    public bool Contains(Vector3 worldPosition) => Rect.rect.Contains(Rect.InverseTransformPoint(worldPosition));
}
