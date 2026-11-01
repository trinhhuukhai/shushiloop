using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasNewItem : UICanvas
{
    public TMP_Text des;
    public TMP_Text txtTile;

    public GameObject imgItemHoiCham;
    public GameObject imgItemHoiBang;
    public GameObject imgItemLink;
    public GameObject imgItemKhay;

    public RectTransform mainPanelRect;
    private Tween popTween;
    private FoodState currentItem;

    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

    }
    private void ShowItem(FoodState item)
    {
        currentItem = item;
        if (imgItemHoiCham != null) imgItemHoiCham.SetActive(item == FoodState.HoiCham);
        if (imgItemHoiBang != null) imgItemHoiBang.SetActive(item == FoodState.Ice);
        if (imgItemLink != null) imgItemLink.SetActive(item == FoodState.Lock);
        if (imgItemKhay != null) imgItemKhay.SetActive(item == FoodState.RigidTray);
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
        }
    }


    public void Setupp()
    {

    }
   

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
