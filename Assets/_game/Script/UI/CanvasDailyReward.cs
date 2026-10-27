using System;
using System.Collections;
using System.Collections.Generic;
using AssetKits.ParticleImage;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasDailyReward : UICanvas
{
    public GameObject[] buttonObjectList;
    public GameObject[] tickObjects;
    public GameObject[] freeTextObjects;
    public GameObject[] freeTextObjectsImg;
    public Animation[] adsIconAnim;

    public GameObject oneButton;
    public GameObject FrmTimeButton;
    public TMP_Text oneCountDownText;

    public TMP_Text timeLeftText;
    public GameObject timeLeftObject;

    private const string PREFIX = "Reset in ";

    public RectTransform mainPanelRect;
    private Tween popTween;
    //private static AnimationCurve PopCurve => UIManager.Ins.popCanvasCurve;

    public Image progressBarImage;
    public GameObject[] baseDotObjects;
    public GameObject[] emptyDotObjects;
    public GameObject[] lineTickObjects;

    public RectTransform bgRect;

    public GameObject[] lineObjects;
    public GameObject[] rewardObjects;

    public TMP_Text freeText;
    public TMP_Text[] rewardTextList;

    public List<ParticleImage> rewardImages;

    //

    //public CanvasGroup canvasGroup;

    //public GameObject mainPanelObject;
    //public RectTransform mainPanelRect;

    //private Vector2 rootPos;
    //private Vector2 upperPos;
    //private Vector2 bottomPos;

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

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        //mainPanelObject.SetActive(false);
        //mainPanelRect.anchoredPosition = upperPos;

        //canvasGroup.alpha = 0;
        //canvasGroup.interactable = true;
        //if (UIManager.Ins.IsOpened<CanvasFloating>())
        //{
        //    UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();
        //    UIManager.Ins.GetUI<CanvasFloating>().HideAllSetting();
        //    UIManager.Ins.GetUI<CanvasFloating>().ShowGold_Heart();
        //}

        Pop();


        RefreshAll();

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

    private void CheckTime()
    {
        StartCoroutine(ie_CheckTime());
        return;

        IEnumerator ie_CheckTime()
        {
            while (true)
            {
                HandleFirstButton();

                if (DailyRewardDataFragment.cur.gameData.itemFlags[0])
                {
                    //count down
                    if (DailyRewardDataFragment.cur.CheckNewDailyDay(out var timeLeft))
                    {
                        timeLeftText.text = "";
                        timeLeftObject.SetActive(false);
                        RefreshAll();
                    }
                    else
                    {
                        timeLeftText.text = PREFIX + timeLeft.ToTimeFormat_H_M_S_Dynamic_Lower_Text();
                        timeLeftObject.SetActive(true);
                    }
                }
                else
                {
                    timeLeftText.text = "";
                    timeLeftObject.SetActive(false);
                }

                yield return Yielders.Get(1f);
            }
        }
    }

    private void RefreshAll()
    {
        HandleFirstButton();
        for (int i = 0; i < buttonObjectList.Length; i++) HandleOtherButton(i);

        SetupLine();
        NoffDailyReward.UpdateNoffStatic();
        RefreshButton();
    }

    private void HandleFirstButton()
    {
        var isFree = DailyRewardDataFragment.cur.CheckIfItsTimeToFree(out var timeLeft);
        if (!isFree) oneCountDownText.text = timeLeft.ToTimeFormat_H_M_S();

        oneButton.SetActive(isFree);
        FrmTimeButton.SetActive(!isFree);
    }

    private void HandleOtherButton(int index) // return if claimable
    {
        var data = DailyRewardDataFragment.cur.gameData;
        if (data.itemFlags[index])
        {
            buttonObjectList[index].SetActive(false);
            tickObjects[index].SetActive(true);
            freeTextObjects[index].SetActive(false);
            freeTextObjectsImg[index].SetActive(false);
        }
        else
        {
            if (CheckValid(index))
            {
                buttonObjectList[index].SetActive(true);
                // adsIconAnim[index].Play();
                tickObjects[index].SetActive(false);
                freeTextObjects[index].SetActive(false);
                freeTextObjectsImg[index].SetActive(false);
            }
            else
            {
                buttonObjectList[index].SetActive(false);
                tickObjects[index].SetActive(false);
                freeTextObjects[index].SetActive(true);
                freeTextObjectsImg[index].SetActive(true);
            }
        }
    }

    private bool CheckValid(int index) //return if ads-able
    {
        var data = DailyRewardDataFragment.cur.gameData;
        return index == 0 || data.itemFlags[index - 1];
    }

    private void SetupLine()
    {
        int currentIndex = 4;
        for (int i = 0; i < lineTickObjects.Length; i++)
        {
            Handle(i);
        }

        progressBarImage.fillAmount = currentIndex switch
        {
            0 => 0,
            1 => .33f,
            2 => .66f,
            _ => 1
        };

        return;

        void Handle(int index)
        {
            var data = DailyRewardDataFragment.cur.gameData;
            if (data.itemFlags[index])
            {
                emptyDotObjects[index].SetActive(true);
                //lineTickObjects[index].SetActive(true);
                baseDotObjects[index].SetActive(false);
            }
            else
            {
                if (CheckValid(index))
                {
                    emptyDotObjects[index].SetActive(true);
                    currentIndex = index;
                    //lineTickObjects[index].SetActive(false);
                    baseDotObjects[index].SetActive(false);
                }
                else
                {
                    emptyDotObjects[index].SetActive(false);
                    //lineTickObjects[index].SetActive(false);
                    baseDotObjects[index].SetActive(true);
                }
            }
        }
    }

    public void OnClickFirstButton()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        if (!DailyRewardDataFragment.cur.CheckIfItsTimeToFree(out _)) return;
        //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, Vector2.zero, null);

        //var config = GetConfig();
        //ResourcesDataFragment.cur.AddGold(config.freeReward, "DAILY_REWARD");
        //BlockUI.Ins.Block();
        rewardImages[0].Play();
        //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);
        //LivesDataFragment.cur.AddUnlimitedLivesSeconds(30);
        //UIManager.Ins.GetUI<CanvasFloating>().showHeart();
        GameDataFragment.cur.ChangeMoney(10, "Daily reward");
        UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

        //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "NORMAL_GOLD_5");
        DailyRewardDataFragment.cur.gameData.lastFreeTime = ExtensionMethodUltimate.GetCurrentTimeSpanToRootDate();
        RefreshAll();

        DOVirtual.DelayedCall(1.3f, () =>
        {
            //BlockUI.Ins.UnBlock();
        });
    }

    public void OnClickReward(int index)
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        if (!CheckValid(index)) return;
        //AdsManager.Ins.ShowRewardedAd("DAILY_" + (index + 1), () =>
        //{
        //AdsManager.Ins.ShowRewardedAd(
        //              GameDataFragment.cur.dataPlayer.cur_level,
        //              "Daily_reward",
        //              "Daily_reward",
        //              "Daily_reward",
        //              1,
        //              "popup_daily_reward",
        //              OnCloseAds,
        //              OnFailed
        //          );
        OnCloseAds();
        void OnFailed()
        {
            //BlockUI.Ins.UnBlock();
        }
        void OnCloseAds()
        {
            DailyRewardDataFragment.cur.gameData.itemFlags[index] = true;
            if (index == 0) DailyRewardDataFragment.cur.PrepareCountDown();
            RefreshAll();

            //var config = GetConfig();
            //BlockUI.Ins.Block();

            switch (index)
            {
                case 0:
                    //ResourcesDataFragment.cur.AddGold(config.adsRewardList[0], "DAILY_REWARD");
                    //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "ADS_GOLD_15");
                    //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, Vector2.zero, null);
                    rewardImages[1].Play();
                    //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                    GameDataFragment.cur.ChangeMoney(25, "Daily reward");
                    //UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

                    break;
                case 1:
                    //ResourcesDataFragment.cur.ActivateUnlimitedLivesForSeconds(15 * 60);
                    //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "ADS_LIVES_15M");
                    //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.LivesSplash, Vector2.zero, null);
                    rewardImages[2].Play();
                    //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                    GameDataFragment.cur.ChangeBoosterCount(BoosterType.Shuffle, 1, "Daily reward");
                    break;
                case 2:
                    //ResourcesDataFragment.cur.AddGold(config.adsRewardList[2], "DAILY_REWARD");
                    //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "ADS_GOLD_25");
                    //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, Vector2.zero, null);
                    rewardImages[3].Play();
                    //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                    GameDataFragment.cur.ChangeMoney(75, "Daily reward");
                    //UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

                    break;
                case 3:
                    //ResourcesDataFragment.cur.AddSort(1, "DAILY_REWARD");
                    //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "ADS_SORT_1");
                    //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.SortSplash, Vector2.zero, null);
                    rewardImages[4].Play();
                    //SoundManager.PlayEfx(SoundID.sfx_ui_reward_claim);

                    GameDataFragment.cur.ChangeBoosterCount(BoosterType.Magnet, 1, "Daily reward");
                    break;
            }
            //});

            DOVirtual.DelayedCall(1.3f, () =>
            {
                //BlockUI.Ins.UnBlock();
            });
        }

    }

    public void OnCLickExit()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_ui_popup_disappear);
        //BlockUI.Ins.UnBlock();
        //UIManager.Ins.GetUI<CanvasFloating>().ShowGoldAndHeart();
        //UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
        CloseDirectly();
    }

    //private ConfigHolder.DailyRewardForCampIapConfigData GetConfig() => GameManager.Ins.IsIapCamp() ? ConfigHolder.cur.dailyRewardForCampIap.config : new ConfigHolder.DailyRewardForCampIapConfigData();

    private void RefreshButton()
    {
        //var config = GetConfig();

        //freeText.text = "x" + config.freeReward;

        //int activeCount = 0;

        //rewardObjects.SetActive(false);

        //for (int i = 0; i < config.adsRewardList.Count && i < rewardObjects.Length; i++)
        //{
        //    //rewardTextList[i].text = "x" + config.adsRewardList[i];
        //    if (config.adsRewardList[i] > 0)
        //    {
        //        activeCount++;
        //        rewardObjects[i].SetActive(true);
        //    }
        //    else
        //    {
        //        rewardObjects[i].SetActive(false);
        //    }


        //    lineObjects.SetActive(activeCount == 4);
        //}
    }
}