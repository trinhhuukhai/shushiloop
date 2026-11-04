using UnityEngine;

// Keep the authored page composition readable on both tall phones and tablets.
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public sealed class SushiUIFitContent : MonoBehaviour
{
    // Cập nhật tỉ lệ nội dung sau các thay đổi bố cục trong frame.
    private void LateUpdate() => Refresh();
    // Thu phóng nội dung theo khung chuẩn để vừa chiều rộng lẫn chiều cao của đối tượng cha.
    public void Refresh()
    {
        var rect = (RectTransform)transform;
        var parent = rect.parent as RectTransform;
        if (parent == null) return;
        float scale = Mathf.Min(parent.rect.width / 1080f, parent.rect.height / 1450f);
        rect.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }
}
