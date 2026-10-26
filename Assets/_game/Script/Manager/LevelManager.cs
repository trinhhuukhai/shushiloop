using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public enum SushiReviveReason { None, RushCustomer, FrozenHeads }

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

    public SushiLevelData CurrentLevel { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsPopupOpen => popupCount > 0 || PendingRevive != SushiReviveReason.None;
    public bool CanInteract => IsPlaying && !IsPopupOpen && !tutorialHold;
    public SushiReviveReason PendingRevive { get; private set; }
    public Customer ExpiredCustomer { get; private set; }
    public int ReviveCost => reviveCount == 0 ? 300 : 600;
    public bool CanWatchReviveAd => CanPurchaseRevive && reviveProvider.CanShowRewardedAd;
    [SerializeField, Min(0)] private int victoryReward = 100;
    public int VictoryReward => victoryReward;
    private bool rewardClaimed;
    public bool CanPurchaseRevive => reviveProvider != null && !awaitingAd;
    public ConveyorManager Conveyor => conveyorManager;
    public BoardShishiManager Board => boardManager;
    public bool HasWon { get; private set; }
    public bool HasNextLevel => currentLevelIndex + 1 < LevelCount;
    public bool HasPreviousLevel => currentLevelIndex > 0;
    public bool ShowLevelSelector => showLevelSelector;
    public int LevelCount => Levels != null ? Levels.Count : 0;
    public int CurrentLevelIndex => currentLevelIndex;
    public event System.Action<SushiReviveReason> OnReviveRequested;
    private int popupCount;
    private int reviveCount;
    private int loadVersion;
    private bool pauseApplied;
    private bool tutorialHold;
    private bool gameplayHudShown;
    public bool IsTutorialHeld => tutorialHold;
    private bool awaitingAd;
    private float previousTimeScale = 1;
    public bool isPlay = false;
    public float totalPlayTime = 0;
    public override void Awake()
    {
        base.Awake();

    }

    private void OnEnable()
    {
        if (customerManager != null) customerManager.OnRushExpired += HandleRushExpired;
    }

    private void OnDisable()
    {
        GetComponent<SushiLevelOneTutorial>()?.Cancel();
        if (customerManager != null) customerManager.OnRushExpired -= HandleRushExpired;
        if (pauseApplied) Time.timeScale = previousTimeScale;
        pauseApplied = false;
    }

    private void Update()
    {
        isPlay = CanInteract;
        if (CanInteract)
        {
            totalPlayTime += Time.unscaledDeltaTime;
            customerManager.TickRush(Time.unscaledDeltaTime);
        }
    }

    public void SetPopupOpen(bool open)
    {
        popupCount = Mathf.Max(0, popupCount + (open ? 1 : -1));
        ApplyPopupPause();
    }

    public void SetTutorialHold(bool held)
    {
        tutorialHold = held;
        ApplyPopupPause();
    }

    private void ApplyPopupPause()
    {
        bool held = IsPopupOpen || tutorialHold;
        if (held)
        {
            if (!pauseApplied) previousTimeScale = Time.timeScale;
            // A second popup (such as replay's transition) may still own the pause
            // after Settings requests resume. Keep dish and ingredient flights frozen.
            Time.timeScale = 0;
            pauseApplied = true;
        }
        else if (pauseApplied)
        {
            Time.timeScale = previousTimeScale;
            pauseApplied = false;
        }
        if (conveyorManager != null) conveyorManager.SetPaused(held || !IsPlaying);
    }

    private void HandleRushExpired(Customer customer)
    {
        if (!IsPlaying || PendingRevive != SushiReviveReason.None) return;
        ExpiredCustomer = customer;
        RequestRevive(SushiReviveReason.RushCustomer);
    }

    private void RequestRevive(SushiReviveReason reason)
    {
        PendingRevive = reason;
        ApplyPopupPause();
        OnReviveRequested?.Invoke(reason);
        if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.OpenUI<CanvasFail>();
    }

    public bool TryReviveWithCoins()
    {
        if (PendingRevive == SushiReviveReason.None || !CanPurchaseRevive || !reviveProvider.TrySpendCoins(ReviveCost)) return false;
        CompleteRevive();
        return true;
    }

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

    private void CompleteRevive()
    {
        if (!IsPlaying || PendingRevive == SushiReviveReason.None) return;
        if (PendingRevive == SushiReviveReason.RushCustomer) ExpiredCustomer?.ReviveRush();
        else boardManager.BreakHeadIce();
        reviveCount++;
        if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.CloseUI<CanvasFail>();
        ExpiredCustomer = null;
        PendingRevive = SushiReviveReason.None;
        ApplyPopupPause();
    }

    public void DeclineRevive()
    {
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        awaitingAd = false;
        LoseLevel();
        ApplyPopupPause();
    }

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
        int index = GameDataFragment.cur != null && DataManager.ins != null &&
            (GameManager.ins == null || !GameManager.ins.isPlayTestingLevel)
            ? GameDataFragment.cur.dataPlayer.cur_level : currentLevelIndex;
        LoadLevel(Mathf.Clamp(index, 0, Mathf.Max(0, LevelCount - 1)));
    }

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

        // Tạo dữ liệu mới trước khi dọn màn cũ.
        List<FoodQueueData> boardData = levelData.CreateBoardData();
        GetComponent<SushiLevelOneTutorial>()?.Cancel();
        gameplayHudShown = false;

        if (showGameplayHud && UIManager.Ins != null)
        {
            UIManager.Ins.CloseAll(UIManager.Ins.IsOpened<CanvasTransit>()
                ? UIManager.Ins.GetUI<CanvasTransit>() : null);
        }
        rewardClaimed = false;
        totalPlayTime = 0;
        IsPlaying = false;
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        awaitingAd = false;
        reviveCount = 0;
        loadVersion++;
        HasWon = false;
        ApplyPopupPause();

        // Bắt buộc dọn Food đang giao trước khi xóa khách.
        conveyorManager.Clear();
        customerManager.ClearCustomers();

        currentLevelIndex = levelIndex;
        CurrentLevel = levelData;

        boardManager.Init(boardData);
        customerManager.Init(levelData.customers, levelData.number >= 30);
        conveyorManager.Init(levelData.beltSlots, levelData.loopsPerSecond);

        IsPlaying = true;
        isPlay = true;
        GameManager.ChangeScene(GameScene.GamePlay);
        GameManager.ChangeState(GameState.GamePlay);
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        // Covered loads build the board first and reveal its HUD only after the loading canvas closes.
        if (openGameplayHud) ShowGameplayHud();
        ApplyPopupPause();
    }

    public void ShowGameplayHud()
    {
        if (!IsPlaying || !showGameplayHud || UIManager.Ins == null || gameplayHudShown ||
            (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return;
        UIManager.Ins.OpenUI<CanvasGamePlay>();
        UIManager.Ins.OpenUI<CanvasFloating>();
        gameplayHudShown = true;
        GetComponent<SushiLevelOneTutorial>()?.Prepare(this);
    }

    private void LateUpdate()
    {
        if (!CanInteract)
            return;

        if (boardManager.AllRemainingHeadsFrozen())
        {
            RequestRevive(SushiReviveReason.FrozenHeads);
            return;
        }

        if (!customerManager.AllCustomersCompleted)
            return;

        if (!boardManager.IsEmpty())
            return;

        CompleteLevel();
    }

    private void CompleteLevel()
    {
        // Đánh dấu trước để chỉ xử lý thắng một lần.
        IsPlaying = false;
        HasWon = true;
        isPlay = false;
        GameManager.ChangeState(GameState.Victory);
        if (CanSaveProgress)
        {
            var player = GameDataFragment.cur.dataPlayer;
            if (!player.completedLevels.Contains(currentLevelIndex)) player.completedLevels.Add(currentLevelIndex);
            player.cur_level = Mathf.Max(player.cur_level, Mathf.Min(currentLevelIndex + 1, LevelCount - 1));
            player.loseStreak = 0;
            GameDataFragment.cur.Save();
            DataManager.ins.SetLevelHigest(currentLevelIndex + 1);
        }
        if (showGameplayHud && UIManager.Ins != null) UIManager.Ins.OpenUI<CanvasVictory>();

        conveyorManager.SetPaused(true);
        SpinDataFragment.cur.PassedLevel();
        Debug.Log("WIN: Tất cả khách đã nhận đủ món.");

        // Hiển thị popup thắng tại đây.
    }
    public void OnWIn()
    {
        CompleteLevel();
    }
    public void LoseLevel()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;
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

        // Hiển thị popup thua tại đây.
    }

    private bool CanSaveProgress => DataManager.ins != null && DataManager.ins.isLoaded &&
        GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null &&
        (GameManager.ins == null || !GameManager.ins.isPlayTestingLevel);

    public bool ClaimVictoryReward()
    {
        if (!HasWon || rewardClaimed) return false;
        rewardClaimed = true;
        if (CanSaveProgress) GameDataFragment.cur.ChangeMoney(victoryReward, "win");
        return true;
    }

    public void EnterMainMenu()
    {
        GetComponent<SushiLevelOneTutorial>()?.Cancel();
        tutorialHold = false;
        gameplayHudShown = false;
        IsPlaying = false;
        isPlay = false;
        HasWon = false;
        PendingRevive = SushiReviveReason.None;
        ExpiredCustomer = null;
        awaitingAd = false;
        rewardClaimed = false;
        reviveCount = 0;
        loadVersion++;
        totalPlayTime = 0;
        popupCount = 0;
        if (pauseApplied) Time.timeScale = previousTimeScale;
        pauseApplied = false;
        previousTimeScale = 1;
        CurrentLevel = null;
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        // Release deliveries before destroying their customers and ingredient queues.
        if (conveyorManager != null) conveyorManager.Clear();
        if (customerManager != null) customerManager.ClearCustomers();
        if (boardManager != null) boardManager.ClearBoard();
    }

    public void ReplayLevel()
    {
        LoadLevel(currentLevelIndex);
    }

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

    public void PreviousLevel()
    {
        if (HasPreviousLevel) LoadLevel(currentLevelIndex - 1);
    }

    // Input gọi hàm này khi click vào một cột.
    public void OnClickColumn(FoodQueue column)
    {
        if (!IsPlaying)
            return;
        conveyorManager.TryTakeFromQueue(column);
    }
}
