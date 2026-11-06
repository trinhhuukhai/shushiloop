using DG.Tweening;
using UnityEngine;

// Keep the same Home building visible above the upgrade drawer at every screen size.
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public sealed class SushiHomeRestaurantLayout : MonoBehaviour
{
    public RectTransform preview;
    public GameObject[] homeOnly;
    private bool[] previousVisibility;
    private bool expanded;
    private Vector2 lastSize;
    private Sequence transition;
    public bool IsExpanded => expanded;

    public void SetExpanded(bool value, bool animate = true)
    {
        if (expanded == value) return;
        expanded = value;
        if (value)
        {
            previousVisibility = new bool[homeOnly.Length];
            for (int i = 0; i < homeOnly.Length; i++)
                if (homeOnly[i] != null)
                {
                    previousVisibility[i] = homeOnly[i].activeSelf;
                    homeOnly[i].SetActive(false);
                }
        }
        else if (previousVisibility != null)
            for (int i = 0; i < homeOnly.Length; i++)
                if (homeOnly[i] != null) homeOnly[i].SetActive(previousVisibility[i]);
        Refresh(animate && Application.isPlaying);
    }

    private void OnEnable() => Refresh(false);
    private void LateUpdate()
    {
        if (((RectTransform)transform).rect.size != lastSize) Refresh(false);
    }

    public void Refresh(bool animate = false)
    {
        if (preview == null) return;
        lastSize = ((RectTransform)transform).rect.size;
        float bottom = expanded ? lastSize.y * 0.48f + 15 : 360;
        float top = lastSize.y - (expanded ? 50 : 220);
        float left = expanded ? 40 : 130;
        float right = lastSize.x - (expanded ? 40 : 130);
        float scale = Mathf.Max(0.1f, Mathf.Min((right - left) / 1000, (top - bottom) / 760));
        var position = new Vector2((left + right - lastSize.x) * 0.5f, (top + bottom - lastSize.y) * 0.5f);
        transition?.Kill();
        if (animate)
            transition = DOTween.Sequence().SetUpdate(true)
                .Join(preview.DOAnchorPos(position, 0.3f).SetEase(Ease.InOutCubic))
                .Join(preview.DOScale(scale, 0.3f).SetEase(Ease.InOutCubic));
        else { preview.anchoredPosition = position; preview.localScale = Vector3.one * scale; }
    }

    private void OnDisable() { transition?.Kill(); transition = null; }
}
