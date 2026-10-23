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

    private IEnumerator ie_LoadGame()
    {
        yield return new WaitUntil(() => DataManager.ins != null && DataManager.ins.isLoaded && UIManager.Ins != null);
        Time.timeScale = 1;
        LoadingScreen loading = LoadingScreen.ins;
        if (loading != null)
        {
            loading.SetPercent(0.15f, 0.15f);
            loading.StartCoroutine(loading.ie_txtLoading());
        }
        Scene scene = SceneManager.GetSceneByName("GamePlay");
        if (!scene.IsValid() || !scene.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync("GamePlay");
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f)
            {
                if (loading != null) loading.SetPercent(0.15f + operation.progress / 0.9f * 0.8f, 0.1f);
                yield return null;
            }
            if (loading != null) loading.SetPercent(1, 0.2f);
            yield return new WaitForSecondsRealtime(0.25f);
            operation.allowSceneActivation = true;
            yield return operation;
        }
        else if (loading != null) loading.Close();
        ChangeScene(GameScene.GamePlay);
        ChangeState(GameState.GamePlay);
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
