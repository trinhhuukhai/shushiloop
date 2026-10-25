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
        if (ins != null && ins != this) { Destroy(gameObject); return; }
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

    public bool IsLoadingLevel { get; private set; }

    private IEnumerator ie_LoadGame()
    {
        Time.timeScale = 1;
        LoadingScreen loading = LoadingScreen.ins;
        if (loading != null)
        {
            loading.SetPercent(0.15f, 0.15f);
            loading.StartCoroutine(loading.ie_txtLoading());
        }
        yield return new WaitUntil(() => DataManager.ins != null && DataManager.ins.isLoaded && UIManager.Ins != null);
        // Load the scene systems first, while the startup cover remains visible.
        if (loading != null)
        {
            DontDestroyOnLoad(loading.gameObject);
            loading.SetPercent(0.55f, 0.2f);
        }
        Scene scene = SceneManager.GetSceneByName("GamePlay");
        if (!scene.IsValid() || !scene.isLoaded)
            yield return SceneManager.LoadSceneAsync("GamePlay");
        if (loading != null) loading.SetPercent(1, 0.2f);
        yield return new WaitForSecondsRealtime(0.25f);
        ReturnToMainMenu();
        if (loading != null) { loading.Close(); Destroy(loading.gameObject); }
    }

    // Loading prepares the profile and home UI. Only a deliberate Level button press enters gameplay.
    public bool StartCurrentLevel()
    {
        if (IsLoadingLevel || !IsScene(GameScene.Home) || DataManager.ins == null ||
            !DataManager.ins.isLoaded || UIManager.Ins == null) return false;
        IsLoadingLevel = true;
        StartCoroutine(LoadSelectedLevel());
        return true;
    }

    private IEnumerator LoadSelectedLevel()
    {
        CanvasTransit transit = null;
        try
        {
            LevelManager level = LevelManager.Ins;
            if (level == null)
            {
                Debug.LogError("GamePlay scene is missing its LevelManager.");
                yield break;
            }
            int index = GameDataFragment.cur != null && !isPlayTestingLevel
                ? GameDataFragment.cur.dataPlayer.cur_level : level.CurrentLevelIndex;
            index = Mathf.Clamp(index, 0, Mathf.Max(0, level.LevelCount - 1));
            transit = UIManager.Ins.OpenUI<CanvasTransit>();
            // Real frames show the cover before the board and customers are initialized.
            yield return transit.CoverLevel(index);
            level.LoadLevel(index);
            if (!level.IsPlaying) yield break;
            // Keep gameplay paused under the cover while layout and world sprites settle.
            yield return null;
            yield return null;
            yield return transit.RevealLevel(false);
        }
        finally
        {
            if (transit != null) transit.CloseDirectly();
            IsLoadingLevel = false;
            if (this != null && (LevelManager.Ins == null || !LevelManager.Ins.IsPlaying))
                ReturnToMainMenu();
        }
    }
    public void ReturnToMainMenu()
    {
        if (IsLoadingLevel || UIManager.Ins == null) return;
        UIManager.Ins.CloseAll();
        LevelManager.Ins?.EnterMainMenu();
        Time.timeScale = 1;
        ChangeScene(GameScene.Home);
        ChangeState(GameState.MainMenu);
        UIManager.Ins.OpenUI<CanvasMainMenu>();
    }

    private void OnDestroy() { if (ins == this) ins = null; }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (DataManager.ins != null) DataManager.ins.SaveData();
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
