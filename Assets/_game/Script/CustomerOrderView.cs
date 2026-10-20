using TMPro;
using UnityEngine;

public class CustomerOrderView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private GameObject completedIcon;

    public void Refresh(OrderItem order, Sprite sprite)
    {
        int remaining = Mathf.Max(
            0,
            order.requiredAmount - order.receivedAmount
        );

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (completedIcon != null)
            completedIcon.SetActive(remaining == 0);
    }
}