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
    [SerializeField] private RectTransform incompleteNotification;
    [Tooltip("Nền ô khi xem chi tiết công thức (fr_order_inside): mỗi món một ô, nguyên liệu xếp dọc.")]
    [SerializeField] private Sprite recipeCellSprite;
    [SerializeField] private Color recipeCellColor = Color.white;
    [Tooltip("Chiều cao hiển thị tối đa của một nguyên liệu khi xem chi tiết (đơn vị canvas).")]
    [SerializeField, Min(8f)] private float maxRecipeIngredientSize = 58f;
    [Tooltip("Khi xem chi tiết, panel nền của ô thu vào từng này mỗi cạnh để các ô không dính sát nhau và sát viền bảng.")]
    [SerializeField, Min(0f)] private float recipeCellInset = 4f;
    private Sprite defaultBackgroundSprite;
    private Image.Type defaultBackgroundType;
    private bool hasDefaultBackground;
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
    private float cellHeight;
    private float collapsedIngredientHeight;
    private int recipeIngredientCount;
    private bool originalDishNameActive;
    private bool orderComplete;
    private bool hasCachedRecipeLayout;
    private bool recipeExpanded;
    private readonly List<Image> recipeLayers = new List<Image>();
    private Image recipePlate;

    public RectTransform Rect => (RectTransform)transform;
    public Image DishImage => finished;
    public string PortionProgress => portionCount != null ? portionCount.text : "";
    public Transform DishDeliverySlot => dishDeliverySlot != null ? dishDeliverySlot : finished.transform;
    // Lấy thành phần UI hiển thị một mục nguyên liệu trên thẻ món.
    public SushiOrderIngredientView IngredientAt(int index) => ingredients[index];
    // Lấy điểm đích của nguyên liệu bay tới mục tương ứng trên thẻ.
    public Transform DeliverySlot(int index) => ingredients[index].DeliverySlot;
    public bool IsRecipeExpanded => recipeExpanded;
    public bool IsOrderComplete => orderComplete;

    // Tạo nhịp phóng to và lắc cho biểu tượng nhắc xem món chưa hoàn thành.
    private void LateUpdate()
    {
        if (incompleteNotification == null || !incompleteNotification.gameObject.activeSelf) return;
        float phase = Time.unscaledTime * 5f;
        incompleteNotification.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(phase));
        incompleteNotification.localRotation = Quaternion.Euler(0, 0, 7f * Mathf.Sin(phase * 1.4f));
    }

    // Hiện biểu tượng nhắc khi món chưa xong và chi tiết công thức đang đóng.
    private void RefreshNotification()
    {
        if (incompleteNotification == null) return;
        // Details replace the dish artwork, so keep the badge beside the collapsed dish only.
        incompleteNotification.gameObject.SetActive(!orderComplete && !recipeExpanded);
    }

    // Gắn dữ liệu món vào thẻ, lưu bố cục gốc và khởi tạo hình ảnh cùng tiến độ.
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
        if (cellHeight > 0) collapsedCardHeight = cellHeight;
        // Bảng order chỉ hiện hình món; số phần vẫn được cập nhật trong text nhưng không hiển thị.
        if (portionCount != null) portionCount.gameObject.SetActive(false);
        dishName.text = dish.Recipe.Name;
        for (int i = 0; i < ingredients.Length; i++)
            ingredients[i].gameObject.SetActive(i < dish.Ingredients.Count);
        SetRecipeExpanded(false);
        RefreshArtwork(customer, dish, art);
        Refresh(dish, false);
    }

    // Chiều cao một ô trên bảng order (bảng chia đều theo số hàng); thẻ thu gọn cao đúng bằng ô.
    public void SetCellHeight(float height)
    {
        if (height <= 0) return;
        cellHeight = height;
        collapsedCardHeight = height;
        if (cardLayout == null) cardLayout = GetComponent<LayoutElement>();
        if (cardLayout != null && !recipeExpanded) cardLayout.preferredHeight = height;
    }

    // Gắn nhóm các phần cùng công thức vào một thẻ món dùng chung.
    public void BindGroup(Customer customer, IReadOnlyList<SushiDishOrder> group, SushiDishArtLibrary art)
    {
        portions = group;
        Bind(customer, group[0], art);
    }

    // Cập nhật sprite món hoàn chỉnh, các lớp ghép dự phòng và biểu tượng nguyên liệu.
    public void RefreshArtwork(Customer customer, SushiDishOrder dish, SushiDishArtLibrary art)
    {
        finished.sprite = art != null ? art.GetSprite(dish.Recipe) : null;
        finished.enabled = !recipeExpanded && finished.sprite != null;
        RefreshRecipeLayers(customer, dish.Recipe);
        for (int i = 0; i < dish.Ingredients.Count; i++)
            ingredients[i].SetSprite(customer.GetIngredientSprite(dish.Ingredients[i].typeFood));
    }

    // Ghép hình món từ đĩa và từng lớp nguyên liệu khi chưa có sprite món hoàn chỉnh.
    private void RefreshRecipeLayers(Customer customer, SushiRecipe recipe)
    {
        foreach (Image layer in recipeLayers) layer.gameObject.SetActive(false);
        if (recipePlate != null) recipePlate.gameObject.SetActive(false);
        // Catalog sashimi images already include their decorated plate.
        if (finished.sprite != null) return;
        SushiPlatedIngredientArtLibrary plated = SushiArtCatalog.Default?.platedIngredientImages;
        var plateArtwork = plated != null ? plated.GetPlate(recipe) : null;
        if (plateArtwork != null)
        {
            if (recipePlate == null)
            {
                var plateObject = new GameObject("Sashimi plate", typeof(RectTransform), typeof(Image));
                plateObject.layer = finished.gameObject.layer;
                plateObject.transform.SetParent(finished.transform, false);
                plateObject.transform.SetAsFirstSibling();
                recipePlate = plateObject.GetComponent<Image>();
                recipePlate.raycastTarget = false;
            }
            recipePlate.sprite = plateArtwork.sprite;
            recipePlate.rectTransform.anchorMin = Vector2.one * 0.5f + plateArtwork.position - plateArtwork.size * 0.5f;
            recipePlate.rectTransform.anchorMax = Vector2.one * 0.5f + plateArtwork.position + plateArtwork.size * 0.5f;
            recipePlate.rectTransform.offsetMin = recipePlate.rectTransform.offsetMax = Vector2.zero;
            recipePlate.gameObject.SetActive(true);
        }
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
                recipeLayers.Add(newLayer);
            }
            Image layer = recipeLayers[i];
            var entry = plated != null ? plated.Find(types[i], recipe) : null;
            layer.sprite = entry != null ? entry.sprite : customer.GetIngredientSprite(types[i]);
            layer.preserveAspect = entry == null;
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

    // Cập nhật số phần đã nhận, dấu hoàn thành, màu mục tiêu và tiến độ từng nguyên liệu.
    public void Refresh(SushiDishOrder dish, bool targeting)
    {
        int received = 0, total = portions != null ? portions.Count : 1;
        if (portions != null) { foreach (var serving in portions) if (serving.IsComplete) received++; }
        else if (dish.IsComplete) received = 1;
        bool complete = received == total;
        orderComplete = complete;
        RefreshNotification();
        if (complete && recipeExpanded) SetRecipeExpanded(false);
        background.color = recipeExpanded && recipeCellSprite != null ? recipeCellColor
            : complete ? completedColor : targeting ? targetingColor : pendingColor;
        completedMark.SetActive(complete);
        if (completedTick != null) completedTick.SetActive(complete);
        if (portionCount != null) portionCount.text = received + "/" + total;
        for (int i = 0; i < dish.Ingredients.Count; i++) ingredients[i].SetProgress(dish.Ingredients[i]);
    }

    // Chuyển giữa hình món thu gọn và chi tiết nguyên liệu, đồng bộ kích thước cùng bố cục.
    public void SetRecipeExpanded(bool expanded)
    {
        if (expanded && orderComplete) return;
        recipeExpanded = expanded;
        RefreshNotification();
        ApplyRecipeCell(expanded);
        dishName.gameObject.SetActive(!expanded && originalDishNameActive);
        finished.gameObject.SetActive(!expanded);
        finished.enabled = !expanded && finished.sprite != null;
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
                // Nguyên liệu nằm trong panel nền đã thu vào.
                ingredientDetailsRect.offsetMin = new Vector2(6 + recipeCellInset, 4 + recipeCellInset);
                ingredientDetailsRect.offsetMax = new Vector2(-6 - recipeCellInset, -4 - recipeCellInset);
            }
            // Ô chi tiết cao đúng bằng ô của bảng (không làm bảng dài ra); nguyên liệu chia đều chiều cao ô
            // và to hết mức ô cho phép, tối đa maxRecipeIngredientSize.
            int count = Mathf.Max(1, recipeIngredientCount);
            float available = Mathf.Max(1f, collapsedCardHeight - 8f - recipeCellInset * 2f);
            float slot = available / count;
            float itemScale = ingredients.Length > 0 ? Mathf.Abs(ingredients[0].transform.localScale.y) : 1f;
            if (itemScale < 0.01f) itemScale = 1f;
            float visual = Mathf.Max(8f, Mathf.Min(slot - 2f, maxRecipeIngredientSize));
            // Item có scale riêng trong prefab nên chiều cao layout phải chia lại cho scale đó.
            float layoutHeight = visual / itemScale;
            for (int i = 0; i < recipeIngredientCount; i++) ingredients[i].SetRecipeDisplay(true, layoutHeight);
            if (ingredientsColumnLayout != null)
            {
                // Khoảng cách + lề trên dưới lấp hết phần dư để tâm các nguyên liệu cách đều nhau trong ô.
                int edge = Mathf.RoundToInt(Mathf.Max(0f, slot - layoutHeight) * 0.5f);
                ingredientsColumnLayout.padding = new RectOffset(0, 0, edge, edge);
                ingredientsColumnLayout.spacing = Mathf.Max(0f, slot - layoutHeight);
            }
            if (ingredientDetailsLayout != null)
                ingredientDetailsLayout.preferredHeight = available;
            if (cardLayout != null)
                cardLayout.preferredHeight = collapsedCardHeight;
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

    // Khi xem chi tiết: nền thẻ đổi sang ô fr_order_inside; thu gọn thì trả nền gốc.
    private void ApplyRecipeCell(bool expanded)
    {
        if (background == null || recipeCellSprite == null) return;
        if (!hasDefaultBackground)
        {
            defaultBackgroundSprite = background.sprite;
            defaultBackgroundType = background.type;
            hasDefaultBackground = true;
        }
        background.sprite = expanded ? recipeCellSprite : defaultBackgroundSprite;
        background.type = expanded ? Image.Type.Sliced : defaultBackgroundType;
        // Panel nền nhỏ hơn ô một chút khi xem chi tiết; thu gọn thì trả về phủ kín thẻ như prefab.
        float inset = expanded ? recipeCellInset : 0f;
        background.rectTransform.offsetMin = new Vector2(inset, inset);
        background.rectTransform.offsetMax = new Vector2(-inset, -inset);
        background.color = expanded ? recipeCellColor : orderComplete ? completedColor : pendingColor;
    }

    // Kiểm tra một vị trí thế giới có nằm trong vùng chữ nhật của thẻ món.
    public bool Contains(Vector3 worldPosition) => Rect.rect.Contains(Rect.InverseTransformPoint(worldPosition));
}
