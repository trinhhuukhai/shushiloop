using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class CanvasLive : UICanvas
{
    public TMP_Text txtCountLives;
    public GameObject timeObject;
    public TMP_Text txtTime;
    public TMP_Text txtTile;

    public GameObject HeartRed;
    public GameObject HeartBlack;
    public TMP_Text priceTxt;
    public RectTransform mainPanelRect;

    private const int ReviveLivesPrice = 800;

    private bool isopen;
    private Tween popTween;
    private Coroutine coLives;

    public override void Open()
    {
        base.Open();               // chỉ gọi một lần (trước đây gọi hai lần)
        if (isopen) return;
        isopen = true;

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        setUp();

        if (coLives != null) StopCoroutine(coLives);
        coLives = StartCoroutine(IE_Lives());
    }

    public void Close()
    {
        isopen = false;
        CloseDirectly();
    }

    public override void BackKey() => Close();

    // UIManager.CloseAll / CloseDirectly không đi qua Close(), nên reset trạng thái ở đây
    // để lần Open sau luôn chạy lại đầy đủ (animation + coroutine).

    private IEnumerator IE_Lives()
    {
        while (true)
        {
            ShowLives();
            yield return Yielders.Get(1f);
        }
    }

    public void ShowLives()
    {
        var data = LivesDataFragment.cur;
        if (data == null) return;

        data.RefreshLives();
        int lives = data.gameData.lives;
        txtCountLives.text = lives.ToString();

        if (lives >= LivesDataFragment.MAX_LIVES)
        {
            timeObject.SetActive(false);
            txtTime.text = "FULL";
            txtTile.text = "Live";
        }
        else
        {
            timeObject.SetActive(true);
            txtTime.gameObject.SetActive(true);
            txtTime.text = data.GetRemainTime().ToTimeFormat_H_M_S();
            txtTile.text = lives <= 0 ? "Out Of Live" : "Live";
        }
    }

    public void setUp()
    {
        priceTxt.text = ReviveLivesPrice.ToString();
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        priceTxt.color = player != null && player.money < ReviveLivesPrice ? Color.red : Color.white;

        bool outOfLives = LivesDataFragment.cur != null && LivesDataFragment.cur.gameData.lives <= 0;
        HeartRed.SetActive(!outOfLives);
        HeartBlack.SetActive(outOfLives);
    }

    private void RefreshFloatingAndClose(bool refreshHeartOnly = false)
    {
        if (!UIManager.Ins.IsOpened<CanvasFail>())
        {
            var floating = UIManager.Ins.GetUI<CanvasFloating>();
            if (floating != null)
            {
                if (refreshHeartOnly) floating.showHeart();
                else floating.ShowLives();
                floating.ShowGold();
            }
        }
        Close();
    }

    public void ButtonUseGold()
    {
        if (GameDataFragment.cur.dataPlayer.money >= ReviveLivesPrice)
        {
            GameDataFragment.cur.ChangeMoney(-ReviveLivesPrice, "get_more_lives");
            SoundManager.PlayEfx(SoundID.sfx_click);
            LivesDataFragment.cur.AddLife(5);
            RefreshFloatingAndClose();
        }
        else
        {
            // Không đủ vàng: đóng popup (mở shop tại đây nếu cần).
            //UIManager.Ins.OpenUI<CanvasShop>();
            Close();
        }
    }

    public void BtnWatchAds()
    {
        //AdsManager.Ins.ShowRewardedAd(
        //    GameDataFragment.cur.dataPlayer.cur_level,
        //    "More_lives", "More_lives", "More_lives", 1, "popup_more_live",
        //    OnRewarded, null);

        OnRewarded();
    }

    private void OnRewarded()
    {
        LivesDataFragment.cur.AddLife(1);
        RefreshFloatingAndClose();
    }

    public void btnClaimFreeInDay()
    {
        if (!LivesDataFragment.cur.TryClaimFirstEmptyLivesToday())
            return;
        SoundManager.PlayEfx(SoundID.sfx_click);

        LivesDataFragment.cur.AddUnlimitedLivesMinutes(30);
        RefreshFloatingAndClose(true);
    }
}