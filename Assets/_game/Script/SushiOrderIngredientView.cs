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

    public Image Image => image;
    public Transform DeliverySlot => deliverySlot;

    // Read the authored UI bounds to match the flying ingredient to its destination.
    public float ReceivingFoodScale
    {
        get
        {
            Vector2 rectSize = image.rectTransform.rect.size;
            Vector3 size = deliverySlot.InverseTransformVector(image.rectTransform.TransformVector(rectSize));
            return Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)) / 1.05f;
        }
    }

    public void SetSprite(Sprite sprite) { image.sprite = sprite; image.enabled = sprite != null; }

    public void SetProgress(OrderItem item)
    {
        image.color = item.IsComplete ? completedImageColor : item.incomingAmount > 0 ? incomingImageColor : pendingImageColor;
        progress.text = item.receivedAmount + "/" + item.requiredAmount +
            (item.incomingAmount > 0 ? " +" + item.incomingAmount : "");
        progress.color = item.IsComplete ? completedTextColor : item.incomingAmount > 0 ? incomingTextColor : pendingTextColor;
    }
}
