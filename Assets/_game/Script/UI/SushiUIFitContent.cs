using UnityEngine;

// Keep the authored page composition readable on both tall phones and tablets.
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public sealed class SushiUIFitContent : MonoBehaviour
{
    private void LateUpdate() => Refresh();
    public void Refresh()
    {
        var rect = (RectTransform)transform;
        var parent = rect.parent as RectTransform;
        if (parent == null) return;
        float scale = Mathf.Min(parent.rect.width / 1080f, parent.rect.height / 1450f);
        rect.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }
}
