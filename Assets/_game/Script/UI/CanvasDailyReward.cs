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

    // Mở chuỗi thưởng hằng ngày với hiệu ứng, làm mới các mốc và bắt đầu theo dõi thời gian.
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

    // Điểm mở rộng hiệu ứng popup; hiện chưa có xử lý đang hoạt động.
    private void Pop()
    {
        //canvasGroup.DOFade(1, .28f);

        //mainPanelObject.SetActive(true);
        //mainPanelRect.DOAnchorPos(rootPos, .36f).SetEase(Ease.OutSine);
    }

    // Chuyển thao tác đóng popup trực tiếp về lớp cơ sở.
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

    // Khởi chạy coroutine cập nhật quà miễn phí và chu kỳ thưởng hằng ngày.
    private void CheckTime()
    {
        StartCoroutine(ie_CheckTime());
        return;

        // Theo dõi mỗi giây để làm mới nút miễn phí, đếm thời gian và đặt lại chuỗi khi sang ngày.
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

    // Đồng bộ các nút nhận quà, đường tiến trình và thông báo phần thưởng.
    private void RefreshAll()
    {
        HandleFirstButton();
        for (int i = 0; i < buttonObjectList.Length; i++) HandleOtherButton(i);

        SetupLine();
        NoffDailyReward.UpdateNoffStatic();
        RefreshButton();
    }

    // Bật nút nhận miễn phí hoặc hiển thị thời gian chờ tới lượt miễn phí tiếp theo.
    private void HandleFirstButton()
    {
        var isFree = DailyRewardDataFragment.cur.CheckIfItsTimeToFree(out var timeLeft);
        if (!isFree) oneCountDownText.text = timeLeft.ToTimeFormat_H_M_S();

        oneButton.SetActive(isFree);
        FrmTimeButton.SetActive(!isFree);
    }

    // Cập nhật nút, dấu đã nhận và nhãn khóa theo điều kiện của một mốc thưởng.
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

    // Kiểm tra mốc thưởng đầu hoặc mốc có phần quà liền trước đã được nhận.
    private bool CheckValid(int index) //return if ads-able
    {
        var data = DailyRewardDataFragment.cur.gameData;
        return index == 0 || data.itemFlags[index - 1];
    }

    // Cập nhật các chấm mốc và lượng fill của thanh tiến trình chuỗi thưởng.
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

        // Chọn trạng thái chấm mốc và ghi nhận mốc hiện tại để tính thanh tiến trình.
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
    public TMP_Text txtGold;
    // Hiển thị số tiền trong ví trên popup phần thưởng.
    public void setGold()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString("N0");
    }
    // Đồng bộ số tiền hiển thị mỗi frame.
    private void Update()
    {
        setGold();
    }

    // Nhận 10 tiền khi đủ thời gian miễn phí, ghi mốc nhận và làm mới giao diện.
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

    // Kiểm tra thứ tự mốc rồi gọi nhận thưởng trực tiếp; hiện chưa chạy quảng cáo.
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
        // Điểm xử lý quảng cáo thất bại cục bộ; hiện chưa có nội dung.
        void OnFailed()
        {
            //BlockUI.Ins.UnBlock();
        }
        // Đánh dấu mốc đã nhận, khởi động chu kỳ khi cần và trao tiền hoặc booster tương ứng.
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

    // Đóng popup phần thưởng khi nhấn nút thoát.
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

    // Điểm làm mới nút được giữ lại; hiện chưa có xử lý đang hoạt động.
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