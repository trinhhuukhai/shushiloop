using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMainMenu : UICanvas
{
    public RectTransform canvasReact;
    public RectTransform playButtonRect;
    public RectTransform rootPanel;

    [Header("Level Map")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("Config")]
    [SerializeField] private int maxLevel = 500;


    private int CurrentLevel => GameDataFragment.cur.dataPlayer.cur_level + 1;
    [SerializeField] private int visibleLevelCount = 6;
    private int topVisibleLevel;
    // hoặc SaveData.CurrentLevel

    public TMP_Text txtLevel;


    public RectTransform left;
    public RectTransform right;

    private Vector2 leftTargetPos;
    private Vector2 rightTargetPos;

    public Image bgLevelImg;
    //[SerializeField] private Sprite levelNormalSprite;
    //[SerializeField] private Sprite levelHardSprite;
    //[SerializeField] private Sprite levelSHardSprite;

    //public GameObject iconNoads;
    //public GameObject iconNoadsBundle;

    //public GameObject dailyRewardButtonObject;
    //public GameObject starterPackButtonObject;
    //public GameObject iconSpin;
    //public GameObject iconDailyLogin;

    //public GameObject lavaFake;
    //public GameObject spinFake;
    //public GameObject DalyRewardFake;
    //public GameObject battlePassFake;
    //public GameObject OceanOdysFake;

    //public Transform lockDailyRwTf;
    //public Transform lockSpinTf;
    //public Transform lockLavaQuestTf;
    //public Transform lockBattlePassTf;
    //public Transform lockOceanOdysTf;

    private bool _isWaitingHomePopups;


    private void Awake()
    {
        leftTargetPos = left.anchoredPosition;
        rightTargetPos = right.anchoredPosition;
        rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());

}

    public void AnimStart()
    {
        left.DOKill();
        right.DOKill();

        left.anchoredPosition = new Vector2(leftTargetPos.x - 300f, leftTargetPos.y);
        right.anchoredPosition = new Vector2(rightTargetPos.x + 300f, rightTargetPos.y);
        left.DOAnchorPos(leftTargetPos, 0.3f)
            .SetEase(Ease.OutBack);
        right.DOAnchorPos(rightTargetPos, 0.3f)
            .SetEase(Ease.OutBack);
    }

    public override void Open()
    {
        base.Open();
        _isWaitingHomePopups = !GameManager.ins.isPlayTestingLevel;
        //SoundManager.PlayMusicBg(SoundID.sfx_bgm_home);
        CheckMoneyTemp();
        //RecenterToCurrentLevel();
        RefreshLevelMap();
        AnimStart();
        CheckTypeLevel();
        CheckLavaSpinAndDailyRWFake();
        //LivesDataFragment.cur.gameData.TruTymOnKillApp = false;
        if (!GameManager.ins.isPlayTestingLevel)
        {
            CheckAutoShowMinigame(() =>
            {
                var showStuffList = new List<IEnumerator>();
                //CheckLavaQuestEventEnumerator(ref showStuffList);
                //CheckAquaRaceEventEnumerator(ref showStuffList);
                //CheckBattlePassEventEnumerator(ref showStuffList);
                if (UIManager.Ins.IsOpened<CanvasMainMenu>())
                {
                    //BuyingPackDataFragment.cur.CheckShowOnHome(ref showStuffList);
                }
                //CheckDailyLoginEnumerator(ref showStuffList);
                //CheckDailyRewardEnumerator(ref showStuffList);
                //CheckSpinEnumerator(ref showStuffList);
                //CheckOceanOdysseyEventEnumerator(ref showStuffList);
                CheckStuff();
                WaitCheckPuzzleBreakAndMore(showStuffList);
            });
        }
        //GenerateMap();
        //FocusCurrentLevel();
    }

    public void CheckLavaSpinAndDailyRWFake()
    {
        //DalyRewardFake.SetActive(!GameManager.ins.IsDailyRewardUnlock());
        //spinFake.SetActive(!GameManager.ins.IsSpinUnlock());
        //if(LavaQuestDataFragment.cur != null)
        //{
        //    if (LavaQuestDataFragment.cur.IsUnlock())
        //    {
        //        lavaFake.SetActive(false);
        //    }
        //    else lavaFake.SetActive(true);
        //}
        //if (BattlePassDataFragment.cur != null)
        //{
        //    if (BattlePassDataFragment.cur.IsUnlock())
        //    {
        //        battlePassFake.SetActive(false);
        //    }
        //    else battlePassFake.SetActive(true);
        //}
        //bool visible = SandyRaceDataFragment.cur.IsScheduledOpen;
        //if (btnAquaFake != null && btnAquaFake != gameObject)
        //    btnAquaFake.SetActive(!visible);
        //if (btnAquaReal != null && btnAquaReal != gameObject)
        //    btnAquaReal.SetActive(visible);

        //if (OceanOdysseyDataFragment.cur != null)
        //{
        //    if (OceanOdysseyDataFragment.cur.IsTimeEventOpen)
        //    {
        //        if (GameDataFragment.cur.dataPlayer.cur_level < 17)
        //        {
        //            OceanOdysFake.SetActive(true);
        //        }
        //        else
        //        {
        //            OceanOdysFake.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        OceanOdysFake.SetActive(false);
        //    }
        //}
    }

    public void ClickLavaFake()
    {
        //if (lockLavaQuestTf == null)
        //    return;
        ////SoundManager.PlayEfx(SoundID.sfx_click);
        //lockLavaQuestTf.DOKill(true);
        //lockLavaQuestTf.DOPunchRotation(
        //        new Vector3(0f, 0f, 15f),
        //        0.3f,                   // Thời gian
        //        8,                       // Số lần rung
        //        1f
        //    )
        //    .SetEase(Ease.OutQuad);
    }

   

    private void CheckStuff()
    {
        //CheckOfferStuffInHome.cur.Check();

    }
    private void CheckNoAdsOffer()
    {
        //iconNoads.SetActive(!AdsManager.isNoAds);

    }

    private void CheckStarterPack()
    {
        //starterPackButtonObject.SetActive(BuyingPackDataFragment.cur.gameData.isShowStarterPack && !BuyingPackDataFragment.cur.gameData.isStarterPackBought);
    }

    private void CheckRemoveAdsBundle()
    {
        //var isOn = BuyingPackDataFragment.cur.gameData.isShowRemoveAdsBundle;
        //iconNoadsBundle.SetActive(isOn);
    }
    private void HideAllButtonsIfPossible()
    {
        //iconNoads.SetActive(false);
        //starterPackButtonObject.SetActive(false);
        //iconNoadsBundle.SetActive(false);
        //iconDailyLogin.SetActive(false);
        //dailyRewardButtonObject.SetActive(false);
        //iconSpin.SetActive(false);
        
    }


    public void WaitCheckPuzzleBreakAndMore(
        List<IEnumerator> stuffShowList)
    {
        _isWaitingHomePopups = true;
        StartCoroutine(ie_Wait());

        IEnumerator ie_Wait()
        {
            foreach (var stuffShow in stuffShowList)
            {
                if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
                    yield break;

                if (stuffShow != null)
                {
                    // Đợi popup hiện tại xử lý và đóng xong.
                    yield return stuffShow;
                }
            }

            if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
                yield break;

            //if (btn_BattlePass != null)
            //{
            //    btn_BattlePass.Check();
            //    btn_BattlePass.PopKeyIfNeeded();
            //}

            // Chỉ mở khóa khi đã chạy hết danh sách.
            // Không phụ thuộc btn_BattlePass có null hay không.
            _isWaitingHomePopups = false;
        }
    }
    public void CheckAutoShowMinigame(Action onDone)
    {
        HideAllButtonsIfPossible();
        onDone?.Invoke();
    }

    public void CheckTypeLevel()
    {
        var levels = DataManager.ins.allLevelData.listAllLevel;

        if (levels == null || levels.Count == 0)
            return;

        // CurrentLevel của UI bắt đầu từ 1
        // Chuyển về index bắt đầu từ 0
        int curLevelData = CurrentLevel - 1;

        int index;

        // Chưa vượt quá list -> lấy bình thường
        if (curLevelData < levels.Count)
        {
            index = curLevelData;
        }
        else
        {
            // Lặp lại từ index 10 (tức level thứ 11)
            int startIndex = 51;
            int cycleLength = levels.Count - startIndex;

            if (cycleLength <= 0)
            {
                index = levels.Count - 1;
            }
            else
            {
                int loopIndex = (curLevelData - startIndex) % cycleLength;
                index = startIndex + loopIndex;
            }
        }

        TypeLevel type = levels[index].TypeLevel;

        switch (type)
        {
            case TypeLevel.Normal:
                //bgLevelImg.sprite = levelNormalSprite;
                break;

            case TypeLevel.Hard:
                //bgLevelImg.sprite = levelHardSprite;
                break;

            case TypeLevel.SuperHard:
                //bgLevelImg.sprite = levelSHardSprite;
                break;
        }
    }
    public void CheckMoneyTemp()
    {
        int temp = GameDataFragment.cur.dataPlayer.moneyTemp;

        if (temp <= 0) {
            BlockUI.Ins.UnBlock();
            return;

        }

        StartCoroutine(CoReceiveGold(temp));
    }

    private IEnumerator CoReceiveGold(int amount)
    {
        BlockUI.Ins.Block();
        //yield return new WaitForSeconds(0.2f);

        CanvasFloating floating = UIManager.Ins.GetUI<CanvasFloating>();

        floating.PlayEffGold();

        // đợi particle bay
        yield return new WaitForSeconds(1.2f);
        GameDataFragment.cur.ChangeMoney(amount, "home");
        GameDataFragment.cur.dataPlayer.moneyTemp = 0;

        DataManager.ins.SaveData();

        floating.AnimGold(amount, false);
    }

    public void RecenterToCurrentLevel()
    {
        //topVisibleLevel = CurrentLevel + visibleLevelCount - 1;
        RefreshLevelMap();
        //FocusCurrentLevel(); // cuộn về đúng vị trí 
    }

    public void Close()
    {
        base.Close(0f);
    }


    public void CheckShowListIconPackLeft()
    {
        //if (AdsManager.isNoAds)
        //{
        //    iconNoads.SetActive(false);
        //    iconNoadsBundle.SetActive(false);
        //}
        //else
        //{
        //    iconNoads.SetActive(true);
        //    iconNoadsBundle.SetActive(true);
        //}
    }

    public void Play()
    {
        if (_isWaitingHomePopups)
            return;

        //SoundManager.PlayEfx(SoundID.sfx_click);
        //if (!LivesDataFragment.cur.HasLife())
        //{
        //    UIManager.Ins.OpenUI<CanvasGetMoreLives>();
        //}
        else
        {
            //if(SoundManager.ins != null) SoundManager.StopMusicBg();
            UIManager.Ins.OpenUI<CanvasTransit>();
            UIManager.Ins.GetUI<CanvasTransit>()
                .MakeDelayTransit(() =>
                {
                    //play game
                    Close(0);
                }, 1f);
        }


    }


    public void RefreshLevelMap()
    {
        txtLevel.text = CurrentLevel.ToString();
    }

    public void OnWinLevel()
    {
        topVisibleLevel++;
        RefreshLevelMap();
    }

    public void openGalary()
    {
        UIManager.Ins.OpenUI<CanvasGalary>();
        UIManager.Ins.CloseUI<CanvasFloating>();
    } 
    
    public void openDailyRW()
    {

        //UIManager.Ins.OpenUI<CanvasDailyReward>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }

    public void openDailyLogin()
    {

        //UIManager.Ins.OpenUI<CanvasDailyLogin>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }

    public void openSpinWheel()
    {

        //UIManager.Ins.OpenUI<CanvasSpin>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }

    public void openShop()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);

        //UIManager.Ins.OpenUI<CanvasShop>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }

    public void openRemoveAdsBundle()
    {

        //UIManager.Ins.OpenUI<CanvasRemoveAdsBundle>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }

    public void openRemoveAdsPack()
    {

        //UIManager.Ins.OpenUI<CanvasRemoveAdsPack>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }

    public void openStarterPack()
    {

        //UIManager.Ins.OpenUI<CanvasStarterPack>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }


    ////public void CheckLavaQuestButtonState()
    ////{
    ////    Debug.Log("Check Lava Quest Button State");

    ////    LavaQuestDataFragment lavaQuestData = LavaQuestDataFragment.cur;

    ////    if (lavaQuestData == null ||
    ////        lavaQuestData.playTimeLeft <= 0 ||
    ////        !lavaQuestData.IsUnlock())
    ////    {
    ////        btn_LavaQuest.HideAllObj();
    ////        btn_LavaQuest.gameObject.SetActive(false);
    ////        return;
    ////    }

    ////    btn_LavaQuest.gameObject.SetActive(true);
    ////    btn_LavaQuest.HideAllObj();

    ////    bool canShow = btn_LavaQuest.CheckState(lavaQuestData);

    ////    if (!canShow)
    ////    {
    ////        //btn_LavaQuest.gameObject.SetActive(false);
    ////    }
    ////}

    ////private void CheckLavaQuestEventEnumerator(ref List<IEnumerator> eventList)
    ////{
    ////    //Debug.LogError("check lava");

    ////    LavaQuestDataFragment.cur.CalculateTime();
    ////    LavaQuestEvent.RemoveListenerChangeState(CheckLavaQuestButtonState);
    ////    LavaQuestEvent.AddListenerChangeState(CheckLavaQuestButtonState);
    ////    btn_LavaQuest.gameObject.SetActive(false);
    ////    if (LavaQuestDataFragment.cur.playTimeLeft > 0 && LavaQuestDataFragment.cur.IsUnlock())
    ////    {
    ////        if (LavaQuestDataFragment.cur.gameData.IsShowEventOnNewSession)
    ////        {
    ////            CheckLavaQuestButtonState();
    ////        }
    ////        eventList.Add(ie_Show());
    ////    }

    ////    return;

    ////    IEnumerator ie_Show()
    ////    {
    ////        CanvasLavaQuest.IsOpen = false;
    ////        if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    ////            LavaQuestDataFragment.cur.OpenLavaQuestPanel();

    ////        if (CanvasLavaQuest.IsOpen)
    ////        {
    ////            yield return new WaitUntil(() => !CanvasLavaQuest.IsOpen);
    ////            CheckLavaQuestButtonState();

    ////        }
    ////    }
    ////}

    //private void CheckLavaQuestEventEnumerator(
    //ref List<IEnumerator> eventList)
    //{
    //    var data = LavaQuestDataFragment.cur;

    //    if (data == null || btn_LavaQuest == null)
    //        return;

    //    data.CalculateTime();

    //    LavaQuestEvent.RemoveListenerChangeState(
    //        CheckLavaQuestButtonState);

    //    LavaQuestEvent.AddListenerChangeState(
    //        CheckLavaQuestButtonState);

    //    btn_LavaQuest.gameObject.SetActive(false);

    //    if (data.playTimeLeft <= 0 || !data.IsUnlock())
    //        return;

    //    if (data.gameData.IsShowEventOnNewSession)
    //    {
    //        CheckLavaQuestButtonState();
    //    }

    //    eventList.Add(ShowLavaQuest());

    //    IEnumerator ShowLavaQuest()
    //    {
    //        if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
    //            yield break;

    //        // Không tự gán CanvasLavaQuest.IsOpen = false.
    //        // Nếu Lava đang mở thì chờ luôn, không mở lại.
    //        if (!IsLavaQuestShowing())
    //        {
    //            data.OpenLavaQuestPanel();
    //        }

    //        // Cho UI cập nhật trạng thái.
    //        yield return null;

    //        while (IsLavaQuestShowing())
    //        {
    //            if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
    //                yield break;

    //            yield return null;
    //        }

    //        if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    //        {
    //            CheckLavaQuestButtonState();
    //        }
    //    }
    //}

    //// Đặt trong class CanvasMainMenu, bên ngoài các hàm khác.
    //public static bool IsLavaQuestShowing()
    //{
    //    return CanvasLavaQuest.IsOpen ||
    //           UIManager.Ins.IsOpened<CanvasLavaQuest>();
    //}

    //private void CheckDailyLoginEnumerator(ref List<IEnumerator> eventList)
    //{
    //    if (GameManager.ins == null ||
    //GameManager.ins.showPopupDailyLogin)
    //        return;
    //    if (DailyLoginDataFragment.cur == null)
    //        return;

    //    if (!UnbiasedTime.IsValidTime)
    //        return;
    //    if (!GameManager.ins.IsDailyLoginUnlock()) return;
    //    // Check ngày mới
    //    DailyLoginDataFragment.cur.CheckNewDay(out _);
    //    if (DailyLoginDataFragment.cur.gameData.isClaimedToday)
    //        return;

    //    // Chỉ add popup khi sang ngày mới và chưa claim

    //    eventList.Add(ie_Show());
    
    //    IEnumerator ie_Show()
    //    {
    //        // Chỉ mở khi đang ở Main Menu
    //        if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    //        {
    //            UIManager.Ins.OpenUI<CanvasDailyLogin>();
    //            GameManager.ins.showPopupDailyLogin = true;

    //            DailyLoginDataFragment.cur.showFirstToday();
    //        }

    //        // Chờ popup đóng rồi mới chạy event tiếp theo
    //        if (UIManager.Ins.IsOpened<CanvasDailyLogin>())
    //        {
    //            yield return new WaitUntil(() =>
    //                !UIManager.Ins.IsOpened<CanvasDailyLogin>()
    //            );

    //            NoffDailyLogin.UpdateNoffStatic();
    //        }
    //    }
    //}

    //private void CheckDailyRewardEnumerator(ref List<IEnumerator> eventList)
    //{
    //    if (DailyRewardDataFragment.cur == null)
    //        return;
    //    if (!GameManager.ins.IsDailyRewardUnlock()) return;
    //    // Chỉ show khi reward FREE đã tới thời gian nhận
    //    if (!DailyRewardDataFragment.cur.CheckIfItsTimeToFree(out _))
    //        return;

    //    eventList.Add(ie_Show());

    //    return;

    //    IEnumerator ie_Show()
    //    {
    //        // Chỉ mở khi đang ở Home
    //        if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    //        {
    //            UIManager.Ins.OpenUI<CanvasDailyReward>();
    //        }

    //        // Chờ user đóng Daily Reward rồi mới chạy popup tiếp theo
    //        if (UIManager.Ins.IsOpened<CanvasDailyReward>())
    //        {
    //            yield return new WaitUntil(() =>
    //                !UIManager.Ins.IsOpened<CanvasDailyReward>()
    //            );

    //            NoffDailyReward.UpdateNoffStatic();
    //        }
    //    }
    //}

    //private void CheckSpinEnumerator(ref List<IEnumerator> eventList)
    //{
    //    if (SpinDataFragment.cur == null)
    //        return;
    //    if (!GameManager.ins.IsSpinUnlock()) return;
    //    int require = SpinDataFragment.cur.GetRequiredPassedLevel();
    //    int passed = Mathf.Min(
    //        SpinDataFragment.cur.gameData.levelPassedNum,
    //        require
    //    );

    //    // Chỉ auto popup khi có FREE SPIN
    //    bool canFreeSpin = passed >= require;

    //    if (!canFreeSpin)
    //        return;

    //    eventList.Add(ie_Show());

    //    return;

    //    IEnumerator ie_Show()
    //    {
    //        if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    //        {
    //            UIManager.Ins.OpenUI<CanvasSpin>();
    //        }

    //        if (UIManager.Ins.IsOpened<CanvasSpin>())
    //        {
    //            yield return new WaitUntil(() =>
    //                !UIManager.Ins.IsOpened<CanvasSpin>()
    //            );

    //            NoffSpin.UpdateNoffStatic();
    //        }
    //    }
    //}

    //private void CheckOceanOdysseyEventEnumerator(ref List<IEnumerator> eventList)
    //{
    //    //Debug.LogError("check ocean");

    //    OceanOdysseyDataFragment.cur.CalculateTime();
    //    btn_OceanOdyssey.gameObject.SetActive(false);
    //    if (OceanOdysseyDataFragment.cur.IsEventOpen || OceanOdysseyDataFragment.cur.IsRewardable)
    //    {
    //        btn_OceanOdyssey.gameObject
    //            .SetActive(true);
    //        btn_OceanOdyssey.CheckState();
    //        eventList.Add(ie_Show());
    //    }
    //    return;

    //    IEnumerator ie_Show()
    //    {
    //        if (OceanOdysseyDataFragment.cur.gameData.LastGainScore > 0 && OceanOdysseyDataFragment.cur.IsEventInprogress)
    //        {
    //            btn_OceanOdyssey.PlayAddScoreAnim(OceanOdysseyDataFragment.cur.gameData.LastGainScore);
    //            OceanOdysseyDataFragment.cur.gameData.LastGainScore = 0;
    //        }

    //        if (!OceanOdysseyDataFragment.cur.IsEventInprogress
    //           &&
    //           !OceanOdysseyDataFragment.cur.gameData.HasShowPopupOnNewSession
    //           )
    //        {
    //            if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    //            {
    //                OceanOdysseyDataFragment.cur.gameData.HasShowPopupOnNewSession = true;
    //                UIManager.Ins.OpenUI<CanvasOceanOdyssey>().ChangeSection<CanvasOceanOdyssey.SectionState>(CanvasOceanOdyssey.SectionState.Start);
    //            }
    //        }

    //        if (CanvasOceanOdyssey.IsOpen) yield return new WaitUntil(() => !CanvasOceanOdyssey.IsOpen);
    //    }
    //}


    //public ButtonOceanOdyssey btn_OceanOdyssey;

    //public void CheckOceanOdysseyButton()
    //{
    //    btn_OceanOdyssey.gameObject.SetActive(false);
    //    if (OceanOdysseyDataFragment.cur.IsEventOpen || OceanOdysseyDataFragment.cur.IsRewardable)
    //    {
    //        btn_OceanOdyssey.gameObject.SetActive(true);
    //        btn_OceanOdyssey.CheckState();
    //    }
    //}

    //public void CheckOceanOdysseyEvent()
    //{
    //    OceanOdysseyDataFragment.cur.CalculateTime();
    //    btn_OceanOdyssey.gameObject.SetActive(false);
    //    if (OceanOdysseyDataFragment.cur.IsEventOpen || OceanOdysseyDataFragment.cur.IsRewardable)
    //    {
    //        btn_OceanOdyssey.gameObject
    //            .SetActive(true);
    //        btn_OceanOdyssey.CheckState();
    //        if (OceanOdysseyDataFragment.cur.gameData.LastGainScore > 0 && OceanOdysseyDataFragment.cur.IsEventInprogress)
    //        {
    //            btn_OceanOdyssey.PlayAddScoreAnim(OceanOdysseyDataFragment.cur.gameData.LastGainScore);
    //            OceanOdysseyDataFragment.cur.gameData.LastGainScore = 0;
    //        }
    //    }
    //    else
    //    {
    //        //btn_OceanOdyssey.TryCheckYesterdayEventState();
    //    }
    //}

    //public void CheckLavaQuestEvent()
    //{
    //    LavaQuestDataFragment.cur.CalculateTime();
    //    LavaQuestEvent.RemoveListenerChangeState(CheckLavaQuestButtonState);
    //    LavaQuestEvent.AddListenerChangeState(CheckLavaQuestButtonState);
    //    btn_LavaQuest.gameObject.SetActive(false);
    //    if (LavaQuestDataFragment.cur.playTimeLeft > 0 && LavaQuestDataFragment.cur.IsUnlock())
    //    {
    //        CheckLavaQuestButtonState();
    //        if (UIManager.Ins.IsOpened<CanvasMainMenu>() && !CanvasOceanOdyssey.IsOpen /*&& !CanvasTreasureStart.IsOpen*/)
    //            LavaQuestDataFragment.cur.OpenLavaQuestPanel();
    //    }
    //}

    //[Header("Battle Pass")]
    //public UIHomeBattlePassButton btn_BattlePass;

    //public void CheckBattlePassButtonState()
    //{
    //    if (btn_BattlePass == null)
    //        return;

    //    var data = BattlePassDataFragment.cur;

    //    if (data == null)
    //    {
    //        btn_BattlePass.gameObject.SetActive(false);
    //        return;
    //    }

    //    // Cập nhật trạng thái icon, thông báo và tiến độ.
    //    btn_BattlePass.Check();
    //}

    //private void CheckBattlePassEventEnumerator(
    //    ref List<IEnumerator> eventList)
    //{
    //    if (btn_BattlePass == null)
    //        return;

    //    var data = BattlePassDataFragment.cur;

    //    if (data == null)
    //    {
    //        btn_BattlePass.gameObject.SetActive(false);
    //        return;
    //    }

    //    CheckBattlePassButtonState();

    //    if (!data.IsUnlock())
    //        return;

    //    eventList.Add(ShowBattlePassEvent());

    //    IEnumerator ShowBattlePassEvent()
    //    {
    //        if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
    //            yield break;

    //        if (btn_BattlePass == null ||
    //            BattlePassDataFragment.cur == null)
    //        {
    //            yield break;
    //        }

    //        // Tạo danh sách khi đến lượt Battle Pass.
    //        var battlePassList = new List<IEnumerator>();

    //        btn_BattlePass.CheckLastEventStuff(
    //            ref battlePassList);

    //        foreach (var popup in battlePassList)
    //        {
    //            yield return popup;
    //        }

    //        if (UIManager.Ins.IsOpened<CanvasMainMenu>())
    //        {
    //            CheckBattlePassButtonState();
    //        }
    //    }
    //}

    //private void CheckAquaRaceEventEnumerator(
    //ref List<IEnumerator> eventList)
    //{
    //    // Kiểm tra dữ liệu khi đến lượt Aqua trong hàng đợi.
    //    eventList.Add(ShowAquaRaceOnHome());
    //}

    //private IEnumerator ShowAquaRaceOnHome()
    //{
    //    if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
    //        yield break;

    //    // Nếu Aqua đã mở thì đợi đóng, không mở lại.
    //    if (IsAquaRaceShowing())
    //    {
    //        while (IsAquaRaceShowing())
    //        {
    //            if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
    //                yield break;

    //            yield return null;
    //        }

    //        yield break;
    //    }

    //    // Chỉ kiểm tra lịch khi đã có thời gian hợp lệ.
    //    if (!UnbiasedTime.IsValidTime)
    //        yield break;

    //    var data = SandyRaceDataFragment.cur;

    //    if (data == null)
    //        yield break;

    //    data.EnsureLoaded();

    //    if (!data.IsLoaded)
    //        yield break;

    //    data.TryLoseWhenPodiumFull();

    //    var race = data.gameData.race;

    //    bool hasUnseenDefeat =
    //        race != null &&
    //        race.state == LiveOps.SandyRace.RaceState.Defeat &&
    //        !race.resultAcknowledged;

    //    // Cho phép hiện kết quả thua còn chờ xác nhận.
    //    // Các trường hợp khác vẫn phải đúng lịch và đã unlock.
    //    if (!hasUnseenDefeat &&
    //        (!data.IsScheduledOpen || !data.IsUnlock))
    //        yield break;

    //    bool hasNewProgress =
    //        race != null &&
    //        !race.resultAcknowledged &&
    //        race.state != LiveOps.SandyRace.RaceState.Claimed &&
    //        race.playerWins > race.displayedWins;

    //    bool shouldOpen =
    //        hasUnseenDefeat ||
    //        data.CanStart ||
    //        hasNewProgress;

    //    if (!shouldOpen)
    //        yield break;

    //    UIManager.Ins.OpenUI<CanvasSandyRace>();

    //    // Giữ phần chờ canvas đóng hiện tại ở phía dưới.


    //    data.RefreshState();

    //    // Ngoài lịch sự kiện hoặc chưa unlock thì không tự mở.
    //    //if (!data.IsScheduledOpen || !data.IsUnlock)
    //    //    yield break;

    //    //var race = data.gameData.race;

    //    //bool hasNewProgress =
    //    //    race != null &&
    //    //    !race.resultAcknowledged &&
    //    //    race.state != LiveOps.SandyRace.RaceState.Claimed &&
    //    //    race.playerWins > race.displayedWins;

    //    ////bool hasUnseenResult =
    //    ////    race != null &&
    //    ////    !race.resultAcknowledged &&
    //    ////    (race.state == LiveOps.SandyRace.RaceState.Defeat ||
    //    ////     race.state == LiveOps.SandyRace.RaceState.Expired);

    //    ////bool canClaim =
    //    ////    data.HasPendingReward &&
    //    ////    data.CanClaimReward;

    //    //bool shouldOpen =
    //    //    data.CanStart ||
    //    //    hasNewProgress; /*||*/
    //    //    //hasUnseenResult ||
    //    //    //canClaim;

    //    //if (!shouldOpen)
    //    //    yield break;

    //    //// Canvas tự chọn section phù hợp.
    //    //// Player vẫn phải bấm Claim để chuyển sang Victory.
    //    //UIManager.Ins.OpenUI<CanvasSandyRace>();

    //    yield return null;

    //    // Chờ Aqua đóng rồi mới chạy popup tiếp theo.
    //    while (IsAquaRaceShowing())
    //    {
    //        if (!UIManager.Ins.IsOpened<CanvasMainMenu>())
    //            yield break;

    //        yield return null;
    //    }

    //    if (UIManager.Ins.IsOpened<CanvasMainMenu>() &&
    //        btnAquaReal != null)
    //    {
    //        var aquaButton =
    //            btnAquaReal.GetComponentInChildren<ButtonSandyRace>(true);

    //        if (aquaButton != null)
    //            aquaButton.Refresh();
    //    }
    //}

    //private static bool IsAquaRaceShowing()
    //{
    //    return CanvasSandyRace.IsOpen ||
    //           UIManager.Ins.IsOpened<CanvasSandyRace>();
    //}
}