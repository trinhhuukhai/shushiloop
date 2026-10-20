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

    [Header("Avatar")]
    [SerializeField]
    private List<Sprite> avatars =
        new List<Sprite>();

    // Chỉ chứa khách đã tới ghế và được phép nhận món.
    private readonly Customer[] activeCustomers = new Customer[2];

    // Giữ ghế cho khách đang đi tới.
    private readonly Customer[] movingCustomers = new Customer[2];

    // Khách thật đang đứng trong hàng đợi.
    private readonly List<Customer> waitingCustomers =
        new List<Customer>();

    private readonly object moveTweenId = new object();

    public IReadOnlyList<Customer> ActiveCustomers => activeCustomers;

    public int TotalCustomers { get; private set; }
    public int CompletedCustomers { get; private set; }
    public int WaitingCount => waitingCustomers.Count;

    public bool AllCustomersCompleted =>
        TotalCustomers > 0 &&
        CompletedCustomers == TotalCustomers;

    public event Action<int, int> OnProgressChanged;
    public event Action OnAllCustomersCompleted;
    [Header("Khách hoàn thành")]
    [SerializeField] private float completedDelay = 0.8f;

    private readonly HashSet<Customer> finishingCustomers =
        new HashSet<Customer>();

    private readonly object completedDelayId = new object();
    public void Init(List<CustomerOrderData> customerOrders)
    {
        string error = ValidateData(customerOrders);

        if (error != null)
        {
            Debug.LogError(error, this);
            return;
        }

        ClearCustomers();

        TotalCustomers = customerOrders.Count;

        for (int i = 0; i < customerOrders.Count; i++)
        {
            Transform position = i < 2
                ? GetSeat(i)
                : waitingPositions[i - 2];

            Customer customer = Instantiate(customerPrefab, position);

            customer.transform.localPosition = Vector3.zero;
            customer.transform.localRotation = Quaternion.identity;

            Sprite avatar = avatars.Count > 0
                ? avatars[i % avatars.Count]
                : null;

            customer.Setup(
                i,
                avatar,
                customerOrders[i].CreateFoodList()
            );

            customer.OnCompleted += HandleCustomerCompleted;

            if (i < 2)
                activeCustomers[i] = customer;
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

            if (customerOrder == null ||
                customerOrder.orders == null ||
                customerOrder.orders.Count == 0)
            {
                return $"Khách {i + 1} chưa có order.";
            }

            long total = 0;

            foreach (FoodOrderData item in customerOrder.orders)
            {
                if (item == null || item.amount <= 0)
                    return $"Khách {i + 1} có order không hợp lệ.";

                total += item.amount;
            }

            if (total > customerPrefab.PlateCapacity)
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
        waitingCustomers.RemoveAt(0);

        movingCustomers[seatIndex] = customer;

        // Dừng riêng tween xếp hàng của khách này.
        DOTween.Kill(customer, false);

        Transform seat = GetSeat(seatIndex);

        customer.transform.SetParent(seat, true);

        customer.transform
            .DOLocalMove(Vector3.zero, Mathf.Max(0.01f, moveDuration))
            .SetEase(Ease.OutQuad)
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

    private Transform GetSeat(int seatIndex)
    {
        return seatIndex == 0 ? firstSeat : secondSeat;
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