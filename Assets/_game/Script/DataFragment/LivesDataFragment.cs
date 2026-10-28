using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(
    menuName = "SO/Data Fragment/Lives Fragment",
    fileName = "Lives Fragment")]
public class LivesDataFragment : DataFragment
{
    public static LivesDataFragment cur;

    public const int MAX_LIVES = 5;
    public const int RECOVER_MINUTES = 30;

    // Dùng làm mốc khi không cần chạy timer
    private static readonly DateTime INVALID_TIME = new DateTime(2222, 1, 1);

    public Data gameData;

    public Action OnLivesChanged;

#if UNITY_EDITOR
    private void OnEnable()
    {
        cur = this;
    }
#else
    private void Awake()
    {
        cur = this;
    }
#endif

    // =========================================================
    // LOAD / SAVE
    // =========================================================

    public override void Load()
    {
        if (!LoadData(ref gameData, key) || gameData == null)
        {
            ResetData();
            Save();
            return;
        }
        ResetProperty();
        // =========================
        // LOAD LIFE RECOVER TIME
        // =========================
        if (gameData.lifeRecoverStartTimeLong != 0)
        {
            try
            {
                gameData.lifeRecoverStartTime =
                    DateTime.FromBinary(gameData.lifeRecoverStartTimeLong);
            }
            catch
            {
                gameData.lifeRecoverStartTime = INVALID_TIME;
            }
        }
        else
        {
            gameData.lifeRecoverStartTime = INVALID_TIME;
        }

        // =========================
        // LOAD UNLIMITED TIME
        // =========================
        if (gameData.unlimitedEndTimeLong != 0)
        {
            try
            {
                gameData.unlimitedEndTime =
                    DateTime.FromBinary(gameData.unlimitedEndTimeLong);
            }
            catch
            {
                gameData.unlimitedEndTime = DateTime.MinValue;
            }
        }
        else
        {
            gameData.unlimitedEndTime = DateTime.MinValue;
        }

        // Clamp tránh data lỗi
        gameData.lives = Mathf.Clamp(gameData.lives, 0, MAX_LIVES);

        // Nếu chưa full mà timer không hợp lệ
        // thì bắt đầu tính từ hiện tại.
        if (gameData.lives < MAX_LIVES &&
            !IsValidRecoverTime())
        {
            gameData.lifeRecoverStartTime =
                UnbiasedTime.TrueDateTime;
        }

        // Nếu đang full thì không cần recovery timer
        if (gameData.lives >= MAX_LIVES)
        {
            gameData.lifeRecoverStartTime = INVALID_TIME;
        }

        RefreshLives();

        Save();
    }

    void ResetProperty()
    {
        if (gameData.TruTymOnKillApp)
        {
            ConsumeLife();
            gameData.Reset();
        }

    }

    public override void Save()
    {
        if (gameData == null)
            return;

        gameData.lifeRecoverStartTimeLong =
            gameData.lifeRecoverStartTime.ToBinary();

        gameData.unlimitedEndTimeLong =
            gameData.unlimitedEndTime.ToBinary();

        SaveData(gameData, key);
    }

    public override void ResetData()
    {
        gameData = new Data
        {
            lives = MAX_LIVES,

            lifeRecoverStartTime = INVALID_TIME,
            lastEmptyLivesDateKey = 0,
            lastClaimedFreeLivesDateKey = 0,
            unlimitedEndTime = DateTime.MinValue
        };

        Save();
    }

    // =========================================================
    // LIFE
    // =========================================================

    /// <summary>
    /// Có thể vào level hay không.
    ///
    /// Unlimited -> luôn được chơi.
    /// Bình thường -> cần ít nhất 1 mạng.
    /// </summary>
    public bool HasLife()
    {
        RefreshLives();

        if (IsUnlimitedLives())
            return true;

        return gameData.lives > 0;
    }

    /// <summary>
    /// Trừ 1 mạng.
    ///
    /// CHỈ gọi khi:
    /// - Lose level
    /// - Exit level giữa trận
    ///
    /// KHÔNG gọi khi bắt đầu level.
    /// </summary>
    public bool ConsumeLife()
    {
        RefreshLives();
        if (GameDataFragment.cur.dataPlayer.cur_level < GameManager.ins.levelBackToHome - 1) return true;
        // =========================
        // UNLIMITED
        // =========================
        if (IsUnlimitedLives())
        {
            // Không trừ mạng
            return true;
        }

        // =========================
        // NORMAL
        // =========================
        if (gameData.lives <= 0)
            return false;

        bool wasFull = gameData.lives >= MAX_LIVES;

        gameData.lives--;

        // 5 -> 4
        // bắt đầu timer hồi mạng đầu tiên.
        if (wasFull)
        {
            gameData.lifeRecoverStartTime =
                UnbiasedTime.TrueDateTime;
        }
        else
        {
            // Safety case
            if (!IsValidRecoverTime())
            {
                gameData.lifeRecoverStartTime =
                    UnbiasedTime.TrueDateTime;
            }
        }

        Save();

        OnLivesChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// Reward Ads +1 life hoặc reward khác.
    /// </summary>
    public void AddLife(int amount = 1)
    {
        if (amount <= 0)
            return;

        RefreshLives();

        int oldLives = gameData.lives;

        gameData.lives =
            Mathf.Clamp(
                gameData.lives + amount,
                0,
                MAX_LIVES);

        // Đủ 5 mạng -> dừng recovery timer
        if (gameData.lives >= MAX_LIVES)
        {
            gameData.lifeRecoverStartTime =
                INVALID_TIME;
        }
        else
        {
            // Trường hợp trước đó data timer bị mất
            if (!IsValidRecoverTime())
            {
                gameData.lifeRecoverStartTime =
                    UnbiasedTime.TrueDateTime;
            }
        }

        Save();

        if (oldLives != gameData.lives)
            OnLivesChanged?.Invoke();
    }

    /// <summary>
    /// Fill thẳng lên 5 mạng.
    /// </summary>
    public void FillLives()
    {
        gameData.lives = MAX_LIVES;

        // Full rồi -> không cần timer
        gameData.lifeRecoverStartTime =
            INVALID_TIME;

        Save();

        OnLivesChanged?.Invoke();
    }

    // =========================================================
    // RECOVER LIFE
    // =========================================================

    public void RefreshLives()
    {
        if (gameData == null)
            return;

        // Full mạng
        if (gameData.lives >= MAX_LIVES)
        {
            gameData.lives = MAX_LIVES;

            if (gameData.lifeRecoverStartTime != INVALID_TIME)
            {
                gameData.lifeRecoverStartTime =
                    INVALID_TIME;

                Save();
            }

            return;
        }

        // Chưa full nhưng timer chưa chạy
        if (!IsValidRecoverTime())
        {
            gameData.lifeRecoverStartTime =
                UnbiasedTime.TrueDateTime;

            Save();

            return;
        }

        DateTime now =
            UnbiasedTime.TrueDateTime;

        TimeSpan diff =
            now - gameData.lifeRecoverStartTime;

        // Clock có vấn đề
        if (diff.TotalSeconds < 0)
        {
            gameData.lifeRecoverStartTime = now;

            Save();

            return;
        }

        int recoveredLives =
            Mathf.FloorToInt(
                (float)(diff.TotalMinutes / RECOVER_MINUTES));

        if (recoveredLives <= 0)
            return;

        int oldLives =
            gameData.lives;

        gameData.lives =
            Mathf.Min(
                MAX_LIVES,
                gameData.lives + recoveredLives);

        // =========================
        // ĐÃ FULL
        // =========================
        if (gameData.lives >= MAX_LIVES)
        {
            gameData.lifeRecoverStartTime =
                INVALID_TIME;
        }
        // =========================
        // VẪN CHƯA FULL
        // =========================
        else
        {
            // Giữ phần thời gian dư.
            //
            // Ví dụ:
            // đã offline 35 phút
            // -> hồi 1 tim
            // -> timer mới đã chạy được sẵn 5 phút.
            gameData.lifeRecoverStartTime =
                gameData.lifeRecoverStartTime
                    .AddMinutes(
                        recoveredLives * RECOVER_MINUTES);
        }

        Save();

        if (oldLives != gameData.lives)
            OnLivesChanged?.Invoke();
    }

    /// <summary>
    /// Thời gian còn lại tới mạng tiếp theo.
    /// </summary>
    public TimeSpan GetRemainTime()
    {
        RefreshLives();

        if (gameData.lives >= MAX_LIVES)
            return TimeSpan.Zero;

        if (!IsValidRecoverTime())
            return TimeSpan.FromMinutes(RECOVER_MINUTES);

        DateTime nextLifeTime =
            gameData.lifeRecoverStartTime
                .AddMinutes(RECOVER_MINUTES);

        TimeSpan remain =
            nextLifeTime - UnbiasedTime.TrueDateTime;

        if (remain < TimeSpan.Zero)
            return TimeSpan.Zero;

        return remain;
    }

    public string GetRemainTimeText()
    {
        TimeSpan remain =
            GetRemainTime();

        int minutes =
            Mathf.Max(0, (int)remain.TotalMinutes);

        int seconds =
            Mathf.Max(0, remain.Seconds);

        return $"{minutes:00}:{seconds:00}";
    }

    // =========================================================
    // UNLIMITED LIVES
    // =========================================================

    /// <summary>
    /// Kiểm tra đang có mạng vô hạn hay không.
    /// </summary>
    public bool IsUnlimitedLives()
    {
        if (gameData == null)
            return false;

        if (gameData.unlimitedEndTime == DateTime.MinValue)
            return false;

        return UnbiasedTime.TrueDateTime <
               gameData.unlimitedEndTime;
    }

    /// <summary>
    /// Thêm Unlimited Lives theo số giây.
    ///
    /// Nếu đang có Unlimited:
    /// cộng tiếp từ thời điểm kết thúc hiện tại.
    ///
    /// Nếu chưa có:
    /// cộng từ thời điểm hiện tại.
    /// </summary>
    public void AddUnlimitedLives(TimeSpan duration)
    {
        if (gameData == null)
            return;

        if (duration <= TimeSpan.Zero)
            return;

        DateTime now = UnbiasedTime.TrueDateTime;

        // Nếu đang Unlimited thì cộng tiếp từ end hiện tại.
        // Nếu đã hết / chưa có thì cộng từ hiện tại.
        DateTime startTime =
            IsUnlimitedLives()
                ? gameData.unlimitedEndTime
                : now;

        try
        {
            gameData.unlimitedEndTime =
                startTime.Add(duration);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Safety nếu duration quá lớn vượt DateTime.MaxValue
            gameData.unlimitedEndTime = DateTime.MaxValue;

            Debug.LogWarning(
                "[Lives] Unlimited duration too large -> clamp DateTime.MaxValue");
        }

        Save();

        Debug.Log(
            $"[Lives] Add Unlimited: {duration} | " +
            $"End: {gameData.unlimitedEndTime} | " +
            $"Remain: {gameData.unlimitedEndTime - now}"
        );

        OnLivesChanged?.Invoke();
    }

    public void AddUnlimitedLivesSeconds(double seconds)
    {
        if (seconds <= 0)
            return;

        AddUnlimitedLives(
            TimeSpan.FromSeconds(seconds)
        );
    }

    public void AddUnlimitedLivesMinutes(double minutes)
    {
        if (minutes <= 0)
            return;

        AddUnlimitedLives(
            TimeSpan.FromMinutes(minutes)
        );
    }

    public void AddUnlimitedLivesHours(double hours)
    {
        if (hours <= 0)
            return;

        AddUnlimitedLives(
            TimeSpan.FromHours(hours)
        );
    }

    public void AddUnlimitedLivesDays(double days)
    {
        if (days <= 0)
            return;

        AddUnlimitedLives(
            TimeSpan.FromDays(days)
        );
    }

    /// <summary>
    /// Thời gian Unlimited còn lại.
    /// </summary>
    public TimeSpan GetUnlimitedRemainTime()
    {
        if (!IsUnlimitedLives())
            return TimeSpan.Zero;

        TimeSpan remain =
            gameData.unlimitedEndTime -
            UnbiasedTime.TrueDateTime;

        if (remain < TimeSpan.Zero)
            return TimeSpan.Zero;

        return remain;
    }

    public string GetUnlimitedRemainTimeText()
    {
        TimeSpan remain = GetUnlimitedRemainTime();

        if (remain <= TimeSpan.Zero)
            return "00:00:00";

        long totalHours =
            (long)Math.Floor(remain.TotalHours);

        int minutes = remain.Minutes;
        int seconds = remain.Seconds;

        return $"{totalHours:00}:{minutes:00}:{seconds:00}";
    }

    public void ClearUnlimitedLives()
    {
        gameData.unlimitedEndTime =
            DateTime.MinValue;

        Save();

        OnLivesChanged?.Invoke();
    }


    // =========================================================
    // FIRST EMPTY LIVES DAILY
    // =========================================================

    // =========================================================
    // FIRST EMPTY LIVES DAILY
    // =========================================================

    private int GetTodayKey()
    {
        DateTime now = UnbiasedTime.TrueDateTime;

        return now.Year * 10000 +
               now.Month * 100 +
               now.Day;
    }

    /// <summary>
    /// Kiểm tra phần thưởng free khi hết tim.
    ///
    /// Hàm này KHÔNG làm mất lượt nhận.
    /// Nếu đã mở khóa hôm nay nhưng chưa claim thì vẫn trả về true.
    /// </summary>
    public bool CheckFirstEmptyLivesToday()
    {
        if (gameData == null)
            return false;

        RefreshLives();

        int todayKey = GetTodayKey();

        // Đã nhận thưởng hôm nay
        if (gameData.lastClaimedFreeLivesDateKey == todayKey)
            return false;

        // Đã hết tim và mở khóa phần thưởng hôm nay,
        // nhưng chưa nhận thì vẫn giữ phần thưởng.
        if (gameData.lastEmptyLivesDateKey == todayKey)
            return true;

        // Chưa hết tim nên chưa mở khóa
        if (gameData.lives > 0)
            return false;

        // Lần đầu hết tim trong ngày:
        // chỉ mở khóa phần thưởng, chưa tính là đã nhận.
        gameData.lastEmptyLivesDateKey = todayKey;

        Save();

        return true;
    }

    /// <summary>
    /// Gọi hàm này CHỈ KHI người chơi thực sự claim thành công.
    ///
    /// Trả về true nếu claim hợp lệ.
    /// Trả về false nếu chưa mở khóa hoặc đã claim hôm nay.
    /// </summary>
    public bool TryClaimFirstEmptyLivesToday()
    {
        if (gameData == null)
            return false;

        int todayKey = GetTodayKey();

        // Chưa từng hết tim để mở khóa phần thưởng hôm nay
        if (gameData.lastEmptyLivesDateKey != todayKey)
            return false;

        // Đã nhận rồi
        if (gameData.lastClaimedFreeLivesDateKey == todayKey)
            return false;

        gameData.lastClaimedFreeLivesDateKey = todayKey;

        Save();

        return true;
    }

    public bool HasClaimedFirstEmptyLivesToday()
    {
        if (gameData == null)
            return false;

        return gameData.lastClaimedFreeLivesDateKey == GetTodayKey();
    }



    // =========================================================
    // GETTER
    // =========================================================

    public int GetLives()
    {
        RefreshLives();

        return gameData.lives;
    }

    public bool IsFullLives()
    {
        RefreshLives();

        return gameData.lives >= MAX_LIVES;
    }

    private bool IsValidRecoverTime()
    {
        if (gameData.lifeRecoverStartTime == DateTime.MinValue)
            return false;

        if (gameData.lifeRecoverStartTime == INVALID_TIME)
            return false;

        return true;
    }

    // =========================================================
    // DATA
    // =========================================================

    [Serializable]
    public class Data
    {
        [Header("Normal Lives")]
        public int lives = MAX_LIVES;

        public DateTime lifeRecoverStartTime;
        public long lifeRecoverStartTimeLong;

        [Header("Unlimited Lives")]
        public DateTime unlimitedEndTime;
        public long unlimitedEndTimeLong;

        // Ngày đã hết tim và mở khóa phần thưởng
        public int lastEmptyLivesDateKey;

        // Ngày người chơi thực sự nhận phần thưởng
        public int lastClaimedFreeLivesDateKey;

        [SerializeField] private bool truTymOnKillApp = false;


        public bool TruTymOnKillApp
        {
            get => truTymOnKillApp;
            set => truTymOnKillApp = value;
        }

        public void Reset()
        {
            truTymOnKillApp = false;
        }
    }
}