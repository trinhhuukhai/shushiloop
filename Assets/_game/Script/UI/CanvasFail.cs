using DG.Tweening;
using TMPro;
using UnityEngine;

public class CanvasFail : UICanvas
{
    public TMP_Text txtGold;
    public RectTransform mainPanelRect;
    private Tween popTween;
    public override void Open()
    {
        base.Open();
        GameManager.ChangeState(GameState.Fail);

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        Refresh();
    }
    private void Refresh()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString();
    }
    public void BtnHome()
    {
        GameManager.ins.ReturnToMainMenu();
    }
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

    public void Close() => CloseDirectly();
}
