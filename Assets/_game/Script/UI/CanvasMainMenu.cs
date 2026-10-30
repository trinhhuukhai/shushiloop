using DG.Tweening;
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
    [SerializeField] private TMP_Text profileLevel;
    [SerializeField] private Button playButton;
    [SerializeField] private RectTransform homeContent;
    [SerializeField] private RectTransform tabSelection;
    [SerializeField] private RectTransform[] tabIcons;
    [SerializeField] private TMP_Text[] tabLabels;
    [SerializeField] private GameObject profilePanel;
    [SerializeField, Range(0.15f, 0.6f)] private float tabDuration = 0.32f;
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

    public override void Open()
    {
        base.Open();
        ApplyMenuCanvasScale();
        //HideCheatOverlay();
        starting = false;
        tabTween?.Kill();
        ClosePages();
        selectedTab = 1;
        if (homeContent != null) { homeContent.gameObject.SetActive(true); homeContent.anchoredPosition = Vector2.zero; SetInteraction(homeContent, true); }
        if (profilePanel != null) profilePanel.SetActive(false);
        PositionTabs(false);
        RefreshLevelMap();
        CheckMoneyTemp();
    }

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

    private void RestoreCanvasScale()
    {
        if (menuRootScaler == null) return;
        menuRootScaler.screenMatchMode = previousMatchMode;
        menuRootScaler.matchWidthOrHeight = previousWidthMatch;
        menuRootScaler = null;
    }

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

    private void RestoreCheatOverlay()
    {
        if (hiddenCheatCanvas != null) hiddenCheatCanvas.enabled = cheatCanvasWasEnabled;
        if (hiddenCheatRaycaster != null) hiddenCheatRaycaster.enabled = cheatRaycasterWasEnabled;
        hiddenCheatCanvas = null;
        hiddenCheatRaycaster = null;
    }

    private void Update() => RefreshLevelMap();

    public void RefreshLevelMap()
    {
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        int index = player != null ? player.cur_level : 0;
        int count = DataManager.ins != null && DataManager.ins.allLevelData != null
            ? DataManager.ins.allLevelData.listAllLevel.Count : 0;
        if (count > 0) index = Mathf.Clamp(index, 0, count - 1);
        if (txtLevel != null) txtLevel.text = "Level " + (index + 1);
        if (coinText != null) coinText.text = (player != null ? player.money : 0).ToString("N0");
        if (starText != null) starText.text = (player?.completedLevels?.Count ?? 0).ToString();
        if (profileLevel != null) profileLevel.text = "Level " + (index + 1) + "\nĐã hoàn thành " +
            (player?.completedLevels?.Count ?? 0) + " màn";
        if (playButton != null) playButton.interactable = !starting && DataManager.ins != null && DataManager.ins.isLoaded && count > 0;
    }

    public void Play()
    {
        if (starting || GameManager.ins == null || playButton != null && !playButton.interactable) return;
        starting = true;
        RefreshLevelMap();
        if (!GameManager.ins.StartCurrentLevel())
        {
            starting = false;
            RefreshLevelMap();
        }
        CloseDirectly();
    }

    public void SelectTab(int index)
    {
        if (starting || index < 0 || index > 2) return;
        if (profilePanel != null) profilePanel.SetActive(false);
        if (index == selectedTab) { PositionTabs(true); return; }
        SwitchPage(index);
    }

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
    }

    private static void SetInteraction(RectTransform content, bool value)
    {
        if (content == null) return;
        var group = content.GetComponent<CanvasGroup>();
        if (group != null) group.interactable = group.blocksRaycasts = value;
    }

    private CanvasHomePage Page(int index)
    {
        if (UIManager.Ins == null) return null;
        if (index == 0) return UIManager.Ins.GetUI<CanvasShop>();
        if (index == 2) return UIManager.Ins.GetUI<CanvasLeadBoard>();
        if (index == 3) return UIManager.Ins.GetUI<CanvasDecoration>();
        return null;
    }
    private RectTransform PageContent(int index) => index == 1 ? homeContent : Page(index)?.pageContent;

    private void PositionTabs(bool animate, int direction = 0)
    {
        int selected = selectedTab == 3 ? 1 : selectedTab;
        if (tabSelection != null)
        {
            tabSelection.DOKill();
            float targetX = (selected - 1) * 360f;
            if (animate) tabSelection.DOAnchorPosX(targetX, tabDuration).SetEase(Ease.InOutCubic).SetUpdate(true);
            else tabSelection.anchoredPosition = new Vector2(targetX, tabSelection.anchoredPosition.y);
        }
        for (int i = 0; i < tabIcons.Length; i++)
        {
            RectTransform icon = tabIcons[i];
            icon.DOKill();
            Vector2 target = new Vector2(0, i == selected ? 26 : 0);
            Vector3 scale = Vector3.one * (i == selected ? 1.16f : 0.88f);
            if (animate)
            {
                if (i == selected && direction != 0)
                    icon.anchoredPosition = new Vector2(-direction * 42f, icon.anchoredPosition.y);
                icon.DOAnchorPos(target, tabDuration).SetEase(Ease.OutBack).SetUpdate(true);
                icon.DOScale(scale, tabDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else { icon.anchoredPosition = target; icon.localScale = scale; }
            if (i < tabLabels.Length && tabLabels[i] != null)
                tabLabels[i].color = i == selected ? new Color(1, 0.87f, 0.46f) : new Color(0.84f, 0.9f, 1);
        }
    }

    public void OpenShop() => SelectTab(0);
    public void OpenHome() => SelectTab(1);
    public void OpenLeaderboard() => SelectTab(2);
    public void OpenDecoration() { if (!starting && selectedTab != 3) SwitchPage(3); }
    public void OpenSettings() => UIManager.Ins?.OpenUI<CanvasSetting>();
    public void OpenDailYLogin() => UIManager.Ins?.OpenUI<CanvasDailyLogin>();
    public void OpenDailyReward() => UIManager.Ins?.OpenUI<CanvasDailyReward>();
    public void OpenDailySpin() => UIManager.Ins?.OpenUI<CanvasSpin>();
    public void OpenProfile() => UIManager.Ins?.OpenUI<CanvasProfile>();
    public void OpenLive()
    {
        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        if (isUnlimited /*&& LivesDataFragment.cur.gameData.lives == 5*/) return;
        UIManager.Ins?.OpenUI<CanvasLive>();

    }
    public void CloseProfile() { if (profilePanel != null) profilePanel.SetActive(false); }
    public override void BackKey()
    {
        if (profilePanel != null && profilePanel.activeSelf) CloseProfile();
        else SelectTab(1);
    }

    private void ClosePages()
    {
        if (UIManager.Ins == null) return;
        UIManager.Ins.CloseUI<CanvasShop>();
        UIManager.Ins.CloseUI<CanvasLeadBoard>();
        UIManager.Ins.CloseUI<CanvasDecoration>();
    }
    protected override void OnDisable()
    {
        tabTween?.Kill();
        tabSelection?.DOKill();
        foreach (RectTransform icon in tabIcons) if (icon != null) icon.DOKill();
        ClosePages();
        RestoreCanvasScale();
        //RestoreCheatOverlay();
        base.OnDisable();
    }
    public void CheckMoneyTemp()
    {
        var data = GameDataFragment.cur;
        if (data?.dataPlayer == null || data.dataPlayer.moneyTemp <= 0) return;
        int reward = data.dataPlayer.moneyTemp;
        data.dataPlayer.moneyTemp = 0;
        data.ChangeMoney(reward, "home");
    }
    public void Close() => CloseDirectly();
    public void openShop() => OpenShop();
    public void openGalary() => OpenDecoration();
    public void OnWinLevel() => RefreshLevelMap();
    public void RecenterToCurrentLevel() => RefreshLevelMap();
}
