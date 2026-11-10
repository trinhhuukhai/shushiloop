using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UICMainMenu prefab owns the shared header/navigation; pages only cover the middle.
public class CanvasMainMenu : UICanvas
{
    public RectTransform rootPanel;
    public RectTransform playButtonRect;
    public TMP_Text txtLevel;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text starText;
    public SushiDecorationRestaurantView decorationRestaurant;
    public TMP_Text decorationProgress;
    public GameObject restaurantCompleteBadge;
    public SushiHomeRestaurantLayout restaurantLayout;
    public SushiRestaurantCarousel restaurantCarousel;
    public string SelectedRestaurantId => restaurantCarousel != null ? restaurantCarousel.Selected.id : SushiRestaurantCatalog.FirstRestaurantId;
    public bool IsRestaurantUnlocked => restaurantCarousel == null || restaurantCarousel.IsUnlocked;
    [SerializeField] private TMP_Text profileLevel;
    [SerializeField] private Button playButton;
    [SerializeField] private RectTransform homeContent;
    [SerializeField] private RectTransform tabSelection;
    [SerializeField] private RectTransform[] tabIcons;
    [SerializeField] private TMP_Text[] tabLabels;
    [SerializeField] private GameObject profilePanel;
    [SerializeField, Range(0.15f, 0.6f)] private float tabDuration = 0.32f;

    [Header("Thanh tab kiểu mới (để trống tabButtons = kiểu cũ)")]
    [Tooltip("Nền từng tab Shop/Home/Ranking; tab đang chọn rộng hơn.")]
    [SerializeField] private RectTransform[] tabButtons;
    [SerializeField] private Sprite selectedTabSprite;
    [SerializeField] private Sprite idleTabSprite;
    [SerializeField] private float selectedTabWidth = 408f;
    [SerializeField] private float selectedIconY = 26f;
    [SerializeField] private float selectedIconScale = 1.16f;
    [SerializeField] private float idleIconScale = 0.88f;
    [SerializeField] private Color selectedLabelColor = new Color(1, 0.87f, 0.46f);
    [SerializeField] private Color idleLabelColor = new Color(0.84f, 0.9f, 1);
    [Tooltip("Header trang chủ (avatar, ví, mạng, sao); ẩn khi sang Shop/Ranking vì trang đó có header riêng.")]
    [SerializeField] private CanvasGroup headerGroup;

    [Header("Nút Decorate")]
    [SerializeField] private Image decorationFill;
    [SerializeField] private GameObject decorationBadge;
    [SerializeField] private TMP_Text decorationBadgeText;
    private Sequence tabTween;
    private int selectedTab = 1;
    private bool starting;
    private CanvasScaler menuRootScaler;
    private CanvasScaler.ScreenMatchMode previousMatchMode;
    private float previousWidthMatch;
    private Canvas hiddenCheatCanvas;
    private GraphicRaycaster hiddenCheatRaycaster;
    private bool cheatCanvasWasEnabled;
    private bool cheatRaycasterWasEnabled;
    public int SelectedTab => selectedTab;
    public bool IsTabAnimating => tabTween != null && tabTween.IsActive() && tabTween.IsPlaying();
    protected override bool BlocksGameplay => false;

    public TMP_Text txtLive;

    // Mở trang chủ, chọn tab chính và cập nhật tiền, level cùng trạng thái mạng.
    public override void Open()
    {
        base.Open();
        GameManager.ChangeState(GameState.MainMenu);
        ApplyMenuCanvasScale();
        //HideCheatOverlay();
        starting = false;
        tabTween?.Kill();
        ClosePages();
        selectedTab = 1;
        if (homeContent != null) { homeContent.gameObject.SetActive(true); homeContent.anchoredPosition = Vector2.zero; SetInteraction(homeContent, true); }
        if (profilePanel != null) profilePanel.SetActive(false);
        PositionTabs(false);
        FadeHeader(true, false);
        if (restaurantCarousel != null) restaurantCarousel.RestoreSelection();
        RefreshLevelMap();
        CheckMoneyTemp();

        StartCoroutine(IE_LivesUnlimited());
        StartCoroutine(IE_Lives());

    }

    // Lưu cấu hình CanvasScaler gốc và cho menu co giãn theo chiều rộng màn hình.
    private void ApplyMenuCanvasScale()
    {
        if (menuRootScaler != null) return;
        Canvas canvas = GetComponent<Canvas>();
        menuRootScaler = canvas != null ? canvas.rootCanvas.GetComponent<CanvasScaler>() : null;
        if (menuRootScaler == null) return;
        previousMatchMode = menuRootScaler.screenMatchMode;
        previousWidthMatch = menuRootScaler.matchWidthOrHeight;
        menuRootScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        menuRootScaler.matchWidthOrHeight = 0;
    }

    // Phục hồi chế độ co giãn canvas đã lưu khi rời menu.
    private void RestoreCanvasScale()
    {
        if (menuRootScaler == null) return;
        menuRootScaler.screenMatchMode = previousMatchMode;
        menuRootScaler.matchWidthOrHeight = previousWidthMatch;
        menuRootScaler = null;
    }

    // Lưu và tắt canvas cùng raycaster của lớp cheat.
    private void HideCheatOverlay()
    {
        if (hiddenCheatCanvas != null || UICheat.Ins == null) return;
        Canvas canvas = UICheat.Ins.GetComponentInParent<Canvas>();
        hiddenCheatCanvas = canvas != null ? canvas.rootCanvas : null;
        if (hiddenCheatCanvas == null) return;
        cheatCanvasWasEnabled = hiddenCheatCanvas.enabled;
        hiddenCheatRaycaster = hiddenCheatCanvas.GetComponent<GraphicRaycaster>();
        cheatRaycasterWasEnabled = hiddenCheatRaycaster != null && hiddenCheatRaycaster.enabled;
        hiddenCheatCanvas.enabled = false;
        if (hiddenCheatRaycaster != null) hiddenCheatRaycaster.enabled = false;
    }

    // Khôi phục trạng thái canvas và raycaster cheat đã lưu.
    private void RestoreCheatOverlay()
    {
        if (hiddenCheatCanvas != null) hiddenCheatCanvas.enabled = cheatCanvasWasEnabled;
        if (hiddenCheatRaycaster != null) hiddenCheatRaycaster.enabled = cheatRaycasterWasEnabled;
        hiddenCheatCanvas = null;
        hiddenCheatRaycaster = null;
    }

    // Đồng bộ thông tin level và tài nguyên trên trang chủ mỗi frame.
    private void Update() => RefreshLevelMap();

    // Cập nhật nhãn level, tiền, số màn đã hoàn thành và điều kiện bật nút chơi.
    public void RefreshLevelMap()
    {
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        int index = player != null ? player.cur_level : 0;
        int count = DataManager.ins != null && DataManager.ins.allLevelData != null
            ? DataManager.ins.allLevelData.listAllLevel.Count : 0;
        if (count > 0) index = Mathf.Clamp(index, 0, count - 1);
        if (txtLevel != null) txtLevel.text = "Level " + (index + 1);
        if (coinText != null) coinText.text = (player != null ? player.money : 0).ToString("N0");
        var decoration = SushiDecorationProgress.Catalog;
        if (restaurantCarousel != null) restaurantCarousel.Refresh();
        int built = SushiDecorationProgress.Built(player, decoration, SelectedRestaurantId);
        if (starText != null) starText.text = SushiDecorationProgress.AvailableStars(player, decoration).ToString();
        if (decorationRestaurant != null) decorationRestaurant.Show(built);
        int total = decoration != null ? decoration.items.Length : 12;
        if (decorationProgress != null) decorationProgress.text = built + "/" + total;
        if (decorationFill != null) decorationFill.fillAmount = total > 0 ? Mathf.Clamp01((float)built / total) : 0f;
        if (decorationBadge != null)
        {
            int stars = SushiDecorationProgress.AvailableStars(player, decoration);
            decorationBadge.SetActive(stars > 0);
            if (decorationBadgeText != null) decorationBadgeText.text = stars.ToString();
        }
        if (restaurantCompleteBadge != null) restaurantCompleteBadge.SetActive(IsRestaurantUnlocked && decoration != null && built == decoration.items.Length);
        if (profileLevel != null) profileLevel.text = "Level " + (index + 1) + "\nĐã hoàn thành " +
            (player?.completedLevels?.Count ?? 0) + " màn";
        if (playButton != null) playButton.interactable = !starting && IsRestaurantUnlocked && DataManager.ins != null && DataManager.ins.isLoaded && count > 0;
    }

    // Kiểm tra mạng và trạng thái nạp trước khi yêu cầu bắt đầu màn hiện tại.
    public void Play()
    {
        if (starting || !IsRestaurantUnlocked || GameManager.ins == null || playButton != null && !playButton.interactable) return;

        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        if (!isUnlimited && LivesDataFragment.cur.gameData.lives <= 0)
        {
            UIManager.Ins?.OpenUI<CanvasLive>();
            return;
        }

        starting = true;
        RefreshLevelMap();
        if (!GameManager.ins.StartCurrentLevel())
        {
            starting = false;
            RefreshLevelMap();
        }
        CloseDirectly();
    }

    // Xử lý chọn tab hợp lệ, đóng panel hồ sơ và chuyển trang nếu cần.
    public void SelectTab(int index)
    {
        if (starting || index < 0 || index > 2) return;
        if (UIManager.Ins != null) UIManager.Ins.CloseUI<CanvasDecoration>();
        if (profilePanel != null) profilePanel.SetActive(false);
        if (index == selectedTab) { PositionTabs(true); return; }
        SwitchPage(index);
    }

    // Trượt trang cũ và trang mới theo hướng tab, khóa thao tác trong lúc chuyển.
    private void SwitchPage(int index)
    {
        // Complete a previous transition first so rapid alternating taps cannot orphan a page.
        tabTween?.Kill(true);
        int previous = selectedTab;
        RectTransform outgoing = PageContent(previous);
        CanvasHomePage oldPage = Page(previous);
        CanvasHomePage newPage = Page(index);
        selectedTab = index;
        if (newPage != null) { newPage.Setup(); newPage.Open(); }
        RectTransform incoming = PageContent(index);
        if (incoming == null) return;
        incoming.gameObject.SetActive(true);
        float width = Mathf.Max(1, rootPanel.rect.width);
        int direction = index > previous ? 1 : -1;
        incoming.anchoredPosition = new Vector2(direction * width, 0);
        SetInteraction(outgoing, false);
        SetInteraction(incoming, false);
        tabTween = DOTween.Sequence().SetUpdate(true);
        if (outgoing != null)
            tabTween.Join(outgoing.DOAnchorPos(new Vector2(-direction * width, 0), tabDuration).SetEase(Ease.InOutCubic));
        tabTween.Join(incoming.DOAnchorPos(Vector2.zero, tabDuration).SetEase(Ease.InOutCubic));
        tabTween.OnComplete(() =>
        {
            if (oldPage != null) oldPage.CloseDirectly();
            else if (previous == 1 && homeContent != null) homeContent.gameObject.SetActive(false);
            SetInteraction(incoming, true);
        });
        PositionTabs(true, direction);
        FadeHeader(index == 1, true);
    }

    // Hiện header trang chủ chỉ ở tab Home; Shop/Ranking tự vẽ header riêng.
    private void FadeHeader(bool visible, bool animate)
    {
        if (headerGroup == null) return;
        headerGroup.DOKill();
        headerGroup.interactable = headerGroup.blocksRaycasts = visible;
        float alpha = visible ? 1f : 0f;
        if (animate) headerGroup.DOFade(alpha, tabDuration * 0.6f).SetUpdate(true);
        else headerGroup.alpha = alpha;
    }

    // Bật hoặc tắt tương tác cùng chặn raycast của nội dung trang qua CanvasGroup.
    private static void SetInteraction(RectTransform content, bool value)
    {
        if (content == null) return;
        var group = content.GetComponent<CanvasGroup>();
        if (group != null) group.interactable = group.blocksRaycasts = value;
    }

    // Lấy canvas trang con tương ứng với tab cửa hàng hoặc bảng xếp hạng.
    private CanvasHomePage Page(int index)
    {
        if (UIManager.Ins == null) return null;
        if (index == 0) return UIManager.Ins.GetUI<CanvasShop>();
        if (index == 2) return UIManager.Ins.GetUI<CanvasLeadBoard>();
        return null;
    }
    // Lấy RectTransform nội dung trang chủ hoặc trang con ứng với tab.
    private RectTransform PageContent(int index) => index == 1 ? homeContent : Page(index)?.pageContent;

    // Căn dấu chọn, biểu tượng và màu nhãn theo tab hiện tại, tùy chọn chạy tween.
    private void PositionTabs(bool animate, int direction = 0)
    {
        int selected = selectedTab == 3 ? 1 : selectedTab;
        float selectedX = (selected - 1) * 360f;
        bool resizable = tabButtons != null && tabButtons.Length == 3;
        if (resizable)
        {
            // Tab đang chọn rộng selectedTabWidth, hai tab còn lại chia đều phần còn lại của thanh.
            var bar = tabButtons[0].parent as RectTransform;
            float total = bar != null && bar.rect.width > 1f ? bar.rect.width : 1080f;
            float idleWidth = Mathf.Max(1f, (total - selectedTabWidth) * 0.5f);
            float left = -total * 0.5f;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                RectTransform tab = tabButtons[i];
                if (tab == null) continue;
                float width = i == selected ? selectedTabWidth : idleWidth;
                float x = left + width * 0.5f;
                left += width;
                if (i == selected) selectedX = x;
                var image = tab.GetComponent<Image>();
                Sprite sprite = i == selected ? selectedTabSprite : idleTabSprite;
                if (image != null && sprite != null) image.sprite = sprite;
                tab.DOKill();
                Vector2 size = new Vector2(width, tab.sizeDelta.y);
                if (animate)
                {
                    tab.DOAnchorPosX(x, tabDuration).SetEase(Ease.InOutCubic).SetUpdate(true);
                    tab.DOSizeDelta(size, tabDuration).SetEase(Ease.InOutCubic).SetUpdate(true);
                }
                else { tab.anchoredPosition = new Vector2(x, tab.anchoredPosition.y); tab.sizeDelta = size; }
            }
        }
        if (tabSelection != null)
        {
            tabSelection.DOKill();
            float targetX = selectedX;
            if (animate) tabSelection.DOAnchorPosX(targetX, tabDuration).SetEase(Ease.InOutCubic).SetUpdate(true);
            else tabSelection.anchoredPosition = new Vector2(targetX, tabSelection.anchoredPosition.y);
        }
        for (int i = 0; i < tabIcons.Length; i++)
        {
            RectTransform icon = tabIcons[i];
            icon.DOKill();
            Vector2 target = new Vector2(0, i == selected ? selectedIconY : 0);
            Vector3 scale = Vector3.one * (i == selected ? selectedIconScale : idleIconScale);
            if (animate)
            {
                if (i == selected && direction != 0)
                    icon.anchoredPosition = new Vector2(-direction * 42f, icon.anchoredPosition.y);
                icon.DOAnchorPos(target, tabDuration).SetEase(Ease.OutBack).SetUpdate(true);
                icon.DOScale(scale, tabDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else { icon.anchoredPosition = target; icon.localScale = scale; }
            if (i < tabLabels.Length && tabLabels[i] != null)
                tabLabels[i].color = i == selected ? selectedLabelColor : idleLabelColor;
        }
    }

    // Chọn tab cửa hàng.
    public void OpenShop() => SelectTab(0);
    // Chọn tab trang chủ.
    public void OpenHome() => SelectTab(1);
    // Chọn tab bảng xếp hạng.
    public void OpenLeaderboard() => SelectTab(2);
    // Open a drawer over Home. The building and Home tab stay active throughout upgrades.
    public void OpenDecoration()
    {
        if (starting || !IsRestaurantUnlocked || UIManager.Ins == null || UIManager.Ins.IsOpened<CanvasDecoration>()) return;
        tabTween?.Kill(true);
        if (selectedTab != 1) { SelectTab(1); tabTween?.Kill(true); }
        UIManager.Ins.OpenUI<CanvasDecoration>();
    }

    public void SetUpgradeMode(bool value)
    {
        if (value && restaurantCarousel != null) restaurantCarousel.Settle();
        if (restaurantLayout != null) restaurantLayout.SetExpanded(value, isActiveAndEnabled);
    }
    // Mở popup cài đặt từ trang chủ.
    public void OpenSettings() => UIManager.Ins?.OpenUI<CanvasSetting>();
    // Mở popup quà đăng nhập hằng ngày.
    public void OpenDailYLogin() => UIManager.Ins?.OpenUI<CanvasDailyLogin>();
    // Mở popup chuỗi phần thưởng hằng ngày.
    public void OpenDailyReward() => UIManager.Ins?.OpenUI<CanvasDailyReward>();
    // Mở popup vòng quay thưởng.
    public void OpenDailySpin() => UIManager.Ins?.OpenUI<CanvasSpin>();
    // Mở popup hồ sơ người chơi.
    public void OpenProfile() => UIManager.Ins?.OpenUI<CanvasProfile>();
    // Mở popup mạng trừ trường hợp vừa vô hạn mạng vừa đã đủ năm mạng.
    public void OpenLive()
    {
        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        if (isUnlimited && LivesDataFragment.cur.gameData.lives == 5) return;
        UIManager.Ins?.OpenUI<CanvasLive>();

    }
    // Ẩn panel hồ sơ gắn trong menu.
    public void CloseProfile() { if (profilePanel != null) profilePanel.SetActive(false); }
    // Đóng panel hồ sơ nếu đang mở, nếu không thì quay về tab trang chủ.
    public override void BackKey()
    {
        if (profilePanel != null && profilePanel.activeSelf) CloseProfile();
        else SelectTab(1);
    }

    // Đóng các canvas trang con thuộc menu.
    private void ClosePages()
    {
        if (UIManager.Ins == null) return;
        UIManager.Ins.CloseUI<CanvasShop>();
        UIManager.Ins.CloseUI<CanvasLeadBoard>();
        UIManager.Ins.CloseUI<CanvasDecoration>();
    }
    // Dừng hiệu ứng tab, đóng trang con, phục hồi co giãn canvas và dừng cập nhật mạng.
    protected override void OnDisable()
    {
        tabTween?.Kill();
        tabSelection?.DOKill();
        headerGroup?.DOKill();
        foreach (RectTransform icon in tabIcons) if (icon != null) icon.DOKill();
        if (tabButtons != null) foreach (RectTransform tab in tabButtons) if (tab != null) tab.DOKill();
        ClosePages();
        RestoreCanvasScale();
        //RestoreCheatOverlay();
        base.OnDisable();

        if (coLives != null)
            StopCoroutine(coLives);
        if (coLivesUnlimited != null)
            StopCoroutine(coLivesUnlimited);
    

}
    // Chuyển thưởng tiền tạm chưa nhận vào ví và xóa khoản tạm để tránh nhận lại.
    public void CheckMoneyTemp()
    {
        var data = GameDataFragment.cur;
        if (data?.dataPlayer == null || data.dataPlayer.moneyTemp <= 0) return;
        int reward = data.dataPlayer.moneyTemp;
        data.dataPlayer.moneyTemp = 0;
        data.ChangeMoney(reward, "home");
    }
    // Đóng trực tiếp trang chủ.
    public void Close() => CloseDirectly();
    // Chuyển điểm gọi UI cũ sang thao tác mở cửa hàng.
    public void openShop() => OpenShop();
    // Chuyển điểm gọi thư viện cũ sang trang trang trí.
    public void openGalary() => OpenDecoration();
    // Làm mới thông tin level sau khi người chơi thắng màn.
    public void OnWinLevel() => RefreshLevelMap();
    // Đồng bộ thông tin menu theo level hiện tại; hiện không có thao tác cuộn bản đồ.
    public void RecenterToCurrentLevel() => RefreshLevelMap();

    [Header("Lives")]
    public TMP_Text txtLives;
    public TMP_Text txtLivesTime;
    //public ParticleImage effLives;

    public GameObject btnPlus;
    public Button addPlus;
    public GameObject FrmHeartUnlimited;
    public GameObject FrmHeartNormal;
    public TMP_Text txtLivesTimeUnlimited;


    private Coroutine coLives;
    private Coroutine coLivesUnlimited;

    // Cập nhật số mạng và thời gian hồi mạng mỗi giây.
    IEnumerator IE_Lives()
    {
        while (true)
        {
            ShowLives();

            yield return Yielders.Get(1f);
        }
    }

    // Cập nhật trạng thái cùng thời gian vô hạn mạng mỗi giây.
    IEnumerator IE_LivesUnlimited()
    {
        while (true)
        {
            ShowLivesUnlimited();
            yield return Yielders.Get(1f);
        }
    }

    // Làm mới số mạng, nhãn FULL hoặc thời gian hồi và trạng thái nút bổ sung mạng.
    public void ShowLives()
    {
        var data = LivesDataFragment.cur;

        data.RefreshLives();

        txtLives.text = data.gameData.lives.ToString();

        if (data.gameData.lives >= LivesDataFragment.MAX_LIVES)
        {
            txtLivesTime.text = "FULL";
            btnPlus.SetActive(false);
            addPlus.interactable = false;
        }
        else
        {
            btnPlus.SetActive(true);
            txtLivesTime.gameObject.SetActive(true);
            addPlus.interactable = true;

            txtLivesTime.text =
                data.GetRemainTime().ToTimeFormat_H_M_S();
        }
    }

    // Chọn khung mạng thường hoặc vô hạn và hiển thị thời gian vô hạn còn lại.
    public void ShowLivesUnlimited()
    {
        var data = LivesDataFragment.cur;

        if (data == null)
            return;

        bool isUnlimited = data.IsUnlimitedLives();

        if (isUnlimited)
        {
            FrmHeartUnlimited.SetActive(true);
            FrmHeartNormal.SetActive(false);

            txtLivesTimeUnlimited.gameObject.SetActive(true);

            // Không dùng ToTimeFormat_H_M_S()
            // vì extension đó có thể reset Hours sau 24h.
            txtLivesTimeUnlimited.text =
                data.GetUnlimitedRemainTimeText();
        }
        else
        {
            FrmHeartUnlimited.SetActive(false);
            FrmHeartNormal.SetActive(true);

            txtLivesTimeUnlimited.gameObject.SetActive(false);
        }
    }



}
