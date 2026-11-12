using System;
using System.Collections.Generic;
using UnityEngine;

// Quản lý bảng xếp hạng: điểm thật của người chơi (cộng khi thắng màn) và đối thủ giả lập.
// - Weekly: cuộc đua theo tuần (thứ Hai 00:00 -> thứ Hai tuần sau). Đối thủ có điểm mục tiêu cả tuần,
//   điểm hiện tại tăng dần theo thời gian trong tuần như người chơi thật.
// - Rank (World / Country): điểm tích lũy toàn thời gian, đối thủ tăng điểm chậm theo số ngày.
[Serializable, CreateAssetMenu(menuName = "Stuff/Data Fragment/Rank Fragment", fileName = "Rank Fragment")]
public class RankDataFragment : DataFragment
{
    public static RankDataFragment cur;

    public struct RankEntry
    {
        public string name;
        public int score;
        // Chỉ số avatar (đối thủ); người chơi dùng avatar trong Profile.
        public int avatar;
        public bool isPlayer;
    }

    [Header("Điểm người chơi")]
    [Min(1)] public int scorePerLevel = 10;
    [Tooltip("Thưởng thêm khi thắng màn mà không thua lần nào trước đó.")]
    [Min(0)] public int firstTryBonus = 5;

    [Header("Đua tuần")]
    [Min(3)] public int weeklyRivalCount = 30;
    [Tooltip("Nhịp điểm tuần dự kiến khi chưa có dữ liệu tuần trước.")]
    [Min(10)] public int defaultWeeklyPace = 400;
    [Tooltip("Điểm cuối tuần của hạng 1 so với nhịp điểm tuần của người chơi.")]
    [Min(1f)] public float weeklyLeaderFactor = 1.8f;
    [Tooltip("Ước tính số người tham gia đua tuần (dùng để tính hạng khi ngoài danh sách).")]
    [Min(30)] public int weeklyPopulation = 500;

    [Header("Xếp hạng tổng")]
    [Min(3)] public int rankRivalCount = 30;
    public int worldTopScore = 5200;
    public int worldDailyGrowth = 120;
    [Min(30)] public int worldPopulation = 50000;
    public int countryTopScore = 2600;
    public int countryDailyGrowth = 60;
    [Min(30)] public int countryPopulation = 3000;

    public Data gameData;

    // Gọi khi điểm người chơi đổi hoặc sang tuần mới.
    public event Action OnRankChanged;

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
            return;
        }
        gameData.weeklyRivals ??= new List<Rival>();
        gameData.worldRivals ??= new List<Rival>();
        gameData.countryRivals ??= new List<Rival>();
        if (gameData.seed == 0) gameData.seed = NewSeed();
        if (gameData.installTimeLong == 0) gameData.installTimeLong = Now.ToBinary();
        if (gameData.worldRivals.Count == 0 || gameData.countryRivals.Count == 0) CreateRankRivals();
        EnsureCurrentWeek();
        Save();
    }

    public override void Save()
    {
        if (gameData == null) return;
        SaveData(gameData, key);
    }

    public override void ResetData()
    {
        gameData = new Data
        {
            seed = NewSeed(),
            installTimeLong = Now.ToBinary(),
            // Người chơi cũ: quy đổi số màn đã qua thành điểm tổng.
            totalScore = CompletedLevels() * scorePerLevel
        };
        CreateRankRivals();
        EnsureCurrentWeek();
        Save();
    }

    // =========================================================
    // ĐIỂM NGƯỜI CHƠI
    // =========================================================

    // Gọi khi thắng màn (chỉ khi lưu tiến trình). Trả về số điểm được cộng.
    public int AddLevelWin(bool firstTry)
    {
        if (gameData == null) return 0;
        EnsureCurrentWeek();
        int points = scorePerLevel + (firstTry ? firstTryBonus : 0);
        gameData.weeklyScore += points;
        gameData.totalScore += points;
        Save();
        OnRankChanged?.Invoke();
        return points;
    }

    public int WeeklyScore { get { EnsureCurrentWeek(); return gameData.weeklyScore; } }
    public int TotalScore => gameData?.totalScore ?? 0;
    // Kết quả tuần trước (-1 nếu chưa có), dùng cho popup thưởng tuần sau này.
    public int LastWeekRank => gameData?.lastWeekRank ?? -1;
    public int LastWeekScore => gameData?.lastWeekScore ?? -1;

    // =========================================================
    // BẢNG XẾP HẠNG
    // =========================================================

    // Bảng đua tuần đã sắp xếp (gồm người chơi). playerRank là hạng thật, kể cả khi ngoài danh sách.
    public List<RankEntry> GetWeekly(out int playerRank)
    {
        EnsureCurrentWeek();
        float progress = WeekProgress(Now);
        var entries = new List<RankEntry>(gameData.weeklyRivals.Count + 1);
        foreach (Rival rival in gameData.weeklyRivals)
            entries.Add(new RankEntry { name = rival.name, avatar = rival.avatar, score = WeeklyRivalScore(rival, progress) });
        return Finish(entries, gameData.weeklyScore, weeklyPopulation, out playerRank);
    }

    // Bảng xếp hạng tổng World hoặc Country.
    public List<RankEntry> GetRanking(bool world, out int playerRank)
    {
        if (gameData.worldRivals.Count == 0 || gameData.countryRivals.Count == 0) CreateRankRivals();
        double days = Math.Max(0, (Now - DateTime.FromBinary(gameData.installTimeLong)).TotalDays);
        List<Rival> rivals = world ? gameData.worldRivals : gameData.countryRivals;
        var entries = new List<RankEntry>(rivals.Count + 1);
        foreach (Rival rival in rivals)
            entries.Add(new RankEntry
            {
                name = rival.name, avatar = rival.avatar,
                // Đối thủ chơi đều đặn mỗi ngày, tròn theo bậc điểm một màn.
                score = RoundToStep(rival.baseScore + rival.growth * days)
            });
        return Finish(entries, gameData.totalScore, world ? worldPopulation : countryPopulation, out playerRank);
    }

    // Thời gian còn lại của tuần đua hiện tại.
    public TimeSpan WeeklyRemaining()
    {
        TimeSpan left = WeekStart(Now).AddDays(7) - Now;
        return left < TimeSpan.Zero ? TimeSpan.Zero : left;
    }

    // Sắp xếp giảm dần, chèn người chơi (hòa điểm thì người chơi đứng trên) và ước lượng hạng ngoài danh sách.
    private List<RankEntry> Finish(List<RankEntry> entries, int playerScore, int population, out int playerRank)
    {
        entries.Sort((a, b) => b.score.CompareTo(a.score));
        int index = entries.FindIndex(entry => playerScore >= entry.score);
        var player = new RankEntry { name = PlayerName(), score = playerScore, isPlayer = true, avatar = -1 };
        if (index >= 0)
        {
            entries.Insert(index, player);
            playerRank = index + 1;
            return entries;
        }
        // Thấp hơn cả danh sách: ước lượng hạng trong số người tham gia, điểm càng gần càng sát top.
        int lowest = entries.Count > 0 ? entries[entries.Count - 1].score : 0;
        float ratio = lowest <= 0 ? 1f : Mathf.Clamp01((float)playerScore / lowest);
        int listed = entries.Count;
        playerRank = listed + 1 + Mathf.RoundToInt((population - listed - 1) * Mathf.Pow(1f - ratio, 1.6f));
        entries.Add(player);
        return entries;
    }

    // =========================================================
    // ĐUA TUẦN
    // =========================================================

    // Sang tuần mới: chốt kết quả tuần cũ, reset điểm tuần và sinh nhóm đối thủ mới.
    private void EnsureCurrentWeek()
    {
        if (gameData == null) return;
        DateTime weekStart = WeekStart(Now);
        long startKey = weekStart.ToBinary();
        if (gameData.weekStartLong == startKey && gameData.weeklyRivals.Count > 0) return;

        if (gameData.weekStartLong != 0 && gameData.weeklyRivals.Count > 0)
        {
            DateTime oldStart = DateTime.FromBinary(gameData.weekStartLong);
            bool lastWeek = oldStart.AddDays(7) == weekStart;
            if (oldStart < weekStart)
            {
                // Kết quả chung cuộc tuần cũ (đối thủ đã đạt điểm mục tiêu).
                var finals = new List<RankEntry>();
                foreach (Rival rival in gameData.weeklyRivals)
                    finals.Add(new RankEntry { score = WeeklyRivalScore(rival, 1f) });
                Finish(finals, gameData.weeklyScore, weeklyPopulation, out int rank);
                gameData.lastWeekRank = lastWeek ? rank : -1;
                gameData.lastWeekScore = lastWeek ? gameData.weeklyScore : 0;
            }
        }
        else
        {
            gameData.lastWeekRank = -1;
            gameData.lastWeekScore = -1;
        }

        gameData.weekStartLong = startKey;
        gameData.weeklyScore = 0;
        CreateWeeklyRivals(weekStart);
        Save();
        OnRankChanged?.Invoke();
    }

    // Sinh đối thủ tuần: mục tiêu bám theo nhịp chơi tuần trước của người chơi để cuộc đua luôn sát sao.
    private void CreateWeeklyRivals(DateTime weekStart)
    {
        var random = new System.Random(gameData.seed ^ (weekStart.Year * 1000 + weekStart.DayOfYear));
        int pace = gameData.lastWeekScore > 0
            ? Mathf.Clamp(gameData.lastWeekScore, defaultWeeklyPace / 2, defaultWeeklyPace * 4)
            : defaultWeeklyPace;
        float leader = pace * weeklyLeaderFactor;
        var names = new NamePicker(random);
        gameData.weeklyRivals.Clear();
        for (int i = 0; i < weeklyRivalCount; i++)
        {
            float t = weeklyRivalCount <= 1 ? 0f : i / (float)(weeklyRivalCount - 1);
            // Top đông người bám đuổi, cuối bảng thưa dần.
            float share = Mathf.Lerp(1f, 0.06f, Mathf.Pow(t, 0.75f)) * Range(random, 0.9f, 1.1f);
            gameData.weeklyRivals.Add(new Rival
            {
                name = names.Next(),
                avatar = random.Next(1000),
                targetScore = Mathf.Max(scorePerLevel, Mathf.RoundToInt(leader * share)),
                // curve < 1: chơi nhiều đầu tuần; > 1: dồn cuối tuần.
                curve = Range(random, 0.65f, 1.5f),
                // Một số người bắt đầu muộn vài giờ tới hơn một ngày.
                delay = random.NextDouble() < 0.35 ? Range(random, 0.02f, 0.18f) : 0f
            });
        }
    }

    // Điểm đối thủ tại tiến độ tuần progress (0..1), tăng đơn điệu theo bậc điểm một màn.
    private int WeeklyRivalScore(Rival rival, float progress)
    {
        float active = rival.delay >= 1f ? 0f : Mathf.Clamp01((progress - rival.delay) / (1f - rival.delay));
        float value = rival.targetScore * Mathf.Pow(active, rival.curve);
        return Mathf.FloorToInt(value / scorePerLevel) * scorePerLevel;
    }

    private static float WeekProgress(DateTime now) =>
        Mathf.Clamp01((float)((now - WeekStart(now)).TotalSeconds / TimeSpan.FromDays(7).TotalSeconds));

    // Thứ Hai 00:00 của tuần chứa thời điểm now.
    private static DateTime WeekStart(DateTime now)
    {
        int back = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return now.Date.AddDays(-back);
    }

    // =========================================================
    // XẾP HẠNG TỔNG
    // =========================================================

    // Đối thủ World / Country cố định theo seed cài đặt, điểm tăng theo ngày.
    private void CreateRankRivals()
    {
        gameData.worldRivals = CreateRankList(gameData.seed + 7001, worldTopScore, worldDailyGrowth);
        gameData.countryRivals = CreateRankList(gameData.seed + 7002, countryTopScore, countryDailyGrowth);
    }

    private List<Rival> CreateRankList(int seed, int topScore, int dailyGrowth)
    {
        var random = new System.Random(seed);
        var names = new NamePicker(random);
        var result = new List<Rival>(rankRivalCount);
        for (int i = 0; i < rankRivalCount; i++)
        {
            float t = rankRivalCount <= 1 ? 0f : i / (float)(rankRivalCount - 1);
            float share = Mathf.Lerp(1f, 0.05f, Mathf.Pow(t, 0.6f)) * Range(random, 0.92f, 1.08f);
            result.Add(new Rival
            {
                name = names.Next(),
                avatar = random.Next(1000),
                baseScore = Mathf.RoundToInt(topScore * share),
                // Người điểm cao chơi đều hơn; vẫn có người cuối bảng tăng nhanh để bảng thay đổi.
                growth = dailyGrowth * Mathf.Lerp(share, 1f, 0.3f) * Range(random, 0.5f, 1.5f)
            });
        }
        return result;
    }

    // =========================================================
    // HELPER
    // =========================================================

    private static DateTime Now => UnbiasedTime.TrueDateTime;

    private int RoundToStep(double value) =>
        Mathf.Max(0, (int)(value / scorePerLevel) * scorePerLevel);

    private static float Range(System.Random random, float min, float max) =>
        min + (float)random.NextDouble() * (max - min);

    private static int NewSeed() => Guid.NewGuid().GetHashCode() & 0x3FFFFFFF | 1;

    private static int CompletedLevels() =>
        GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer?.completedLevels != null
            ? GameDataFragment.cur.dataPlayer.completedLevels.Count : 0;

    private static string PlayerName()
    {
        string name = ProfileDataFragment.cur != null ? ProfileDataFragment.cur.gameData?.userName : null;
        return string.IsNullOrEmpty(name) ? "You" : name;
    }

    // Lấy tên ngẫu nhiên không trùng trong cùng bảng và khác tên người chơi.
    private sealed class NamePicker
    {
        private readonly System.Random random;
        private readonly List<string> pool;
        private int round;

        public NamePicker(System.Random random)
        {
            this.random = random;
            List<string> source = ProfileDataFragment.cur != null ? ProfileDataFragment.cur.names : null;
            pool = source != null && source.Count > 0 ? new List<string>(source) : new List<string> { "Player" };
            pool.Remove(PlayerName());
            if (pool.Count == 0) pool.Add("Player");
        }

        public string Next()
        {
            if (pool.Count == 0) return "Player";
            int index = random.Next(pool.Count);
            string name = pool[index];
            pool.RemoveAt(index);
            // Hết tên: thêm hậu tố số để vẫn không trùng.
            if (pool.Count == 0)
            {
                round++;
                List<string> source = ProfileDataFragment.cur != null ? ProfileDataFragment.cur.names : null;
                if (source != null) foreach (string item in source) pool.Add(item + round);
            }
            return name;
        }
    }

    // =========================================================
    // DATA
    // =========================================================

    [Serializable]
    public class Rival
    {
        public string name;
        public int avatar;
        // Weekly
        public int targetScore;
        public float curve = 1f;
        public float delay;
        // World / Country
        public int baseScore;
        public float growth;
    }

    [Serializable]
    public class Data
    {
        public int seed;
        public long installTimeLong;

        public long weekStartLong;
        public int weeklyScore;
        public int totalScore;

        public int lastWeekRank = -1;
        public int lastWeekScore = -1;

        public List<Rival> weeklyRivals = new List<Rival>();
        public List<Rival> worldRivals = new List<Rival>();
        public List<Rival> countryRivals = new List<Rival>();
    }
}
