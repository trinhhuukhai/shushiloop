using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Scene-authored tutorial: point to an order, then guide one marked head tile.
public class ControlTutorial : SingletonMonoBehaviour<ControlTutorial>
{
    // The Click lesson may reach behind the head, but never into the deep rows.
    private const int ClickTutorialRows = 2;
    public enum TutorialStep { Inactive, Waiting, Order, Food, Booster }
    public enum TutorialLesson { LevelOne, ClickBooster, Item, MagnetBooster, RushCustomer }

    public SpriteRenderer img_lop_phu_duoi;
    public GameObject objDesOrder;
    public GameObject objDesFood;
    public GameObject handOrder;
    public GameObject handFood;
    public bool step1;
    public bool step2;

    [Header("Pointer animation")]
    [SerializeField] private Vector2 fingertip = new Vector2(0.17f, 0.95f);
    [SerializeField, Min(0)] private float pointerTravel = 0.055f;
    [SerializeField, Min(0)] private float pointerSpeed = 4.5f;

    public TutorialStep Step { get; private set; }
    public TutorialLesson Lesson { get; private set; }
    public FoodShushi TargetFood { get; private set; }
    public SushiDishCardView TargetOrder { get; private set; }
    public Customer TargetCustomer { get; private set; }
    public SushiDishOrder TargetDish { get; private set; }
    public static bool IsInputLocked => Ins != null && Ins.Step != TutorialStep.Inactive;
    public static bool IsConfirmingPickup => Ins != null && Ins.confirmingPickup;
    public static bool KeepsClickBoosterSelected => IsInputLocked && Ins.Lesson == TutorialLesson.ClickBooster &&
        (Ins.Step == TutorialStep.Food || Ins.Step == TutorialStep.Waiting);
    public static bool KeepsBoosterSelected => KeepsClickBoosterSelected || IsInputLocked &&
        Ins.Lesson == TutorialLesson.MagnetBooster && Ins.Step == TutorialStep.Order;

    private LevelManager level;
    private CustomerRecipeView recipeView;
    private FoodQueue targetQueue;
    private bool confirmingPickup;
    private int loadVersion;
    private FoodState tutorialItem;
    private FoodInputController input;
    private System.Action closeBoosterSpotlight;
    private TMP_Text foodDescription;
    private string defaultFoodDescription;
    private TMP_Text orderDescription;
    private string defaultOrderDescription;
    private Vector3 foodPanelPosition;
    private Vector3 orderHandScale;
    private Vector3 foodHandScale;
    private Vector3 orderPanelScale;
    private Vector3 foodPanelScale;
    private readonly List<KeyValuePair<Renderer, int>> highlightedRenderers = new List<KeyValuePair<Renderer, int>>();
    private readonly List<RaycastResult> targetUiHits = new List<RaycastResult>();

    public override void Awake()
    {
        base.Awake();
        orderHandScale = handOrder != null ? handOrder.transform.localScale : Vector3.one;
        foodHandScale = handFood != null ? handFood.transform.localScale : Vector3.one;
        orderPanelScale = objDesOrder != null ? objDesOrder.transform.localScale : Vector3.one;
        foodPanelScale = objDesFood != null ? objDesFood.transform.localScale : Vector3.one;
        foodPanelPosition = objDesFood != null ? objDesFood.transform.position : Vector3.zero;
        foodDescription = objDesFood != null ? objDesFood.GetComponentInChildren<TMP_Text>(true) : null;
        defaultFoodDescription = foodDescription != null ? foodDescription.text : "";
        orderDescription = objDesOrder != null ? objDesOrder.GetComponentInChildren<TMP_Text>(true) : null;
        defaultOrderDescription = orderDescription != null ? orderDescription.text : "";
        HideVisuals();
    }

    public FoodShushi getFoodTut()
    {
        var queues = level != null ? level.Board.FoodQueues : BoardShishiManager.Ins != null ? BoardShishiManager.Ins.FoodQueues : null;
        if (queues == null) return null;
        foreach (FoodQueue queue in queues)
        {
            FoodShushi food = queue != null ? queue.GetTopFood() : null;
            if (food != null && food.TutorialType == TypeTutFood.ClickTut && food.CanPick() &&
                food.LinkId == 0 && food.TrayId == 0 && !queue.IsPickupPending) return food;
        }
        return null;
    }

    public void setUpTutLevel1() => Prepare(LevelManager.Ins);

    public void Prepare(LevelManager owner)
    {
        // A booster/item lesson may have started while the HUD was opening.
        if (owner == null || owner.CurrentLevelIndex != 0) return;
        CancelTutorial();
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (owner == null || !owner.IsPlaying || owner.CurrentLevelIndex != 0 || player == null ||
            player.showTutorialLevel1 || (player.completedLevels != null && player.completedLevels.Contains(0))) return;
        if (img_lop_phu_duoi == null || objDesOrder == null || objDesFood == null || handOrder == null || handFood == null)
        {
            Debug.LogWarning("Assign the shade, both description panels and both hands on TutControl.", this);
            return;
        }
        owner.GetComponent<SushiLevelOneTutorial>()?.Cancel();
        Lesson = TutorialLesson.LevelOne;
        level = owner;
        loadVersion = owner.LoadVersion;
        TargetFood = getFoodTut();
        if (TargetFood == null) { InvalidTarget(); return; }
        targetQueue = TargetFood.Owner;
        foreach (Customer customer in owner.Conveyor.Customers.ActiveCustomers)
        {
            if (customer == null || !customer.IsSeated) continue;
            CustomerRecipeView view = customer.GetComponent<CustomerRecipeView>();
            if (view == null) continue;
            foreach (SushiDishOrder dish in customer.Dishes)
            {
                if (dish.FindAvailableIngredient(TargetFood.Type) == null) continue;
                recipeView = view;
                TargetOrder = view.CardAt(dish.Index);
                break;
            }
            if (TargetOrder != null) break;
        }
        if (TargetOrder == null) { InvalidTarget(); return; }
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        Step = TutorialStep.Waiting;
        level.SetTutorialHold(true);
    }

    public bool BeginClickBooster(LevelManager owner, System.Action hideSpotlight)
    {
        if (!CanBegin(owner)) return false;
        CancelTutorial();
        level = owner;
        loadVersion = owner.LoadVersion;
        Lesson = TutorialLesson.ClickBooster;
        input = FindFirstObjectByType<FoodInputController>();
        TargetFood = FindMarkedFood(true);
        if (TargetFood == null || input == null) { CancelTutorial(); return false; }
        targetQueue = TargetFood.Owner;
        closeBoosterSpotlight = hideSpotlight;
        Step = TutorialStep.Booster;
        level.SetTutorialHold(true);
        return true;
    }

    public bool ConfirmClickBooster()
    {
        if (Lesson != TutorialLesson.ClickBooster || Step != TutorialStep.Booster || input == null || level == null || level.IsPopupOpen ||
            (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return false;
        confirmingPickup = true;
        level.SetTutorialHold(false);
        try { input.SelectChopsticks(); }
        finally { confirmingPickup = false; level.SetTutorialHold(true); }
        if (input.PickMode != FoodPickMode.Chopsticks) return false;
        HideBoosterSpotlight();
        SetFoodDescription("BOOSTER TONGS", "Gắp một nguyên liệu đang thấy", "Chạm ô có bàn tay để gắp lên băng chuyền.");
        SetStep(TutorialStep.Food);
        return true;
    }

    public bool BeginMagnetBooster(LevelManager owner, System.Action hideSpotlight)
    {
        if (!CanBegin(owner)) return false;
        CancelTutorial();
        level = owner;
        loadVersion = owner.LoadVersion;
        Lesson = TutorialLesson.MagnetBooster;
        input = FindFirstObjectByType<FoodInputController>();
        if (input == null || !FoodInputController.CanUseMagnetBooster || !FindMagnetTarget())
        { CancelTutorial(); return false; }
        closeBoosterSpotlight = hideSpotlight;
        Step = TutorialStep.Booster;
        level.SetTutorialHold(true);
        return true;
    }

    private bool FindMagnetTarget()
    {
        foreach (Customer customer in level.Conveyor.Customers.ActiveCustomers)
        {
            if (customer == null || !customer.IsSeated) continue;
            var view = customer.GetComponent<CustomerRecipeView>();
            if (view == null) continue;
            foreach (SushiDishOrder dish in customer.Dishes)
            {
                var card = view.CardAt(dish.Index);
                if (card == null || !card.gameObject.activeInHierarchy ||
                    !level.Conveyor.CanTargetMagnet(level.Board, customer, dish)) continue;
                TargetCustomer = customer;
                TargetDish = dish;
                TargetOrder = card;
                recipeView = view;
                return true;
            }
        }
        return false;
    }

    public bool ConfirmMagnetBooster()
    {
        if (Lesson != TutorialLesson.MagnetBooster || Step != TutorialStep.Booster || input == null ||
            level == null || level.IsPopupOpen || (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return false;
        confirmingPickup = true;
        level.SetTutorialHold(false);
        try { input.SelectMagnet(); }
        finally { confirmingPickup = false; level.SetTutorialHold(true); }
        if (input.PickMode != FoodPickMode.Magnet) return false;
        HideBoosterSpotlight();
        if (orderDescription != null)
            orderDescription.text = "<size=65%><color=#3479AD>BOOSTER MAGNET</color></size>\n" +
                "<b>Hoàn thành ngay một món ăn</b>\n<size=78%>Chạm món có bàn tay để hút đủ nguyên liệu.</size>";
        recipeView.SetMagnetTargeting(true);
        SetStep(TutorialStep.Order);
        return true;
    }

    public bool BeginItem(LevelManager owner, FoodState item)
    {
        if (item == FoodState.Time) return BeginRushCustomer(owner);
        if (!CanBegin(owner) || item == FoodState.Normal) return false;
        CancelTutorial();
        level = owner;
        loadVersion = owner.LoadVersion;
        Lesson = TutorialLesson.Item;
        tutorialItem = item;
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        TargetFood = FindMarkedFood(false);
        if (TargetFood == null) { CancelTutorial(); return false; }
        targetQueue = TargetFood.Owner;
        SetItemDescription();
        Step = TutorialStep.Waiting;
        level.SetTutorialHold(true);
        return true;
    }

    public static bool HasRushCustomer(LevelManager owner)
    {
        if (owner == null || !owner.IsPlaying || owner.Conveyor == null || owner.Conveyor.Customers == null) return false;
        foreach (Customer customer in owner.Conveyor.Customers.ActiveCustomers)
            if (customer != null && customer.IsRush && customer.IsSeated && !customer.IsCompleted && !customer.RushExpired)
                return true;
        return false;
    }

    private bool BeginRushCustomer(LevelManager owner)
    {
        if (!CanBegin(owner) || !HasRushCustomer(owner)) return false;
        CancelTutorial();
        level = owner;
        loadVersion = owner.LoadVersion;
        Lesson = TutorialLesson.RushCustomer;
        tutorialItem = FoodState.Time;
        foreach (Customer customer in owner.Conveyor.Customers.ActiveCustomers)
        {
            if (customer == null || !customer.IsRush || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) continue;
            var view = customer.GetComponent<CustomerRecipeView>();
            if (view == null) continue;
            foreach (SushiDishOrder dish in customer.Dishes)
            {
                var card = view.CardAt(dish.Index);
                if (dish.IsComplete || card == null || !card.gameObject.activeInHierarchy) continue;
                TargetCustomer = customer;
                TargetDish = dish;
                TargetOrder = card;
                recipeView = view;
                break;
            }
            if (TargetOrder != null) break;
        }
        if (TargetOrder == null) { CancelTutorial(); return false; }
        FindFirstObjectByType<FoodInputController>()?.CancelBooster();
        if (orderDescription != null)
            orderDescription.text = "<size=65%><color=#3479AD>KHÁCH VỘI</color></size>\n" +
                "<b>Ưu tiên hoàn thành order này!</b>\n<size=78%>Hoàn thành order trước khi hết giờ!\n" +
                "Chạm để xem nguyên liệu và tiếp tục.</size>";
        Step = TutorialStep.Waiting;
        level.SetTutorialHold(true);
        return true;
    }

    private bool CanBegin(LevelManager owner) => owner != null && owner.IsPlaying &&
        img_lop_phu_duoi != null && objDesOrder != null && handOrder != null && objDesFood != null &&
        handFood != null && Step == TutorialStep.Inactive;

    private FoodShushi FindMarkedFood(bool chopsticks)
    {
        // Respect the level author's marker. Prefer the shallowest marked tile if
        // several exist; an unusable marker must never redirect the hand to a Normal tile.
        for (int row = 0; row < (chopsticks ? ClickTutorialRows : 1); row++)
        {
            foreach (FoodQueue queue in level.Board.FoodQueues)
            {
                if (queue == null || queue.IsPickupPending || row >= queue.Count) continue;
                FoodShushi food = queue.Foods[row];
                if (food.TutorialType != TypeTutFood.ClickTut || food.IsHidden || !IsVisibleFood(food) ||
                    (chopsticks ? !food.CanUseChopsticks || food.Amount > level.Conveyor.FreeSlotCount :
                    !food.CanPick() || !level.Board.CanTap(queue, level.Conveyor.FreeSlotCount))) continue;
                return food;
            }
        }
        return null;
    }

    private bool IsVisibleFood(FoodShushi food)
    {
        Camera camera = Camera.main;
        if (camera == null) return false;
        Vector3 viewport = camera.WorldToViewportPoint(food.transform.position);
        if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return false;
        if (EventSystem.current == null) return true;
        targetUiHits.Clear();
        var pointer = new PointerEventData(EventSystem.current) { position = camera.WorldToScreenPoint(food.transform.position) };
        EventSystem.current.RaycastAll(pointer, targetUiHits);
        return targetUiHits.Count == 0;
    }

    private int ItemPriority(FoodQueue queue, FoodShushi food)
    {
        if (tutorialItem == FoodState.Ice)
            foreach (FoodQueue other in level.Board.FoodQueues)
                if (Mathf.Abs(other.columnIndex - queue.columnIndex) == 1 && other.GetTopFood()?.IceLayers > 0) return 2;
        if (tutorialItem == FoodState.Lock && food.LinkId > 0 || tutorialItem == FoodState.RigidTray && food.TrayId > 0) return 2;
        foreach (FoodShushi next in queue.Foods)
            if (tutorialItem == FoodState.HoiCham && next.IsHidden || tutorialItem == FoodState.Lock && next.LinkId > 0 ||
                tutorialItem == FoodState.RigidTray && next.TrayId > 0) return 1;
        return 0;
    }

    private void SetItemDescription()
    {
        switch (tutorialItem)
        {
            case FoodState.HoiCham:
                SetFoodDescription("Ô BÍ ẨN", "Mở đường cho nguyên liệu mới", "Chạm ô đầu cột để đưa ô bí ẩn lên trước.");
                break;
            case FoodState.Ice:
                SetFoodDescription("Ô ĐÓNG BĂNG", "Lấy nguyên liệu để phá băng", ItemPriority(targetQueue, TargetFood) > 0
                    ? "Chạm ô được chỉ để phá băng bên cạnh." : "Chạm ô được chỉ để tiếp tục chuẩn bị món.");
                break;
            case FoodState.Lock:
                SetFoodDescription("Ô LIÊN KẾT", "Đưa cặp nguyên liệu lên trước", TargetFood.LinkId > 0
                    ? "Chạm ô được chỉ để lấy cả cặp liên kết." : "Lấy ô được chỉ để đưa ô liên kết lên đầu cột.");
                break;
            case FoodState.RigidTray:
                SetFoodDescription("KHAY NGUYÊN LIỆU", "Các ô cùng khay đi cùng nhau", TargetFood.TrayId > 0
                    ? "Chạm ô được chỉ để lấy nguyên liệu trong khay." : "Lấy ô được chỉ để đưa khay lên đầu cột.");
                break;
        }
    }

    private void SetFoodDescription(string label, string title, string hint)
    {
        if (foodDescription != null)
            foodDescription.text = "<size=65%><color=#3479AD>" + label + "</color></size>\n<b>" + title +
                "</b>\n<size=78%>" + hint + "</size>";
    }

    private void Update()
    {
        if (Step == TutorialStep.Inactive) return;
        bool isClick = Lesson == TutorialLesson.ClickBooster;
        bool isRush = Lesson == TutorialLesson.RushCustomer;
        bool hasOrderTarget = Lesson == TutorialLesson.MagnetBooster || isRush;
        bool invalidTarget = hasOrderTarget
            ? TargetCustomer == null || !TargetCustomer.IsSeated || TargetCustomer.IsCompleted || TargetCustomer.RushExpired ||
                (isRush && !TargetCustomer.IsRush) || TargetDish == null || TargetDish.IsComplete ||
                TargetOrder == null || !TargetOrder.gameObject.activeInHierarchy
            : TargetFood == null || TargetFood.TutorialType != TypeTutFood.ClickTut ||
                targetQueue == null || TargetFood.Owner != targetQueue ||
                (isClick ? targetQueue.IndexOf(TargetFood) < 0 || targetQueue.IndexOf(TargetFood) >= ClickTutorialRows :
                    targetQueue.GetTopFood() != TargetFood) ||
                (Lesson == TutorialLesson.LevelOne && (TargetOrder == null || !TargetOrder.gameObject.activeInHierarchy));
        if (level == null || !level.IsPlaying || level.LoadVersion != loadVersion || invalidTarget)
        { CancelTutorial(); return; }
        if (Step != TutorialStep.Waiting || level.IsPopupOpen ||
            (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return;
        Canvas.ForceUpdateCanvases();
        SetStep(Lesson == TutorialLesson.LevelOne || isRush ? TutorialStep.Order : TutorialStep.Food);
    }

    private void SetStep(TutorialStep next)
    {
        Step = next;
        step1 = next == TutorialStep.Order;
        step2 = next == TutorialStep.Food;
        img_lop_phu_duoi.gameObject.SetActive(true);
        handOrder.SetActive(step1);
        handFood.SetActive(step2);
        objDesOrder.SetActive(step1);
        objDesFood.SetActive(step2);
        GameObject panel = step1 ? objDesOrder : objDesFood;
        Vector3 scale = step1 ? orderPanelScale : foodPanelScale;
        panel.transform.DOKill();
        panel.transform.localScale = scale * 0.93f;
        panel.transform.DOScale(scale, 0.24f).SetEase(Ease.OutBack, 1.2f).SetUpdate(true);
        if (step2) HighlightFood();
        RefreshLayout();
    }

    // Called before normal input while the level is held. An outside tap is simply consumed.
    public bool TryHandlePress(Vector2 screenPosition, Camera camera)
    {
        if (!IsInputLocked) return false;
        if (camera == null || level == null || level.IsPopupOpen ||
            (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return true;
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Step == TutorialStep.Order && TargetOrder != null)
        {
            Plane plane = new Plane(TargetOrder.transform.forward, TargetOrder.transform.position);
            if (plane.Raycast(ray, out float distance) && TargetOrder.Contains(ray.GetPoint(distance)))
            {
                if (Lesson == TutorialLesson.MagnetBooster) TryCompleteMagnet();
                else if (Lesson == TutorialLesson.RushCustomer)
                {
                    if (recipeView.ShowRecipeDetails(TargetOrder))
                    {
                        GameDataFragment.cur?.MarkItemTutorialDone(FoodState.Time);
                        CancelTutorial();
                    }
                }
                else if (recipeView.ShowRecipeDetails(TargetOrder)) SetStep(TutorialStep.Food);
            }
        }
        else if (Step == TutorialStep.Food && TargetFood != null && targetQueue != null)
        {
            Plane plane = new Plane(targetQueue.transform.forward, TargetFood.transform.position);
            if (!plane.Raycast(ray, out float distance)) return true;
            Vector3 local = TargetFood.transform.InverseTransformPoint(ray.GetPoint(distance));
            if (Mathf.Abs(local.x) <= 0.6f && Mathf.Abs(local.y) <= 0.6f) TryPickTarget();
        }
        return true;
    }

    public static bool AllowsQueue(FoodQueue queue) => Ins != null && Ins.confirmingPickup &&
        Ins.Step == TutorialStep.Food && Ins.Lesson != TutorialLesson.ClickBooster &&
        queue != null && queue == Ins.targetQueue && queue.GetTopFood() == Ins.TargetFood;

    public static bool AllowsChopsticks(FoodShushi food) => IsConfirmingPickup && Ins.Step == TutorialStep.Food &&
        Ins.Lesson == TutorialLesson.ClickBooster && food != null && food == Ins.TargetFood;

    public static bool AllowsMagnet(Customer customer, SushiDishOrder dish) => IsConfirmingPickup &&
        Ins.Step == TutorialStep.Order && Ins.Lesson == TutorialLesson.MagnetBooster &&
        customer != null && customer == Ins.TargetCustomer && dish != null && dish == Ins.TargetDish;

    private void TryCompleteMagnet()
    {
        if (confirmingPickup || input == null) return;
        bool taken = false;
        confirmingPickup = true;
        LevelManager owner = level;
        owner.SetTutorialHold(false);
        try { taken = input.TryUseMagnet(TargetCustomer, TargetDish); }
        finally
        {
            confirmingPickup = false;
            if (!taken && level == owner) owner.SetTutorialHold(true);
        }
        if (!taken) return;
        GameDataFragment.cur?.MarkBoosterTutorialDone(BoosterType.Magnet);
        CancelTutorial();
    }

    private void TryPickTarget()
    {
        bool isClick = Lesson == TutorialLesson.ClickBooster;
        if (confirmingPickup || TargetFood == null || targetQueue == null || TargetFood.Owner != targetQueue ||
            (isClick ? !TargetFood.CanUseChopsticks : targetQueue.GetTopFood() != TargetFood || !TargetFood.CanPick())) return;
        bool taken = false;
        RestoreHighlight();
        confirmingPickup = true;
        LevelManager owner = level;
        owner.SetTutorialHold(false);
        try
        {
            taken = isClick ? input != null && input.TryUseChopsticks(TargetFood) : owner.Conveyor.TryTakeFromQueue(targetQueue);
        }
        finally
        {
            confirmingPickup = false;
            if (!taken && level == owner)
            {
                owner.SetTutorialHold(true);
                HighlightFood();
            }
        }
        if (!taken) return;
        var data = GameDataFragment.cur;
        if (data != null && data.dataPlayer != null)
        {
            if (Lesson == TutorialLesson.ClickBooster) data.MarkBoosterTutorialDone(BoosterType.Click);
            else if (Lesson == TutorialLesson.Item) data.MarkItemTutorialDone(tutorialItem);
            else
            {
                data.dataPlayer.showTutorialLevel1 = true;
                data.Save();
            }
        }
        CancelTutorial();
    }

    private void LateUpdate()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (Step != TutorialStep.Order && Step != TutorialStep.Food) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        if (img_lop_phu_duoi != null && img_lop_phu_duoi.sprite != null && camera.orthographic)
        {
            Vector3 size = img_lop_phu_duoi.sprite.bounds.size;
            Vector3 parentScale = img_lop_phu_duoi.transform.parent.lossyScale;
            img_lop_phu_duoi.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 0);
            img_lop_phu_duoi.transform.localScale = new Vector3(
                camera.orthographicSize * 2 * camera.aspect / (size.x * Mathf.Abs(parentScale.x)),
                camera.orthographicSize * 2 / (size.y * Mathf.Abs(parentScale.y)), 1);
        }
        GameObject hand = Step == TutorialStep.Order ? handOrder : handFood;
        if (hand == null || (Step == TutorialStep.Order ? TargetOrder == null : TargetFood == null)) return;
        Vector3 target = Step == TutorialStep.Order
            ? TargetOrder.DishImage.rectTransform.TransformPoint(TargetOrder.DishImage.rectTransform.rect.center)
            : TargetFood.transform.position;
        float pulse = Mathf.Sin(Time.unscaledTime * pointerSpeed);
        hand.transform.localScale = (Step == TutorialStep.Order ? orderHandScale : foodHandScale) * (1 + 0.035f * pulse);
        if (Step == TutorialStep.Food && camera.WorldToViewportPoint(target).y < 0.22f)
        {
            Vector3 scale = hand.transform.localScale;
            scale.y = -scale.y;
            hand.transform.localScale = scale;
        }
        if (Step == TutorialStep.Food && Lesson != TutorialLesson.LevelOne && objDesFood != null)
        {
            SpriteRenderer panel = objDesFood.GetComponent<SpriteRenderer>();
            float halfHeight = panel != null ? panel.bounds.extents.y : 1.25f;
            float bottom = camera.transform.position.y - camera.orthographicSize;
            float top = camera.transform.position.y + camera.orthographicSize;
            float panelY = Mathf.Clamp(target.y + 0.85f + halfHeight, bottom + halfHeight + 0.2f, top - halfHeight - 0.2f);
            objDesFood.transform.position = new Vector3(camera.transform.position.x, panelY, foodPanelPosition.z);
        }
        SpriteRenderer sprite = hand.GetComponent<SpriteRenderer>();
        Vector3 tip = Vector3.zero;
        if (sprite != null && sprite.sprite != null)
        {
            Bounds bounds = sprite.sprite.bounds;
            tip = bounds.min + Vector3.Scale(bounds.size, new Vector3(fingertip.x, fingertip.y, 0));
            tip.z = 0;
        }
        hand.transform.position = target - hand.transform.TransformVector(tip) +
            new Vector3(pointerTravel, -pointerTravel, 0) * (1 + pulse);
    }

    private void HighlightFood()
    {
        if (TargetFood == null || highlightedRenderers.Count > 0) return;
        Renderer[] renderers = TargetFood.GetComponentsInChildren<Renderer>(true);
        int minimum = int.MaxValue;
        foreach (Renderer renderer in renderers) minimum = Mathf.Min(minimum, renderer.sortingOrder);
        int offset = Mathf.Max(0, img_lop_phu_duoi.sortingOrder + 2 - minimum);
        foreach (Renderer renderer in renderers)
        {
            highlightedRenderers.Add(new KeyValuePair<Renderer, int>(renderer, renderer.sortingOrder));
            renderer.sortingOrder += offset;
        }
    }

    private void RestoreHighlight()
    {
        foreach (var entry in highlightedRenderers)
            if (entry.Key != null) entry.Key.sortingOrder = entry.Value;
        highlightedRenderers.Clear();
    }

    public void CancelTutorial()
    {
        bool cancelBooster = Lesson == TutorialLesson.ClickBooster || Lesson == TutorialLesson.MagnetBooster;
        Step = TutorialStep.Inactive;
        step1 = step2 = confirmingPickup = false;
        RestoreHighlight();
        HideVisuals();
        HideBoosterSpotlight();
        if (cancelBooster && input != null) input.CancelBooster();
        if (foodDescription != null) foodDescription.text = defaultFoodDescription;
        if (orderDescription != null) orderDescription.text = defaultOrderDescription;
        if (Lesson == TutorialLesson.MagnetBooster && recipeView != null) recipeView.SetMagnetTargeting(false);
        if (objDesFood != null) objDesFood.transform.position = foodPanelPosition;
        if (level != null) level.SetTutorialHold(false);
        level = null;
        recipeView = null;
        targetQueue = null;
        TargetFood = null;
        TargetOrder = null;
        TargetCustomer = null;
        TargetDish = null;
        input = null;
        Lesson = TutorialLesson.LevelOne;
        tutorialItem = FoodState.Normal;
    }

    private void HideBoosterSpotlight()
    {
        var cleanup = closeBoosterSpotlight;
        closeBoosterSpotlight = null;
        cleanup?.Invoke();
    }

    private void HideVisuals()
    {
        if (img_lop_phu_duoi != null) img_lop_phu_duoi.gameObject.SetActive(false);
        ResetVisual(handOrder, orderHandScale);
        ResetVisual(handFood, foodHandScale);
        ResetVisual(objDesOrder, orderPanelScale);
        ResetVisual(objDesFood, foodPanelScale);
    }

    private static void ResetVisual(GameObject visual, Vector3 scale)
    {
        if (visual == null) return;
        visual.transform.DOKill();
        visual.transform.localScale = scale;
        visual.SetActive(false);
    }

    private void InvalidTarget()
    {
        Debug.LogWarning("Level 1 tutorial needs a usable ClickTut head tile and a seated order using that ingredient.", this);
        CancelTutorial();
    }

    private void OnDisable() => CancelTutorial();
    private void OnDestroy()
    {
        CancelTutorial();
        if (instance == this) instance = null;
    }
}
