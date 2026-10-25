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
    public static bool IsInputLocked => Instance != null && Instance.Stage != TutorialStage.Inactive;

    private LevelManager level;
    private CanvasTutorial overlay;
    private bool acceptingTarget;

    private void Awake() => Instance = this;

    // The target is accepted only inside the overlay's confirmed click, never by a world tap behind it.
    public static bool AllowsQueue(FoodQueue queue) => Instance != null && Instance.acceptingTarget &&
        Instance.Stage == TutorialStage.Ingredient && queue != null && queue == Instance.TargetQueue &&
        queue.GetTopFood() == Instance.TargetFood;

    public void Prepare(LevelManager owner)
    {
        Cancel();
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (!enableTutorial || owner == null || owner.CurrentLevelIndex != 0 || !owner.IsPlaying || UIManager.Ins == null ||
            player == null || player.showTutorialLevel1 ||
            (player.completedLevels != null && player.completedLevels.Contains(0))) return;

        level = owner;
        var queues = level.Board.FoodQueues;
        var customers = level.Conveyor.Customers.ActiveCustomers;
        if (targetColumn >= queues.Count || targetCustomerIndex >= customers.Count ||
            customers[targetCustomerIndex] == null)
        { InvalidConfiguration(); return; }

        TargetQueue = queues[targetColumn];
        TargetFood = TargetQueue.GetTopFood();
        Customer customer = customers[targetCustomerIndex];
        if (TargetFood == null || TargetFood.Type != targetIngredient || TargetFood.Amount != 1 || !TargetFood.CanPick() ||
            TargetFood.LinkId > 0 || TargetFood.TrayId > 0 || targetDishIndex >= customer.Dishes.Count)
        { InvalidConfiguration(); return; }

        var dish = customer.Dishes[targetDishIndex];
        bool usesIngredient = false;
        foreach (PoolType ingredient in dish.Recipe.Ingredients) usesIngredient |= ingredient == targetIngredient;
        CustomerRecipeView recipe = customer.GetComponent<CustomerRecipeView>();
        if (!usesIngredient || recipe == null || !customer.IsSeated)
        { InvalidConfiguration(); return; }

        RecipeTarget = recipe.CardAt(targetDishIndex).Rect;
        DishName = dish.Recipe.Name;
        Stage = TutorialStage.Waiting;
        level.SetTutorialHold(true);
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
    }

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
            level.SetTutorialHold(false);
            taken = level.Conveyor.TryTakeFromQueue(TargetQueue);
        }
        finally
        {
            acceptingTarget = false;
            if (!taken && level != null) level.SetTutorialHold(true);
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

    public void Cancel()
    {
        Stage = TutorialStage.Inactive;
        acceptingTarget = false;
        CanvasTutorial closing = overlay;
        overlay = null;
        if (closing != null) closing.Dismiss();
        if (level != null) level.SetTutorialHold(false);
        level = null;
        TargetQueue = null;
        TargetFood = null;
        RecipeTarget = null;
    }

    public void OverlayClosed(CanvasTutorial view)
    {
        if (view != overlay) return;
        overlay = null;
        Cancel();
    }

    private void InvalidConfiguration()
    {
        Debug.LogWarning("Level 1 tutorial target no longer matches the board/recipe. Configure SushiLevelOneTutorial on LevelManager.", this);
        Cancel();
    }

    private void OnDisable() => Cancel();
    private void OnDestroy() { Cancel(); if (Instance == this) Instance = null; }
}