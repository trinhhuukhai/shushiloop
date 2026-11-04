using DG.Tweening;
using TMPro;
using UnityEngine;

public class CanvasNewItem : UICanvas
{
    public TMP_Text des;
    public TMP_Text txtTile;

    public GameObject imgItemHoiCham;
    public GameObject imgItemHoiBang;
    public GameObject imgItemLink;
    public GameObject imgItemKhay;
    public GameObject imgItemTime;

    public RectTransform mainPanelRect;
    private Tween popTween;
    private FoodState currentItem;
    private System.Action onClosed;

    //public override void Open()
    //{
    //    base.Open();
    //    PlayPop();
    //}

    private void PlayPop()
    {
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
    }

    // Gọi ngay sau OpenUI<CanvasNewItem>() để chọn item cần hiển thị.
    public void Setupp(FoodState item, System.Action onClosed = null)
    {
        PlayPop();
        this.onClosed = onClosed;
        // Directly opening the rush popup also starts its lesson after dismissal.
        if (item == FoodState.Time && onClosed == null)
        {
            LevelManager owner = LevelManager.Ins;
            int version = owner != null ? owner.LoadVersion : -1;
            this.onClosed = () =>
            {
                if (owner != null && owner.IsPlaying && owner.LoadVersion == version)
                    ControlTutorial.Ins?.BeginItem(owner, FoodState.Time);
            };
            GameDataFragment.cur?.MarkItemPopupShown(FoodState.Time);
        }
        ShowItem(item);
    }

    private void ShowItem(FoodState item)
    {
        currentItem = item;
        if (imgItemHoiCham != null) imgItemHoiCham.SetActive(item == FoodState.HoiCham);
        if (imgItemHoiBang != null) imgItemHoiBang.SetActive(item == FoodState.Ice);
        if (imgItemLink != null) imgItemLink.SetActive(item == FoodState.Lock);
        if (imgItemKhay != null) imgItemKhay.SetActive(item == FoodState.RigidTray);
        if (imgItemTime != null) imgItemTime.SetActive(item == FoodState.Time);

        switch (item)
        {
            case FoodState.HoiCham:
                txtTile.text = "Hidden Tiles";
                des.text = "Hidden ingredients are revealed when they reach the front of the column.";
                break;
            case FoodState.Ice:
                txtTile.text = "Frozen Tiles";
                des.text = "Collect nearby ingredients to break the ice before picking up frozen ingredients.";
                break;
            case FoodState.Lock:
                txtTile.text = "Linked Tiles";
                des.text = "Bring both linked tiles to the front, then tap either one to collect them together.";
                break;
            case FoodState.RigidTray:
                txtTile.text = "Trays";
                des.text = "Ingredients in the same tray move together. Make room on the conveyor to collect them all!";
                break;
            case FoodState.Time:
                txtTile.text = "Customer Rush";
                des.text = "Khách vội chỉ chờ trong thời gian giới hạn. Hãy ưu tiên hoàn thành toàn bộ order của khách trước khi thanh thời gian cạn!";
                break;
        }
    }

    protected override void OnDisable()
    {
        popTween?.Kill();
        onClosed = null;
        base.OnDisable();
    }

    private void OnDestroy() => popTween?.Kill();

    private void CloseAndContinue()
    {
        var callback = onClosed;
        onClosed = null;
        CloseDirectly();
        callback?.Invoke();
    }

    public void Close() => CloseAndContinue();
    public override void BackKey() => CloseAndContinue();
}
