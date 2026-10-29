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

    public const int MainMenuUnlockLevel = 5;
    [SerializeField, Min(0.5f)] private float minimumStartupLoadingDuration = 1.65f;
    public float MinimumStartupLoadingDuration => minimumStartupLoadingDuration;
    public bool IsLoadingLevel { get; private set; }

    // Gate Home using the level being played, even after victory advances the saved profile.
    public bool CanReturnHome => LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null &&
        LevelManager.Ins.CurrentLevelIndex + 1 >= MainMenuUnlockLevel;
    public bool CanOpenMainMenu => SelectedLevelIndex() + 1 >= MainMenuUnlockLevel;

    private int SelectedLevelIndex()
    {
        LevelManager level = LevelManager.Ins;
        int index = GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null && !isPlayTestingLevel
            ? GameDataFragment.cur.dataPlayer.cur_level : level != null ? level.CurrentLevelIndex : 0;
        return level != null && level.LevelCount > 0 ? Mathf.Clamp(index, 0, level.LevelCount - 1) : Mathf.Max(0, index);
    }

    private IEnumerator ie_LoadGame()
    {
        IsLoadingLevel = true;
        Time.timeScale = 1;
        float startedAt = Time.realtimeSinceStartup;
        LoadingScreen loading = LoadingScreen.ins;
        LevelManager coveredLevel = null;
        bool enteredGameplay = false;
        try
        {
            if (loading != null)
            {
                loading.SetPercent(0.15f, 0.15f);
                loading.StartCoroutine(loading.ie_txtLoading());
            }
            yield return new WaitUntil(() => DataManager.ins != null && DataManager.ins.isLoaded && UIManager.Ins != null);
            if (loading != null)
            {
                DontDestroyOnLoad(loading.gameObject);
                loading.SetPercent(0.55f, 0.25f);
            }
            Scene scene = SceneManager.GetSceneByName("GamePlay");
            if (!scene.IsValid() || !scene.isLoaded)
                yield return SceneManager.LoadSceneAsync("GamePlay");

            LevelManager level = LevelManager.Ins;
            if (level == null)
                Debug.LogError("GamePlay scene is missing its LevelManager.");
            else if (!CanOpenMainMenu)
            {
                coveredLevel = level;
                coveredLevel.SetPopupOpen(true);
                level.LoadLevel(SelectedLevelIndex(), false);
                enteredGameplay = level.IsPlaying;
                yield return null;
                yield return null;

                if (enteredGameplay)
                {
                    // Mở HUD khi loading còn che, đợi canvas build + render xong.
                    level.ShowGameplayHud(true);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    yield return new WaitForEndOfFrame();
                }
            }

            if (loading != null) loading.SetPercent(1, 0.25f);
            float remaining = Mathf.Max(0.25f, minimumStartupLoadingDuration - (Time.realtimeSinceStartup - startedAt));
            yield return new WaitForSecondsRealtime(remaining);
        }
        finally
        {
            // Release input and timers only after the startup cover disappears.
            if (loading != null) { loading.Close(); Destroy(loading.gameObject); }
            if (coveredLevel != null) coveredLevel.SetPopupOpen(false);
            IsLoadingLevel = false;
            if (this != null)
            {
                if (enteredGameplay && coveredLevel != null) coveredLevel.ShowGameplayHud();
                else OpenMainMenu();
            }
        }
    }

    public bool StartCurrentLevel()
    {
        if (IsLoadingLevel || !IsScene(GameScene.Home) || DataManager.ins == null ||
            !DataManager.ins.isLoaded || UIManager.Ins == null) return false;
        IsLoadingLevel = true;
        StartCoroutine(LoadSelectedLevel(SelectedLevelIndex()));
        return true;
    }

    public bool ContinueAfterVictory()
    {
        LevelManager level = LevelManager.Ins;
        if (IsLoadingLevel || level == null || !level.HasWon || UIManager.Ins == null) return false;
        // Claim exactly once before Home clears the current level or a transition replaces it.
        level.ClaimVictoryReward();
        if (CanReturnHome)
        {
            ReturnToMainMenu();
            return true;
        }
        int nextIndex = level.HasNextLevel ? level.CurrentLevelIndex + 1 : level.CurrentLevelIndex;
        IsLoadingLevel = true;
        StartCoroutine(LoadSelectedLevel(nextIndex));
        return true;
    }

    public bool ReplayCurrentLevel()
    {
        LevelManager level = LevelManager.Ins;
        if (IsLoadingLevel || level == null || level.CurrentLevel == null || UIManager.Ins == null ||
            !IsScene(GameScene.GamePlay)) return false;
        IsLoadingLevel = true;
        StartCoroutine(LoadSelectedLevel(level.CurrentLevelIndex));
        return true;
    }

    private IEnumerator LoadSelectedLevel(int index)
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
            index = Mathf.Clamp(index, 0, Mathf.Max(0, level.LevelCount - 1));
            transit = UIManager.Ins.OpenUI<CanvasTransit>();
            // Real frames show the cover before the board and customers are initialized.
            yield return transit.CoverLevel(index);
            level.LoadLevel(index, false);
            if (!level.IsPlaying) yield break;
            yield return null;
            yield return null;

            // Mở HUD dưới lớp transit, rồi mới reveal.
            level.ShowGameplayHud(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            yield return transit.RevealLevel(false);
        }
        finally
        {
            if (transit != null) transit.CloseDirectly();
            IsLoadingLevel = false;
            if (this != null)
            {
                if (LevelManager.Ins != null && LevelManager.Ins.IsPlaying) LevelManager.Ins.ShowGameplayHud();
                else OpenMainMenu();
            }
        }
    }

    public void ReturnToMainMenu()
    {
        if (IsLoadingLevel || UIManager.Ins == null) return;
        if (LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null && !CanReturnHome) return;
        OpenMainMenu();
    }

    private void OpenMainMenu()
    {
        if (UIManager.Ins == null) return;
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
