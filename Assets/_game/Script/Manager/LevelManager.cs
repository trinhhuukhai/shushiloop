using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SushiReviveReason { None, RushCustomer, FrozenHeads, FullConveyor }

public class LevelManager : SingletonMonoBehaviour<LevelManager>
{
    [Header("Danh sách level")]
    [SerializeField]
    private AllLevelData allLevelData;
    private IReadOnlyList<SushiLevelData> Levels => DataManager.ins != null && DataManager.ins.allLevelData != null
        ? DataManager.ins.allLevelData.listAllLevel : allLevelData != null ? allLevelData.listAllLevel : null;

    [SerializeField] private int currentLevelIndex;

    [Header("Các hệ thống")]
    [SerializeField] private BoardShishiManager boardManager;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private ConveyorManager conveyorManager;
    [SerializeField] private SushiReviveProvider reviveProvider;
    [SerializeField] private bool showGameplayHud = true;
    [Tooltip("Hiện nút chuyển màn để chơi thử bộ level mẫu.")]
    [SerializeField] private bool showLevelSelector;

    [Header("Kiểm tra hết nước đi")]
    [SerializeField, Min(0f)] private float noMovesLoseDelay = 5f;
    private float noMovesElapsed;

    [Header("Thưởng")]
    [SerializeField, Min(0)] private int victoryReward = 100;

    // ===== Trạng thái =====
    public SushiLevelData CurrentLevel { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool HasWon { get; private set; }
    public SushiReviveReason PendingRevive { get; private set; }
    public Customer ExpiredCustomer { get; private set; }

    public bool IsPopupOpen => popupCount > 0 || PendingRevive != SushiReviveReason.None;
    public bool CanInteract => IsPlaying && !IsPopupOpen && !tutorialHold;
    public bool IsTutorialHeld => tutorialHold;

    // Đang dùng booster (Click / Shuffle / Magnet) -> không cho thua.
    public bool IsUsingBooster => BoosterManager.Existing != null && BoosterManager.Existing.IsUsingAny;

    public int VictoryReward => victoryReward;
    public int VictoryReachedLevelBefore { get; private set; }
    public int VictoryReachedLevelAfter { get; private set; }
    public int ReviveCost => reviveCount == 0 ? 300 : 600;
    public bool CanWatchReviveAd => CanPurchaseRevive && reviveProvider.CanShowRewardedAd;
    public bool CanPurchaseRevive => reviveProvider != null && !awaitingAd && CanApplyRevive;
    private bool CanApplyRevive => IsPlaying && PendingRevive != SushiReviveReason.None &&
        (PendingRevive != SushiReviveReason.FullConveyor || conveyorManager.CanReviveFullConveyor);
    public bool WillReviveBreakIce =>
        (PendingRevive == SushiReviveReason.FullConveyor || PendingRevive == SushiReviveReason.FrozenHeads) &&
        boardManager != null && boardManager.HasIce;

    public ConveyorManager Conveyor => conveyorManager;
    public BoardShishiManager Board => boardManager;

    public bool HasNextLevel => currentLevelIndex + 1 < LevelCount;
    public bool HasPreviousLevel => currentLevelIndex > 0;
    public bool ShowLevelSelector => showLevelSelector;
    public int LevelCount => Levels != null ? Levels.Count : 0;
    public int CurrentLevelIndex => currentLevelIndex;
    public int LoadVersion => loadVersion;

    public event System.Action<SushiReviveReason> OnReviveRequested;

    private bool rewardClaimed;
    private int popupCount;
    private int freezeCount;
    private int reviveCount;
    private int loadVersion;
    private bool tutorialHold;
    private bool gameplayHudShown;
    private bool awaitingAd;
    // Khách hết giờ rush lúc đang dùng booster: hoãn xử lý tới khi dùng xong.
    private Customer deferredRushCustomer;

    public bool isPlay = false;
    public float totalPlayTime = 0;

    // Khởi tạo singleton quản lý màn chơi qua lớp cơ sở.
    public override void Awake()
    {
        base.Awake();
    }

    // Đăng ký nhận sự kiện khách rush hết thời gian chờ.
    private void OnEnable()
    {
        if (customerManager != null) customerManager.OnRushExpired += HandleRushExpired;
    }

    // Hủy các tutorial đang chạy và bỏ đăng ký sự kiện khách hết giờ.
    private void OnDisable()
    {
        ControlTutorial.Ins?.CancelTutorial();
        GetComponent<SushiLevelOneTutorial>()?.Cancel();
        if (customerManager != null) customerManager.OnRushExpired -= HandleRushExpired;
    }

    // Đồng bộ trạng thái chơi, cộng thời gian chơi và cập nhật khách rush khi được phép tương tác.
    private void Update()
    {
        isPlay = CanInteract;
        if (CanInteract)
        {
            totalPlayTime += Time.unscaledDeltaTime;
            customerManager.TickRush(Time.unscaledDeltaTime);
        }
    }

    // Kiểm tra thắng, thua và yêu cầu hồi sinh sau các cập nhật gameplay trong frame.
    private void LateUpdate() => CheckLevelState(Time.unscaledDeltaTime);

    // =====================================================================
    //  START / CHỌN LEVEL
    // =====================================================================
    // Chờ dữ liệu sẵn sàng, chuyển về Loading nếu thiếu UI và tự nạp màn cho cảnh chơi độc lập.
    private IEnumerator Start()
    {
        if (showGameplayHud && UIManager.Ins == null)
        {
            SceneManager.LoadScene("Loading");
            yield break;
        }
        if (DataManager.ins != null)
            yield return new WaitUntil(() => DataManager.ins == null || DataManager.ins.isLoaded);
        // GameManager owns normal app navigation; loading a scene must not start a game by itself.
        // Standalone gameplay tests/dev scenes can still auto-start with the HUD disabled.
        if (showGameplayHud && GameManager.ins != null) yield break;
        LoadLevel(ResolveStartLevelIndex());
    }

    // Level cần chơi: theo tiến trình đã lưu, trừ khi đang test level (khi đó dùng index trong Inspector).
    public int ResolveStartLevelIndex()
    {
        bool useSaved = DataManager.ins != null && GameDataFragment.cur != null &&
            GameDataFragment.cur.dataPlayer != null &&
            (GameManager.ins == null || !GameManager.ins.isPlayTestingLevel);
        int index = useSaved ? GameDataFragment.cur.dataPlayer.cur_level : currentLevelIndex;
        return LevelCount > 0 ? Mathf.Clamp(index, 0, LevelCount - 1) : Mathf.Max(0, index);
    }

    // ----- Các hàm cùng tên với Sand (UI cũ gọi được luôn) -----

    // Sand: ClickPlayGame -> vào level đã lưu (có transit).
    public void ClickPlayGame()
    {
        if (GameManager.ins != null) GameManager.ins.StartCurrentLevel();
        else LoadData();
    }

    // Sand: OnRetry -> chơi lại level hiện tại (có transit).
    public void OnRetry()
    {
        if (GameManager.ins != null) GameManager.ins.ReplayCurrentLevel();
        else ReplayLevel();
    }

    // Sand: LoadData -> nạp level theo tiến trình đã lưu (không transit).
    public void LoadData(bool openGameplayHud = true)
    {
        LoadLevel(ResolveStartLevelIndex(), openGameplayHud);
    }

    // =====================================================================
    //  LOAD LEVEL
    // =====================================================================
    // Kiểm tra dữ liệu, dọn màn cũ đúng thứ tự và dựng bàn, khách, băng chuyền cho màn mới.
    public void LoadLevel(int levelIndex, bool openGameplayHud = true)
    {
        if (boardManager == null ||
            customerManager == null ||
            conveyorManager == null)
        {
            Debug.LogError("LevelManager chưa đủ tham chiếu.", this);
            return;
        }

        if (levelIndex < 0 || levelIndex >= LevelCount)
        {
            Debug.LogError("Index level không hợp lệ.", this);
            return;
        }

        SushiLevelData levelData = Levels[levelIndex];

        if (levelData == null)
        {
            Debug.LogError("Chưa gán dữ liệu level.", this);
            return;
        }

        string error = levelData.ValidateData();

        if (error != null)
        {
            Debug.LogError(error, levelData);
            return;
        }

        if (BoosterManager.Existing != null) BoosterManager.Existing.ResetUsing();

        // Tạo dữ liệu mới trước khi dọn màn cũ.
        List<FoodQueueData> boardData = levelData.CreateBoardData();
        ControlTutorial.Ins?.CancelTutorial();
        GetComponent<SushiLevelOneTutorial>()?.Cancel();
        gameplayHudShown = false;

        // Đóng UI cũ nhưng giữ lại CanvasTransit đang che màn hình.
        if (showGameplayHud && UIManager.Ins != null)
        {
            //UIManager.Ins.CloseAll(UIManager.Ins.IsOpened<CanvasTransit>()
            //    ? UIManager.Ins.GetUI<CanvasTransit>() : null);
        }

        ResetRuntimeState();
        ApplyPopupPause();

        // Bắt buộc dọn Food đang giao trước khi xóa khách.
        conveyorManager.Clear();
        customerManager.ClearCustomers();

        currentLevelIndex = levelIndex;
        CurrentLevel = levelData;

        boardManager.Init(boardData);
        customerManager.Init(levelData.customers);
        conveyorManager.Init(levelData.beltSlots, levelData.loopsPerSecond);

        IsPlaying = true;
        isPlay = true;
        GameManager.ChangeScene(GameScene.GamePlay);
        GameManager.ChangeState(GameState.GamePlay);
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();

        // Covered loads build the board first and reveal its HUD only after the loading canvas closes.
        if (openGameplayHud) ShowGameplayHud();
        ApplyPopupPause();
        OnLevelStarted();
    }

    // Đặt lại kết quả, thời gian, hồi sinh và tăng phiên nạp để loại bỏ callback của màn cũ.
    private void ResetRuntimeState()
    {
        rewardClaimed = false;
        VictoryReachedLevelBefore = 0;
        VictoryReachedLevelAfter = 0;
        noMovesElapsed = 0;
        totalPlayTime = 0;
        IsPlaying = false;
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        deferredRushCustomer = null;
        awaitingAd = false;
        reviveCount = 0;
        loadVersion++;
        HasWon = false;
    }

    // Sand: OnPlay -> chỗ gắn tracking / ads / analytics khi level bắt đầu.
    // Điểm mở rộng sau khi bắt đầu màn; hiện chưa có xử lý bổ sung.
    private void OnLevelStarted()
    {
        //AdsManager.Ins.SetInterLevel(GameDataFragment.cur.dataPlayer.cur_level);
        //FirebaseManager.Ins?.checkpoint_level(currentLevelIndex);
        //AppsflyerEventRegister.af_Level_Start(currentLevelIndex + 1);
    }

    // Mở HUD một lần khi màn sẵn sàng và chuẩn bị tutorial sau lớp chuyển cảnh.
    public void ShowGameplayHud(bool force = false)
    {
        if (!IsPlaying || !showGameplayHud || UIManager.Ins == null || gameplayHudShown ||
            (!force && GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return;
        UIManager.Ins.OpenUI<CanvasGamePlay>();
        UIManager.Ins.OpenUI<CanvasFloating>();
        gameplayHudShown = true;
        if (ControlTutorial.Ins != null) ControlTutorial.Ins.Prepare(this);
        else GetComponent<SushiLevelOneTutorial>()?.Prepare(this);
    }

    // =====================================================================
    //  POPUP / PAUSE
    // =====================================================================
    // freezeTime = true chỉ dành cho Settings.
    // Cộng hoặc trừ bộ đếm popup cùng yêu cầu đóng băng thời gian, rồi đồng bộ tạm dừng.
    public void SetPopupOpen(bool open, bool freezeTime = false)
    {
        int delta = open ? 1 : -1;
        popupCount = Mathf.Max(0, popupCount + delta);
        if (freezeTime) freezeCount = Mathf.Max(0, freezeCount + delta);
        ApplyPopupPause();
    }

    // Giữ hoặc thả trạng thái tạm dừng gameplay do tutorial yêu cầu.
    public void SetTutorialHold(bool held)
    {
        tutorialHold = held;
        ApplyPopupPause();
    }

    // Đồng bộ dừng băng chuyền khi có popup, tutorial giữ màn hoặc màn không còn chơi.
    private void ApplyPopupPause()
    {
        bool held = IsPopupOpen || tutorialHold;   // pause logic game
        // Time.timeScale không bị đụng tới: Settings (freezeCount) chỉ dừng logic qua băng chuyền + CanInteract.
        if (conveyorManager != null) conveyorManager.SetPaused(held || !IsPlaying);
    }

    // =====================================================================
    //  REVIVE
    // =====================================================================
    // Yêu cầu hồi sinh khi khách hết giờ, hoãn xử lý nếu người chơi còn đang dùng booster.
    private void HandleRushExpired(Customer customer)
    {
        if (!IsPlaying || PendingRevive != SushiReviveReason.None) return;

        // Đang dùng booster thì chưa cho thua: nhớ khách này, xử lý sau khi dùng xong.
        if (IsUsingBooster)
        {
            deferredRushCustomer = customer;
            return;
        }

        ExpiredCustomer = customer;
        RequestRevive(SushiReviveReason.RushCustomer);
    }

    // Lưu lý do hồi sinh, dừng gameplay và phát sự kiện cùng popup hồi sinh.
    private void RequestRevive(SushiReviveReason reason)
    {
        PendingRevive = reason;
        ApplyPopupPause();
        OnReviveRequested?.Invoke(reason);
        if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.OpenUI<CanvasRevive>();
    }

    // Thử áp dụng hồi sinh khi đủ điều kiện; hàm này không trực tiếp trừ tiền.
    public bool TryReviveWithCoins()
    {
        if (PendingRevive == SushiReviveReason.None || !CanPurchaseRevive) return false;
        CompleteRevive();
        return true;
    }

    // Áp dụng yêu cầu hồi sinh đang chờ qua điểm gọi tương thích của UI.
    public void ReVive()
    {
        if (PendingRevive == SushiReviveReason.None) return;
        CompleteRevive();
    }

    // Chờ kết quả quảng cáo thưởng và chỉ hồi sinh nếu callback vẫn thuộc phiên màn hiện tại.
    public void TryReviveWithAd()
    {
        if (PendingRevive == SushiReviveReason.None || !CanWatchReviveAd) return;
        awaitingAd = true;
        int version = loadVersion;
        reviveProvider.ShowRewardedAd(success =>
        {
            if (this == null || version != loadVersion || !awaitingAd) return;
            awaitingAd = false;
            if (success) CompleteRevive();
        });
    }

    // Gia hạn khách, gom túi hoặc phá băng theo lý do, rồi đóng popup và tiếp tục màn.
    private void CompleteRevive()
    {
        if (!CanApplyRevive) return;
        if (PendingRevive == SushiReviveReason.RushCustomer) ExpiredCustomer?.ReviveRush();
        else if (PendingRevive == SushiReviveReason.FullConveyor)
        {
            if (!conveyorManager.TryReviveFullConveyor()) return;
            if (WillReviveBreakIce) boardManager.BreakAllIce();
        }
        else if (PendingRevive == SushiReviveReason.FrozenHeads) boardManager.BreakAllIce();
        reviveCount++;
        noMovesElapsed = 0;
        if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.CloseUI<CanvasRevive>();
        ExpiredCustomer = null;
        PendingRevive = SushiReviveReason.None;
        ApplyPopupPause();
    }

    // Hủy yêu cầu hồi sinh và kết thúc màn với kết quả thua.
    public void DeclineRevive()
    {
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        awaitingAd = false;
        LoseLevel();
        ApplyPopupPause();
    }

    // =====================================================================
    //  THẮNG / THUA
    // =====================================================================
    // Kiểm tra thắng hoặc hết nước đi sau thời gian chờ, bỏ qua lúc tutorial hay booster còn xử lý.
    private void CheckLevelState(float deltaTime)
    {
        if (!CanInteract || SushiLevelOneTutorial.IsInputLocked)
        {
            noMovesElapsed = 0;
            return;
        }

        // Thắng vẫn được tính kể cả lúc đang dùng booster.
        if (customerManager.AllCustomersCompleted && boardManager.IsEmpty())
        {
            CompleteLevel();
            return;
        }

        // Đang dùng booster nào thì không cho thua: dừng đếm hết nước đi, hoãn rush hết giờ.
        if (IsUsingBooster)
        {
            noMovesElapsed = 0;
            return;
        }

        // Dùng booster xong mới xử lý khách hết giờ bị hoãn.
        if (deferredRushCustomer != null)
        {
            Customer expired = deferredRushCustomer;
            deferredRushCustomer = null;
            if (expired != null) HandleRushExpired(expired);
            return;
        }

        if (conveyorManager.Capacity == 0 || boardManager.HasPendingMovement ||
            boardManager.HasAvailableTap(conveyorManager.FreeSlotCount) || conveyorManager.CanMakeProgress)
        {
            noMovesElapsed = 0;
            return;
        }

        // Require a continuous deadlock; a pickup, delivery, booster or pause cancels the countdown.
        noMovesElapsed += Mathf.Max(0, deltaTime);
        if (noMovesElapsed < noMovesLoseDelay) return;
        noMovesElapsed = 0;

        bool frozenHeads = boardManager.AllRemainingHeadsFrozen();
        // With space available, frozen heads still need ice removal rather than more slots.
        if (conveyorManager.CanReviveFullConveyor && (!frozenHeads || conveyorManager.FreeSlotCount == 0))
            RequestRevive(SushiReviveReason.FullConveyor);
        else if (frozenHeads) RequestRevive(SushiReviveReason.FrozenHeads);
        else LoseLevel();
    }

    // Chốt thắng một lần, lưu tiến trình, mở popup chiến thắng và dừng băng chuyền.
    private void CompleteLevel()
    {
        // Đánh dấu trước để chỉ xử lý thắng một lần.
        if (HasWon) return;
        CaptureVictoryProgress();
        IsPlaying = false;
        HasWon = true;
        noMovesElapsed = 0;
        isPlay = false;
        //GameDataFragment.cur.dataPlayer.cur_level++;
        GameManager.ChangeState(GameState.Victory);
        if (CanSaveProgress)
        {
            var player = GameDataFragment.cur.dataPlayer;
            if (!player.completedLevels.Contains(currentLevelIndex)) player.completedLevels.Add(currentLevelIndex);
            player.cur_level = Mathf.Max(player.cur_level, Mathf.Min(currentLevelIndex + 1, LevelCount - 1));
            player.loseStreak = 0;
            GameDataFragment.cur.Save();
            DataManager.ins.SetLevelHigest(currentLevelIndex + 1);
            // Chỉ tính tiến trình spin khi lưu progress (test level / scene dev không tính).
            if (SpinDataFragment.cur != null) SpinDataFragment.cur.PassedLevel();
        }

        conveyorManager.SetPaused(true);

        DOVirtual.DelayedCall(1f, () =>
        {
            if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.OpenUI<CanvasVictory>();

        });
        Debug.Log("WIN: Tất cả khách đã nhận đủ món.");
    }

    // Lưu mốc tiến trình trước và sau chiến thắng để UI hiển thị phần mở khóa.
    private void CaptureVictoryProgress()
    {
        // Capture before saving: cur_level has already advanced when the Victory canvas opens.
        // Replaying an older level must not rewind the preview or unlock the same item again.
        VictoryReachedLevelBefore = Mathf.Max(1, currentLevelIndex + 1);
        if (CanSaveProgress)
            VictoryReachedLevelBefore = Mathf.Max(VictoryReachedLevelBefore, GameDataFragment.cur.dataPlayer.cur_level + 1);
        VictoryReachedLevelAfter = Mathf.Max(VictoryReachedLevelBefore, Mathf.Min(currentLevelIndex + 2, LevelCount));
    }

    // Giữ tên cũ cho nút cheat / debug.
    // Chuyển yêu cầu thắng từ điểm gọi bên ngoài vào xử lý hoàn tất màn.
    public void OnWIn()
    {
        CompleteLevel();
    }

    // Chốt thua, dừng màn, cập nhật chuỗi thua và mở giao diện thất bại.
    public void LoseLevel()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;
        noMovesElapsed = 0;
        deferredRushCustomer = null;
        conveyorManager.SetPaused(true);
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        awaitingAd = false;
        ApplyPopupPause();

        isPlay = false;
        GameManager.ChangeState(GameState.Fail);
        if (CanSaveProgress)
        {
            GameDataFragment.cur.dataPlayer.loseStreak++;
            GameDataFragment.cur.Save();
        }
        if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.OpenUI<CanvasFail>();
        Debug.Log("Thua level");
    }

    private bool CanSaveProgress => DataManager.ins != null && DataManager.ins.isLoaded &&
        GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null &&
        (GameManager.ins == null || !GameManager.ins.isPlayTestingLevel);

    // Nhận thưởng chiến thắng với hệ số mặc định bằng 1.
    public bool ClaimVictoryReward() => ClaimVictoryReward(1);

    // Nhận thưởng thắng một lần, giới hạn hệ số từ 1 đến 2 và cộng tiền vào dữ liệu người chơi.
    public bool ClaimVictoryReward(int multiplier)
    {
        if (!HasWon || rewardClaimed) return false;
        rewardClaimed = true;
        if (CanSaveProgress) GameDataFragment.cur.ChangeMoney(victoryReward * Mathf.Clamp(multiplier, 1, 2), "win");
        return true;
    }

    // =====================================================================
    //  MAIN MENU / ĐIỀU HƯỚNG LEVEL
    // =====================================================================
    // Sand: OnMainMenu -> dọn toàn bộ gameplay khi về Home.
    // Hủy tutorial, vô hiệu callback màn cũ và dọn trạng thái, bàn, khách, băng chuyền khi về trang chủ.
    public void EnterMainMenu()
    {
        ControlTutorial.Ins?.CancelTutorial();
        GetComponent<SushiLevelOneTutorial>()?.Cancel();
        tutorialHold = false;
        gameplayHudShown = false;
        IsPlaying = false;
        isPlay = false;
        HasWon = false;
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        deferredRushCustomer = null;
        awaitingAd = false;
        rewardClaimed = false;
        noMovesElapsed = 0;
        reviveCount = 0;
        loadVersion++;
        totalPlayTime = 0;
        popupCount = 0;
        freezeCount = 0;
        CurrentLevel = null;
        if (BoosterManager.Existing != null) BoosterManager.Existing.ResetUsing();
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        // Release deliveries before destroying their customers and ingredient queues.
        if (conveyorManager != null) conveyorManager.Clear();
        if (customerManager != null) customerManager.ClearCustomers();
        if (boardManager != null) boardManager.ClearBoard();
    }

    // Nạp lại dữ liệu màn hiện tại để bắt đầu từ đầu.
    public void ReplayLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    // Nạp màn kế tiếp nếu chưa đến cuối danh sách.
    public void NextLevel()
    {
        int nextIndex = currentLevelIndex + 1;

        if (nextIndex >= LevelCount)
        {
            Debug.Log("Đã hết danh sách level.");
            return;
        }

        LoadLevel(nextIndex);
    }

    // Nạp màn liền trước khi có màn trước đó.
    public void PreviousLevel()
    {
        if (HasPreviousLevel) LoadLevel(currentLevelIndex - 1);
    }

    // Sand: GoToLevel
    // Giới hạn chỉ số yêu cầu và nạp màn tương ứng.
    public void GoToLevel(int index)
    {
        LoadLevel(Mathf.Clamp(index, 0, Mathf.Max(0, LevelCount - 1)));
    }

    // Input gọi hàm này khi click vào một cột.
    public void OnClickColumn(FoodQueue column)
    {
        if (!IsPlaying)
            return;
        conveyorManager.TryTakeFromQueue(column);
    }
}
