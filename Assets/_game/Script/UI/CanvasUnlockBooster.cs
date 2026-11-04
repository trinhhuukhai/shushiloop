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

    // Mở giới thiệu booster và chạy hiệu ứng phóng panel lên kích thước chuẩn.
    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
    }

    // Dừng tween và xóa callback tiếp tục khi popup bị tắt.
    protected override void OnDisable()
    {
        popTween?.Kill();
        popTween = null;
        onClosed = null;
        base.OnDisable();
    }

    // Gán callback đóng và chọn hình ảnh, tiêu đề cùng mô tả cho booster vừa mở khóa.
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

    // Xác nhận giới thiệu, đóng popup rồi tiếp tục hướng dẫn.
    public void btnOk() => CloseAndContinue();
    // Đóng popup và gọi callback tiếp tục khi nhận phím quay lại.
    public override void BackKey() => CloseAndContinue();

    // Tách callback khỏi popup trước khi đóng và gọi tiếp để tránh thực hiện lặp.
    private void CloseAndContinue()
    {
        var callback = onClosed;
        onClosed = null;
        CloseDirectly();
        callback?.Invoke();
    }

    // Đóng popup qua luồng có gọi callback tiếp tục.
    public void Close() => CloseAndContinue();
}
