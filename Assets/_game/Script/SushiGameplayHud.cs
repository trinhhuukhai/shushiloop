using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// HUD mặc định giúp scene cũ dùng ngay tính năng mới; có thể tắt ở LevelManager.
public class SushiGameplayHud : MonoBehaviour
{
    private LevelManager level;
    private FoodInputController input;
    private GameObject canvasObject;
    private GameObject modal;
    private TextMeshProUGUI occupancy;
    private TextMeshProUGUI selection;
    private TextMeshProUGUI title;
    private TextMeshProUGUI coinText;
    private Button coins;
    private Button advertisement;
    private Button decline;
    private Button next;
    private GameObject levelSelector;
    private TextMeshProUGUI sampleTitle;
    private Button previousSample;
    private Button nextSample;
    private Button cancelBooster;
    private readonly System.Collections.Generic.List<Button> boosterButtons = new System.Collections.Generic.List<Button>();

    private void Start()
    {
        level = GetComponent<LevelManager>();
        input = FindFirstObjectByType<FoodInputController>();
        canvasObject = new GameObject("Sushi Gameplay HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem));
            events.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        RectTransform root = (RectTransform)canvasObject.transform;
        occupancy = Label(root, new Vector2(0, -45), new Vector2(500, 60), new Vector2(0.5f, 1), 34);
        selection = Label(root, new Vector2(0, 20), new Vector2(1000, 35), new Vector2(0.5f, 0), 22);
        occupancy.color = selection.color = Color.white;
        boosterButtons.Add(Button(root, "Cây gắp", new Vector2(-320, 85), new Vector2(280, 90), new Vector2(0.5f, 0), () => input?.SelectChopsticks()));
        boosterButtons.Add(Button(root, "Swap Row", new Vector2(0, 85), new Vector2(280, 90), new Vector2(0.5f, 0), () => input?.UseSwapRow()));
        boosterButtons.Add(Button(root, "Magnet", new Vector2(320, 85), new Vector2(280, 90), new Vector2(0.5f, 0), () => input?.SelectMagnet()));
        Button(root, "2×", new Vector2(-80, -65), new Vector2(115, 65), new Vector2(1, 1), () => level.Conveyor.ToggleSpeed());
        cancelBooster = Button(root, "Hủy", new Vector2(-80, -105), new Vector2(115, 55), new Vector2(1, 1), () => input?.CancelBooster());
        modal = Panel(root, "Result or revive", new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        RectTransform overlay = modal.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        GameObject card = Panel(overlay, "Card", new Color(0.97f, 0.88f, 0.68f), Vector2.zero, new Vector2(850, 620), new Vector2(0.5f, 0.5f));
        RectTransform cardRoot = card.GetComponent<RectTransform>();
        title = Label(cardRoot, new Vector2(0, 195), new Vector2(780, 150), new Vector2(0.5f, 0.5f), 38);
        coins = Button(cardRoot, "300 xu", new Vector2(-200, 0), new Vector2(350, 95), new Vector2(0.5f, 0.5f), () => level.TryReviveWithCoins());
        coinText = coins.GetComponentInChildren<TextMeshProUGUI>();
        advertisement = Button(cardRoot, "Quảng cáo", new Vector2(200, 0), new Vector2(350, 95), new Vector2(0.5f, 0.5f), () => level.TryReviveWithAd());
        decline = Button(cardRoot, "Thua level", new Vector2(0, -170), new Vector2(350, 95), new Vector2(0.5f, 0.5f), () =>
        {
            if (level.PendingRevive != SushiReviveReason.None) level.DeclineRevive();
            else level.ReplayLevel();
        });
        next = Button(cardRoot, "Level tiếp", Vector2.zero, new Vector2(350, 95), new Vector2(0.5f, 0.5f), () => level.NextLevel());
        modal.SetActive(false);
        // Nằm trên modal để có thể đổi màn mẫu cả khi đang xem popup hết giờ.
        levelSelector = new GameObject("Sample level selector", typeof(RectTransform));
        levelSelector.transform.SetParent(root, false);
        RectTransform selector = levelSelector.GetComponent<RectTransform>();
        selector.anchorMin = Vector2.zero;
        selector.anchorMax = Vector2.one;
        selector.offsetMin = selector.offsetMax = Vector2.zero;
        sampleTitle = Label(selector, new Vector2(0, -95), new Vector2(950, 45), new Vector2(0.5f, 1), 25);
        sampleTitle.color = Color.white;
        previousSample = Button(selector, "Màn trước", new Vector2(-275, -150), new Vector2(250, 60), new Vector2(0.5f, 1), () => level.PreviousLevel());
        Button(selector, "Chơi lại", new Vector2(0, -150), new Vector2(250, 60), new Vector2(0.5f, 1), () => level.ReplayLevel());
        nextSample = Button(selector, "Màn sau", new Vector2(275, -150), new Vector2(250, 60), new Vector2(0.5f, 1), () => level.NextLevel());
        levelSelector.SetActive(level.ShowLevelSelector);
    }

    private void Update()
    {
        if (level == null || occupancy == null) return;
        occupancy.text = "Level " + (level.CurrentLevel != null ? level.CurrentLevel.number : 1) + "   " +
            level.Conveyor.OccupiedSlotCount + "/" + level.Conveyor.Capacity;
        selection.text = input == null || input.PickMode == FoodPickMode.Tap ?
            (level.ShowLevelSelector && level.CurrentLevel != null ? level.CurrentLevel.hint : "") :
            input.PickMode == FoodPickMode.Chopsticks ? "Chạm 1 ô đang thấy để đưa lên băng chuyền (không gắp ô băng)" :
            "Chạm 1 món trong order để hút đủ nguyên liệu từ board/vòng xoay vào khay";
        selection.enableAutoSizing = true;
        selection.fontSizeMin = 17;
        selection.fontSizeMax = 22;
        cancelBooster.gameObject.SetActive(input != null && input.PickMode != FoodPickMode.Tap);
        levelSelector.SetActive(level.ShowLevelSelector);
        if (level.ShowLevelSelector)
        {
            sampleTitle.text = (level.CurrentLevelIndex + 1) + "/" + level.LevelCount + " · " +
                (level.CurrentLevel != null ? level.CurrentLevel.levelName : "");
            previousSample.interactable = level.HasPreviousLevel;
            nextSample.interactable = level.HasNextLevel;
        }
        for (int i = 0; i < boosterButtons.Count; i++)
        {
            boosterButtons[i].interactable = level.CanInteract && input != null;
            bool selected = input != null && (i == 0 && input.PickMode == FoodPickMode.Chopsticks ||
                i == 2 && input.PickMode == FoodPickMode.Magnet);
            boosterButtons[i].GetComponent<Image>().color = selected ? new Color(1, 0.9f, 0.45f) : new Color(1f, 0.72f, 0.28f);
        }
        bool revive = level.PendingRevive != SushiReviveReason.None;
        bool result = level.CurrentLevel != null && !level.IsPlaying;
        modal.SetActive(revive || result);
        if (!revive && !result) return;
        title.text = revive ? (level.PendingRevive == SushiReviveReason.RushCustomer ? "Khách hết giờ!\nTiếp tục: +30 giây" : "Các cột đều đóng băng!\nTiếp tục: phá băng hàng 1") :
            level.HasWon ? "Hoàn thành level!" : "Thua level";
        coinText.text = level.ReviveCost + " xu";
        coins.gameObject.SetActive(revive);
        advertisement.gameObject.SetActive(revive);
        coins.interactable = advertisement.interactable = level.CanPurchaseRevive;
        next.gameObject.SetActive(result && level.HasWon && level.HasNextLevel);
        decline.GetComponentInChildren<TextMeshProUGUI>().text = revive ? "Thua level" : "Chơi lại";
    }

    private static GameObject Panel(RectTransform parent, string name, Color color, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private static TextMeshProUGUI Label(RectTransform parent, Vector2 position, Vector2 size, Vector2 anchor, float fontSize)
    {
        var label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = anchor;
        label.rectTransform.sizeDelta = size;
        label.rectTransform.anchoredPosition = position;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.2f, 0.1f, 0.04f);
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(RectTransform parent, string text, Vector2 position, Vector2 size, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = Panel(parent, text, new Color(1f, 0.72f, 0.28f), position, size, anchor);
        var button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(action);
        Label(obj.GetComponent<RectTransform>(), Vector2.zero, size, new Vector2(0.5f, 0.5f), 32).text = text;
        return button;
    }

    private void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
}
