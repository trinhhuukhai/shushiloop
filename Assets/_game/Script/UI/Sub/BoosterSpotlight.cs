using UnityEngine;
using UnityEngine.UI;

public class BoosterSpotlight : MonoBehaviour
{
    [Header("Overlay đen phủ toàn màn hình")]
    public GameObject dimOverlay;

    [Header("Sorting order")]
    public int overlayOrder = 500;
    public int targetOrder = 501;

    private Canvas overlayCanvas;
    private Canvas targetCanvas;
    private RectTransform currentTarget;

    // Chuẩn bị canvas, lớp vẽ và raycaster cho lớp phủ tối rồi ẩn lớp phủ.
    void Awake()
    {
        // Đảm bảo overlay có Canvas + GraphicRaycaster riêng để set sorting order
        overlayCanvas = dimOverlay.GetComponent<Canvas>();
        if (overlayCanvas == null) overlayCanvas = dimOverlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = overlayOrder;

        if (dimOverlay.GetComponent<GraphicRaycaster>() == null)
            dimOverlay.AddComponent<GraphicRaycaster>();

        dimOverlay.SetActive(false);
    }

    // Gọi hàm này, truyền vào RectTransform của nút booster cần sáng
    // Bật lớp phủ và nâng canvas nút booster lên lớp vẽ mục tiêu để vẫn nhận thao tác.
    public void ShowSpotlight(RectTransform boosterTarget)
    {
        currentTarget = boosterTarget;

        dimOverlay.SetActive(true);
        targetCanvas = boosterTarget.GetComponent<Canvas>();
        if (targetCanvas == null) targetCanvas = boosterTarget.gameObject.AddComponent<Canvas>();
        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = targetOrder; // cao hơn overlay -> nổi lên trên, nhận click bình thường

        if (boosterTarget.GetComponent<GraphicRaycaster>() == null)
            boosterTarget.gameObject.AddComponent<GraphicRaycaster>();
    }

    // Ẩn lớp phủ và bỏ ghi đè thứ tự vẽ của nút booster.
    public void HideSpotlight()
    {
        dimOverlay.SetActive(false);
        if (targetCanvas != null)
            targetCanvas.overrideSorting = false;
    }
}