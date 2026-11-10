using UnityEngine.EventSystems;
using UnityEngine.UI;

// ScrollRect dọc nằm trong trang có vuốt chuyển tab: kéo dọc thì cuộn danh sách,
// kéo ngang thì chuyển cho SushiTabSwipeArea ở cha để đổi tab.
public sealed class SushiNestedScrollRect : ScrollRect
{
    private SushiTabSwipeArea swipe;
    private bool routing;

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (swipe == null && transform.parent != null) swipe = transform.parent.GetComponentInParent<SushiTabSwipeArea>();
        base.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        routing = swipe != null && UnityEngine.Mathf.Abs(eventData.delta.x) > UnityEngine.Mathf.Abs(eventData.delta.y);
        if (routing) swipe.OnBeginDrag(eventData);
        else base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (routing) swipe.OnDrag(eventData);
        else base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (routing) { routing = false; swipe.OnEndDrag(eventData); }
        else base.OnEndDrag(eventData);
    }
}
