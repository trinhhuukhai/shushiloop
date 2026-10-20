using System.Collections.Generic;
using UnityEngine;

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

    public SushiLevelData CurrentLevel { get; private set; }
    public bool IsPlaying { get; private set; }

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

        // Bắt buộc dọn Food đang giao trước khi xóa khách.
        conveyorManager.Clear();
        customerManager.ClearCustomers();

        currentLevelIndex = levelIndex;
        CurrentLevel = levelData;

        boardManager.Init(boardData);
        customerManager.Init(levelData.customers);
        conveyorManager.Init(levelData.beltSlots, levelData.loopsPerSecond);

        IsPlaying = true;
    }

    private void LateUpdate()
    {
        if (!IsPlaying)
            return;

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

    // Input gọi hàm này khi click vào một cột.
    public void OnClickColumn(FoodQueue column)
    {
        if (!IsPlaying)
            return;
    }
}