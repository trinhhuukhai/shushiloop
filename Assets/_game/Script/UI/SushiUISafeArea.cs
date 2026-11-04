using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class SushiUISafeArea : MonoBehaviour
{
    private Rect lastArea;
    private Vector2Int lastSize;
    // Áp dụng vùng an toàn màn hình ngay khi UI được bật.
    private void OnEnable() => Apply();
    // Tính lại vùng an toàn khi độ phân giải hoặc safe area thay đổi.
    private void Update()
    {
        if (Screen.safeArea != lastArea || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply();
    }
    // Đổi safe area sang anchor của RectTransform và xóa các khoảng lệch mép.
    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        lastArea = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(lastArea.xMin / Screen.width, lastArea.yMin / Screen.height);
        rect.anchorMax = new Vector2(lastArea.xMax / Screen.width, lastArea.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
