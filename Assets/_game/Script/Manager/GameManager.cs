using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    GamePlay,
    Victory,
    Fail,
    Setting,
    Booster,
    Shop,
    Hold,
    ShowTutorial
}

public enum GameScene
{
    Home,
    GamePlay
}
public enum BoosterType
{
    Click,
    Shuffle,
    Magnet
}
public class GameManager : MonoBehaviour
{
    public static GameManager ins;
    private static GameState gameState = GameState.MainMenu;
    private static GameScene gameScene = GameScene.Home;

    public static GameState CurrentGameState => gameState;

    public bool isCheat;
    public bool isCheatIAP;
    public bool isBuildMKT;
    public bool isCheckInternet;
    public bool isEnableCheatTime;
    public bool isEnableDebug;
    public bool isEnableFirebaseDebug;
    public bool isPlayTestingLevel;
    public int levelBackToHome;
    public int levelUnlockSpin;
    public int levelUnlockDailyReward;
    public int levelUnlockDailyLogin;
    public int levelShowInter;

    public bool vuaMuaIAPSeasonpass = false;
    public bool showPopupDailyLogin = false;
    protected void Awake()
    {
        ins = this;
        DontDestroyOnLoad(gameObject);
        DOTween.SetTweensCapacity(2000, 1000);

        Input.multiTouchEnabled = false;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // int maxScreenHeight = 1280;
        // float ratio = (float)Screen.currentResolution.width / (float)Screen.currentResolution.height;
        // if (Screen.currentResolution.height > maxScreenHeight)
        // {
        //     Screen.SetResolution(Mathf.RoundToInt(ratio * (float)maxScreenHeight), maxScreenHeight, true);
        // }
        vuaMuaIAPSeasonpass = false;
        showPopupDailyLogin = false;
        ChangeState(GameState.MainMenu);
    }


    /*private void Update()
    {
        if (!DataManager.Ins.isLoaded) return;
        if (DataManager.Ins.playerData.spinAdsExpire.IsNullOrEmpty()) return;

        DateTime expireDateTime = DateTime.Parse(DataManager.Ins.playerData.spinAdsExpire);
        if (WorldTimeAPI.Ins.GetCurrentDateTime() > expireDateTime)
        {
            
        }

    }*/

    //public void FetchRemoteConfig()
    //{
    //    try
    //    {
    //        levelShowInter = (int)FirebaseManager.Ins.GetValue_Long("level_show_inter");
    //    }
    //    catch (Exception e)
    //    {
    //        levelShowInter = 9;
    //        Debug.LogError("ex: " + e.ToString());
    //    }
    //    try
    //    {
    //        AdsManager.Ins.isUsingRemoteLevelShowInter = FirebaseManager.Ins.GetValue_Boolean("using_remote_show_inter");
    //    }
    //    catch (Exception e)
    //    {
    //        AdsManager.Ins.isUsingRemoteLevelShowInter = true;
    //        Debug.LogError("ex: " + e.ToString());
    //    }
    //    try
    //    {
    //        AdsManager.Ins.capping_5_15 = (int)FirebaseManager.Ins.GetValue_Long("capping_5_15");
    //    }
    //    catch (Exception e)
    //    {
    //        AdsManager.Ins.capping_5_15 = 120;
    //        Debug.LogError("ex: " + e.ToString());
    //    }

    //    Debug.Log("fettt remote ne");
    //}


    //public int GetLevelShowInter()
    //{
    //    if (AdsManager.Ins == null ||
    //        !AdsManager.Ins.isUsingRemoteLevelShowInter)
    //    {
    //        return 9;
    //    }

    //    //if (FirebaseManager.Ins != null)
    //    //{
    //    //    int remoteValue = (int)Firebase.RemoteConfig
    //    //        .FirebaseRemoteConfig.DefaultInstance
    //    //        .GetValue("level_show_inter")
    //    //        .LongValue;

    //    //    if (remoteValue > 0)
    //    //    {
    //    //        return remoteValue;
    //    //    }
    //    //}
    //    return levelShowInter;
    //}

    //public bool IsDailyRewardUnlock()
    //{
    //    return ((GameDataFragment.cur.dataPlayer.cur_level + 1) > levelUnlockDailyReward);
    //}



    //public bool IsDailyLoginUnlock()
    //{
    //    return ((GameDataFragment.cur.dataPlayer.cur_level + 1) > levelUnlockDailyLogin);
    //}

    //public bool IsSpinUnlock()
    //{
    //    return ((GameDataFragment.cur.dataPlayer.cur_level + 1) > levelUnlockSpin);
    //}

    public static void ChangeState(GameState state)
    {
        gameState = state;
    }

    public static void ChangeScene(GameScene state)
    {
        gameScene = state;
    }

    public static bool IsState(GameState state)
    {
        return gameState == state;
    }

    public static bool IsScene(GameScene scence)
    {
        return gameScene == scence;
    }

    private void Start()
    {
        StartCoroutine(ie_LoadGame());
    }

    private IEnumerator ie_LoadGame()
    {
        yield return new WaitUntil(() =>
             LoadingScreen.ins != null 
            //SoundManager.ins != null &&
            //DataManager.ins != null &&
            //AdsManager.Ins != null
        );
        //DataManager.ins.LoadData();
        //WorldTimeAPI.Ins.OnInit();
        //SoundManager.PlayMusicBg(SoundID.sfx_bgm_gameplay);
        LoadingScreen.ins.SetPercent(0.7f, 1f);
        StartCoroutine(LoadingScreen.ins.ie_txtLoading());
        yield return Cache.GetWFS(1f);

        //WorldTimeAPI.Ins.OnInit();

        //yield return new WaitUntil(() => AdsManager.Ins.IsAdsSetupDone);
        //yield return new WaitUntil(() => DataManager.ins.isLoaded);

        //if (AdsManager.Ins.IsInAppUpdate)
        //{
        //    GoogleInAppUpdate.CheckUpdate();
        //    yield return new WaitUntil(() => !GoogleInAppUpdate.isCheckingUpdate);
        //}

        //StartCoroutine(CountTime());
        yield return Cache.GetWFS(0.6f);
        LoadingScreen.ins.SetPercent(1f, 1f);

        yield return Cache.GetWFS(1f);
        //yield return new WaitUntil(() => FirebaseManager.Ins.is_remote_config_done);
        //FetchRemoteConfig();

        //Debug.Log("-------0");
        //UIManager.Ins.OpenUI<CanvasBlockRaycast>();
        // Check GamePlay đã được load chưa
        Scene gameplayScene = SceneManager.GetSceneByName("GamePlay");

        if (gameplayScene.IsValid() && gameplayScene.isLoaded)
        {
            // Scene đã mở -> vào game luôn
            //if (AdsManager.Ins != null) {
            //    AdsManager.Ins.SetInterLevel(GameDataFragment.cur.dataPlayer.cur_level);
            //}

            if (isPlayTestingLevel)
            {
                //UIManager.Ins.OpenUI<CanvasFloating>();
                //LevelManager.Ins.CheckHidebgFade();
                //ChangeState(GameState.MainMenu);
                //UIManager.Ins.OpenUI<CanvasMainMenu>();

                //GameDataFragment.cur.dataPlayer.showTutorialBooster1 = true;
                //GameDataFragment.cur.dataPlayer.showTutorialBooster2 = true;
                //GameDataFragment.cur.dataPlayer.showTutorialBooster3 = true;
                //GameDataFragment.cur.dataPlayer.showTutorialLevel1 = true;
                //GameDataFragment.cur.dataPlayer.showTutorialLevel2 = true;
                //GameDataFragment.cur.dataPlayer.isClickBucketTutorialClear = true;
                //GameDataFragment.cur.dataPlayer.isClickBucketTutorialMagnet = true;
                //GameDataFragment.cur.dataPlayer.fisrtUseBoosterClear = true;
                //GameDataFragment.cur.dataPlayer.fisrtUseBoosterMagnet = true;
                //GameDataFragment.cur.dataPlayer.fisrtUseBoosterSuffle = true;

                //DataManager.ins.SaveData();
            }else
            {
                //UIManager.Ins.OpenUI<CanvasFloating>();
                //LevelManager.Ins.CheckHidebgFade();

                //if (GameDataFragment.cur.dataPlayer.cur_level < levelBackToHome - 1)
                //{
                //    if(GameDataFragment.cur.dataPlayer.moneyTemp > 0)
                //    {
                //        GameDataFragment.cur.ChangeMoney(GameDataFragment.cur.dataPlayer.moneyTemp, "win");
                //        GameDataFragment.cur.dataPlayer.moneyTemp = 0;
                //        UIManager.Ins.GetUI<CanvasFloating>().ShowGold();
                //    }
                //    LevelManager.Ins.ClickPlayGame();
                //}
                //else
                //{
                //    ChangeState(GameState.MainMenu);
                //    UIManager.Ins.OpenUI<CanvasMainMenu>();
                //}
                //DataManager.ins.SaveData();
            }
            yield break;
        }

        // ==============================
        // GamePlay chưa load -> Load scene
        // ==============================

        var sync = SceneManager.LoadSceneAsync("GamePlay");
        sync.allowSceneActivation = false;

        while (sync.progress < 0.9f)
        {
            yield return null;
        }

        // Cho phép chuyển sang GamePlay
        sync.allowSceneActivation = true;

        // Quan trọng: chờ LoadSceneAsync hoàn thành thật sự
        while (!sync.isDone)
        {
            yield return null;
        }
        //if (AdsManager.Ins != null)
        //{
        //    AdsManager.Ins.SetInterLevel(GameDataFragment.cur.dataPlayer.cur_level);
        //}
        // Chờ 1 frame để Awake/OnEnable/Start của các Manager chạy
        //yield return null;

        // ==============================
        // VÀO GAME
        // ==============================
        if (isPlayTestingLevel)
        {
            //UIManager.Ins.OpenUI<CanvasFloating>();
            //LevelManager.Ins.CheckHidebgFade();
            //ChangeState(GameState.MainMenu);
            //UIManager.Ins.OpenUI<CanvasMainMenu>();
            //GameDataFragment.cur.dataPlayer.showTutorialBooster1 = true;
            //GameDataFragment.cur.dataPlayer.showTutorialBooster2 = true;
            //GameDataFragment.cur.dataPlayer.showTutorialBooster3 = true;
            //GameDataFragment.cur.dataPlayer.showTutorialLevel1 = true;
            //GameDataFragment.cur.dataPlayer.showTutorialLevel2 = true;
            //GameDataFragment.cur.dataPlayer.isClickBucketTutorialClear = true;
            //GameDataFragment.cur.dataPlayer.isClickBucketTutorialMagnet = true;
            //GameDataFragment.cur.dataPlayer.fisrtUseBoosterClear = true;
            //GameDataFragment.cur.dataPlayer.fisrtUseBoosterMagnet = true;
            //GameDataFragment.cur.dataPlayer.fisrtUseBoosterSuffle = true;
            //DataManager.ins.SaveData();
        }
        else
        {
            if (LevelManager.Ins != null)
            {
                //UIManager.Ins.OpenUI<CanvasFloating>();
                //LevelManager.Ins.CheckHidebgFade();

                //if (GameDataFragment.cur.dataPlayer.cur_level < levelBackToHome - 1)
                //{
                //    if (GameDataFragment.cur.dataPlayer.moneyTemp > 0)
                //    {
                //        GameDataFragment.cur.ChangeMoney(GameDataFragment.cur.dataPlayer.moneyTemp, "win");
                //        GameDataFragment.cur.dataPlayer.moneyTemp = 0;
                //        UIManager.Ins.GetUI<CanvasFloating>().ShowGold();
                //    }
                //    LevelManager.Ins.ClickPlayGame();
                //}
                //else
                //{
                //    ChangeState(GameState.MainMenu);
                //    UIManager.Ins.OpenUI<CanvasMainMenu>();

                //}
                //DataManager.ins.SaveData();
            }
        }

    }


    //private IEnumerator ie_LoadGame()
    //{
    //    WorldTimeAPI.Ins.OnInit();

    //    yield return new WaitUntil(() => AdsManager.Ins.IsAdsSetupDone);

    //    DataManager.ins.LoadData();

    //    EnergyManager.Ins.OnInit();
    //    AdsRewardBonusManager.Ins.OnInit();

    //    SceneManager.LoadScene("GamePlay");

    //    ChangeState(GameState.MainMenu);
    //    ChangeScene(GameScene.Home);
    //}

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            DataManager.ins.SaveData();
            //GameDataFragment.cur.Save();
            PlayerPrefs.Save();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (DataManager.ins == null || !DataManager.ins.isLoaded) return;

        if (!hasFocus)
        {
            DataManager.ins.SaveData();

            //GameDataFragment.cur.Save();
            PlayerPrefs.Save();
        }
    }


    public void IntoLevelHandle()
    {
        //LivesDataFragment.cur.gameData.TruTymOnKillApp = true;
        //LavaQuestDataFragment.cur.gameData.ShouldDefeatOnKillApp = true;
        //OceanOdysseyDataFragment.cur.gameData.ShouldDefeatOnKillApp = true;
        //OceanOdysseyDataFragment.cur.CheckJoinOceanOdessey();

        //ResolutionFitter.cur.Apply();
        //NotificationManager.Ins.MarkLevelStarted();
    }


    //review
    //private ReviewManager _reviewManager;
    //private PlayReviewInfo _playReviewInfo;
    //[HideInInspector] public bool isReviewing;

    //public void CallReview()
    //{
    //    isReviewing = true;
    //    StartCoroutine(Review());

    //    IEnumerator Review()
    //    {
    //        _reviewManager = new ReviewManager();

    //        var requestFlowOperation = _reviewManager.RequestReviewFlow();
    //        yield return requestFlowOperation;
    //        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
    //        {
    //            // Log error. For example, using requestFlowOperation.Error.ToString().
    //            isReviewing = false;
    //            yield break;
    //        }

    //        _playReviewInfo = requestFlowOperation.GetResult();

    //        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
    //        yield return launchFlowOperation;
    //        _playReviewInfo = null; // Reset the object
    //        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
    //        {
    //            // Log error. For example, using requestFlowOperation.Error.ToString().
    //            isReviewing = false;
    //            yield break;
    //        }
    //        // The flow has finished. The API does not indicate whether the user
    //        // reviewed or not, or even whether the review dialog was shown. Thus, no
    //        // matter the result, we continue our app flow.

    //        isReviewing = false;
    //        yield return null;
    //    }
    //}
}