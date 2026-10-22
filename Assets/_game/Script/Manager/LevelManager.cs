using System.Collections.Generic;
using UnityEngine;

public enum SushiReviveReason { None, RushCustomer, FrozenHeads }

public class LevelManager : SingletonMonoBehaviour<LevelManager>
{
    [Header("Danh sách level")]
    [SerializeField]
    private List<SushiLevelData> levels = new List<SushiLevelData>();

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
    public bool CanInteract => IsPlaying && !IsPopupOpen;
    public SushiReviveReason PendingRevive { get; private set; }
    public Customer ExpiredCustomer { get; private set; }
    public int ReviveCost => reviveCount == 0 ? 300 : 600;
    public bool CanPurchaseRevive => reviveProvider != null && !awaitingAd;
    public ConveyorManager Conveyor => conveyorManager;
    public BoardShishiManager Board => boardManager;
    public bool HasWon { get; private set; }
    public bool HasNextLevel => currentLevelIndex + 1 < levels.Count;
    public bool HasPreviousLevel => currentLevelIndex > 0;
    public bool ShowLevelSelector => showLevelSelector;
    public int LevelCount => levels.Count;
    public int CurrentLevelIndex => currentLevelIndex;
    public event System.Action<SushiReviveReason> OnReviveRequested;
    private int popupCount;
    private int reviveCount;
    private int loadVersion;
    private bool pauseApplied;
    private bool awaitingAd;
    private float previousTimeScale = 1;

    public override void Awake()
    {
        base.Awake();
        if (showGameplayHud && GetComponent<SushiGameplayHud>() == null) gameObject.AddComponent<SushiGameplayHud>();
    }

    private void OnEnable()
    {
        if (customerManager != null) customerManager.OnRushExpired += HandleRushExpired;
    }

    private void OnDisable()
    {
        if (customerManager != null) customerManager.OnRushExpired -= HandleRushExpired;
        if (pauseApplied) Time.timeScale = previousTimeScale;
        pauseApplied = false;
    }

    private void Update()
    {
        if (IsPlaying && !IsPopupOpen) customerManager.TickRush(Time.unscaledDeltaTime);
    }

    public void SetPopupOpen(bool open)
    {
        popupCount = Mathf.Max(0, popupCount + (open ? 1 : -1));
        ApplyPopupPause();
    }

    private void ApplyPopupPause()
    {
        if (IsPopupOpen && !pauseApplied)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            pauseApplied = true;
        }
        else if (!IsPopupOpen && pauseApplied)
        {
            Time.timeScale = previousTimeScale;
            pauseApplied = false;
        }
        if (conveyorManager != null) conveyorManager.SetPaused(IsPopupOpen || !IsPlaying);
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
    }

    public bool TryReviveWithCoins()
    {
        if (PendingRevive == SushiReviveReason.None || !CanPurchaseRevive || !reviveProvider.TrySpendCoins(ReviveCost)) return false;
        CompleteRevive();
        return true;
    }

    public void TryReviveWithAd()
    {
        if (PendingRevive == SushiReviveReason.None || !CanPurchaseRevive) return;
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

    private void Start()
    {
        LoadLevel(currentLevelIndex);
    }

    public void LoadLevel(int levelIndex)
    {
        if (boardManager == null ||
            customerManager == null ||
            conveyorManager == null)
        {
            Debug.LogError("LevelManager chưa đủ tham chiếu.", this);
            return;
        }

        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogError("Index level không hợp lệ.", this);
            return;
        }

        SushiLevelData levelData = levels[levelIndex];

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
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        ApplyPopupPause();
    }

    private void LateUpdate()
    {
        if (!IsPlaying || IsPopupOpen)
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

        conveyorManager.SetPaused(true);

        Debug.Log("WIN: Tất cả khách đã nhận đủ món.");

        // Hiển thị popup thắng tại đây.
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

        Debug.Log("Thua level");

        // Hiển thị popup thua tại đây.
    }

    public void ReplayLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public void NextLevel()
    {
        int nextIndex = currentLevelIndex + 1;

        if (nextIndex >= levels.Count)
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
