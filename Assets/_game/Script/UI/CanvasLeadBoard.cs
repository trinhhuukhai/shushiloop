using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Ranking: tab Weekly (bục top 3 + danh sách) và tab Rank (World / Country).
// Chưa có server bảng xếp hạng: đối thủ được sinh cố định theo tuần/khu vực, điểm người chơi lấy từ số màn đã qua.
public sealed class CanvasLeadBoard : CanvasHomePage
{
    private struct Entry
    {
        public string name;
        public int score;
        public Sprite avatar;
        public bool isPlayer;
    }

    [Header("Tab Weekly / Rank")]
    public Image weeklyTab;
    public Image rankTab;
    public Sprite tabSelected;
    public Sprite tabIdle;
    public GameObject weeklyView;
    public GameObject rankView;
    public TMP_Text timerText;

    [Header("World / Country")]
    public GameObject worldSelected;
    public GameObject countrySelected;

    [Header("Bục top 3 tuần (thứ tự: hạng 1, 2, 3)")]
    public Image[] podiumAvatars = new Image[3];
    public TMP_Text[] podiumNames = new TMP_Text[3];
    public TMP_Text[] podiumScores = new TMP_Text[3];

    [Header("Danh sách")]
    public SushiLeaderboardRow rowTemplate;
    public RectTransform weeklyList;
    public RectTransform rankList;
    public SushiLeaderboardRow weeklyPlayerRow;
    public SushiLeaderboardRow rankPlayerRow;
    [Min(48f)] public float rowSpacing = 192f;
    [Min(3)] public int listedPlayers = 30;
    [Tooltip("Ảnh huy chương hạng 1, 2, 3 cho danh sách.")]
    public Sprite[] medals = new Sprite[3];
    public Sprite[] avatars = Array.Empty<Sprite>();
    [Tooltip("Điểm mỗi màn đã hoàn thành.")]
    [Min(1)] public int scorePerLevel = 10;

    // Giữ tương thích với bố cục cũ.
    [HideInInspector] public TMP_Text progressValue;

    private readonly Dictionary<RectTransform, List<SushiLeaderboardRow>> rows = new Dictionary<RectTransform, List<SushiLeaderboardRow>>();
    private bool showWeekly = true;
    private bool showWorld = true;
    private float nextTimerUpdate;

    protected override void Refresh()
    {
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        ApplyTab();
    }

    protected override void Update()
    {
        base.Update();
        if (Time.unscaledTime < nextTimerUpdate) return;
        nextTimerUpdate = Time.unscaledTime + 1f;
        if (timerText != null) timerText.text = WeeklyRemaining();
    }

    // Nút tab Weekly.
    public void ShowWeekly() { showWeekly = true; ApplyTab(); }
    // Nút tab Rank.
    public void ShowRank() { showWeekly = false; ApplyTab(); }
    // Nút World.
    public void ShowWorld() { showWorld = true; ApplyTab(); }
    // Nút Country.
    public void ShowCountry() { showWorld = false; ApplyTab(); }

    // Đổi hình tab, bật đúng phần nội dung và dựng lại danh sách.
    private void ApplyTab()
    {
        SetTab(weeklyTab, showWeekly);
        SetTab(rankTab, !showWeekly);
        if (weeklyView != null) weeklyView.SetActive(showWeekly);
        if (rankView != null) rankView.SetActive(!showWeekly);
        if (worldSelected != null) worldSelected.SetActive(showWorld);
        if (countrySelected != null) countrySelected.SetActive(!showWorld);
        if (timerText != null) timerText.text = WeeklyRemaining();

        int playerScore = PlayerScore();
        if (showWeekly)
        {
            List<Entry> weekly = Generate(WeekSeed(), 900, playerScore);
            for (int i = 0; i < 3; i++) ShowPodium(i, i < weekly.Count ? weekly[i] : default);
            // Bục đã hiện top 3, danh sách bắt đầu từ hạng 4.
            Fill(weeklyList, weekly, 3);
            ShowPlayer(weeklyPlayerRow, weekly, playerScore);
        }
        else
        {
            List<Entry> ranking = Generate(showWorld ? 7001 : 7002, showWorld ? 5200 : 2600, playerScore);
            Fill(rankList, ranking, 0);
            ShowPlayer(rankPlayerRow, ranking, playerScore);
        }
    }

    // Tab đang chọn dùng khung sáng và cao hơn tab còn lại.
    private void SetTab(Image tab, bool selected)
    {
        if (tab == null) return;
        Sprite sprite = selected ? tabSelected : tabIdle;
        if (sprite == null) return;
        tab.sprite = sprite;
        tab.SetNativeSize();
    }

    // Hiển thị một vị trí trên bục top 3.
    private void ShowPodium(int index, Entry entry)
    {
        if (index < podiumNames.Length && podiumNames[index] != null) podiumNames[index].text = entry.name ?? "";
        if (index < podiumScores.Length && podiumScores[index] != null) podiumScores[index].text = "Score\n" + entry.score;
        if (index < podiumAvatars.Length && podiumAvatars[index] != null && entry.avatar != null) podiumAvatars[index].sprite = entry.avatar;
    }

    // Dựng các dòng của danh sách từ vị trí start, tái sử dụng dòng đã tạo.
    private void Fill(RectTransform list, List<Entry> entries, int start)
    {
        if (list == null || rowTemplate == null) return;
        if (!rows.TryGetValue(list, out var pool)) rows[list] = pool = new List<SushiLeaderboardRow>();
        int count = Mathf.Max(0, Mathf.Min(entries.Count, listedPlayers) - start);
        while (pool.Count < count)
        {
            SushiLeaderboardRow row = Instantiate(rowTemplate, list, false);
            row.name = "Row " + (pool.Count + start + 1);
            pool.Add(row);
        }
        for (int i = 0; i < pool.Count; i++)
        {
            bool visible = i < count;
            pool[i].gameObject.SetActive(visible);
            if (!visible) continue;
            int rank = start + i + 1;
            Entry entry = entries[start + i];
            var rect = (RectTransform)pool[i].transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -i * rowSpacing);
            pool[i].Show(rank.ToString(), Medal(rank), entry.avatar, entry.name, entry.score);
        }
        list.sizeDelta = new Vector2(list.sizeDelta.x, count * rowSpacing + rowSpacing * 0.6f);
    }

    // Dòng của người chơi ghim cuối danh sách; ngoài top hiển thị "999+".
    private void ShowPlayer(SushiLeaderboardRow row, List<Entry> entries, int score)
    {
        if (row == null) return;
        int rank = entries.FindIndex(entry => entry.isPlayer) + 1;
        string label = rank > 0 && rank <= listedPlayers ? rank.ToString() : "999+";
        row.Show(label, null, PlayerAvatar(), PlayerName(), score);
    }

    private Sprite Medal(int rank) => rank >= 1 && rank <= medals.Length ? medals[rank - 1] : null;

    // Sinh đối thủ cố định theo seed (giảm dần điểm) rồi chèn người chơi vào đúng thứ hạng.
    private List<Entry> Generate(int seed, int topScore, int playerScore)
    {
        var random = new System.Random(seed);
        IReadOnlyList<string> names = ProfileDataFragment.cur != null && ProfileDataFragment.cur.names != null &&
            ProfileDataFragment.cur.names.Count > 0 ? ProfileDataFragment.cur.names : (IReadOnlyList<string>)new[] { "William" };
        var result = new List<Entry>(listedPlayers + 1);
        int score = topScore;
        for (int i = 0; i < listedPlayers; i++)
        {
            result.Add(new Entry
            {
                name = names[random.Next(names.Count)],
                score = Mathf.Max(0, score),
                avatar = avatars.Length > 0 ? avatars[random.Next(avatars.Length)] : null
            });
            score -= 1 + random.Next(Mathf.Max(2, topScore / listedPlayers));
        }
        int index = result.FindIndex(entry => playerScore > entry.score);
        // Điểm thấp hơn cả danh sách: người chơi nằm ngoài top (hiện "999+").
        if (index >= 0)
            result.Insert(index, new Entry { name = PlayerName(), score = playerScore, avatar = PlayerAvatar(), isPlayer = true });
        return result;
    }

    private int PlayerScore()
    {
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        return (player?.completedLevels?.Count ?? 0) * scorePerLevel;
    }

    private static string PlayerName()
    {
        string name = ProfileDataFragment.cur != null ? ProfileDataFragment.cur.gameData.userName : null;
        return string.IsNullOrEmpty(name) ? "You" : name;
    }

    private Sprite PlayerAvatar()
    {
        var profile = ProfileDataFragment.cur;
        if (profile != null && profile.spr_avatars != null && profile.spr_avatars.Length > 0)
            return profile.spr_avatars[Mathf.Clamp(profile.gameData.idAvatar, 0, profile.spr_avatars.Length - 1)];
        return avatars.Length > 0 ? avatars[0] : null;
    }

    // Seed đổi theo tuần để đối thủ tuần mới khác tuần cũ.
    private static int WeekSeed()
    {
        DateTime monday = NextMonday().AddDays(-7);
        return monday.Year * 1000 + monday.DayOfYear;
    }

    private static DateTime NextMonday()
    {
        DateTime now = DateTime.Now;
        int days = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        return now.Date.AddDays(days == 0 ? 7 : days);
    }

    // Thời gian còn lại tới hết tuần, dạng "5d21h".
    private static string WeeklyRemaining()
    {
        TimeSpan left = NextMonday() - DateTime.Now;
        return left.Days > 0 ? $"{left.Days}d{left.Hours}h" : $"{left.Hours}h{left.Minutes}m";
    }
}
