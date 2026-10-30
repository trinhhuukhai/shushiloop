using DG.Tweening;
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

    // ===== Loading / level flow =====
    public const int MainMenuUnlockLevel = 5;
    private const string GamePlaySceneName = "GamePlay";

    [Header("Loading")]
    [SerializeField, Min(0.5f)] private float minimumStartupLoadingDuration = 1.65f;
    private float levelTransitDuration = 1.44f;

    public float MinimumStartupLoadingDuration => minimumStartupLoadingDuration;
    public bool IsLoadingLevel { get; private set; }

    // Level từ đó trở đi mới có Home (giống levelBackToHome của Sand). Để 0 thì dùng hằng số mặc định.
    private int HomeUnlockLevel => levelBackToHome > 0 ? levelBackToHome : MainMenuUnlockLevel;

    // Gate Home using the level being played, even after victory advances the saved profile.
    public bool CanReturnHome => LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null &&
        LevelManager.Ins.CurrentLevelIndex + 1 >= HomeUnlockLevel;
    public bool CanOpenMainMenu => SelectedLevelIndex() + 1 >= HomeUnlockLevel;

    protected void Awake()
    {
        if (ins != null && ins != this) { Destroy(gameObject); return; }
        ins = this;
        DontDestroyOnLoad(gameObject);
        DOTween.SetTweensCapacity(2000, 1000);

        Input.multiTouchEnabled = false;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        vuaMuaIAPSeasonpass = false;
        showPopupDailyLogin = false;
        ChangeState(GameState.MainMenu);
    }

    private void Start()
    {
        StartCoroutine(ie_LoadGame());
    }

    private void OnDestroy() { if (ins == this) ins = null; }

    // ===== State helpers =====
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

    private int SelectedLevelIndex()
    {
        LevelManager level = LevelManager.Ins;
        if (level != null) return level.ResolveStartLevelIndex();
        return 0;
    }

    // =====================================================================
    //  STARTUP: Loading -> Data -> GamePlay scene -> (vào level | Home)
    //  Cùng thứ tự với game Sand:
    //  chờ hệ thống -> LoadData -> load scene GamePlay -> quyết định vào level hay Home.
    // =====================================================================
    private IEnumerator ie_LoadGame()
    {
        IsLoadingLevel = true;
        Time.timeScale = 1;
        float startedAt = Time.realtimeSinceStartup;
        LoadingScreen loading = null;
        LevelManager coveredLevel = null;
        bool enteredGameplay = false;
        try
        {
            // 1. Chờ LoadingScreen (tối đa 2s; scene dev có thể không có).
            float loadingDeadline = Time.realtimeSinceStartup + 2f;
            while (LoadingScreen.ins == null && Time.realtimeSinceStartup < loadingDeadline)
                yield return null;
            loading = LoadingScreen.ins;
            if (loading != null)
            {
                loading.SetPercent(0.15f, 0.15f);
                loading.StartCoroutine(loading.ie_txtLoading());
            }

            // 2. Chờ các hệ thống cần thiết rồi load data.
            yield return new WaitUntil(() => DataManager.ins != null && UIManager.Ins != null);
            yield return new WaitUntil(() => DataManager.ins != null && DataManager.ins.isLoaded);

            if (loading != null)
            {
                // Giữ loading che màn hình khi scene GamePlay được load.
                DontDestroyOnLoad(loading.gameObject);
                loading.SetPercent(0.55f, 0.25f);
            }

            // 3. Load scene GamePlay (nếu chưa có).
            yield return LoadGamePlayScene();

            // 4. Vào level ngay (che dưới loading) hoặc về Home.
            LevelManager level = LevelManager.Ins;
            if (level == null)
                Debug.LogError("GamePlay scene is missing its LevelManager.");
            else if (!CanOpenMainMenu)
            {
                coveredLevel = level;
                coveredLevel.SetPopupOpen(true);
                IntoLevelHandle();
                yield return LoadLevelUnderCover(level, SelectedLevelIndex(), false);
                enteredGameplay = level.IsPlaying;
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

    // Giống Sand: allowSceneActivation = false tới 0.9 rồi mới cho kích hoạt.
    private IEnumerator LoadGamePlayScene()
    {
        Scene scene = SceneManager.GetSceneByName(GamePlaySceneName);
        if (scene.IsValid() && scene.isLoaded) yield break;

        AsyncOperation op = SceneManager.LoadSceneAsync(GamePlaySceneName);
        if (op == null)
        {
            Debug.LogError("Cannot load scene " + GamePlaySceneName);
            yield break;
        }
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
    }

    // Dựng board + mở HUD dưới lớp phủ (loading hoặc transit) để khi lớp phủ đóng thì UI đã sẵn sàng.
    // openHudUnderCover = false: chỉ dựng board, HUD sẽ được mở đúng lúc lớp phủ biến mất
    // (dùng cho lúc vào game để HUD và scene GamePlay hiện cùng một frame).
    private IEnumerator LoadLevelUnderCover(LevelManager level, int index, bool openHudUnderCover)
    {
        level.LoadLevel(index, false);
        if (!level.IsPlaying) yield break;

        // Chờ layout và sprite của board ổn định.
        yield return null;
        yield return null;

        if (openHudUnderCover)
        {
            level.ShowGameplayHud(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
        }
        yield return new WaitForEndOfFrame();
    }

    // =====================================================================
    //  LEVEL FLOW (tương đương ClickPlayGame / OnRetry 
    // =====================================================================
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
        LevelManager level = LevelManager.Ins;
        bool held = false;
        try
        {
            if (level == null)
            {
                Debug.LogError("GamePlay scene is missing its LevelManager.");
                yield break;
            }

            index = Mathf.Clamp(index, 0, Mathf.Max(0, level.LevelCount - 1));

            transit = UIManager.Ins.OpenUI<CanvasTransit>();
            if (transit == null) yield break;

            // Transit tự đóng sau levelTransitDuration và gọi callback này.
            bool transitDone = false;
            transit.MakeDelayTransit(() => transitDone = true, levelTransitDuration);

            // Chờ vài frame thật để lớp phủ hiện ra trước khi dựng board.
            yield return null;
            yield return null;

            // Giữ gameplay đứng yên (rush timer, băng chuyền, input) dưới lớp phủ.
            level.SetPopupOpen(true);
            held = true;

            IntoLevelHandle();
            yield return LoadLevelUnderCover(level, index, true);
            if (!level.IsPlaying) yield break;

            // Đợi transit tự đóng (có timeout để không bị treo nếu transit bị huỷ).
            float deadline = Time.realtimeSinceStartup + levelTransitDuration + 2f;
            while (!transitDone && transit != null && Time.realtimeSinceStartup < deadline)
                yield return null;
        }
        finally
        {
            // MakeDelayTransit đã tự đóng; chỉ đóng thêm nếu còn đang mở (lỗi/yield break giữa chừng).
            if (transit != null && UIManager.Ins != null && UIManager.Ins.IsOpened<CanvasTransit>())
                transit.CloseDirectly();

            // Thả giữ sau khi lớp phủ đã biến mất.
            if (held && level != null) level.SetPopupOpen(false);

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

    // Hook chạy ngay trước khi vào level (Sand: ResolutionFitter, NotificationManager...).
    public void IntoLevelHandle()
    {
        //ResolutionFitter.cur.Apply();
        //NotificationManager.Ins.MarkLevelStarted();
    }

    // =====================================================================
    //  APP LIFECYCLE
    // =====================================================================
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) return;
        if (DataManager.ins != null && DataManager.ins.isLoaded) DataManager.ins.SaveData();
        PlayerPrefs.Save();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (DataManager.ins == null || !DataManager.ins.isLoaded) return;

        if (!hasFocus)
        {
            DataManager.ins.SaveData();
            PlayerPrefs.Save();
        }
    }
}