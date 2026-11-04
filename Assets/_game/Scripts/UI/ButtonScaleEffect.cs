using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ButtonScaleEffect : MonoBehaviour,
                                  IPointerDownHandler,
                                  IPointerUpHandler,
                                  IPointerExitHandler
{
    [SerializeField] private float scaleUp = 1.1f;
    [SerializeField] private float duration = 0.07f;
    private Vector3 originalScale;

    // Lưu kích thước ban đầu của nút để phục hồi sau tương tác.
    void Start()
    {
        originalScale = transform.localScale;
    }

    // Chạm xuống → scale lên
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale * scaleUp, duration)
                 .SetEase(Ease.OutQuad);
    }

    // Thả tay trên nút → scale về gốc
    public void OnPointerUp(PointerEventData eventData)
    {
        ReturnToOriginal();
    }

    // Nếu rê chuột/finger ra ngoài trước khi thả → cũng về gốc
    public void OnPointerExit(PointerEventData eventData)
    {
        ReturnToOriginal();
    }

    // Dừng tween hiện tại và thu nút về tỉ lệ gốc bằng hiệu ứng chuyển động.
    private void ReturnToOriginal()
    {
        transform.DOKill();
        transform.DOScale(originalScale, duration)
                 .SetEase(Ease.InQuad);
    }
}
