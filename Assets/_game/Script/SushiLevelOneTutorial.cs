using UnityEngine;

// Owns the first-session lesson; the overlay only presents its current target.
public sealed class SushiLevelOneTutorial : MonoBehaviour
{
    public enum TutorialStage { Inactive, Waiting, Recipe, Ingredient }
    public static SushiLevelOneTutorial Instance { get; private set; }
    [Header("Tutorial (disabled until the onboarding design is ready)")]
    [SerializeField] private bool enableTutorial;
    public bool IsTutorialEnabled => enableTutorial;
    [Header("Level 1: configured first pickup (zero-based indices)")]
    [SerializeField, Min(0)] private int targetColumn;
    [SerializeField] private PoolType targetIngredient;
    [SerializeField, Min(0)] private int targetCustomerIndex;
    [SerializeField, Min(0)] private int targetDishIndex;
    public TutorialStage Stage { get; private set; }
    public FoodQueue TargetQueue { get; private set; }
    public FoodShushi TargetFood { get; private set; }
    public RectTransform RecipeTarget { get; private set; }
    public string DishName { get; private set; }
    public string IngredientName => SushiRecipeCatalog.IngredientName(targetIngredient);
    public static bool IsInputLocked => ControlTutorial.IsInputLocked ||
        (Instance != null && Instance.Stage != TutorialStage.Inactive);

    private LevelManager level;
    private CanvasTutorial overlay;
    private bool acceptingTarget;

    // Lưu đối tượng điều khiển hướng dẫn màn đầu hiện tại.
    private void Awake() => Instance = this;

    // The target is accepted only inside the overlay's confirmed click, never by a world tap behind it.
    // Cho phép lấy cột được tutorial xác nhận hoặc cột mục tiêu của hướng dẫn màn đầu.
    public static bool AllowsQueue(FoodQueue queue) => ControlTutorial.AllowsQueue(queue) ||
        Instance != null && Instance.acceptingTarget &&
        Instance.Stage == TutorialStage.Ingredient && queue != null && queue == Instance.TargetQueue &&
        queue.GetTopFood() == Instance.TargetFood;

    // Kiểm tra tiến trình chưa học, chọn mục tiêu và chờ mở lớp hướng dẫn màn đầu.
    public void Prepare(LevelManager owner)
    {
        Cancel();
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (!enableTutorial || owner == null || owner.CurrentLevelIndex != 0 || !owner.IsPlaying || UIManager.Ins == null ||
            player == null || player.showTutorialLevel1 ||
            (player.completedLevels != null && player.completedLevels.Contains(0))) return;

        level = owner;
        if (!FindMarkedTarget()) { InvalidConfiguration(); return; }
        Stage = TutorialStage.Waiting;
        //level.SetTutorialHold(true, false);
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
    }

    // Ghép ô đầu cột ClickTut hợp lệ với một món của khách đang ngồi cần nguyên liệu đó.
    private bool FindMarkedTarget()
    {
        foreach (FoodQueue queue in level.Board.FoodQueues)
        {
            FoodShushi food = queue.GetTopFood();
            if (food == null || food.TutorialType != TypeTutFood.ClickTut || food.Amount != 1 ||
                !food.CanPick() || queue.IsPickupPending || food.LinkId > 0 || food.TrayId > 0) continue;
            var customers = level.Conveyor.Customers.ActiveCustomers;
            for (int c = 0; c < customers.Count; c++)
            {
                Customer customer = customers[c];
                if (customer == null || !customer.IsSeated) continue;
                var recipe = customer.GetComponent<CustomerRecipeView>();
                if (recipe == null) continue;
                foreach (SushiDishOrder dish in customer.Dishes)
                {
                    var card = recipe.CardAt(dish.Index);
                    if (dish.FindAvailableIngredient(food.Type) == null || card == null) continue;
                    TargetQueue = queue;
                    TargetFood = food;
                    targetColumn = queue.columnIndex;
                    targetIngredient = food.Type;
                    targetCustomerIndex = c;
                    targetDishIndex = dish.Index;
                    RecipeTarget = card.Rect;
                    DishName = dish.Recipe.Name;
                    return true;
                }
            }
        }
        return false;
    }

    // Hủy khi màn hoặc mục tiêu mất hiệu lực; mở overlay khi popup và chuyển cảnh đã kết thúc.
    private void Update()
    {
        if (Stage == TutorialStage.Inactive) return;
        if (!enableTutorial || level == null || !level.IsPlaying || level.CurrentLevelIndex != 0 ||
            TargetFood == null || RecipeTarget == null)
        { Cancel(); return; }
        if (Stage != TutorialStage.Waiting || level.IsPopupOpen ||
            (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return;
        try
        {
            Stage = TutorialStage.Recipe;
            overlay = UIManager.Ins.OpenUI<CanvasTutorial>();
            overlay.Present(this);
        }
        catch (System.Exception error)
        {
            Debug.LogException(error, this);
            Cancel();
        }
    }

    // Chuyển từ xem công thức sang chọn nguyên liệu, rồi lưu hoàn thành khi lấy đúng ô thành công.
    public void ConfirmTarget()
    {
        if (level == null || !level.IsPlaying || level.IsPopupOpen ||
            (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return;
        if (Stage == TutorialStage.Recipe)
        {
            Stage = TutorialStage.Ingredient;
            overlay.Present(this);
            return;
        }
        if (Stage != TutorialStage.Ingredient || acceptingTarget || TargetQueue == null ||
            TargetQueue.GetTopFood() != TargetFood || !TargetFood.CanPick()) return;

        bool taken = false;
        acceptingTarget = true;
        try
        {
            // No frame elapses while the pause is released: only this exact pickup is authorized.
            //level.SetTutorialHold(false);
            taken = level.Conveyor.TryTakeFromQueue(TargetQueue);
        }
        finally
        {
            acceptingTarget = false;
            //if (!taken && level != null) level.SetTutorialHold(true);
        }
        if (!taken) return;
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (player != null)
        {
            player.showTutorialLevel1 = true;
            GameDataFragment.cur.Save();
        }
        Cancel();
    }

    // Đưa tutorial về trạng thái nghỉ, đóng overlay và xóa các tham chiếu màn cùng mục tiêu.
    public void Cancel()
    {
        Stage = TutorialStage.Inactive;
        acceptingTarget = false;
        CanvasTutorial closing = overlay;
        overlay = null;
        if (closing != null) closing.Dismiss();
        //if (level != null) level.SetTutorialHold(false);
        level = null;
        TargetQueue = null;
        TargetFood = null;
        RecipeTarget = null;
    }

    // Hủy tutorial khi overlay đang gắn với nó được đóng từ bên ngoài.
    public void OverlayClosed(CanvasTutorial view)
    {
        if (view != overlay) return;
        overlay = null;
        Cancel();
    }

    // Cảnh báo không tìm thấy ô cùng đơn món phù hợp và dừng tutorial.
    private void InvalidConfiguration()
    {
        Debug.LogWarning("Level 1 tutorial needs a usable ClickTut head tile matching a seated order.", this);
        Cancel();
    }

    // Dọn tutorial khi component bị tắt.
    private void OnDisable() => Cancel();
    // Dọn tutorial và xóa đối tượng điều khiển hiện tại khi bị hủy.
    private void OnDestroy() { Cancel(); if (Instance == this) Instance = null; }
}
