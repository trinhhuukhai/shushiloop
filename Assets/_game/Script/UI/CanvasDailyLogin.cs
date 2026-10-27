using System.Collections;
using System.Collections.Generic;
using AssetKits.ParticleImage;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CanvasDailyLogin : UICanvas
{
    public static bool IsOpen { get; private set; }

    private bool isNeedUpdate = true;

    //public RectTransform mainPanelRect;
    ////private static AnimationCurve PopCurve => UIManager.Ins.popCanvasCurve;
    private Tween popTween;

    // public GameObject comeBackTextObject;
    public GameObject claimButtonObject;
    // public GameObject claimMoreButtonObject;

    public GameObject[] tickObjects;
    public GameObject[] prizeObject;
    public Image[] bgImages;
    [SerializeField] private Sprite claimedSprite;
    [SerializeField] private Sprite currentSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite wideClaimedSprite;
    [SerializeField] private Sprite wideCurrentSprite;
    [SerializeField] private Sprite wideNeutralSprite;

    public Button[] rewardButtons;
    public List<ParticleImage> rewardImages;


    //

    //public CanvasGroup canvasGroup;

    //public GameObject mainPanelObject;
    public RectTransform mainPanelRect;

    //private Vector2 rootPos;
    //private Vector2 upperPos;
    //private Vector2 bottomPos;

    // public TMP_Text timeText;

    //private void Awake()
    //{
    //    rootPos = mainPanelRect.anchoredPosition;
    //    upperPos = rootPos + Vector2.up * 220;
    //    bottomPos = rootPos - Vector2.up * 180;
    //}

    public override void Open()
    {
        base.Open();
        //SoundManager.PlayEfx(SoundID.sfx_ui_popup_show);

        //mainPanelObject.SetActive(false);
        //mainPanelRect.anchoredPosition = upperPos;

        //canvasGroup.alpha = 0;
        //canvasGroup.interactable = true;

        //if(UIManager.Ins.IsOpened<CanvasFloating>()) {
        //    UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();
        //    UIManager.Ins.GetUI<CanvasFloating>().HideAllSetting();
        //    UIManager.Ins.GetUI<CanvasFloating>().ShowGold_Heart();
        //}

        Pop();
        IsOpen = true;

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        if (isNeedUpdate || DailyLoginDataFragment.cur.CheckNewDay(out _))
        {
            isNeedUpdate = false;

            SetClaimButton();
            RefreshItem();
        }

        //CanvasFloatingStuff.cur.SetHighLightGold(true);
        //CanvasFloatingStuff.SetPlusIcon(false);

        CheckTime();
    }

    private void Pop()
    {
        //canvasGroup.DOFade(1, .28f);

        //mainPanelObject.SetActive(true);
        //mainPanelRect.DOAnchorPos(rootPos, .36f).SetEase(Ease.OutSine);
    }


    public override void CloseDirectly()
    {
        IsOpen = false;
        base.CloseDirectly();

        //canvasGroup.interactable = false;
        //canvasGroup.DOFade(0, .28f);
        //mainPanelRect.DOAnchorPos(bottomPos, .28f).SetEase(Ease.Linear)
        //    .OnComplete(() =>
        //    {
        //        //UIManager.Ins.OpenUI<CanvasFloating>();
        //        base.CloseDirectly();

        //    });
    }

    public void OnCLickExit()
    {
        //if (DailyLoginDataFragment.cur.gameData.isClaimedToday) AudioManager.ins.PlaySound(SoundType.UIClick);
        //else OnClickClaim();
        //SoundManager.PlayEfx(SoundID.sfx_ui_popup_disappear);
        //UIManager.Ins.GetUI<CanvasFloating>().ShowGoldAndHeart();
        //UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
        CloseDirectly();

    }

    public void OnClickClaim()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        DailyLoginDataFragment.cur.gameData.isClaimedToday = true;
        ClaimHandle();
        SetClaimButton();

        NoffDailyLogin.UpdateNoffStatic();
    }

    // public void OnClickClaimMore()
    // {
    //     AudioManager.ins.PlaySound(SoundType.UIClick);
    //     AdsManager.Ins.ShowRewardedAd("DAILY_LOGIN_MORE", () =>
    //     {
    //         DailyLoginDataFragment.cur.gameData.isClaimMoreToday = true;
    //         ClaimHandle();
    //         SetClaimButton();
    //     });
    // }

    private void CheckTime()
    {
        StartCoroutine(ie_Check());
        return;

        IEnumerator ie_Check()
        {
            yield return new WaitUntil(() => UnbiasedTime.IsValidTime);
            while (true)
            {
                if (DailyLoginDataFragment.cur.CheckNewDay(out var timeLeft))
                {
                    SetClaimButton();
                    RefreshItem();
                }

                // timeText.text = timeLeft.ToTimeFormatPro();
                yield return Yielders.Get(1);
            }
        }
    }

    private void ClaimHandle()
    {
        //BlockUI.Ins.Block();
        var index = DailyLoginDataFragment.cur.GetCurrentDayIndex();
        // DailyLoginDataFragment.cur.TickDay(index);

        // FirebaseManager.Ins.claim_daily_login(index + 1);

        switch (index)
        {
            case 0:
                //ResourcesDataFragment.cur.ActivateUnlimitedLivesForSeconds(30 * 60);
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click, 1, "Daily Login");
                rewardImages[0].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "UNLIMITED_LIVE_30M");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.LivesSplash, Vector2.zero, null);
                break;
            case 1:
                GameDataFragment.cur.ChangeMoney(200, "Daily Login");
                //UIManager.Ins.GetUI<CanvasFloating>().ShowGold();
                rewardImages[1].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);


                //ResourcesDataFragment.cur.AddRefresh(1, "DAILY_LOGIN");
                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "REFRESH");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.RefreshSplash, Vector2.zero, null);
                break;
            case 2:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Shuffle, 1, "Daily Login");
                rewardImages[2].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                //ResourcesDataFragment.cur.AddGold(35, "DAILY_LOGIN");
                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "GOLD_35");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, Vector2.zero, null);
                break;
            case 3:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Magnet, 1, "Daily Login");
                rewardImages[3].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                //ResourcesDataFragment.cur.AddClear(1, "DAILY_LOGIN");
                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "CLEAR");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.ClearSplash, Vector2.zero, null);
                break;
            case 4:
                GameDataFragment.cur.ChangeMoney(300, "Daily Login");
                //UIManager.Ins.GetUI<CanvasFloating>().ShowGold();
                rewardImages[4].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                //ResourcesDataFragment.cur.AddGold(50, "DAILY_LOGIN");
                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "GOLD_50");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, Vector2.zero, null);
                break;
            case 5:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click, 1, "Daily Login");
                rewardImages[5].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                //ResourcesDataFragment.cur.AddSort(1, "DAILY_LOGIN");
                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "SORT");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.SortSplash, Vector2.zero, null);
                break;
            case 6:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click, 1, "Daily Login");
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Shuffle, 1, "Daily Login");
                rewardImages[6].Play();
                rewardImages[7].Play();
                //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                //ResourcesDataFragment.cur.AddGold(100, "DAILY_LOGIN");
                //FirebaseManager.Ins.reward_claim("DAILY_LOGIN", "GOLD_100");
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, Vector2.zero, null);
                break;
        }

        RefreshItem();
        DOVirtual.DelayedCall(1.1f, () =>
        {
            //BlockUI.Ins.UnBlock();
        });
    }

    private void RefreshItem()
    {
        var index = DailyLoginDataFragment.cur.GetCurrentDayIndex();
        var isClaimed = DailyLoginDataFragment.cur.gameData.isClaimedToday && DailyLoginDataFragment.cur.gameData.isClaimMoreToday;

        for (int i = 0; i < 7; i++)
        {
            if (i < index)
            {
                tickObjects[i].SetActive(true);
                prizeObject[i].SetActive(false);
                bgImages[i].sprite = i == 6 ? wideClaimedSprite : claimedSprite;
                rewardButtons[i].interactable = false;
            }
            else if (i == index)
            {
                tickObjects[i].SetActive(isClaimed);
                prizeObject[i].SetActive(!isClaimed);
                bgImages[i].sprite = i == 6 ? wideCurrentSprite : currentSprite;
                rewardButtons[i].interactable = !isClaimed;
            }
            else
            {
                tickObjects[i].SetActive(false);
                prizeObject[i].SetActive(true);
                bgImages[i].sprite = i == 6 ? wideNeutralSprite : neutralSprite;
                rewardButtons[i].interactable = false;
            }
        }
    }

    private void SetClaimButton()
    {
        if (!DailyLoginDataFragment.cur.gameData.isClaimedToday)
        {
            // comeBackTextObject.SetActive(false);
            claimButtonObject.SetActive(true);
            // claimMoreButtonObject.SetActive(false);
        }
        else if (!DailyLoginDataFragment.cur.gameData.isClaimMoreToday)
        {
            // comeBackTextObject.SetActive(false);
            claimButtonObject.SetActive(false);
            // claimMoreButtonObject.SetActive(true);
        }
        else
        {
            // comeBackTextObject.SetActive(true);
            claimButtonObject.SetActive(false);
            // claimMoreButtonObject.SetActive(false);
        }
    }
}