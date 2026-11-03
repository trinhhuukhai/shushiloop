using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class CanvasUnlockBooster : UICanvas
{
    public TMP_Text des;
    public TMP_Text txtTile;

    public GameObject togs;   // icon Click
    public GameObject swap;   // icon Swap
    public GameObject magnet; // icon Magnet

    public RectTransform mainPanelRect;
    private Tween popTween;

    private Action onClosed;

    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
    }

    protected override void OnDisable()
    {
        popTween?.Kill();
        popTween = null;
        onClosed = null;
        base.OnDisable();
    }

    public void Setupp(BoosterType type, Action onClosed)
    {
        this.onClosed = onClosed;

        if (togs != null) togs.SetActive(type == BoosterType.Click);
        if (swap != null) swap.SetActive(type == BoosterType.Shuffle);
        if (magnet != null) magnet.SetActive(type == BoosterType.Magnet);
        if (des != null)
        {
            switch (type)
            {
                case BoosterType.Click:
                    txtTile.text = "Booster Tongs";
                    des.text = "Pick 1 visible item and send it to the conveyor.";
                    break;
                case BoosterType.Shuffle:
                    txtTile.text = "Booster Swap Row";

                    des.text = "Move the top row to the bottom and orther rows shift up.";
                    break;
                case BoosterType.Magnet:
                    txtTile.text = "Booster Magnet";

                    des.text = "Collect all ingredients to instantly complete 1 dish.";
                    break;
            }
        }
    }

    public void btnOk() => CloseAndContinue();
    public override void BackKey() => CloseAndContinue();

    private void CloseAndContinue()
    {
        var callback = onClosed;
        onClosed = null;
        CloseDirectly();
        callback?.Invoke();
    }

    public void Close() => CloseAndContinue();
}
