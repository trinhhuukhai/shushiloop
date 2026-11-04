using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasWarningLiveSetting : UICanvas
{
    public TMP_Text txtLive;
    public GameObject ObjLive;

    public GameObject objHome;
    public GameObject objRetry;

    public RectTransform mainPanelRect;
    private Tween popTween;

    // Mở cảnh báo mất mạng với hiệu ứng xuất hiện.
    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

    }
    // Cập nhật số mạng và chọn nhóm xác nhận về nhà hoặc chơi lại.
    public void Setupp(bool onHome)
    {
        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (!isUnlimited)
        {
            txtLive.text = LivesDataFragment.cur.gameData.lives.ToString();
        }

        objHome.SetActive(onHome);
        objRetry.SetActive(!onHome);

    }

    // Kiểm tra quyền về nhà, khôi phục thời gian, trừ một mạng và quay về trang chủ.
    public void Home()
    {
        if (GameManager.ins == null || !GameManager.ins.CanReturnHome || GameManager.ins.IsLoadingLevel) return;
        Time.timeScale = 1f;
        LivesDataFragment.cur.ConsumeLife();
        GameManager.ins.ReturnToMainMenu();
    }

    // Đóng cảnh báo để tiếp tục ở giao diện hiện tại.
    public void Stay()
    {
        CloseDirectly();
    }

    // Yêu cầu chơi lại, trừ một mạng và đóng cài đặt sau khi khôi phục thời gian.
    public void Retry()
    {
        Time.timeScale = 1f;
        if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
        LivesDataFragment.cur.ConsumeLife();
        UIManager.Ins.CloseUI<CanvasSetting>();
    }

    // Đóng trực tiếp cảnh báo mất mạng từ cài đặt.
    public void Close() => CloseDirectly();
    // Đóng cảnh báo khi nhận phím quay lại.
    public override void BackKey() => CloseDirectly();
}
