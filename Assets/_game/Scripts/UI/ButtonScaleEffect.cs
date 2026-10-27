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

    private void ReturnToOriginal()
    {
        transform.DOKill();
        transform.DOScale(originalScale, duration)
                 .SetEase(Ease.InQuad);
    }
}
