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
    private float defaultImageHeight;
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

    public void SetSprite(Sprite sprite) { image.sprite = sprite; image.enabled = sprite != null; }

    public void SetProgress(OrderItem item)
    {
        isComplete = item.IsComplete;
        isIncoming = item.incomingAmount > 0;
        RefreshImageColor();
        progress.text = item.receivedAmount + "/" + item.requiredAmount +
            (item.incomingAmount > 0 ? " +" + item.incomingAmount : "");
        progress.color = item.IsComplete ? completedTextColor : item.incomingAmount > 0 ? incomingTextColor : pendingTextColor;
    }

    public void SetRecipeDisplay(bool active)
    {
        recipeDisplay = active;
        progress.gameObject.SetActive(!active);
        if (imageLayout == null) imageLayout = image.GetComponent<LayoutElement>();
        if (imageLayout != null)
        {
            if (!hasCachedImageHeight)
            {
                defaultImageHeight = imageLayout.preferredHeight;
                hasCachedImageHeight = true;
            }
            LayoutElement itemLayout = GetComponent<LayoutElement>();
            float availableHeight = itemLayout != null && itemLayout.preferredHeight > 0
                ? itemLayout.preferredHeight - 4 : defaultImageHeight + 10;
            imageLayout.preferredHeight = active
                ? Mathf.Min(defaultImageHeight + 10, availableHeight)
                : defaultImageHeight;
        }
        RefreshImageColor();
    }

    private void RefreshImageColor()
    {
        Color color = isComplete ? completedImageColor : isIncoming ? incomingImageColor : pendingImageColor;
        if (recipeDisplay) color.a = 1;
        image.color = color;
    }
}
