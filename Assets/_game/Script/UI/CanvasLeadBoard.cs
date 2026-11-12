using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Ranking: tab Weekly (bục top 3 + danh sách) và tab Rank (World / Country).
// Dữ liệu lấy từ RankDataFragment: điểm thật của người chơi, đối thủ giả lập điểm tăng dần như một cuộc đua.
public sealed class CanvasLeadBoard : CanvasHomePage
{
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
    [Tooltip("Avatar cho đối thủ; để trống thì dùng avatar trong Profile Fragment.")]
    public Sprite[] avatars = Array.Empty<Sprite>();
    [Tooltip("Chu kỳ (giây) làm mới bảng khi đang mở để thấy đối thủ tăng điểm.")]
    [Min(5f)] public float liveRefreshInterval = 30f;

    // Giữ tương thích với bố cục cũ.
    [HideInInspector] public TMP_Text progressValue;

    private readonly Dictionary<RectTransform, List<SushiLeaderboardRow>> rows = new Dictionary<RectTransform, List<SushiLeaderboardRow>>();
    private bool showWeekly = true;
    private bool showWorld = true;
    private float nextTimerUpdate;
    private float nextListUpdate;
    // Vị trí thật của người chơi trong bảng (dòng danh sách hoặc ô trên bục); null nếu ngoài danh sách.
    private RectTransform playerTarget;
    private readonly Vector3[] corners = new Vector3[4];

    protected override void Refresh()
    {
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        ApplyTab();
    }

    protected override void Update()
    {
        base.Update();
        if (Time.unscaledTime >= nextListUpdate) ApplyTab();
        if (Time.unscaledTime < nextTimerUpdate) return;
        nextTimerUpdate = Time.unscaledTime + 1f;
        if (timerText != null) timerText.text = WeeklyRemaining();
    }

    // Sau khi ScrollRect cập nhật vị trí: ẩn/hiện dòng ghim của người chơi theo vị trí cuộn.
    private void LateUpdate()
    {
        UpdatePinnedRow(showWeekly ? weeklyPlayerRow : rankPlayerRow);
    }

    // Dòng ghim (ô xanh) chỉ hiện khi dòng thật của người chơi còn nằm bên dưới chỗ ghim;
    // cuộn tới (hoặc quá) vị trí đó thì ẩn, để dòng thật trong danh sách thay chỗ như các game khác.
    private void UpdatePinnedRow(SushiLeaderboardRow pinned)
    {
        if (pinned == null) return;
        bool show = true;
        if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
        {
            playerTarget.GetWorldCorners(corners);
            float targetBottom = corners[0].y;
            ((RectTransform)pinned.transform).GetWorldCorners(corners);
            float pinnedBottom = corners[0].y;
            // Ngưỡng nhỏ để không nhấp nháy khi dòng thật đứng đúng chỗ ghim.
            show = targetBottom < pinnedBottom - 1f;
        }
        if (pinned.gameObject.activeSelf != show) pinned.gameObject.SetActive(show);
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
        nextListUpdate = Time.unscaledTime + liveRefreshInterval;
        SetTab(weeklyTab, showWeekly);
        SetTab(rankTab, !showWeekly);
        if (weeklyView != null) weeklyView.SetActive(showWeekly);
        if (rankView != null) rankView.SetActive(!showWeekly);
        if (worldSelected != null) worldSelected.SetActive(showWorld);
        if (countrySelected != null) countrySelected.SetActive(!showWorld);
        if (timerText != null) timerText.text = WeeklyRemaining();

        playerTarget = null;
        var rank = RankDataFragment.cur;
        if (rank == null || rank.gameData == null) return;
        if (showWeekly)
        {
            List<RankDataFragment.RankEntry> weekly = rank.GetWeekly(out int playerRank);
            for (int i = 0; i < 3; i++) ShowPodium(i, i < weekly.Count ? weekly[i] : default);
            // Người chơi ở top 3: vị trí thật là ô trên bục.
            if (playerRank >= 1 && playerRank <= 3 && playerRank <= podiumAvatars.Length && podiumAvatars[playerRank - 1] != null)
                playerTarget = podiumAvatars[playerRank - 1].rectTransform;
            // Bục đã hiện top 3, danh sách bắt đầu từ hạng 4.
            Fill(weeklyList, weekly, 3);
            ShowPlayer(weeklyPlayerRow, playerRank, rank.WeeklyScore);
        }
        else
        {
            List<RankDataFragment.RankEntry> ranking = rank.GetRanking(showWorld, out int playerRank);
            Fill(rankList, ranking, 0);
            ShowPlayer(rankPlayerRow, playerRank, rank.TotalScore);
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
    private void ShowPodium(int index, RankDataFragment.RankEntry entry)
    {
        if (index < podiumNames.Length && podiumNames[index] != null) podiumNames[index].text = entry.name ?? "";
        if (index < podiumScores.Length && podiumScores[index] != null) podiumScores[index].text = "Score\n" + entry.score;
        if (index < podiumAvatars.Length && podiumAvatars[index] != null && entry.name != null)
        {
            Sprite sprite = Avatar(entry);
            if (sprite != null) podiumAvatars[index].sprite = sprite;
        }
    }

    // Dựng các dòng của danh sách từ vị trí start, tái sử dụng dòng đã tạo.
    private void Fill(RectTransform list, List<RankDataFragment.RankEntry> entries, int start)
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
            RankDataFragment.RankEntry entry = entries[start + i];
            var rect = (RectTransform)pool[i].transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -i * rowSpacing);
            pool[i].Show(rank.ToString(), Medal(rank), Avatar(entry), entry.name, entry.score);
            if (entry.isPlayer) playerTarget = rect;
        }
        list.sizeDelta = new Vector2(list.sizeDelta.x, count * rowSpacing + rowSpacing * 0.6f);
    }

    // Dòng của người chơi ghim cuối danh sách, luôn hiện hạng thật (kể cả ngoài top).
    private void ShowPlayer(SushiLeaderboardRow row, int rank, int score)
    {
        if (row == null) return;
        row.Show(rank > 0 ? rank.ToString() : "-", null, PlayerAvatar(), PlayerName(), score);
    }

    private Sprite Medal(int rank) => rank >= 1 && rank <= medals.Length ? medals[rank - 1] : null;

    // Avatar của một dòng: người chơi dùng avatar Profile, đối thủ chọn theo chỉ số cố định.
    private Sprite Avatar(RankDataFragment.RankEntry entry)
    {
        if (entry.isPlayer) return PlayerAvatar();
        Sprite[] pool = avatars.Length > 0 ? avatars : ProfileDataFragment.cur != null ? ProfileDataFragment.cur.spr_avatars : null;
        if (pool == null || pool.Length == 0) return null;
        return pool[Mathf.Abs(entry.avatar) % pool.Length];
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

    // Thời gian còn lại tới hết tuần đua, dạng "5d21h".
    private static string WeeklyRemaining()
    {
        if (RankDataFragment.cur == null || RankDataFragment.cur.gameData == null) return "";
        TimeSpan left = RankDataFragment.cur.WeeklyRemaining();
        return left.Days > 0 ? $"{left.Days}d{left.Hours}h" : $"{left.Hours}h{left.Minutes}m";
    }
}
