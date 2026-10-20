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

    [Header("Food nhảy lên")]
    [SerializeField] private float jumpDuration = 0.3f;
    [SerializeField] private float jumpHeight = 0.8f;

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

    [Tooltip("Mỗi điểm nằm trên băng chuyền, phía trước một ghế khách.")]
    [SerializeField] private Transform[] deliveryPoints = new Transform[2];

    [SerializeField] private Transform flyingFoodRoot;

    [Min(0.01f)]
    [SerializeField] private float deliveryRange = 0.3f;

    [Min(0.01f)]
    [SerializeField] private float deliveryDuration = 0.4f;

    [Min(0f)]
    [SerializeField] private float deliveryHeight = 0.6f;

    private readonly Dictionary<FoodShushi, Customer> deliveringFoods =
        new Dictionary<FoodShushi, Customer>();

    private readonly object deliveryTweenId = new object();

    public int DeliveringCount => deliveringFoods.Count;
    public void Init(int count, float speed)
    {
        if (customerManager == null || flyingFoodRoot == null)
        {
            Debug.LogError(
                "Chưa gán CustomerManager hoặc FlyingFoodRoot.",
                this
            );
            return;
        }

        if (deliveryPoints == null || deliveryPoints.Length != 2)
        {
            Debug.LogError("Cần hai Delivery Points tương ứng hai ghế khách.", this);
            return;
        }

        for (int i = 0; i < deliveryPoints.Length; i++)
        {
            if (deliveryPoints[i] == null)
            {
                Debug.LogError($"Chưa gán Delivery Points[{i}].", this);
                return;
            }
        }
        Clear();

        slotCount = Mathf.Max(1, count);
        loopsPerSecond = Mathf.Max(0f, speed);

        spline.Initialize();
        BuildDeliveryPoints();
        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = Instantiate(slotPrefab, slotRoot);
            slot.name = "Slot_" + i;
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }

        progress = 0f;
        doubleSpeed = false;

        UpdateSlotPositions();
        isRunning = true;
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
    }


    public bool TryTakeFromQueue(FoodQueue queue)
    {
        if (!isRunning || queue == null || !queue.CanTakeTopFood())
            return false;

        FoodShushi topFood = queue.GetTopFood();

        Transform targetSlot = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (Transform slot in slots)
        {
            if (slot == null || slotFoods.ContainsKey(slot))
                continue;

            float distance =
                (slot.position - topFood.transform.position).sqrMagnitude;

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            targetSlot = slot;
        }

        // Đầy băng chuyền: không lấy món khỏi cột.
        if (targetSlot == null)
            return false;

        // Giữ slot trước khi lấy món.
        slotFoods.Add(targetSlot, topFood);

        if (!queue.TryTakeTopFood(targetSlot, out FoodShushi food))
        {
            slotFoods.Remove(targetSlot);
            return false;
        }

        // Food đã là con của slot nên đích vẫn đúng khi slot di chuyển.
        food.transform
            .DOLocalJump(
                Vector3.zero,
                jumpHeight,
                1,
                Mathf.Max(0.01f, jumpDuration)
            )
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (food != null)
                    food.ArriveAtConveyor();
            });

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

                if (customer == null)
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

                Debug.Log(
                    $"Giao {food.name} cho khách {customer.id}, " +
                    $"đích: {target.name}",
                    customer
                );

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

        Vector3 startPosition = food.transform.position;

        deliveringFoods.Add(food, customer);

        DOVirtual.Float(
            0f,
            1f,
            Mathf.Max(0.01f, deliveryDuration),
            progress =>
            {
                if (food == null || target == null)
                    return;

                Vector3 position = Vector3.Lerp(
                    startPosition,
                    target.position,
                    progress
                );

                position += Vector3.up *
                    (4f * deliveryHeight * progress * (1f - progress));

                food.transform.position = position;
            }
        )
        .SetEase(Ease.Linear)
        .SetId(deliveryTweenId)
        .OnComplete(() =>
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

    private float FindDeliveryProgress(Vector2 targetPosition)
    {
        const int samples = 256;

        float nearestProgress = 0f;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < samples; i++)
        {
            float sampleProgress = (float)i / samples;
            Vector2 position = spline.AtDistance(sampleProgress);

            float distance = (position - targetPosition).sqrMagnitude;

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestProgress = sampleProgress;
        }

        return nearestProgress;
    }

    private float[] deliveryProgresses;

    private void BuildDeliveryPoints()
    {
        deliveryProgresses = new float[deliveryPoints.Length];

        for (int i = 0; i < deliveryPoints.Length; i++)
        {
            deliveryProgresses[i] =
                FindDeliveryProgress(deliveryPoints[i].position);
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