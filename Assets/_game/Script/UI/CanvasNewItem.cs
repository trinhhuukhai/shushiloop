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

    public override void Open()
    {
        base.Open();

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        Setupp();
    }

    public void Setupp()
    {

    }
   

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
