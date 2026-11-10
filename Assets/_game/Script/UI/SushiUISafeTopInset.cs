using UnityEngine;

// Né phần tai thỏ ở mép trên cho từng phần tử, dùng khi SushiUISafeArea tắt applyTop
// (ảnh nền/header vẫn chạm đỉnh màn hình, còn nút và nội dung tụt xuống dưới tai thỏ).
[RequireComponent(typeof(RectTransform))]
public sealed class SushiUISafeTopInset : MonoBehaviour
{
    public enum Mode
    {
        [Tooltip("Dời cả phần tử xuống (nút, tiêu đề...).")] Move,
        [Tooltip("Hạ mép trên của phần tử kéo giãn (vùng nội dung, danh sách...).")] ShrinkTop,
        [Tooltip("Kéo dài phần tử neo mép trên xuống thêm (ảnh header cần che kín tới nội dung).")] Grow,
    }

    public Mode mode = Mode.Move;
    private RectTransform rect;
    private Vector2 basePosition, baseOffsetMax, baseSize;
    private bool captured;
    private float lastInset = -1f;

    private void OnEnable()
    {
        Capture();
        lastInset = -1f;
        Apply();
    }

    // Tính lại mỗi frame (rẻ): canvas có thể chưa co giãn xong ở frame bật đầu tiên.
    private void Update() => Apply();

    private void Capture()
    {
        if (captured) return;
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;
        baseOffsetMax = rect.offsetMax;
        baseSize = rect.sizeDelta;
        captured = true;
    }

    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        float inset = TopInset();
        if (Mathf.Approximately(inset, lastInset)) return;
        lastInset = inset;
        switch (mode)
        {
            case Mode.Move:
                rect.anchoredPosition = basePosition - new Vector2(0f, inset);
                break;
            case Mode.ShrinkTop:
                rect.offsetMax = baseOffsetMax - new Vector2(0f, inset);
                break;
            case Mode.Grow:
                rect.sizeDelta = baseSize + new Vector2(0f, inset);
                break;
        }
    }

    // Chiều cao tai thỏ (đơn vị canvas) = phần màn hình nằm trên safe area.
    private float TopInset()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return 0f;
        var root = canvas.rootCanvas.transform as RectTransform;
        if (root == null || root.rect.height <= 0f) return 0f;
        float pixels = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
        return pixels * root.rect.height / Screen.height;
    }
}
