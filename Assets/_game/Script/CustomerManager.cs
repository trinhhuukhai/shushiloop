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
    [SerializeField, Range(0f, 1f)] private float waitingScale = 0.6f;

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

    // Duyệt khách đang ngồi trước, rồi khách chờ hoặc đang di chuyển theo thứ tự mã khách.
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
    public bool HasPendingTransitions => finishingCustomers.Count > 0 ||
        movingCustomers[0] != null || movingCustomers[1] != null;

    public bool AllCustomersCompleted =>
        TotalCustomers > 0 &&
        CompletedCustomers == TotalCustomers;

    public event Action<int, int> OnProgressChanged;
    public event Action OnAllCustomersCompleted;
    public event Action<Customer> OnRushExpired;

    // Cập nhật thời gian chờ cho khách tại ghế và dừng ngay khi màn chơi mở popup.
    public void TickRush(float deltaTime)
    {
        foreach (Customer customer in activeCustomers)
        {
            // Timeout đầu tiên có thể mở popup ngay trong vòng lặp này.
            if (LevelManager.Ins != null && LevelManager.Ins.IsPopupOpen) break;
            customer?.TickRush(deltaTime);
        }
    }

    // Tìm khách có mã nhỏ nhất tại ghế còn có thể nhận loại nguyên liệu này.
    public Customer FirstCustomerFor(PoolType type)
    {
        Customer first = null;
        foreach (Customer customer in activeCustomers)
            if (customer != null && customer.CanReceive(type) && (first == null || customer.id < first.id)) first = customer;
        return first;
    }

    // Chuyển tiếp sự kiện khách hết giờ chờ cho hệ thống điều phối màn chơi.
    private void HandleRushExpired(Customer customer) => OnRushExpired?.Invoke(customer);
    [Header("Khách hoàn thành")]
    [SerializeField] private float completedDelay = 0.8f;
    [SerializeField, Min(0f)] private float customerExitDistance = 8f;

    private readonly HashSet<Customer> finishingCustomers =
        new HashSet<Customer>();

    private readonly object completedDelayId = new object();
    private bool allowSimultaneousRush = true;
    // Kiểm tra dữ liệu, tạo khách cùng đơn hàng và bố trí hai ghế phục vụ với hàng chờ.
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
            {
                waitingCustomers.Add(customer);
                customer.SetQueueVisual(waitingCustomers.Count - 1);
            }
        }

        NotifyProgress();
    }

    // Kiểm tra prefab, ghế, vị trí hàng chờ và nội dung đơn hàng trước khi tạo khách.
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

            foreach (FoodOrderData item in customerOrder.CreateIngredientOrders())
            {
                if (item == null || item.amount <= 0)
                    return $"Khách {i + 1} có order không hợp lệ.";
            }
        }

        return null;
    }

    // Chờ khách hoàn tất một nhịp rồi chạy chuyển động rời ghế.
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
                if (customer == null || !finishingCustomers.Contains(customer))
                {
                    return;
                }

                float direction = seatIndex == 0 ? -1f : 1f;
                Vector3 exitPosition = customer.transform.position +
                    Vector3.right * direction * customerExitDistance;
                customer.transform
                    .DOMove(exitPosition, Mathf.Max(0.01f, moveDuration))
                    .SetEase(Ease.InQuad)
                    .SetId(moveTweenId)
                    .SetTarget(customer)
                    .OnComplete(() => FinishCustomerExit(customer, seatIndex));
            },
            false // Dùng thời gian game, dừng khi Time.timeScale = 0.
        )
        .SetId(completedDelayId);
    }

    // Dọn khách đã rời đi, cập nhật tiến độ và gọi khách tiếp theo vào các ghế trống.
    private void FinishCustomerExit(Customer customer, int seatIndex)
    {
        if (customer == null || !finishingCustomers.Remove(customer)) return;

        activeCustomers[seatIndex] = null;
        CompletedCustomers++;
        RemoveCustomer(customer);
        MoveNextCustomerToSeat(seatIndex);
        MoveNextCustomerToSeat(1 - seatIndex);
        NotifyProgress();

        if (AllCustomersCompleted)
            OnAllCustomersCompleted?.Invoke();
    }

    // Đưa khách đầu hàng vào ghế trống, tuân theo giới hạn khách rush đồng thời.
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
        // Rời hàng chờ: hiện rõ ngay khi bắt đầu đi tới ghế.
        customer.SetQueueVisual(-1);

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

    // Dồn các khách còn chờ về đúng vị trí sau khi hàng chờ thay đổi.
    private void RefreshWaitingPositions()
    {
        for (int i = 0; i < waitingCustomers.Count; i++)
        {
            Customer customer = waitingCustomers[i];

            DOTween.Kill(customer, false);

            customer.transform.SetParent(waitingPositions[i], true);
            customer.SetQueueVisual(i);

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

    // Lấy Transform ghế thứ nhất hoặc thứ hai theo chỉ số.
    public Transform GetSeat(int seatIndex)
    {
        return seatIndex == 0 ? firstSeat : seatIndex == 1 ? secondSeat : null;
    }

    // Tìm chỉ số ghế đang phục vụ khách được chỉ định.
    private int FindSeatIndex(Customer customer)
    {
        for (int i = 0; i < activeCustomers.Length; i++)
        {
            if (activeCustomers[i] == customer)
                return i;
        }

        return -1;
    }

    // Hủy đăng ký sự kiện, thu hồi nguyên liệu đã nhận và hủy đối tượng khách.
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

        SushiServingCounter.Ins?.ReleaseAll(customer);
        customer.gameObject.SetActive(false);
        Destroy(customer.gameObject);
    }

    // Phát số khách đã hoàn thành và tổng số khách cho các thành phần theo dõi.
    private void NotifyProgress()
    {
        OnProgressChanged?.Invoke(
            CompletedCustomers,
            TotalCustomers
        );
    }

    // Hủy xử lý khách hoàn tất, dọn mọi khách tại ghế hoặc hàng chờ và đặt lại tiến độ.
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
        SushiServingCounter.Ins?.ClearAll();

        TotalCustomers = 0;
        CompletedCustomers = 0;
    }

    // Hủy các tween di chuyển và trì hoãn hoàn tất khách khi manager bị hủy.
    private void OnDestroy()
    {
        DOTween.Kill(moveTweenId);
        DOTween.Kill(completedDelayId);
    }
}
