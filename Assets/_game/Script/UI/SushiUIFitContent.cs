using UnityEngine;

// Keep the authored page composition readable on both tall phones and tablets.
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public sealed class SushiUIFitContent : MonoBehaviour
{
    public Vector2 referenceSize = new Vector2(1080, 1450);
    public bool fillAvailableHeight;
    // Cập nhật tỉ lệ nội dung sau các thay đổi bố cục trong frame.
    private void LateUpdate() => Refresh();
    // Thu phóng nội dung theo khung chuẩn để vừa chiều rộng lẫn chiều cao của đối tượng cha.
    public void Refresh()
    {
        var rect = (RectTransform)transform;
        var parent = rect.parent as RectTransform;
        if (parent == null) return;
        float scale = Mathf.Min(parent.rect.width / Mathf.Max(1, referenceSize.x), parent.rect.height / Mathf.Max(1, referenceSize.y));
        rect.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        if (fillAvailableHeight)
        {
            float height = Mathf.Max(referenceSize.y, parent.rect.height / Mathf.Max(0.1f, scale));
            if (!Mathf.Approximately(rect.rect.height, height))
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
