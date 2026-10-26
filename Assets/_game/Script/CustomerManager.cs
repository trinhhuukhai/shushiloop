using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private Customer customerPrefab;

    [Header("Hai vị trí nhận đồ ăn")]
    [SerializeField] private Transform firstSeat;
    [SerializeField] private Transform secondSeat;

    [Header("Hàng chờ: pos1, pos2, pos3...")]
    [SerializeField]
    private List<Transform> waitingPositions =
        new List<Transform>();

    [Header("Di chuyển")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField, Range(0.3f, 1f)] private float waitingScale = 0.6f;

    [Header("Avatar")]
    [SerializeField]
    private List<Sprite> avatars =
        new List<Sprite>();
    [SerializeField] private SushiDishArtLibrary dishArt;

    // Chỉ chứa khách đã tới ghế và được phép nhận món.
    private readonly Customer[] activeCustomers = new Customer[2];

    // Giữ ghế cho khách đang đi tới.
    private readonly Customer[] movingCustomers = new Customer[2];

    // Khách thật đang đứng trong hàng đợi.
    private readonly List<Customer> waitingCustomers =
        new List<Customer>();

    private readonly object moveTweenId = new object();

    public IReadOnlyList<Customer> ActiveCustomers => activeCustomers;

    public IEnumerable<Customer> FulfillmentPriority()
    {
        Customer first = activeCustomers[0], second = activeCustomers[1];
        if (first != null && second != null && second.id < first.id)
        { var swap = first; first = second; second = swap; }
        if (first != null) yield return first;
        if (second != null) yield return second;
        // A guest walking to a seat keeps their place ahead of the waiting line.
        var queued = new List<Customer>(waitingCustomers);
        foreach (var customer in movingCustomers) if (customer != null) queued.Add(customer);
        queued.Sort((a, b) => a.id.CompareTo(b.id));
        foreach (var customer in queued) yield return customer;
    }

    public int TotalCustomers { get; private set; }
    public int CompletedCustomers { get; private set; }
    public int WaitingCount => waitingCustomers.Count;

    public bool AllCustomersCompleted =>
        TotalCustomers > 0 &&
        CompletedCustomers == TotalCustomers;

    public event Action<int, int> OnProgressChanged;
    public event Action OnAllCustomersCompleted;
    public event Action<Customer> OnRushExpired;

    public void TickRush(float deltaTime)
    {
        foreach (Customer customer in activeCustomers)
        {
            // Timeout đầu tiên có thể mở popup ngay trong vòng lặp này.
            if (LevelManager.Ins != null && LevelManager.Ins.IsPopupOpen) break;
            customer?.TickRush(deltaTime);
        }
    }

    public Customer FirstCustomerFor(PoolType type)
    {
        Customer first = null;
        foreach (Customer customer in activeCustomers)
            if (customer != null && customer.CanReceive(type) && (first == null || customer.id < first.id)) first = customer;
        return first;
    }

    private void HandleRushExpired(Customer customer) => OnRushExpired?.Invoke(customer);
    [Header("Khách hoàn thành")]
    [SerializeField] private float completedDelay = 0.8f;

    private readonly HashSet<Customer> finishingCustomers =
        new HashSet<Customer>();

    private readonly object completedDelayId = new object();
    private bool allowSimultaneousRush = true;
    public void Init(List<CustomerOrderData> customerOrders, bool simultaneousRush = true)
    {
        string error = ValidateData(customerOrders);

        if (error != null)
        {
            Debug.LogError(error, this);
            return;
        }

        ClearCustomers();
        if (dishArt == null && SushiArtCatalog.Default != null) dishArt = SushiArtCatalog.Default.dishImages;
        if (avatars.Count == 0 && SushiArtCatalog.Default != null)
            avatars.AddRange(SushiArtCatalog.Default.customerAvatars);
        allowSimultaneousRush = simultaneousRush;

        TotalCustomers = customerOrders.Count;

        for (int i = 0; i < customerOrders.Count; i++)
        {
            Transform position = i < 2
                ? GetSeat(i)
                : waitingPositions[i - 2];

            Customer customer = Instantiate(customerPrefab, position);

            customer.transform.localPosition = Vector3.zero;
            customer.transform.localRotation = Quaternion.identity;
            customer.transform.localScale = Vector3.one * (i < 2 ? 1f : waitingScale);

            Sprite avatar = avatars.Count > 0
                ? avatars[i % avatars.Count]
                : null;

            customer.Setup(
                i,
                avatar,
                customerOrders[i],
                dishArt
            );

            customer.OnCompleted += HandleCustomerCompleted;
            customer.OnRushExpired += HandleRushExpired;

            if (i < 2)
            {
                activeCustomers[i] = customer;
                customer.SitDown();
            }
            else
                waitingCustomers.Add(customer);
        }

        NotifyProgress();
    }

    private string ValidateData(List<CustomerOrderData> customerOrders)
    {
        if (customerPrefab == null ||
            firstSeat == null ||
            secondSeat == null ||
            firstSeat == secondSeat)
        {
            return "Cần prefab Customer và hai ghế khác nhau.";
        }

        if (customerOrders == null || customerOrders.Count == 0)
            return "Danh sách khách không được rỗng.";

        int waitingCount = Mathf.Max(0, customerOrders.Count - 2);

        if (waitingPositions == null ||
            waitingPositions.Count < waitingCount)
        {
            return $"Cần {waitingCount} vị trí đứng chờ cho level này.";
        }

        HashSet<Transform> usedPositions = new HashSet<Transform>
        {
            firstSeat,
            secondSeat
        };

        for (int i = 0; i < waitingCount; i++)
        {
            Transform position = waitingPositions[i];

            if (position == null || !usedPositions.Add(position))
                return $"Vị trí chờ {i + 1} bị thiếu hoặc trùng.";
        }

        for (int i = 0; i < customerOrders.Count; i++)
        {
            CustomerOrderData customerOrder = customerOrders[i];

            if (customerOrder == null || customerOrder.ValidateOrders() != null)
            {
                return $"Khách {i + 1} chưa có order.";
            }

            long total = 0;

            foreach (FoodOrderData item in customerOrder.CreateIngredientOrders())
            {
                if (item == null || item.amount <= 0)
                    return $"Khách {i + 1} có order không hợp lệ.";

                total += item.amount;
            }

            if (!customerOrder.UsesRecipes && total > customerPrefab.PlateCapacity)
                return $"Khách {i + 1} order vượt số vị trí trên đĩa.";
        }

        return null;
    }

    private void HandleCustomerCompleted(Customer customer)
    {
        int seatIndex = FindSeatIndex(customer);

        if (seatIndex < 0 || !finishingCustomers.Add(customer))
            return;

        // Khách đã nhận đủ món, nhưng vẫn giữ ở ghế trong lúc chờ.
        // Customer.completed đã chặn nhận thêm Food.
        customer.OnCompleted -= HandleCustomerCompleted;

        DOVirtual.DelayedCall(
            Mathf.Max(0f, completedDelay),
            () =>
            {
                if (customer == null ||
                    !finishingCustomers.Remove(customer))
                {
                    return;
                }

                activeCustomers[seatIndex] = null;
                CompletedCustomers++;

                RemoveCustomer(customer);
                MoveNextCustomerToSeat(seatIndex);
                MoveNextCustomerToSeat(1 - seatIndex);

                NotifyProgress();

                if (AllCustomersCompleted)
                    OnAllCustomersCompleted?.Invoke();
            },
            false // Dùng thời gian game, dừng khi Time.timeScale = 0.
        )
        .SetId(completedDelayId);
    }

    private void MoveNextCustomerToSeat(int seatIndex)
    {
        if (activeCustomers[seatIndex] != null ||
            movingCustomers[seatIndex] != null ||
            waitingCustomers.Count == 0)
        {
            return;
        }

        Customer customer = waitingCustomers[0];
        if (!allowSimultaneousRush && customer.IsRush)
        {
            foreach (var seated in activeCustomers)
                if (seated != null && seated.IsRush && !seated.IsCompleted) return;
            foreach (var moving in movingCustomers)
                if (moving != null && moving.IsRush) return;
        }
        waitingCustomers.RemoveAt(0);

        movingCustomers[seatIndex] = customer;

        // Dừng riêng tween xếp hàng của khách này.
        DOTween.Kill(customer, false);

        Transform seat = GetSeat(seatIndex);

        customer.transform.SetParent(seat, true);

        DOTween.Sequence()
            .Append(customer.transform.DOLocalMove(Vector3.zero, Mathf.Max(0.01f, moveDuration)).SetEase(Ease.OutQuad))
            .Join(customer.transform.DOScale(Vector3.one, Mathf.Max(0.01f, moveDuration)).SetEase(Ease.OutQuad))
            .SetId(moveTweenId)
            .SetTarget(customer)
            .OnComplete(() =>
            {
                if (customer == null ||
                    movingCustomers[seatIndex] != customer)
                {
                    return;
                }

                customer.transform.localRotation = Quaternion.identity;

                movingCustomers[seatIndex] = null;

                // Chỉ nhận Food sau khi đã tới ghế.
                activeCustomers[seatIndex] = customer;
                customer.SitDown();
            });

        RefreshWaitingPositions();
    }

    private void RefreshWaitingPositions()
    {
        for (int i = 0; i < waitingCustomers.Count; i++)
        {
            Customer customer = waitingCustomers[i];

            DOTween.Kill(customer, false);

            customer.transform.SetParent(waitingPositions[i], true);

            customer.transform
                .DOLocalMove(
                    Vector3.zero,
                    Mathf.Max(0.01f, moveDuration)
                )
                .SetEase(Ease.OutQuad)
                .SetId(moveTweenId)
                .SetTarget(customer);
        }
    }

    public Transform GetSeat(int seatIndex)
    {
        return seatIndex == 0 ? firstSeat : seatIndex == 1 ? secondSeat : null;
    }

    private int FindSeatIndex(Customer customer)
    {
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] == customer)
                return i;
        }

        return -1;
    }

    private void RemoveCustomer(Customer customer)
    {
        if (customer == null)
            return;

        customer.OnCompleted -= HandleCustomerCompleted;

        DOTween.Kill(customer, false);
        customer.OnRushExpired -= HandleRushExpired;

        // Food dùng pool: trả về pool trước khi hủy khách.
        foreach (FoodShushi food in customer.ReceivedFoods)
        {
            if (food == null)
                continue;

            food.transform.DOKill();
            food.transform.SetParent(null, true);

            SimplePool.Despawn(food);
        }

        customer.gameObject.SetActive(false);
        Destroy(customer.gameObject);
    }

    private void NotifyProgress()
    {
        OnProgressChanged?.Invoke(
            CompletedCustomers,
            TotalCustomers
        );
    }

    public void ClearCustomers()
    {
        DOTween.Kill(completedDelayId);
        finishingCustomers.Clear();

        for (int i = 0; i < activeCustomers.Length; i++)
        {
            RemoveCustomer(activeCustomers[i]);
            RemoveCustomer(movingCustomers[i]);

            activeCustomers[i] = null;
            movingCustomers[i] = null;
        }

        foreach (Customer customer in waitingCustomers)
            RemoveCustomer(customer);

        waitingCustomers.Clear();

        TotalCustomers = 0;
        CompletedCustomers = 0;
    }

    private void OnDestroy()
    {
        DOTween.Kill(moveTweenId);
        DOTween.Kill(completedDelayId);
    }
}
