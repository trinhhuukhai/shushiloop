using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OrderItem
{
    public PoolType typeFood;
    public int requiredAmount;
    public int receivedAmount;
    public int incomingAmount;

    public bool CanReceive =>  receivedAmount + incomingAmount < requiredAmount;
    public bool IsComplete =>  receivedAmount >= requiredAmount;
}

public class Customer : MonoBehaviour
{
    public int id;
    [SerializeField] private UnityEngine.UI.Image avatarImage;
    [SerializeField] private SushiIngredientArtLibrary ingredientArt;
    [SerializeField] private CustomerRecipeView recipeView;
    [SerializeField] private Transform servingRoot;
    [SerializeField] private Transform[] servingSlots;
    private readonly Dictionary<SushiDishOrder, int> tableReservations = new Dictionary<SushiDishOrder, int>();

    // Bàn phục vụ chung (4 ô cho mọi khách); null thì dùng các ô riêng trong prefab khách như cũ.
    private static SushiServingCounter SharedCounter =>
        SushiServingCounter.Ins != null && SushiServingCounter.Ins.isActiveAndEnabled && SushiServingCounter.Ins.Count > 0
            ? SushiServingCounter.Ins : null;

    private Transform ServingSlot(int index) =>
        SharedCounter != null ? SharedCounter.Slot(index) : servingSlots[index];

    // Bộ lọc tạm do băng chuyền đặt: chỉ cho phép các ô đĩa đang ở ngay cạnh nguyên liệu trên băng.
    // null = không giới hạn (nam châm, túi revive...).
    internal static Predicate<int> ServingSlotFilter;
    private static bool SlotAllowed(int index) => ServingSlotFilter == null || ServingSlotFilter(index);

    // Giữ ô cho món (ghi cả vào bàn chung để khách khác không lấy trùng).
    private void ClaimTable(SushiDishOrder dish, int slotIndex)
    {
        tableReservations[dish] = slotIndex;
        SharedCounter?.Claim(slotIndex, this);
    }

    // Trả ô của món.
    private bool ReleaseTable(SushiDishOrder dish)
    {
        if (!tableReservations.TryGetValue(dish, out int slotIndex)) return false;
        tableReservations.Remove(dish);
        SharedCounter?.Release(slotIndex, this);
        return true;
    }

    // Kiểm tra món của khách còn nguyên trạng và đủ điều kiện bắt đầu chuẩn bị.
    public bool CanPrepareDish(SushiDishOrder dish)
    {
        if (dish == null || !dishes.Contains(dish) || completed || RushExpired || dish.IsComplete || dish.IsPrepared) return false;
        foreach (var ingredient in dish.Ingredients)
            if (ingredient.receivedAmount != 0 || ingredient.incomingAmount != 0) return false;
        return true;
    }

    // Kiểm tra khách đã ngồi, chưa hết giờ và món vẫn có thể được ghép nguyên liệu.
    public bool CanAssembleDish(SushiDishOrder dish)
    {
        return IsSeated && !completed && !RushExpired && dish != null && dishes.Contains(dish) &&
            !dish.IsComplete && !dish.IsOnTable;
    }

    // Kiểm tra thứ tự nguyên liệu, nhu cầu món và chỗ trống trên bàn trước khi nhận.
    public bool CanReceivePreparedIngredient(SushiDishOrder dish, PoolType type, bool includeIncomingFoundation = false)
    {
        if (!CanAssembleDish(dish) || !dish.CanStartIngredient(type, includeIncomingFoundation) ||
            dish.FindAvailableIngredient(type) == null) return false;
        OrderItem order = FindOrder(type);
        return order != null && order.CanReceive &&
            (tableReservations.TryGetValue(dish, out int reserved) ? SlotAllowed(reserved) : FindFreeServingSlot() >= 0);
    }

    // Giữ chỗ trên bàn và tăng số nguyên liệu đang bay tới cho món lẫn đơn tổng.
    public bool TryReservePreparedIngredient(SushiDishOrder dish, PoolType type, out Transform target,
        bool includeIncomingFoundation = false)
    {
        target = null;
        if (!CanReceivePreparedIngredient(dish, type, includeIncomingFoundation)) return false;
        OrderItem ingredient = dish.FindAvailableIngredient(type);
        OrderItem order = FindOrder(type);

        if (!tableReservations.TryGetValue(dish, out int slotIndex))
        {
            slotIndex = FindFreeServingSlot();
            if (slotIndex < 0) return false;
            ClaimTable(dish, slotIndex);
        }

        dish.IsPrepared = true;
        ingredient.incomingAmount++;
        order.incomingAmount++;
        target = ServingSlot(slotIndex);
        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Kiểm tra hai món có thể chuyển phần nền đã chuẩn bị và đổi chỗ giữ trên bàn.
    internal bool CanTransferPreparedFoundation(SushiDishOrder source, SushiDishOrder target) =>
        CanAssembleDish(source) && CanAssembleDish(target) && tableReservations.TryGetValue(source, out int sourceSlot) &&
        SlotAllowed(sourceSlot) &&
        tableReservations.ContainsKey(target) == target.IsPrepared && source.CanTransferFoundationTo(target);

    // Chuyển phần nền giữa hai món, đồng bộ chỗ giữ trên bàn và báo thay đổi đơn.
    internal bool TryTransferPreparedFoundation(SushiDishOrder source, SushiDishOrder target)
    {
        if (!CanTransferPreparedFoundation(source, target)) return false;
        bool swap = tableReservations.TryGetValue(target, out int targetSlot);
        if (!source.TryTransferFoundationTo(target)) return false;
        int slotIndex = tableReservations[source];
        tableReservations.Remove(source);
        tableReservations[target] = slotIndex;
        if (swap) tableReservations.Add(source, targetSlot);
        // Swap both received and incoming shared layers. Aggregate ingredient totals remain unchanged.
        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Chuyển đĩa đang ghép dở (cơm, cơm + rong biển, cơm + wasabi...) của món source sang món target của khách khác:
    // dùng khi topping khách kia cần đang tới mà khách đó chưa có đĩa nền. Ô đĩa trên bàn và tổng nguyên liệu
    // của cả hai khách được cập nhật; món source quay lại chưa chuẩn bị và sẽ nhận phần nền sau.
    internal bool TryGiveFoundation(SushiDishOrder source, Customer receiver, SushiDishOrder target)
    {
        if (receiver == null || receiver == this || source == null || target == null) return false;
        if (!CanAssembleDish(source) || !receiver.CanAssembleDish(target)) return false;
        if (!tableReservations.TryGetValue(source, out int slot) || !SlotAllowed(slot)) return false;
        if (target.IsPrepared || receiver.tableReservations.ContainsKey(target)) return false;
        // Không có gì đang bay tới đĩa và phần nền đã đáp đủ; topping/đồ trang trí đã đáp thì đi theo đĩa.
        foreach (OrderItem item in source.Ingredients)
        {
            if (item.incomingAmount > 0) return false;
            if (source.IsSharedBaseIngredient(item.typeFood) && item.receivedAmount < item.requiredAmount) return false;
        }
        if (!source.CanTransferFoundationTo(target)) return false;
        var moved = new List<KeyValuePair<PoolType, int>>();
        foreach (OrderItem item in source.Ingredients)
            if (item.receivedAmount > 0) moved.Add(new KeyValuePair<PoolType, int>(item.typeFood, item.receivedAmount));
        foreach (var pair in moved)
            if (receiver.FindOrder(pair.Key) == null || FindOrder(pair.Key) == null) return false;
        if (!source.TryTransferFoundationTo(target)) return false;
        foreach (var pair in moved)
        {
            OrderItem mine = FindOrder(pair.Key);
            mine.receivedAmount = Mathf.Max(0, mine.receivedAmount - pair.Value);
            receiver.FindOrder(pair.Key).receivedAmount += pair.Value;
        }
        ReleaseTable(source);
        receiver.ClaimTable(target, slot);
        OnOrderChanged?.Invoke(this);
        receiver.OnOrderChanged?.Invoke(receiver);
        return true;
    }

    // Hủy một nguyên liệu đang tới và giải phóng đĩa nếu món không còn nguyên liệu nào.
    public bool CancelPreparedIngredientReservation(SushiDishOrder dish, PoolType type)
    {
        if (dish == null || !dish.IsPrepared) return false;
        OrderItem ingredient = null;
        foreach (OrderItem item in dish.Ingredients)
            if (item.typeFood == type) { ingredient = item; break; }
        OrderItem order = FindOrder(type);
        if (ingredient == null || order == null || ingredient.incomingAmount <= 0 || order.incomingAmount <= 0)
            return false;

        ingredient.incomingAmount--;
        order.incomingAmount--;
        bool hasAssembly = false;
        foreach (OrderItem item in dish.Ingredients)
            hasAssembly |= item.receivedAmount > 0 || item.incomingAmount > 0;
        if (!hasAssembly && ReleaseTable(dish)) dish.IsPrepared = false;
        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Ghi nhận nguyên liệu đã đáp lên đĩa và báo món đã đủ thành phần hay chưa.
    public bool CompletePreparedIngredient(SushiDishOrder dish, PoolType type, out bool dishComplete)
    {
        dishComplete = false;
        if (dish == null || !dish.IsPrepared || !tableReservations.ContainsKey(dish)) return false;
        OrderItem ingredient = null;
        foreach (OrderItem item in dish.Ingredients)
            if (item.typeFood == type) { ingredient = item; break; }
        OrderItem order = FindOrder(type);
        if (ingredient == null || order == null || ingredient.incomingAmount <= 0 || order.incomingAmount <= 0)
            return false;

        ingredient.incomingAmount--;
        ingredient.receivedAmount++;
        order.incomingAmount--;
        order.receivedAmount++;
        dishComplete = dish.HasAllIngredients();
        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Đánh dấu toàn bộ nguyên liệu của món sẽ được giao để tránh đặt trùng.
    public bool ReservePreparedDish(SushiDishOrder dish)
    {
        if (!CanPrepareDish(dish)) return false;
        dish.IsPrepared = true;
        foreach (var ingredient in dish.Ingredients)
        {
            ingredient.incomingAmount = ingredient.requiredAmount;
            FindOrder(ingredient.typeFood).incomingAmount += ingredient.requiredAmount;
        }
        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Giữ một vị trí đĩa trống trên bàn cho món đã bắt đầu chuẩn bị.
    public bool TryReserveTable(SushiDishOrder dish, out Transform target)
    {
        target = null;
        if (!IsSeated || completed || RushExpired || !dish.IsPrepared || tableReservations.ContainsKey(dish)) return false;
        int slotIndex = FindFreeServingSlot();
        if (slotIndex < 0) return false;
        ClaimTable(dish, slotIndex);
        target = ServingSlot(slotIndex);
        return true;
    }

    // Tìm vị trí đĩa còn trống: ở bàn chung nếu có, nếu không thì trên bàn riêng của khách.
    private int FindFreeServingSlot()
    {
        if (SharedCounter != null) return SharedCounter.FindFree(ServingSlotFilter);
        if (servingSlots == null) return -1;
        for (int i = 0; i < servingSlots.Length; i++)
            if (servingSlots[i] != null && !tableReservations.ContainsValue(i) && SlotAllowed(i)) return i;
        return -1;
    }

    // Lấy điểm nhận món hoàn chỉnh trên thẻ đơn hàng tương ứng.
    public Transform DishOrderTarget(SushiDishOrder dish) => recipeView.CardAt(dish.Index).DishDeliverySlot;

    // Hủy tiến độ chuẩn bị món, hoàn lại bộ đếm nguyên liệu và giải phóng chỗ trên bàn.
    public void CancelPreparedDish(SushiDishOrder dish)
    {
        if (dish == null || !dish.IsPrepared || dish.IsComplete) return;
        foreach (var ingredient in dish.Ingredients)
        {
            OrderItem order = FindOrder(ingredient.typeFood);
            order.incomingAmount = Mathf.Max(0, order.incomingAmount - ingredient.incomingAmount);
            order.receivedAmount = Mathf.Max(0, order.receivedAmount - ingredient.receivedAmount);
            ingredient.incomingAmount = 0;
            ingredient.receivedAmount = 0;
        }
        ReleaseTable(dish);
        dish.IsPrepared = dish.IsOnTable = false;
        OnOrderChanged?.Invoke(this);
    }

    // Xác nhận món đã giao đủ, hoàn tất đơn món và báo khách hoàn thành nếu đã nhận hết.
    public bool CompletePreparedDish(SushiDishOrder dish)
    {
        if (dish == null || !dish.IsPrepared || dish.IsComplete || !dish.HasAllIngredients() ||
            !ReleaseTable(dish)) return false;
        dish.IsPrepared = dish.IsOnTable = false;
        CompleteDish(dish);
        OnOrderChanged?.Invoke(this);
        if (!completed && IsOrderComplete()) { completed = true; OnCompleted?.Invoke(this); }
        return true;
    }

    [Header("Các vị trí đặt Food trên đĩa")]
    [SerializeField]
    private List<Transform> plateSlots = new List<Transform>();

    [Header("Order hiện tại")]
    [SerializeField]
    private List<OrderItem> orders = new List<OrderItem>();

    private readonly List<FoodShushi> receivedFoods = new List<FoodShushi>();

    // Một Food đang bay giữ một order và một vị trí trên đĩa.
    private readonly Dictionary<FoodShushi, Reservation> reservations = new Dictionary<FoodShushi, Reservation>();
    private readonly HashSet<int> occupiedSlots = new HashSet<int>();
    private bool completed;
    private readonly List<SushiDishOrder> dishes = new List<SushiDishOrder>();
    private readonly List<Transform> recipeSlots = new List<Transform>();
    private readonly Dictionary<FoodShushi, Reservation> settledFoods = new Dictionary<FoodShushi, Reservation>();

    public IReadOnlyList<OrderItem> Orders => orders;
    public IReadOnlyList<FoodShushi> ReceivedFoods => receivedFoods;
    public int PlateCapacity => plateSlots.Count;
    public IReadOnlyList<SushiDishOrder> Dishes => dishes;
    private IReadOnlyList<Transform> CurrentSlots => dishes.Count > 0 ? recipeSlots : plateSlots;
    public bool IsRush { get; private set; }
    public bool IsSeated { get; private set; }
    public bool RushExpired { get; private set; }
    public float RushRemaining { get; private set; }
    public float RushDuration { get; private set; }
    public bool IsCompleted => completed;
    public float ReceivingFoodScale => dishes.Count > 0 ? recipeView.CardAt(0).IngredientAt(0).ReceivingFoodScale : 0.42f;
    // Lấy tỉ lệ hiển thị nguyên liệu tại ô nhận của món hoặc dùng tỉ lệ mặc định.
    public float GetReceivingFoodScale(FoodShushi food)
    {
        if (reservations.TryGetValue(food, out Reservation reservation) && reservation.dish != null)
        {
            int index = 0;
            while (reservation.dish.Ingredients[index] != reservation.dishIngredient) index++;
            return recipeView.CardAt(reservation.dish.Index).IngredientAt(index).ReceivingFoodScale;
        }
        return ReceivingFoodScale;
    }
    // Lấy sprite nguyên liệu của khách, dùng thư viện hình ảnh chung nếu chưa có.
    public Sprite GetIngredientSprite(PoolType type)
    {
        Sprite sprite = ingredientArt != null ? ingredientArt.GetSprite(type) : null;
        return sprite != null ? sprite : SushiArtCatalog.Default != null ? SushiArtCatalog.Default.GetIngredient(type) : null;
    }

    public event Action<Customer> OnCompleted;
    public event Action<Customer> OnOrderChanged;
    public event Action<Customer> OnRushExpired;
    public event Action<Customer, SushiDishOrder> OnDishCompleted;

    // Khởi tạo khách theo dữ liệu đơn món, dựng ô nhận nguyên liệu, đĩa phục vụ và thời gian rush.
    public void Setup(int customerId, Sprite avatarSprite, CustomerOrderData data, SushiDishArtLibrary art = null)
    {
        Setup(customerId, avatarSprite, data.CreateFoodList());
        if (data.UsesRecipes)
        {
            foreach (DishOrderData order in data.dishes)
                for (int portion = 0; portion < order.amount; portion++)
                    dishes.Add(new SushiDishOrder(dishes.Count, order.GetRecipe()));
            recipeView.Setup(this, art);
            foreach (var dish in dishes)
            {
                for (int i = 0; i < dish.Recipe.Ingredients.Count; i++)
                {
                    recipeSlots.Add(recipeView.DeliverySlot(dish.Index, i));
                }
            }
        }
        ConfigureServingSlots(dishes.Count);
        ConfigureRush(data);
        OnOrderChanged?.Invoke(this);
    }

    // Bổ sung, sắp xếp và ẩn hiện các đĩa phục vụ theo số phần món của khách.
    private void ConfigureServingSlots(int count)
    {
        if (servingRoot == null) return;

        var slots = new List<Transform>(servingSlots ?? Array.Empty<Transform>());
        Transform template = slots.Find(slot => slot != null);
        if (count > slots.Count && template == null)
        {
            Debug.LogError("Customer needs at least one serving slot to create dish plates.", this);
            return;
        }

        while (slots.Count < count)
        {
            Transform slot = Instantiate(template, servingRoot, false);
            slot.name = "Plate " + (slots.Count + 1);
            slots.Add(slot);
        }

        servingSlots = slots.ToArray();
        float center = (count - 1) * 0.5f;
        for (int i = 0; i < servingSlots.Length; i++)
        {
            Transform slot = servingSlots[i];
            if (slot == null) continue;

            bool active = i < count;
            slot.gameObject.SetActive(active);
            if (!active) continue;

            Vector3 position = slot.localPosition;
            position.x = (i - center) * 1.08f;
            slot.localPosition = position;
        }
    }

    // Thiết lập trạng thái khách vội và thời hạn chờ theo dữ liệu hoặc số nguyên liệu.
    public void ConfigureRush(CustomerOrderData data)
    {
        IsRush = data.rushCustomer;
        IsSeated = false;
        RushExpired = false;
        int ingredients = 0;
        foreach (var order in orders) ingredients += order.requiredAmount;
        RushDuration = data.rushSeconds > 0 ? data.rushSeconds : 20f + 5f * ingredients;
        RushRemaining = RushDuration;
    }

    [Header("Khách đang chờ (hiện cả người và order nhưng không nổi bật)")]
    [SerializeField, Range(0f, 1f)] private float waitingAlpha = 0.9f;
    [SerializeField] private Color waitingTint = new Color(0.72f, 0.72f, 0.72f, 1f);
    [Tooltip("Mỗi bậc hàng chờ lùi sorting order thêm bấy nhiêu để vẽ sau khách đang ngồi và khách đứng trước.")]
    [SerializeField, Min(1)] private int waitingSortingStep = 4;
    private Canvas[] sortedCanvases;
    private int[] baseSortingOrders;
    private CanvasGroup visualsGroup;
    private UnityEngine.UI.Graphic[] tintedGraphics;
    private Color[] baseGraphicColors;

    // waitingIndex = -1: đang ngồi (bình thường); 0, 1, 2...: vị trí trong hàng chờ (càng sau càng bị che).
    public void SetQueueVisual(int waitingIndex)
    {
        CacheQueueVisuals();
        bool waiting = waitingIndex >= 0;
        int offset = waiting ? -(waitingIndex + 1) * waitingSortingStep : 0;
        for (int i = 0; i < sortedCanvases.Length; i++)
            if (sortedCanvases[i] != null) sortedCanvases[i].sortingOrder = baseSortingOrders[i] + offset;
        if (visualsGroup != null)
        {
            visualsGroup.alpha = waiting ? waitingAlpha : 1f;
            visualsGroup.interactable = visualsGroup.blocksRaycasts = !waiting;
        }
        for (int i = 0; i < tintedGraphics.Length; i++)
            if (tintedGraphics[i] != null) tintedGraphics[i].color = waiting ? baseGraphicColors[i] * waitingTint : baseGraphicColors[i];
    }

    // Khách rời bàn đi ngang sau lưng khách đang ngồi: lùi toàn bộ canvas xuống dưới mọi lớp của khách ngồi, giữ nguyên màu.
    public void SetBehindSeated()
    {
        CacheQueueVisuals();
        if (baseSortingOrders.Length == 0) return;
        int lowest = int.MaxValue, highest = int.MinValue;
        foreach (int order in baseSortingOrders)
        {
            lowest = Mathf.Min(lowest, order);
            highest = Mathf.Max(highest, order);
        }
        int offset = -(highest - lowest + 1);
        for (int i = 0; i < sortedCanvases.Length; i++)
            if (sortedCanvases[i] != null) sortedCanvases[i].sortingOrder = baseSortingOrders[i] + offset;
    }

    // Lưu sorting order và màu gốc một lần để các trạng thái hiển thị tính từ giá trị prefab.
    private void CacheQueueVisuals()
    {
        if (sortedCanvases == null)
        {
            var canvases = new List<Canvas>(GetComponentsInChildren<Canvas>(true));
            canvases.RemoveAll(c => !c.overrideSorting && c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>(true) != null);
            sortedCanvases = canvases.ToArray();
            baseSortingOrders = new int[sortedCanvases.Length];
            for (int i = 0; i < sortedCanvases.Length; i++) baseSortingOrders[i] = sortedCanvases[i].sortingOrder;
            Canvas root = GetComponentInChildren<Canvas>(true);
            if (root != null)
            {
                visualsGroup = root.GetComponent<CanvasGroup>();
                if (visualsGroup == null) visualsGroup = root.gameObject.AddComponent<CanvasGroup>();
            }
            // Chỉ làm tối ảnh nhân vật; order giữ màu nhưng mờ theo CanvasGroup.
            tintedGraphics = avatarImage != null ? new UnityEngine.UI.Graphic[] { avatarImage } : new UnityEngine.UI.Graphic[0];
            baseGraphicColors = new Color[tintedGraphics.Length];
            for (int i = 0; i < tintedGraphics.Length; i++) baseGraphicColors[i] = tintedGraphics[i].color;
        }
    }

    // Đánh dấu khách đã vào ghế và bật phần đơn món cùng bàn phục vụ.
    public void SitDown()
    {
        SetQueueVisual(-1);
        IsSeated = true;
        GetComponent<CustomerRecipeView>()?.SetSeated(true);
        // Có bàn chung thì không hiện các ô đĩa riêng dưới chân khách.
        if (servingRoot != null) servingRoot.gameObject.SetActive(SharedCounter == null);
    }

    // Ẩn bảng order khi khách bắt đầu rời bàn.
    public void HideOrder()
    {
        if (recipeView != null) recipeView.HideOrder();
    }

    // Giảm thời gian chờ của khách vội đang ngồi và phát sự kiện khi hết giờ.
    public void TickRush(float deltaTime)
    {
        if (!IsRush || !IsSeated || completed || RushExpired || deltaTime <= 0) return;
        RushRemaining = Mathf.Max(0, RushRemaining - deltaTime);
        if (RushRemaining > 0) return;
        RushExpired = true;
        OnRushExpired?.Invoke(this);
    }

    // Gia hạn thêm 30 giây cho khách vội đã hết thời gian chờ.
    public void ReviveRush()
    {
        if (!IsRush || !RushExpired || completed) return;
        RushRemaining += 30f;
        RushDuration = Mathf.Max(RushDuration, RushRemaining);
        RushExpired = false;
    }

    private class Reservation
    {
        public OrderItem order;
        public int slotIndex;
        public SushiDishOrder dish;
        public OrderItem dishIngredient;
        public FoodLocation previousLocation;
    }

    // Gọi trên Customer mới được tạo.
    // Dọn trạng thái khách cũ, gán avatar và gom danh sách nguyên liệu thành các mục đơn hàng.
    public void Setup( int customerId, Sprite avatarSprite, List<PoolType> requestedFoods)
    {
        id = customerId;
        completed = false;
        IsSeated = false;

        orders.Clear();
        dishes.Clear();
        if (recipeView != null) recipeView.Clear();
        recipeSlots.Clear();
        settledFoods.Clear();
        receivedFoods.Clear();
        reservations.Clear();
        occupiedSlots.Clear();
        SharedCounter?.ReleaseAll(this);
        tableReservations.Clear();
        if (servingRoot != null) servingRoot.gameObject.SetActive(false);
        ConfigureServingSlots(0);

        if (avatarImage != null)
        {
            avatarImage.sprite = avatarSprite;
            avatarImage.enabled = avatarSprite != null;
        }

        // Gộp các món cùng loại thành một OrderItem có số lượng.
        foreach (PoolType type in requestedFoods)
        {
            OrderItem order = FindOrder(type);

            if (order == null)
            {
                order = new OrderItem
                {
                    typeFood = type,
                    requiredAmount = 0,
                    receivedAmount = 0,
                    incomingAmount = 0
                };

                orders.Add(order);
            }

            order.requiredAmount++;
        }

        OnOrderChanged?.Invoke(this);
    }

    // Kiểm tra khách còn cần loại nguyên liệu này và còn ô nhận phù hợp.
    public bool CanReceive(PoolType type)
    {
        if (completed || RushExpired || !isActiveAndEnabled)  return false;

        OrderItem order = FindOrder(type);

        return order != null
            && order.CanReceive
            && FindFreeSlot() >= 0;
    }

    // Thử giữ ô nhận phù hợp cho nguyên liệu đang ở trên băng chuyền.
    public bool TryReserveFood(FoodShushi food, out Transform target) => TryReserveFood(food, null, false, out target);

    // Giữ ô nhận cho một món cụ thể, cho phép lấy cả nguyên liệu đang bay lên băng chuyền.
    internal bool TryReserveFoodForDish(FoodShushi food, SushiDishOrder dish, out Transform target) =>
        TryReserveFood(food, dish, true, out target);

    // Giữ ô nhận và bộ đếm cho nguyên liệu, đồng thời chuyển nó sang trạng thái đang giao.
    private bool TryReserveFood(FoodShushi food, SushiDishOrder requestedDish, bool allowPickupFlight, out Transform target)
    {
        target = null;

        if (food == null || reservations.ContainsKey(food) || !CanReceive(food.Type))
        {
            return false;
        }

        OrderItem order = FindOrder(food.Type);
        int slotIndex = FindFreeSlot();
        SushiDishOrder targetDish = null;
        OrderItem dishIngredient = null;
        if (dishes.Count > 0)
        {
            foreach (var dish in dishes)
            {
                if (requestedDish != null && requestedDish != dish) continue;
                dishIngredient = dish.FindAvailableIngredient(food.Type);
                if (dishIngredient == null) continue;
                targetDish = dish;
                break;
            }
            if (targetDish == null) return false;
            slotIndex = FindFreeSlot(targetDish, food.Type);
            if (slotIndex < 0) return false;
        }

        // Ngăn một Food được giao cho hai khách.
        FoodLocation previousLocation = food.Location;
        if (!food.TryMoveToCustomerPlate(allowPickupFlight)) return false;

        order.incomingAmount++;
        if (dishIngredient != null) dishIngredient.incomingAmount++;
        occupiedSlots.Add(slotIndex);

        reservations.Add(food, new Reservation
        {
            order = order,
            slotIndex = slotIndex,
            dish = targetDish,
            dishIngredient = dishIngredient,
            previousLocation = previousLocation
        });

        target = CurrentSlots[slotIndex];

        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Chỉ gọi khi Food đã bay tới đĩa.
    // Chốt nguyên liệu đã đến đĩa, cập nhật đơn món và kiểm tra khách đã nhận đủ.
    public bool CompleteReceive(FoodShushi food)
    {
        if (food == null || food.Location != FoodLocation.MovingToCustomerPlate || !reservations.TryGetValue(food, out Reservation reservation))
        {
            return false;
        }

        Transform target = CurrentSlots[reservation.slotIndex];

        food.transform.SetParent(target, true);
        food.transform.localPosition = Vector3.zero;
        food.ArriveAtCustomerPlate();

        reservations.Remove(food);

        reservation.order.incomingAmount--;
        reservation.order.receivedAmount++;

        receivedFoods.Add(food);
        settledFoods[food] = reservation;
        if (reservation.dishIngredient != null)
        {
            reservation.dishIngredient.incomingAmount--;
            reservation.dishIngredient.receivedAmount++;
            if (reservation.dish.HasAllIngredients()) CompleteDish(reservation.dish);
        }

        OnOrderChanged?.Invoke(this);

        if (!completed && IsOrderComplete())
        {
            completed = true;
            OnCompleted?.Invoke(this);
        }

        return true;
    }

    // Dùng khi animation giao món bị hủy và Food được trả về băng chuyền.
    public bool CancelReceive(FoodShushi food)
    {
        if (food == null || !reservations.TryGetValue(food, out Reservation reservation))
        {
            return false;
        }

        reservations.Remove(food);
        reservation.order.incomingAmount--;
        if (reservation.dishIngredient != null) reservation.dishIngredient.incomingAmount--;
        occupiedSlots.Remove(reservation.slotIndex);
        food.CancelMoveToCustomerPlate(reservation.previousLocation);

        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Tìm mục đơn tổng ứng với một loại nguyên liệu.
    private OrderItem FindOrder(PoolType type)
    {
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].typeFood == type)
                return orders[i];
        }

        return null;
    }

    // Tìm ô nhận chưa bị giữ, giới hạn theo món và loại nguyên liệu nếu được chỉ định.
    private int FindFreeSlot(SushiDishOrder targetDish = null, PoolType type = PoolType.None)
    {
        int start = 0, end = CurrentSlots.Count;
        if (targetDish != null)
        {
            foreach (var dish in dishes)
            {
                if (dish == targetDish) break;
                start += dish.Recipe.Ingredients.Count;
            }
            end = start + targetDish.Recipe.Ingredients.Count;
        }
        for (int i = start; i < end; i++)
        {
            if (targetDish != null && targetDish.Recipe.Ingredients[i - start] != type) continue;
            if (CurrentSlots[i] != null && !occupiedSlots.Contains(i))
                return i;
        }

        return -1;
    }

    // Kiểm tra toàn bộ món hoặc đơn nguyên liệu của khách đã hoàn tất.
    private bool IsOrderComplete()
    {
        if (dishes.Count > 0)
        {
            foreach (var dish in dishes) if (!dish.IsComplete) return false;
            return true;
        }
        if (orders.Count == 0)  return false;

        foreach (OrderItem order in orders)
        {
            if (!order.IsComplete)   return false;
        }
        return true;
    }

    // Đánh dấu món hoàn thành, thu hồi nguyên liệu đã nhận và phát sự kiện hoàn tất món.
    private void CompleteDish(SushiDishOrder dish)
    {
        if (dish.IsComplete) return;
        dish.IsComplete = true;
        // Nguyên liệu đã ghép thành món; trả về pool để chỉ còn món thành phẩm.
        for (int i = receivedFoods.Count - 1; i >= 0; i--)
        {
            FoodShushi ingredient = receivedFoods[i];
            if (!settledFoods.TryGetValue(ingredient, out var reservation) || reservation.dish != dish) continue;
            settledFoods.Remove(ingredient);
            occupiedSlots.Remove(reservation.slotIndex);
            receivedFoods.RemoveAt(i);
            ingredient.transform.SetParent(null, true);
            SimplePool.Despawn(ingredient);
        }
        OnDishCompleted?.Invoke(this, dish);
    }
}
