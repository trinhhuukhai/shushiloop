using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasWarningLive : UICanvas
{
    public TMP_Text txtLive;
    public GameObject ObjLive;
    public RectTransform mainPanelRect;
    private Tween popTween;

    // Mở cảnh báo mạng, chạy hiệu ứng xuất hiện và cập nhật số mạng hiện có.
    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
        Setupp();
    }
    // Hiển thị số mạng còn lại khi người chơi không ở chế độ vô hạn mạng.
    public void Setupp()
    {
        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (!isUnlimited)
        {
            txtLive.text = LivesDataFragment.cur.gameData.lives.ToString();
        }
    }

    // Mở giao diện thất bại, đóng hồi sinh và đóng cảnh báo mạng.
    public void Quit()
    {
        UIManager.Ins.OpenUI<CanvasFail>();
        UIManager.Ins.CloseUI<CanvasRevive>();
        CloseDirectly();
    }

    // Đóng cảnh báo để giữ nguyên lựa chọn trước đó.
    public void Stay()
    {
        CloseDirectly();
    }

    // Đóng trực tiếp cảnh báo mạng.
    public void Close() => CloseDirectly();
    // Đóng cảnh báo khi nhận phím quay lại.
    public override void BackKey() => CloseDirectly();
}
