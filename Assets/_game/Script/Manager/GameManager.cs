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

    [Header("Item unlock levels (1-based, 0 = disabled)")]
    [Min(0)] public int LevelUnlockItemHoiCham = 5;
    [Min(0)] public int LevelUnlockItemBang = 11;
    [Min(0)] public int LevelUnlockItemLinked = 21;
    [Min(0)] public int LevelUnlockItemTray = 31;
    [Header("Rush customer tutorial (1-based, 0 = disabled)")]
    [Min(0)] public int LevelUnlockItemTime = 10;

    // Lấy mốc level mở khóa của từng cơ chế nguyên liệu hoặc khách rush.
    public int GetItemUnlockLevel(FoodState item)
    {
        switch (item)
        {
            case FoodState.HoiCham: return LevelUnlockItemHoiCham;
            case FoodState.Ice: return LevelUnlockItemBang;
            case FoodState.Lock: return LevelUnlockItemLinked;
            case FoodState.RigidTray: return LevelUnlockItemTray;
            case FoodState.Time: return LevelUnlockItemTime;
            default: return 0;
        }
    }

    // Tìm cơ chế nguyên liệu mở khóa kế tiếp và mốc trước đó để tính tiến trình hiển thị.
    public bool TryGetNextItemUnlock(int reachedLevel, out FoodState item, out int previousLevel, out int unlockLevel)
    {
        reachedLevel = Mathf.Max(1, reachedLevel);
        FoodState nextItem = FoodState.Normal;
        int previous = 1;
        int next = int.MaxValue;

        Consider(FoodState.HoiCham, LevelUnlockItemHoiCham);
        Consider(FoodState.Ice, LevelUnlockItemBang);
        Consider(FoodState.Lock, LevelUnlockItemLinked);
        Consider(FoodState.RigidTray, LevelUnlockItemTray);

        item = nextItem;
        previousLevel = previous;
        unlockLevel = nextItem == FoodState.Normal ? 0 : next;
        return nextItem != FoodState.Normal;

        // So sánh một mốc mở khóa với tiến trình để cập nhật mốc trước và mốc kế tiếp gần nhất.
        void Consider(FoodState candidate, int target)
        {
            if (target <= 0) return;
            if (target <= reachedLevel) previous = Mathf.Max(previous, target);
            else if (target < next)
            {
                next = target;
                nextItem = candidate;
            }
        }
    }

    // Giữ một GameManager xuyên cảnh và thiết lập tween, input, tốc độ khung hình cùng trạng thái ban đầu.
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

    // Khởi động coroutine nạp dữ liệu và điều hướng vào game.
    private void Start()
    {
        StartCoroutine(ie_LoadGame());
    }

    // Xóa tham chiếu singleton nếu đối tượng hiện tại bị hủy.
    private void OnDestroy() { if (ins == this) ins = null; }

    // ===== State helpers =====
    // Gán trạng thái hoạt động chung của game.
    public static void ChangeState(GameState state)
    {
        gameState = state;
    }

    // Gán khu vực hiện tại trong luồng điều hướng game.
    public static void ChangeScene(GameScene state)
    {
        gameScene = state;
    }

    // Kiểm tra game có đang ở trạng thái được yêu cầu.
    public static bool IsState(GameState state)
    {
        return gameState == state;
    }

    // Kiểm tra game có đang ở khu vực được yêu cầu.
    public static bool IsScene(GameScene scence)
    {
        return gameScene == scence;
    }

    // Lấy chỉ số màn cần chơi từ LevelManager, dùng màn đầu nếu chưa có manager.
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
    // Điều phối khởi động: chờ dữ liệu và UI, nạp cảnh gameplay rồi hiển thị màn chơi hoặc trang chủ.
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
    // Nạp bất đồng bộ cảnh gameplay nếu chưa tồn tại và chờ cảnh được kích hoạt xong.
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
    // Dựng màn dưới lớp che, chờ bố cục ổn định và tùy chọn mở HUD trước khi bỏ che.
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
    // Kiểm tra điều kiện bắt đầu từ trang chủ và khởi chạy luồng nạp màn đã chọn.
    public bool StartCurrentLevel()
    {
        if (IsLoadingLevel || !IsScene(GameScene.Home) || DataManager.ins == null ||
            !DataManager.ins.isLoaded || UIManager.Ins == null) return false;
        IsLoadingLevel = true;
        StartCoroutine(LoadSelectedLevel(SelectedLevelIndex()));
        return true;
    }

    // Nhận thưởng còn thiếu rồi điều hướng về trang chủ hoặc nạp màn tiếp theo sau chiến thắng.
    public bool ContinueAfterVictory()
    {
        LevelManager level = LevelManager.Ins;
        if (IsLoadingLevel || level == null || !level.HasWon || UIManager.Ins == null) return false;
        // Claim exactly once before Home clears the current level or a transition replaces it.
        level.ClaimVictoryReward();
        if (CanReturnHome)
        {
            IsLoadingLevel = true;
            StartCoroutine(ReturnHomeAfterVictory());
            return true;
        }
        int nextIndex = level.HasNextLevel ? level.CurrentLevelIndex + 1 : level.CurrentLevelIndex;
        IsLoadingLevel = true;
        StartCoroutine(LoadSelectedLevel(nextIndex));
        return true;
    }

    // Kiểm tra trạng thái và khởi chạy luồng chơi lại màn hiện tại.
    public bool ReplayCurrentLevel()
    {
        LevelManager level = LevelManager.Ins;
        if (IsLoadingLevel || level == null || level.CurrentLevel == null || UIManager.Ins == null ||
            !IsScene(GameScene.GamePlay)) return false;
        IsLoadingLevel = true;
        ChangeState(GameState.GamePlay);
        StartCoroutine(LoadSelectedLevel(level.CurrentLevelIndex));
        return true;
    }

    // Che màn bằng hiệu ứng chuyển cảnh, giữ gameplay, dựng level và khôi phục UI khi hoàn tất.
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

    // Chạy chuyển cảnh về trang chủ sau chiến thắng và luôn dọn cờ đang nạp khi kết thúc.
    private IEnumerator ReturnHomeAfterVictory()
    {
        CanvasTransit transit = null;
        try
        {
            transit = UIManager.Ins.OpenUI<CanvasTransit>();
            if (transit == null) yield break;
            bool transitDone = false;
            transit.MakeDelayTransit(() => transitDone = true, levelTransitDuration);
            yield return null;
            yield return null;
            OpenMainMenu(transit);
            float deadline = Time.realtimeSinceStartup + levelTransitDuration + 2f;
            while (!transitDone && transit != null && Time.realtimeSinceStartup < deadline)
                yield return null;
        }
        finally
        {
            if (transit != null && UIManager.Ins != null && UIManager.Ins.IsOpened<CanvasTransit>())
                transit.CloseDirectly();
            IsLoadingLevel = false;
        }
    }

    // Trở về trang chủ khi luồng nạp và chế độ hiện tại cho phép.
    public void ReturnToMainMenu()
    {
        if (IsLoadingLevel || UIManager.Ins == null) return;
        if (LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null && !CanReturnHome) return;
        OpenMainMenu();
    }

    // Đóng các UI liên quan, dọn màn chơi và mở trang chủ với trạng thái game tương ứng.
    private void OpenMainMenu(UICanvas transition = null)
    {
        if (UIManager.Ins == null) return;
        UIManager.Ins.CloseAll(transition);
        LevelManager.Ins?.EnterMainMenu();
        Time.timeScale = 1;
        ChangeScene(GameScene.Home);
        ChangeState(GameState.MainMenu);
        UIManager.Ins.OpenUI<CanvasMainMenu>();
    }

    // Hook chạy ngay trước khi vào level (Sand: ResolutionFitter, NotificationManager...).
    // Điểm mở rộng trước khi vào màn; hiện chưa có xử lý bổ sung.
    public void IntoLevelHandle()
    {
        //ResolutionFitter.cur.Apply();
        //NotificationManager.Ins.MarkLevelStarted();
    }

    // =====================================================================
    //  APP LIFECYCLE
    // =====================================================================
    // Lưu dữ liệu người chơi và PlayerPrefs khi ứng dụng bị tạm dừng.
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) return;
        if (DataManager.ins != null && DataManager.ins.isLoaded) DataManager.ins.SaveData();
        PlayerPrefs.Save();
    }

    // Lưu dữ liệu đã nạp khi ứng dụng mất quyền tập trung.
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
