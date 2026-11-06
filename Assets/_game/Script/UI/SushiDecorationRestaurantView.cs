using DG.Tweening;
using UnityEngine;

// Home owns the only restaurant. The upgrade drawer reveals these same authored layers.
public sealed class SushiDecorationRestaurantView : MonoBehaviour
{
    public RectTransform[] itemRoots;
    private Tween reveal;
    private int shown = -1;

    public void Show(int built)
    {
        if (itemRoots == null) return;
        built = Mathf.Clamp(built, 0, itemRoots.Length);
        if (shown == built) return;
        ResetReveal();
        shown = built;
        for (int i = 0; i < itemRoots.Length; i++)
            if (itemRoots[i] != null) itemRoots[i].gameObject.SetActive(i < built);
    }

    public void Reveal(int index)
    {
        Show(index + 1);
        if (index < 0 || index >= itemRoots.Length || itemRoots[index] == null) return;
        RectTransform item = itemRoots[index];
        item.localScale = Vector3.one * 0.15f;
        reveal = item.DOScale(1, 0.65f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void ResetReveal()
    {
        reveal?.Kill(); reveal = null;
        if (itemRoots != null)
            foreach (var item in itemRoots) if (item != null) item.localScale = Vector3.one;
    }

    private void OnDisable() => ResetReveal();
}
