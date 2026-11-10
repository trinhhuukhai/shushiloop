using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SushiOrderIngredientView : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text progress;
    [SerializeField] private Transform deliverySlot;
    [SerializeField] private Color pendingImageColor;
    [SerializeField] private Color incomingImageColor;
    [SerializeField] private Color completedImageColor;
    [SerializeField] private Color pendingTextColor;
    [SerializeField] private Color incomingTextColor;
    [SerializeField] private Color completedTextColor;
    private LayoutElement imageLayout;
    private LayoutElement itemLayout;
    private float defaultImageHeight;
    private float defaultItemHeight;
    private bool hasCachedImageHeight;
    private bool recipeDisplay;
    private bool isComplete;
    private bool isIncoming;

    public Image Image => image;
    public Transform DeliverySlot => deliverySlot;

    // Read the authored UI bounds to match the flying ingredient to its destination.
    public float ReceivingFoodScale
    {
        get
        {
            Vector2 rectSize = image.rectTransform.rect.size;
            Vector3 size = deliverySlot.InverseTransformVector(image.rectTransform.TransformVector(rectSize));
            return Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)) / 1.05f * 1.2f;
        }
    }

    // Gán sprite nguyên liệu và chỉ bật ảnh khi có sprite hợp lệ.
    public void SetSprite(Sprite sprite) { image.sprite = sprite; image.enabled = sprite != null; }

    // Hiển thị lượng đã nhận, lượng đang tới và màu trạng thái của nguyên liệu.
    public void SetProgress(OrderItem item)
    {
        isComplete = item.IsComplete;
        isIncoming = item.incomingAmount > 0;
        RefreshImageColor();
        progress.text = item.receivedAmount + "/" + item.requiredAmount +
            (item.incomingAmount > 0 ? " +" + item.incomingAmount : "");
        progress.color = item.IsComplete ? completedTextColor : item.incomingAmount > 0 ? incomingTextColor : pendingTextColor;
    }

    // Chuyển sang chế độ xem công thức, ẩn tiến độ và điều chỉnh chiều cao biểu tượng.
    // height > 0: thẻ món ấn định chiều cao layout của nguyên liệu để vừa khít ô chi tiết.
    public void SetRecipeDisplay(bool active, float height = 0)
    {
        recipeDisplay = active;
        progress.gameObject.SetActive(!active);
        if (imageLayout == null) imageLayout = image.GetComponent<LayoutElement>();
        if (itemLayout == null) itemLayout = GetComponent<LayoutElement>();
        if (!hasCachedImageHeight)
        {
            defaultImageHeight = imageLayout != null ? imageLayout.preferredHeight : 0;
            defaultItemHeight = itemLayout != null ? itemLayout.preferredHeight : 0;
            hasCachedImageHeight = true;
        }
        bool fitted = active && height > 0;
        if (itemLayout != null) itemLayout.preferredHeight = fitted ? height : defaultItemHeight;
        if (imageLayout != null)
        {
            float availableHeight = defaultItemHeight > 0 ? defaultItemHeight - 4 : defaultImageHeight + 10;
            imageLayout.preferredHeight = fitted ? height
                : active ? Mathf.Min(defaultImageHeight + 10, availableHeight)
                : defaultImageHeight;
        }
        RefreshImageColor();
    }

    // Chọn màu theo tiến độ nguyên liệu, giữ ảnh rõ khi đang xem chi tiết công thức.
    private void RefreshImageColor()
    {
        Color color = isComplete ? completedImageColor : isIncoming ? incomingImageColor : pendingImageColor;
        if (recipeDisplay) color.a = 1;
        image.color = color;
    }
}
