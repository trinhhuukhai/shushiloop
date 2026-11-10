using UnityEngine;
using UnityEngine.EventSystems;

// Vùng nhận thao tác vuốt ngang để chuyển tab Shop / Home / Ranking (gắn lên nội dung từng trang).
// Vì là drag handler của cả nội dung trang nên vuốt bắt đầu trên nút cũng không bị tính là bấm nút.
public sealed class SushiTabSwipeArea : MonoBehaviour,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private bool active;
    private float velocity;
    private Canvas rootCanvas;

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

    // Chỉ nhận vuốt chủ yếu theo chiều ngang.
    public void OnBeginDrag(PointerEventData eventData)
    {
        velocity = 0f;
        active = false;
        if (Mathf.Abs(eventData.delta.x) < Mathf.Abs(eventData.delta.y)) return;
        CanvasMainMenu menu = Menu();
        active = menu != null && menu.BeginSwipe();
        if (active) OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!active) return;
        CanvasMainMenu menu = Menu();
        if (menu == null) { active = false; return; }
        float delta = eventData.delta.x / ScaleFactor();
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        // Làm mượt vận tốc để nhận ra cú vuốt nhanh khi thả tay.
        velocity = Mathf.Lerp(velocity, delta / dt, 0.5f);
        menu.DragSwipe(delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!active) return;
        active = false;
        Menu()?.EndSwipe(velocity);
    }

    // Trang bị tắt giữa chừng (vd. bị đóng): chốt thao tác để menu không kẹt ở trạng thái đang kéo.
    private void OnDisable()
    {
        if (!active) return;
        active = false;
        Menu()?.EndSwipe(0f);
    }

    private float ScaleFactor()
    {
        if (rootCanvas == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;
        }
        return rootCanvas != null && rootCanvas.scaleFactor > 0f ? rootCanvas.scaleFactor : 1f;
    }

    private static CanvasMainMenu Menu()
    {
        if (UIManager.Ins == null || !UIManager.Ins.IsOpened<CanvasMainMenu>()) return null;
        return UIManager.Ins.GetUI<CanvasMainMenu>();
    }
}
