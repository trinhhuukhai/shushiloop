using DG.Tweening;
using TMPro;
using UnityEngine;

public class CanvasFail : UICanvas
{
    public TMP_Text txtGold;
    public RectTransform mainPanelRect;
    private Tween popTween;
    // Mở popup thua, cập nhật trạng thái game, chạy hiệu ứng xuất hiện và hiển thị tiền.
    public override void Open()
    {
        base.Open();
        GameManager.ChangeState(GameState.Fail);

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        Refresh();
    }
    // Cập nhật số tiền người chơi trên popup thua.
    private void Refresh()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString();
    }
    // Yêu cầu GameManager quay về trang chủ.
    public void BtnHome()
    {
        GameManager.ins.ReturnToMainMenu();
    }
    // Chơi lại nếu còn mạng hoặc đang vô hạn mạng, chỉ trừ mạng sau khi yêu cầu chơi lại được chấp nhận.
    public void BtnRetry()
    {

        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        if (isUnlimited)
        {
            if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
        }
        else
        {
            if (LivesDataFragment.cur.gameData.lives <= 0)
            {
                UIManager.Ins.OpenUI<CanvasLive>();
            }
            else
            {
                if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel())
                {
                    CloseDirectly();
                    LivesDataFragment.cur.ConsumeLife();
                }
            }
        }

    }

    // Đóng trực tiếp popup thất bại.
    public void Close() => CloseDirectly();
}
