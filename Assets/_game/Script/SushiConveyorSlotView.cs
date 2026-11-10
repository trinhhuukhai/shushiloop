using DG.Tweening;
using UnityEngine;

// Hình một ô trên băng chuyền: ô trống là hố tối (conveyor_slot), đĩa trắng hiện ra ngay khi ô được giữ cho nguyên liệu.
public sealed class SushiConveyorSlotView : MonoBehaviour
{
    [Tooltip("Hố tối hiện khi ô chưa có nguyên liệu.")]
    [SerializeField] private SpriteRenderer emptyArt;
    [Tooltip("Đĩa trắng, chỉ hiện khi nguyên liệu đã đáp lên ô.")]
    [SerializeField] private SpriteRenderer plate;
    [SerializeField, Min(0.01f)] private float popDuration = 0.18f;
    [SerializeField, Range(0.3f, 1f)] private float popStartScale = 0.6f;

    private bool filled;
    private bool initialized;
    private Vector3 plateScale;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (plate != null) plateScale = plate.transform.localScale;
        Apply(false, false);
    }

    // Đổi trạng thái ô; animate = đĩa bật ra nhẹ khi vừa có nguyên liệu.
    public void SetFilled(bool value, bool animate = true)
    {
        Initialize();
        if (filled == value) return;
        Apply(value, animate);
    }

    private void Apply(bool value, bool animate)
    {
        filled = value;
        if (emptyArt != null) emptyArt.enabled = !value;
        if (plate == null) return;
        Transform plateTransform = plate.transform;
        plateTransform.DOKill();
        plate.enabled = value;
        plateTransform.localScale = plateScale;
        if (value && animate)
        {
            plateTransform.localScale = plateScale * popStartScale;
            plateTransform.DOScale(plateScale, popDuration).SetEase(Ease.OutBack).SetLink(gameObject);
        }
    }

    private void OnDisable()
    {
        if (plate != null) plate.transform.DOKill();
    }
}
