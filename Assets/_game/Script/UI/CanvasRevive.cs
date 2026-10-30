using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasRevive : UICanvas
{

    //---revive
    public GameObject loseOutSpace;
    public GameObject loseTimeup;

    [SerializeField] private CanvasGroup grandCg;
    public RectTransform mainPanelRect;
    private Tween popTween;

    public override void Open()
    {



        bool revive = LevelManager.Ins.PendingRevive != SushiReviveReason.None;
        if (revive && LevelManager.Ins.PendingRevive == SushiReviveReason.RushCustomer)
        {
            UIManager.Ins.GetUI<CanvasGamePlay>().setImgStuck(true, SushiReviveReason.RushCustomer);
        }
        else if (revive && LevelManager.Ins.PendingRevive == SushiReviveReason.FullConveyor)
        {
            UIManager.Ins.GetUI<CanvasGamePlay>().setImgStuck(true, SushiReviveReason.FullConveyor);
        }

        DOVirtual.DelayedCall(1.3f, () =>
        {
            base.Open();
            popTween?.Kill();
            mainPanelRect.localScale = Vector3.zero;
            popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

            UIManager.Ins.GetUI<CanvasGamePlay>().setImgStuck(false, SushiReviveReason.None);
        });

        Refresh();
    }
    //private void Update() => Refresh();
    private void Refresh()
    {
        LevelManager level = LevelManager.Ins;
        if (level == null) return;
        loseTimeup.SetActive(false);
        loseOutSpace.SetActive(false);
        bool revive = level.PendingRevive != SushiReviveReason.None;
        if(revive && level.PendingRevive == SushiReviveReason.RushCustomer)
        {
            loseTimeup.SetActive(true);
        }else if(revive && level.PendingRevive == SushiReviveReason.FullConveyor)
        {
            loseOutSpace.SetActive(true);
        }
    }
    //public void BtnRevive()
    //{
    //    if (LevelManager.Ins != null)
    //        LevelManager.Ins.ReVive();
    //    CloseDirectly();
    //}


    public void BtnRevive()
    {
        int price = 1000;
        bool isEnough = false;
        int gold = 0;
        if (GameDataFragment.cur.dataPlayer.money >= price)
        {
            isEnough = true;
            GameDataFragment.cur.ChangeMoney(-price, "revive_level");
            gold = price;

        }


        if (isEnough)
        {
            SoundManager.PlayEfx(SoundID.sfx_click);
            LevelManager.Ins.ReVive();

            if (UIManager.Ins.IsOpened<CanvasGamePlay>())
            {
                UIManager.Ins.GetUI<CanvasGamePlay>().setTxtGold();
            }
            var data = GameDataFragment.cur.dataPlayer;
            //FirebaseManager.Ins.REVIVE_offer(data.cur_level, data._retryCountThisLevel, "Revive_gold", "normal_lose");
            Close();

        }
        else
        {
            //UIManager.Ins.OpenUI<CanvasShop>();
            LevelManager.Ins.ReVive();
        }
    }

    public void ReviveWithAd()
    {
        if (LevelManager.Ins != null)
            LevelManager.Ins.ReVive();
        CloseDirectly();
    }
    public void OnHandTouchPanel() { if (grandCg != null) grandCg.alpha = 0.2f; }
    public void OnHAndReleasePanel() { if (grandCg != null) grandCg.alpha = 1; }
    public void Close()
    {
        UIManager.Ins.OpenUI<CanvasFail>();
        CloseDirectly();
    }


    public void backBtn()
    {
        SoundManager.PlayEfx(SoundID.sfx_click);

        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        if (GameDataFragment.cur.dataPlayer.cur_level > 9)
        {
            if (!isUnlimited)
            {
                UIManager.Ins.OpenUI<CanvasWarningLive>();

            }
            else
            {
                UIManager.Ins.OpenUI<CanvasFail>();
                var data = GameDataFragment.cur.dataPlayer;
                //FirebaseManager.Ins.REVIVE_offer(data.cur_level, data._retryCountThisLevel, "Revive_Exit", "normal_lose");
                Close();

            }
        }
        else
        {
            UIManager.Ins.OpenUI<CanvasFail>();
            var data = GameDataFragment.cur.dataPlayer;
            //FirebaseManager.Ins.REVIVE_offer(data.cur_level, data._retryCountThisLevel, "Revive_Exit", "normal_lose");
            Close();
        }


    }

}
