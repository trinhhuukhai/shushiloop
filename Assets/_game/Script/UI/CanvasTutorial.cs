using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Native UI prefab. Customer cards, ingredients, trays and the conveyor remain world objects.
public sealed class CanvasTutorial : UICanvas
{
    protected override bool BlocksGameplay => false;
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RectTransform[] shades;
    [SerializeField] private Button targetButton;
    [SerializeField] private RectTransform focusFrame;
    [SerializeField] private RectTransform hand;
    [SerializeField] private RectTransform instructionPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text stepText;
    public Button TargetButton => targetButton;
    private SushiLevelOneTutorial owner;
    private readonly Vector3[] corners = new Vector3[4];
    private Canvas canvas;

    private Canvas hiddenDebugCanvas;
    private GraphicRaycaster hiddenDebugRaycaster;
    private bool debugCanvasWasEnabled;
    private bool debugRaycasterWasEnabled;

    public override void Open()
    {
        base.Open();
        if (hiddenDebugCanvas != null || UICheat.Ins == null) return;
        Canvas debug = UICheat.Ins.GetComponentInParent<Canvas>();
        hiddenDebugCanvas = debug != null ? debug.rootCanvas : null;
        if (hiddenDebugCanvas == null || hiddenDebugCanvas == GetComponent<Canvas>().rootCanvas)
        { hiddenDebugCanvas = null; return; }
        debugCanvasWasEnabled = hiddenDebugCanvas.enabled;
        hiddenDebugRaycaster = hiddenDebugCanvas.GetComponent<GraphicRaycaster>();
        debugRaycasterWasEnabled = hiddenDebugRaycaster != null && hiddenDebugRaycaster.enabled;
        hiddenDebugCanvas.enabled = false;
        if (hiddenDebugRaycaster != null) hiddenDebugRaycaster.enabled = false;
    }

    public void Present(SushiLevelOneTutorial tutorial)
    {
        owner = tutorial;
        bool recipe = owner.Stage == SushiLevelOneTutorial.TutorialStage.Recipe;
        stepText.text = recipe ? "HƯỚNG DẪN  1/2" : "HƯỚNG DẪN  2/2";
        titleText.text = recipe ? "Khách muốn ăn gì?" : "Chọn " + owner.IngredientName.ToLowerInvariant();
        hintText.text = recipe
            ? "Xem món và các nguyên liệu khách cần.\nChạm vào thẻ món đang sáng để tiếp tục."
            : "Chạm ô nguyên liệu đang sáng để đưa lên băng chuyền.";
        Canvas.ForceUpdateCanvases();
        RefreshLayout();
    }

    private void LateUpdate() => RefreshLayout();

    public void RefreshLayout()
    {
        if (owner == null || overlayRoot == null) return;
        Camera worldCamera = Camera.main;
        if (worldCamera == null) { owner.Cancel(); return; }
        if (canvas == null) canvas = GetComponent<Canvas>();
        Canvas rootCanvas = canvas.rootCanvas;
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        bool recipe = owner.Stage == SushiLevelOneTutorial.TutorialStage.Recipe;
        if (recipe)
        {
            if (owner.RecipeTarget == null) return;
            owner.RecipeTarget.GetWorldCorners(corners);
        }
        else
        {
            if (owner.TargetFood == null) return;
            Vector3 center = owner.TargetFood.transform.position;
            float halfWidth = 0.57f * owner.TargetQueue.transform.lossyScale.x;
            float halfHeight = 0.57f * owner.TargetQueue.transform.lossyScale.y;
            corners[0] = center + new Vector3(-halfWidth, -halfHeight);
            corners[1] = center + new Vector3(-halfWidth, halfHeight);
            corners[2] = center + new Vector3(halfWidth, halfHeight);
            corners[3] = center + new Vector3(halfWidth, -halfHeight);
        }
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 screen = worldCamera.WorldToScreenPoint(corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screen, uiCamera, out Vector2 point);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        Rect area = overlayRoot.rect;
        min -= area.min;
        max -= area.min;
        float padding = recipe ? 9 : 5;
        float x0 = Mathf.Clamp(min.x - padding, 8, area.width - 8);
        float x1 = Mathf.Clamp(max.x + padding, x0, area.width - 8);
        float y0 = Mathf.Clamp(min.y - padding, 8, area.height - 8);
        float y1 = Mathf.Clamp(max.y + padding, y0, area.height - 8);
        Place(shades[0], 0, 0, area.width, y0);
        Place(shades[1], 0, y1, area.width, area.height - y1);
        Place(shades[2], 0, y0, x0, y1 - y0);
        Place(shades[3], x1, y0, area.width - x1, y1 - y0);
        Place((RectTransform)targetButton.transform, x0, y0, x1 - x0, y1 - y0);
        // The hit area stays still while only the pointer animates.
        float pulse = Mathf.Sin(Time.unscaledTime * 4.5f);
        hand.anchorMin = hand.anchorMax = Vector2.zero;
        hand.anchoredPosition = new Vector2(Mathf.Lerp(x0, x1, 0.72f), y0 + (y1 - y0) * 0.33f) +
            new Vector2(7, -7) * (pulse + 1);
        hand.localScale = Vector3.one * (1 + 0.025f * pulse);
        float width = Mathf.Min(850, area.width - 48);
        float height = instructionPanel.sizeDelta.y;
        float panelY = recipe ? y0 - height * 0.5f - 78 : y1 + height * 0.5f + 70;
        panelY = Mathf.Clamp(panelY, height * 0.5f + 35, area.height - height * 0.5f - 35);
        instructionPanel.anchorMin = instructionPanel.anchorMax = Vector2.zero;
        instructionPanel.pivot = Vector2.one * 0.5f;
        instructionPanel.sizeDelta = new Vector2(width, height);
        instructionPanel.anchoredPosition = new Vector2(area.width * 0.5f, panelY);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    public void OnTargetClick() => owner?.ConfirmTarget();
    public void Dismiss() { owner = null; CloseDirectly(); }
    protected override void OnDisable()
    {
        var previous = owner;
        owner = null;
        previous?.OverlayClosed(this);
        if (hiddenDebugCanvas != null) hiddenDebugCanvas.enabled = debugCanvasWasEnabled;
        if (hiddenDebugRaycaster != null) hiddenDebugRaycaster.enabled = debugRaycasterWasEnabled;
        hiddenDebugCanvas = null;
        hiddenDebugRaycaster = null;
        base.OnDisable();
    }
    public override void BackKey() { }
}