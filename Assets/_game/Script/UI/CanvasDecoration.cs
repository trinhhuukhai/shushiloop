using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A bottom drawer over Home, sharing its live restaurant instead of owning a second preview.
public sealed class CanvasDecoration : CanvasHomePage
{
    [System.NonSerialized] public SushiDecorationRestaurantView restaurant;
    public TMP_Text progressText, statusText, rewardText, completionRewardText;
    public Image progressFill;
    public Button buildButton;
    public Button[] itemButtons;
    public TMP_Text[] itemLabels;
    public Image[] itemCards;
    public CanvasGroup[] itemArt;
    public GameObject completionPanel;
    public ScrollRect itemScroll;
    public CanvasGroup sheetGroup;
    private Tween sheetTween;
    private string expectedId;
    private string restaurantId = SushiRestaurantCatalog.FirstRestaurantId;
    private bool building;
    private Coroutine animation;
    private int lastBuilt = -1, lastStars = -1;
    public bool IsBuilding => building;
    public bool IsAnimating => sheetTween != null && sheetTween.IsActive() && sheetTween.IsPlaying();

    public override void Open()
    {
        var menu = UIManager.Ins.GetUI<CanvasMainMenu>();
        if (!menu.IsRestaurantUnlocked) { CloseDirectly(); return; }
        restaurantId = menu.SelectedRestaurantId;
        restaurant = menu.decorationRestaurant;
        base.Open();
        menu.SetUpgradeMode(true);
        sheetTween?.Kill();
        Canvas.ForceUpdateCanvases();
        pageContent.anchoredPosition = new Vector2(0, -pageContent.rect.height - 20);
        sheetGroup.interactable = false;
        sheetTween = pageContent.DOAnchorPosY(0, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true)
            .OnComplete(() => sheetGroup.interactable = true);
        FocusNextItem();
    }

    protected override void Refresh() => RefreshItems();

    public void RefreshItems()
    {
        var catalog = SushiDecorationProgress.Catalog;
        var player = GameDataFragment.cur?.dataPlayer;
        if (catalog == null) return;
        int built = SushiDecorationProgress.Built(player, catalog, restaurantId);
        int stars = SushiDecorationProgress.AvailableStars(player, catalog);
        bool progressChanged = lastBuilt != built;
        lastBuilt = built; lastStars = stars;
        bool complete = built == catalog.items.Length;
        expectedId = complete ? null : catalog.items[built].id;
        if (restaurant != null) restaurant.Show(built);
        progressText.text = "Tiến độ xây  " + built + "/" + catalog.items.Length;
        progressFill.fillAmount = built / (float)catalog.items.Length;
        int missing = complete ? 0 : Mathf.Max(0, catalog.items[built].starCost - stars);
        statusText.text = building ? "Đang nâng cấp..." : complete ? "Nhà hàng của bạn đã hoàn thiện!" :
            missing > 0 ? "Thắng thêm " + missing + " màn để nâng cấp " + catalog.items[built].nameVi.ToLower() + "." :
            "Tiếp theo: " + catalog.items[built].nameVi;
        rewardText.text = "Hoàn thành nhà hàng: +" + catalog.completionCoins.ToString("N0") + " xu";
        rewardText.gameObject.SetActive(!complete);
        completionPanel.SetActive(complete);
        completionRewardText.text = "Nhà hàng hoàn thiện  •  +" + catalog.completionCoins.ToString("N0") + " xu";
        for (int i = 0; i < itemButtons.Length; i++)
        {
            bool next = i == built;
            itemButtons[i].interactable = next && !building && missing == 0 &&
                DataManager.ins != null && DataManager.ins.isLoaded;
            itemLabels[i].text = i < built ? "ĐÃ XÂY" : catalog.items[i].starCost + " sao  •  " + (next ? "Nâng cấp" : "Chưa mở");
            itemLabels[i].color = next && missing > 0 ? new Color(0.7f, 0.21f, 0.17f) : new Color(0.21f, 0.32f, 0.24f);
            itemCards[i].color = i < built ? new Color(0.46f, 0.66f, 0.46f) :
                next ? new Color(1, 0.77f, 0.28f) : new Color(0.5f, 0.57f, 0.6f);
            itemArt[i].alpha = i <= built ? 1 : 0.45f;
        }
        buildButton = complete ? null : itemButtons[built];
        if (progressChanged && !building) FocusNextItem();
    }

    private void Update()
    {
        var catalog = SushiDecorationProgress.Catalog;
        var player = GameDataFragment.cur?.dataPlayer;
        if (!building && (lastBuilt != SushiDecorationProgress.Built(player, catalog, restaurantId) ||
            lastStars != SushiDecorationProgress.AvailableStars(player, catalog))) RefreshItems();
    }

    public void BuildItem(int index)
    {
        if (index == SushiDecorationProgress.Built(GameDataFragment.cur?.dataPlayer, SushiDecorationProgress.Catalog, restaurantId)) BuildNext();
    }

    public void BuildNext()
    {
        if (building || buildButton == null || !buildButton.interactable || !sheetGroup.interactable) return;
        var data = GameDataFragment.cur;
        int index = SushiDecorationProgress.Built(data?.dataPlayer, SushiDecorationProgress.Catalog, restaurantId);
        if (!SushiDecorationProgress.TryBuild(data?.dataPlayer, SushiRestaurantCatalog.Current, restaurantId, expectedId, out _))
        { RefreshItems(); return; }
        data.Save();
        building = true;
        RefreshItems();
        UIManager.Ins.GetUI<CanvasMainMenu>().RefreshLevelMap();
        restaurant.Reveal(index);
        animation = StartCoroutine(FinishBuild());
    }

    private IEnumerator FinishBuild()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        building = false; animation = null;
        RefreshItems();
        FocusNextItem();
    }

    public void FocusNextItem()
    {
        if (itemScroll == null || itemScroll.content == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemScroll.content);
        var grid = itemScroll.content.GetComponent<GridLayoutGroup>();
        int built = SushiDecorationProgress.Built(GameDataFragment.cur?.dataPlayer, SushiDecorationProgress.Catalog, restaurantId);
        float max = Mathf.Max(0, itemScroll.content.rect.height - itemScroll.viewport.rect.height);
        float row = Mathf.Min(built, itemButtons.Length - 1) / 3;
        itemScroll.StopMovement();
        itemScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(row * (grid.cellSize.y + grid.spacing.y), 0, max));
    }

    public void ClosePanel()
    {
        if (!sheetGroup.interactable) return;
        sheetGroup.interactable = false;
        UIManager.Ins.GetUI<CanvasMainMenu>().SetUpgradeMode(false);
        sheetTween?.Kill();
        sheetTween = pageContent.DOAnchorPosY(-pageContent.rect.height - 20, 0.25f)
            .SetEase(Ease.InCubic).SetUpdate(true).OnComplete(CloseDirectly);
    }

    public override void BackKey() => ClosePanel();

    protected override void OnDisable()
    {
        sheetTween?.Kill(); sheetTween = null;
        if (animation != null) StopCoroutine(animation);
        animation = null; building = false;
        if (UIManager.Ins != null && UIManager.Ins.IsLoaded<CanvasMainMenu>())
            UIManager.Ins.GetUI<CanvasMainMenu>().SetUpgradeMode(false);
        base.OnDisable();
    }
}
