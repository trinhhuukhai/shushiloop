using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Only the building track moves; the Home header, navigation and campaign stay in place.
public sealed class SushiRestaurantCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CanvasMainMenu menu;
    public SushiRestaurantCatalog catalog;
    public RectTransform track;
    public SushiDecorationRestaurantView[] views;
    public CanvasGroup[] buildingGroups;
    public GameObject[] closedSigns;
    public Button previousButton, nextButton;
    public TMP_Text title, pageLabel, lockDescription;
    public GameObject lockedPanel, homeActions;
    public RectTransform navigation;
    private const float PageWidth = 1100;
    private Tween transition;
    private Vector2 dragStart;
    private bool dragging;
    private int selected;
    public int SelectedIndex => selected;
    public SushiRestaurantCatalog.Restaurant Selected => catalog.restaurants[selected];
    public bool IsUnlocked => SushiRestaurantCatalog.IsUnlocked(GameDataFragment.cur?.dataPlayer, Selected);
    public bool IsAnimating => transition != null && transition.IsActive() && transition.IsPlaying();
    private bool CanBrowse => isActiveAndEnabled && menu.SelectedTab == 1 && !menu.IsTabAnimating &&
        !menu.restaurantLayout.IsExpanded && !(UIManager.Ins != null && UIManager.Ins.IsOpened<CanvasDecoration>());

    public void RestoreSelection()
    {
        string id = GameDataFragment.cur?.dataPlayer?.selectedRestaurantId;
        int index = System.Array.FindIndex(catalog.restaurants, restaurant => restaurant.id == id);
        selected = Mathf.Clamp(index, 0, views.Length - 1);
        Settle();
        Refresh();
    }

    public void Settle()
    {
        transition?.Kill(); transition = null; dragging = false;
        track.anchoredPosition = new Vector2(-selected * PageWidth, 0);
    }

    public void Refresh()
    {
        var player = GameDataFragment.cur?.dataPlayer;
        menu.decorationRestaurant = views[selected];
        for (int i = 0; i < views.Length; i++)
        {
            bool unlocked = SushiRestaurantCatalog.IsUnlocked(player, catalog.restaurants[i]);
            views[i].Show(SushiDecorationProgress.Built(player, catalog.decorations, catalog.restaurants[i].id));
            buildingGroups[i].alpha = unlocked ? 1 : 0.58f;
            closedSigns[i].SetActive(!unlocked);
        }
        title.text = Selected.nameVi;
        pageLabel.text = "NHÀ HÀNG " + (selected + 1) + " / " + views.Length;
        previousButton.interactable = selected > 0 && CanBrowse;
        nextButton.interactable = selected < views.Length - 1 && CanBrowse;
        previousButton.gameObject.SetActive(selected > 0);
        nextButton.gameObject.SetActive(selected < views.Length - 1);
        lockDescription.text = "Vượt qua level " + Selected.unlockAfterLevel + " của nhà hàng trước để mở khóa.";
        if (!menu.restaurantLayout.IsExpanded)
        {
            lockedPanel.SetActive(!IsUnlocked);
            homeActions.SetActive(IsUnlocked);
        }
    }

    public void Previous() => Move(-1);
    public void Next() => Move(1);
    private void Move(int direction)
    {
        if (!CanBrowse) return;
        int next = Mathf.Clamp(selected + direction, 0, views.Length - 1);
        transition?.Kill();
        dragging = false;
        bool changed = selected != next;
        selected = next;
        transition = track.DOAnchorPosX(-selected * PageWidth, 0.32f).SetEase(Ease.InOutCubic).SetUpdate(true);
        if (changed && GameDataFragment.cur?.dataPlayer != null)
        {
            GameDataFragment.cur.dataPlayer.selectedRestaurantId = Selected.id;
            GameDataFragment.cur.Save();
        }
        menu.RefreshLevelMap();
    }

    public void OnBeginDrag(PointerEventData data)
    {
        if (!CanBrowse || data.button != PointerEventData.InputButton.Left) return;
        transition?.Kill();
        dragging = RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            data.pressPosition, data.pressEventCamera, out dragStart);
    }

    public void OnDrag(PointerEventData data)
    {
        if (!dragging || !CanBrowse) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            data.position, data.pressEventCamera, out var point);
        Vector2 delta = point - dragStart;
        if (Mathf.Abs(delta.x) <= Mathf.Abs(delta.y)) return;
        float target = -selected * PageWidth + delta.x;
        track.anchoredPosition = new Vector2(Mathf.Clamp(target, -(views.Length - 1) * PageWidth - 80, 80), 0);
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            data.position, data.pressEventCamera, out var point);
        Vector2 delta = point - dragStart;
        dragging = false;
        int direction = Mathf.Abs(delta.x) >= 110 && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x < 0 ? 1 : -1) : 0;
        Move(direction);
    }

    private void LateUpdate() => RefreshLayout();
    public void RefreshLayout()
    {
        if (navigation == null || menu.restaurantLayout.preview == null) return;
        var preview = menu.restaurantLayout.preview;
        navigation.anchoredPosition = preview.anchoredPosition;
        navigation.sizeDelta = new Vector2(Mathf.Min(((RectTransform)navigation.parent).rect.width - 36,
            preview.rect.width * preview.localScale.x + 110), 100);
    }

    private void OnDisable()
    {
        transition?.Kill(); transition = null; dragging = false;
        if (track != null) track.anchoredPosition = new Vector2(-selected * PageWidth, 0);
    }
}
