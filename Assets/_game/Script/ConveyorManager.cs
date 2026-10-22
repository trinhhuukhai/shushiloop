using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private ConveyorSpline spline;
    [SerializeField] private Transform slotPrefab;
    [SerializeField] private Transform slotRoot;

    [Header("Cấu hình")]
    [Min(1)]
    [SerializeField] private int slotCount = 14;

    [Min(0f)]
    [SerializeField] private float loopsPerSecond = 0.08f;

    [SerializeField] private float positionZ;
    [SerializeField] private bool playOnStart = true;

    private readonly List<Transform> slots = new List<Transform>();

    private float progress;
    private bool isRunning;
    private bool doubleSpeed;
    private LineRenderer track;
    private Transform trackVisuals;

    [Header("Food nhảy lên")]
    [SerializeField, Min(0.1f)] private float jumpDuration = 0.42f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.95f;
    [SerializeField, Min(0f)] private float pickupStagger = 0.055f;
    [SerializeField, Min(0f)] private float trayPickupStagger = 0.085f;
    [SerializeField, Min(0f)] private float takeoffDuration = 0.065f;
    [SerializeField, Min(0.01f)] private float landingDuration = 0.1f;
    [SerializeField, Min(0.1f)] private float beltFoodScale = 0.78f;

    private readonly Dictionary<Transform, FoodShushi> slotFoods =
        new Dictionary<Transform, FoodShushi>();


    public IReadOnlyList<Transform> Slots => slots;

    //private void Start()
    //{
    //    if (playOnStart)
    //        Init(slotCount, loopsPerSecond);
    //}

    /// <summary>
    /// /
    /// 
    /// </summary>
    /// <param name="count"></param>
    /// <param name="speed"></param>
    [Header("Giao món cho khách")]
    [SerializeField] private CustomerManager customerManager;

    [SerializeField] private Transform flyingFoodRoot;

    [Min(0.01f)]
    [SerializeField] private float deliveryRange = 0.3f;

    [Min(0.01f)]
    [SerializeField] private float deliveryDuration = 0.48f;

    [Min(0f)]
    [SerializeField] private float deliveryHeight = 0.75f;

    private readonly Dictionary<FoodShushi, Customer> deliveringFoods =
        new Dictionary<FoodShushi, Customer>();

    private readonly object deliveryTweenId = new object();

    public int DeliveringCount => deliveringFoods.Count;
    public CustomerManager Customers => customerManager;
    public void Init(int count, float speed)
    {
        if (spline == null || slotPrefab == null || slotRoot == null || customerManager == null || flyingFoodRoot == null)
        {
            Debug.LogError(
                "Chưa gán CustomerManager hoặc FlyingFoodRoot.",
                this
            );
            return;
        }

        for (int i = 0; i < customerManager.ActiveCustomers.Count; i++)
        {
            if (customerManager.GetSeat(i) == null)
            {
                Debug.LogError($"Chưa gán ghế nhận nguyên liệu {i + 1}.", this);
                return;
            }
        }
        Clear();

        slotCount = Mathf.Max(1, count);
        loopsPerSecond = Mathf.Max(0f, speed);

        spline.Initialize();
        RefreshTrack();
        BuildDeliveryPoints();
        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = Instantiate(slotPrefab, slotRoot);
            slot.name = "Slot_" + i;
            slot.gameObject.SetActive(true);
            Sprite plate = SushiArtCatalog.Default != null ? SushiArtCatalog.Default.plate : null;
            SpriteRenderer plateRenderer = slot.GetComponent<SpriteRenderer>();
            if (plate != null && plateRenderer != null)
            {
                // Keep the slot at unit scale; only the plate visual is resized.
                plateRenderer.enabled = false;
                var visual = SushiWorldVisual.Panel(slot, "Ceramic plate", 0);
                SushiWorldVisual.FitSprite(visual, plate, new Vector2(1.03f, 0.82f));
            }
            slots.Add(slot);
        }

        progress = 0f;
        doubleSpeed = false;

        UpdateSlotPositions();
        isRunning = true;
        NotifyOccupancy();
    }

    private void Update()
    {
        if (!isRunning || slots.Count == 0)
            return;

        float previousProgress = progress;

        float speed = loopsPerSecond * (doubleSpeed ? 2f : 1f);
        float advance = speed * Time.deltaTime;

        progress = Mathf.Repeat(progress + advance, 1f);

        UpdateSlotPositions();
        CheckCustomerOrders(previousProgress, advance);
    }

    private void UpdateSlotPositions()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // Chia đều các vị trí theo chiều dài vòng.
            float slotProgress = progress + (float)i / slots.Count;

            Vector2 position = spline.AtDistance(slotProgress);

            slots[i].position = new Vector3(
                position.x,
                position.y,
                positionZ
            );
        }
    }

    public void SetPaused(bool paused)
    {
        isRunning = !paused && slots.Count > 0;
    }

    public void ToggleSpeed()
    {
        doubleSpeed = !doubleSpeed;
    }

    public void Clear()
    {
        isRunning = false;
        DOTween.Kill(pickupTweenId);
        ClearDeliveringFoods();
        foreach (FoodShushi food in slotFoods.Values)
        {
            if (food == null)
                continue;

            food.transform.DOKill();

            // Tách ra để không bị Destroy theo slot.
            food.transform.SetParent(null, true);
            SimplePool.Despawn(food);
        }

        slotFoods.Clear();

        foreach (Transform slot in slots)
        {
            if (slot == null)
                continue;

            slot.gameObject.SetActive(false);
            Destroy(slot.gameObject);
        }

        slots.Clear();
        progress = 0f;
        NotifyOccupancy();
    }


    public int OccupiedSlotCount => slotFoods.Count;
    public int FreeSlotCount => slots.Count - slotFoods.Count;
    public int Capacity => slots.Count;
    private readonly object pickupTweenId = new object();
    public event System.Action<int, int> OnOccupancyChanged;
    private void NotifyOccupancy() => OnOccupancyChanged?.Invoke(OccupiedSlotCount, Capacity);
    private bool CanInteract => isRunning && (LevelManager.Ins == null || LevelManager.Ins.CanInteract);

    public bool TryTakeFromQueue(FoodQueue queue)
    {
        if (!CanInteract || queue == null || queue.Board == null) return false;
        if (!queue.Board.TryBuildTap(queue, out List<FoodPickup> plan)) return false;
        return TryTakePlan(queue.Board, plan, FoodPickMode.Tap);
    }

    public bool TryTakeChopsticks(FoodShushi food)
    {
        if (!CanInteract || food == null || food.Owner == null) return false;
        BoardShishiManager board = food.Owner.Board;
        return board != null && board.TryBuildChopsticks(food, out var plan) &&
            TryTakePlan(board, plan, FoodPickMode.Chopsticks);
    }

    public bool TryTakeMagnet(BoardShishiManager board, Customer customer, SushiDishOrder dish)
    {
        if (!CanInteract || board == null || customer == null || dish == null || !customer.IsSeated ||
            customer.IsCompleted || customer.RushExpired || dish.IsComplete || customerManager == null) return false;
        bool active = false, ownsDish = false;
        foreach (Customer seated in customerManager.ActiveCustomers) active |= seated == customer;
        foreach (SushiDishOrder order in customer.Dishes) ownsDish |= order == dish;
        if (!active || !ownsDish) return false;

        var boardPlan = new List<FoodPickup>();
        var beltPlan = new List<KeyValuePair<Transform, FoodShushi>>();
        int needed = 0;
        foreach (OrderItem ingredient in dish.Ingredients)
        {
            int remaining = ingredient.requiredAmount - ingredient.receivedAmount - ingredient.incomingAmount;
            needed += Mathf.Max(0, remaining);
            // Ưu tiên nguyên liệu trên vòng xoay, kể cả đang nhảy tới đĩa đã giữ chỗ.
            foreach (Transform slot in slots)
            {
                if (remaining <= 0) break;
                if (!slotFoods.TryGetValue(slot, out FoodShushi food) || food.Type != ingredient.typeFood) continue;
                beltPlan.Add(new KeyValuePair<Transform, FoodShushi>(slot, food));
                remaining--;
            }
            if (remaining <= 0) continue;
            List<FoodPickup> pickups = board.BuildMagnet(ingredient.typeFood, remaining);
            foreach (FoodPickup pickup in pickups) { boardPlan.Add(pickup); remaining -= pickup.amount; }
            // Thiếu bất kỳ nguyên liệu nào: giữ nguyên board, vòng xoay và order.
            if (remaining > 0) return false;
        }
        if (needed == 0) return false;

        var spawned = new List<FoodShushi>();
        var units = new List<FoodShushi>();
        try
        {
            foreach (FoodPickup pickup in boardPlan)
                for (int i = 0; i < pickup.amount; i++)
                {
                    FoodShushi unit = SimplePool.Spawn<FoodShushi>(pickup.food.Type);
                    if (unit == null) throw new System.InvalidOperationException("Thiếu prefab nguyên liệu " + pickup.food.Type);
                    spawned.Add(unit);
                    unit.SetupFromData(new FoodData { typeFood = pickup.food.Type });
                    unit.transform.SetParent(flyingFoodRoot, false);
                    unit.transform.position = pickup.food.transform.position;
                    unit.transform.localRotation = Quaternion.identity;
                    SetWorldScale(unit.transform, pickup.food.transform.lossyScale);
                    unit.BeginConveyorMove(false);
                    units.Add(unit);
                }
        }
        catch (System.Exception exception)
        {
            foreach (FoodShushi unit in spawned) SimplePool.Despawn(unit);
            Debug.LogException(exception, this);
            return false;
        }
        foreach (var pickup in beltPlan) units.Add(pickup.Value);
        var targets = new List<Transform>();
        foreach (FoodShushi unit in units)
        {
            if (customer.TryReserveFoodForDish(unit, dish, out Transform target)) { targets.Add(target); continue; }
            for (int i = 0; i < targets.Count; i++) customer.CancelReceive(units[i]);
            foreach (FoodShushi created in spawned) SimplePool.Despawn(created);
            return false;
        }
        foreach (var pickup in beltPlan)
        {
            pickup.Value.transform.DOKill();
            pickup.Value.RevealForMagnet();
            slotFoods.Remove(pickup.Key);
        }
        if (boardPlan.Count > 0) board.CommitPickup(boardPlan, FoodPickMode.Magnet, 0);
        for (int i = 0; i < units.Count; i++) FlyToCustomer(units[i], customer, targets[i]);
        NotifyOccupancy();
        return true;
    }

    private bool TryTakePlan(BoardShishiManager board, IReadOnlyList<FoodPickup> plan, FoodPickMode mode)
    {
        int total = 0;
        var seen = new HashSet<FoodShushi>();
        foreach (FoodPickup item in plan)
        {
            if (item.food == null || item.food.Owner == null || item.food.Owner.IsPickupPending || item.food.Owner.Board != board ||
                item.amount < 0 || item.amount > item.food.Amount || !seen.Add(item.food)) return false;
            total += item.amount;
        }
        // Giữ chỗ cả nhóm trước khi thay đổi bất cứ ô/cột nào, gồm món đang bay.
        if (total > FreeSlotCount)
        {
            board.ShakePickup(plan);
            return false;
        }
        var targets = new List<Transform>();
        foreach (Transform slot in slots)
            if (slot != null && !slotFoods.ContainsKey(slot)) targets.Add(slot);
        Vector3 origin = plan[0].food.transform.position;
        targets.Sort((a, b) => (a.position - origin).sqrMagnitude.CompareTo((b.position - origin).sqrMagnitude));
        if (targets.Count < total) { board.ShakePickup(plan); return false; }
        var units = new List<FoodShushi>();
        var hiddenUnits = new List<bool>();
        var flightDurations = new List<float>();
        float stagger = mode == FoodPickMode.Tap && plan[0].food.TrayId > 0 ? trayPickupStagger : pickupStagger;
        float takeoffDelay = 0;
        try
        {
            foreach (FoodPickup item in plan)
            {
                for (int i = 0; i < item.amount; i++)
                {
                    FoodShushi unit = SimplePool.Spawn<FoodShushi>(item.food.Type);
                    if (unit == null) throw new System.InvalidOperationException("Thiếu prefab nguyên liệu " + item.food.Type);
                    units.Add(unit);
                    unit.SetupFromData(new FoodData { typeFood = item.food.Type });
                    // Chờ ở vị trí board trong world space; không trôi theo đĩa khi chưa cất cánh.
                    unit.transform.SetParent(flyingFoodRoot, false);
                    unit.transform.position = item.food.transform.position;
                    unit.transform.localRotation = Quaternion.identity;
                    SetWorldScale(unit.transform, item.food.transform.lossyScale);
                    hiddenUnits.Add(item.food.IsHidden);
                    unit.BeginConveyorMove(item.food.IsHidden);
                    float duration = TravelDuration(jumpDuration, unit.transform.position, targets[units.Count - 1].position);
                    flightDurations.Add(duration);
                    float delay = (item.food.IsHidden ? 0.25f : 0) + (units.Count - 1) * stagger;
                    takeoffDelay = Mathf.Max(takeoffDelay, delay + Mathf.Max(0, takeoffDuration));
                }
            }
        }
        catch (System.Exception exception)
        {
            foreach (FoodShushi unit in units)
            {
                unit.transform.SetParent(null, true);
                SimplePool.Despawn(unit);
            }
            Debug.LogException(exception, this);
            return false;
        }
        for (int i = 0; i < units.Count; i++) slotFoods.Add(targets[i], units[i]);
        board.CommitPickup(plan, mode, takeoffDelay);
        for (int i = 0; i < units.Count; i++)
        {
            FoodShushi unit = units[i];
            Transform target = targets[i];
            AnimateFlight(unit, target, flightDurations[i], jumpHeight, beltFoodScale,
                (hiddenUnits[i] ? 0.25f : 0) + i * stagger, takeoffDuration, pickupTweenId, () =>
                {
                    if (unit == null || target == null) return;
                    unit.transform.SetParent(target, false);
                    unit.transform.localPosition = Vector3.zero;
                    unit.transform.localRotation = Quaternion.identity;
                    unit.transform.localScale = Vector3.one * beltFoodScale;
                    unit.ArriveAtConveyor();
                });
        }
        NotifyOccupancy();
        return true;
    }
    private void CheckCustomerOrders(
        float previousProgress,
        float advance)
    {
        if (customerManager == null || deliveryProgresses == null)
            return;

        IReadOnlyList<Customer> customers =
            customerManager.ActiveCustomers;

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            Transform slot = slots[slotIndex];

            if (slot == null ||
                !slotFoods.TryGetValue(slot, out FoodShushi food) ||
                food == null ||
                food.Location != FoodLocation.Conveyor)
            {
                continue;
            }

            float slotOffset = (float)slotIndex / slots.Count;

            float previousSlotProgress =
                Mathf.Repeat(previousProgress + slotOffset, 1f);

            float currentSlotProgress =
                Mathf.Repeat(progress + slotOffset, 1f);

            for (int i = 0;
                 i < customers.Count && i < deliveryProgresses.Length;
                 i++)
            {
                Customer customer = customers[i];

                if (customer == null || customerManager.FirstCustomerFor(food.Type) != customer)
                    continue;

                float gateProgress = deliveryProgresses[i];

                // Slot đã đi qua điểm nhận trong frame này.
                float distanceToGate =
                    Mathf.Repeat(gateProgress - previousSlotProgress, 1f);

                bool crossedGate =
                    advance > 0f && distanceToGate <= advance;

                // Hoặc đang ở sát điểm nhận.
                float difference =
                    Mathf.Abs(currentSlotProgress - gateProgress);

                float circularDistance =
                    Mathf.Min(difference, 1f - difference);

                bool nearGate =
                    circularDistance <= 0.15f / slots.Count;

                if (!crossedGate && !nearGate)
                    continue;

                // Khách phải còn thiếu món và còn chỗ trên đĩa.
                if (!customer.TryReserveFood(food, out Transform target))
                    continue;

                slotFoods.Remove(slot);
                NotifyOccupancy();

                FlyToCustomer(food, customer, target);
                break;
            }
        }
    }

    private void FlyToCustomer(
    FoodShushi food,
    Customer customer,
    Transform target)
    {
        food.transform.DOKill();

        // Tách Food khỏi slot đang chạy, giữ nguyên vị trí world.
        food.transform.SetParent(flyingFoodRoot, true);

        deliveringFoods.Add(food, customer);

        AnimateFlight(food, target, TravelDuration(deliveryDuration, food.transform.position, target.position),
            deliveryHeight, customer.ReceivingFoodScale, 0, 0, deliveryTweenId, () =>
        {
            if (food == null || customer == null || target == null)
            {
                Debug.LogError(
                    "Mất Food hoặc khách trong lúc giao. " +
                    "Cần Clear băng chuyền trước khi reset khách.",
                    this
                );
                return;
            }

            // Xóa trước khi Customer phát sự kiện hoàn thành.
            deliveringFoods.Remove(food);

            if (!customer.CompleteReceive(food))
            {
                // Giữ tham chiếu để Clear() vẫn dọn được món này.
                deliveringFoods[food] = customer;

                Debug.LogError("Không xác nhận được Food đã tới đĩa.", this);
            }
        });
    }

    private static float TravelDuration(float duration, Vector3 start, Vector3 end) =>
        Mathf.Max(0.1f, duration) + Mathf.Min(0.16f, Vector3.Distance(start, end) * 0.025f);

    private static void SetWorldScale(Transform subject, Vector3 scale)
    {
        Vector3 parentScale = subject.parent != null ? subject.parent.lossyScale : Vector3.one;
        subject.localScale = new Vector3(scale.x / Nonzero(parentScale.x), scale.y / Nonzero(parentScale.y), scale.z / Nonzero(parentScale.z));
    }

    private static float Nonzero(float value) => Mathf.Abs(value) < 0.0001f ? 1f : value;

    private void AnimateFlight(FoodShushi food, Transform target, float duration, float height, float endScale,
        float delay, float anticipation, object tweenId, System.Action onComplete)
    {
        Transform subject = food.transform;
        Vector3 origin = subject.position;
        Vector3 startScale = subject.lossyScale;
        Quaternion startRotation = subject.rotation;
        float lift = Mathf.Max(0, height) + Mathf.Min(0.25f, Mathf.Abs(target.position.x - origin.x) * 0.04f);
        var sequence = DOTween.Sequence().AppendInterval(Mathf.Max(0, delay));
        if (anticipation > 0)
            sequence.Append(DOVirtual.Float(0, 1, anticipation, t =>
            {
                if (subject == null) return;
                subject.position = origin;
                SetWorldScale(subject, Vector3.Scale(startScale, new Vector3(1 + 0.08f * t, 1 - 0.08f * t, 1)));
            }).SetEase(Ease.OutSine));
        sequence.Append(DOVirtual.Float(0, 1, duration, t =>
        {
            if (subject == null || target == null) return;
            // Đọc đích mỗi frame: bám đúng đĩa đang chạy và ô order của khách đang di chuyển.
            subject.position = Vector3.Lerp(origin, target.position, t) + Vector3.up * (4 * lift * t * (1 - t));
            float arc = Mathf.Sin(Mathf.PI * t);
            float squash = anticipation > 0 ? 0.08f * (1 - Mathf.Clamp01(t * 5)) : 0;
            Vector3 shape = new Vector3(1 + squash - 0.035f * arc, 1 - squash + 0.07f * arc, 1);
            SetWorldScale(subject, Vector3.Scale(Vector3.Lerp(startScale, target.lossyScale * endScale, t), shape));
            float tilt = -Mathf.Sign(target.position.x - origin.x) * 11f * arc;
            subject.rotation = Quaternion.Slerp(startRotation, target.rotation, t) * Quaternion.Euler(0, 0, tilt);
        }).SetEase(Ease.InOutSine));
        sequence.Append(DOVirtual.Float(0, 1, Mathf.Max(0.01f, landingDuration), t =>
        {
            if (subject == null || target == null) return;
            subject.position = target.position;
            subject.rotation = target.rotation;
            float squash = Mathf.Sin(Mathf.PI * t);
            SetWorldScale(subject, Vector3.Scale(target.lossyScale * endScale, new Vector3(1 + 0.09f * squash, 1 - 0.08f * squash, 1)));
        }).SetEase(Ease.Linear));
        sequence.SetTarget(subject).SetId(tweenId).OnComplete(() => onComplete?.Invoke());
    }

    private float[] deliveryProgresses;

    private void RefreshTrack()
    {
        if (trackVisuals != null) Destroy(trackVisuals.gameObject);
        trackVisuals = new GameObject("Conveyor world sprites").transform;
        trackVisuals.SetParent(transform, false);
        Sprite artwork = SushiArtCatalog.Default != null ? SushiArtCatalog.Default.conveyor : null;
        if (artwork != null)
        {
            var frame = SushiWorldVisual.Panel(trackVisuals, "Conveyor frame sprite", -16);
            frame.transform.position = new Vector3(spline.center.x, spline.center.y - 0.18f, positionZ);
            SushiWorldVisual.FitSprite(frame, artwork, spline.halfSize * 2f + new Vector2(1.28f, 1.28f));
            return;
        }
        TrackLayer("Frame shadow", 1.26f, new Color(0.27f, 0.16f, 0.10f), -15, -0.10f);
        TrackLayer("Maple frame", 1.22f, new Color(0.78f, 0.60f, 0.39f), -14);
        TrackLayer("Ivory rim", 1.11f, new Color(0.99f, 0.90f, 0.72f), -13);
        TrackLayer("Inner steel edge", 0.98f, new Color(0.35f, 0.43f, 0.46f), -12);
        track = TrackLayer("Slate conveyor belt", 0.90f, new Color(0.48f, 0.57f, 0.60f), -11);
        var well = SushiWorldVisual.Panel(trackVisuals, "Counter center", -16);
        well.transform.position = new Vector3(spline.center.x, spline.center.y, positionZ);
        well.transform.localScale = new Vector3(spline.halfSize.x * 2f - 0.5f, spline.halfSize.y * 2f - 0.5f, 1);
        well.color = new Color(0.34f, 0.16f, 0.085f);
    }

    private LineRenderer TrackLayer(string name, float width, Color color, int order, float offsetY = 0)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(trackVisuals, false);
        line.sharedMaterial = SushiWorldVisual.LineMaterial;
        line.useWorldSpace = true;
        line.loop = true;
        line.numCornerVertices = 4;
        line.sortingOrder = order;
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        const int segments = 192;
        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            Vector2 point = spline.AtDistance((float)i / segments);
            line.SetPosition(i, new Vector3(point.x, point.y + offsetY, positionZ));
        }
        return line;
    }

    private void BuildDeliveryPoints()
    {
        deliveryProgresses = new float[customerManager.ActiveCustomers.Count];

        for (int i = 0; i < deliveryProgresses.Length; i++)
        {
            deliveryProgresses[i] = (float)spline.ClosestDistance(customerManager.GetSeat(i).position);
        }
    }

    private void ClearDeliveringFoods()
    {
        DOTween.Kill(deliveryTweenId);

        foreach (KeyValuePair<FoodShushi, Customer> item in deliveringFoods)
        {
            FoodShushi food = item.Key;
            Customer customer = item.Value;

            if (food == null)
                continue;

            if (customer != null)
                customer.CancelReceive(food);

            food.transform.SetParent(null, true);
            SimplePool.Despawn(food);
        }

        deliveringFoods.Clear();
    }
}
