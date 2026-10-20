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
    public SpriteRenderer avartar;

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

    public IReadOnlyList<OrderItem> Orders => orders;
    public IReadOnlyList<FoodShushi> ReceivedFoods => receivedFoods;
    public int PlateCapacity => plateSlots.Count;

    public event Action<Customer> OnCompleted;
    public event Action<Customer> OnOrderChanged;

    private class Reservation
    {
        public OrderItem order;
        public int slotIndex;
    }

    // Gọi trên Customer mới được tạo.
    public void Setup( int customerId, Sprite avatarSprite, List<PoolType> requestedFoods)
    {
        id = customerId;
        completed = false;

        orders.Clear();
        receivedFoods.Clear();
        reservations.Clear();
        occupiedSlots.Clear();

        if (avartar != null)  avartar.sprite = avatarSprite;

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

    public bool CanReceive(PoolType type)
    {
        if (completed || !isActiveAndEnabled)  return false;

        OrderItem order = FindOrder(type);

        return order != null
            && order.CanReceive
            && FindFreeSlot() >= 0;
    }

    public bool TryReserveFood( FoodShushi food, out Transform target)
    {
        target = null;

        if (food == null || reservations.ContainsKey(food) || !CanReceive(food.Type))
        {
            return false;
        }

        OrderItem order = FindOrder(food.Type);
        int slotIndex = FindFreeSlot();

        // Ngăn một Food được giao cho hai khách.
        if (!food.TryMoveToCustomerPlate())  return false;

        order.incomingAmount++;
        occupiedSlots.Add(slotIndex);

        reservations.Add(food, new Reservation
        {
            order = order,
            slotIndex = slotIndex
        });

        target = plateSlots[slotIndex];

        OnOrderChanged?.Invoke(this);
        return true;
    }

    // Chỉ gọi khi Food đã bay tới đĩa.
    public bool CompleteReceive(FoodShushi food)
    {
        if (food == null || food.Location != FoodLocation.MovingToCustomerPlate || !reservations.TryGetValue(food, out Reservation reservation))
        {
            return false;
        }

        Transform target = plateSlots[reservation.slotIndex];

        food.transform.SetParent(target, true);
        food.transform.localPosition = Vector3.zero;
        food.ArriveAtCustomerPlate();

        reservations.Remove(food);

        reservation.order.incomingAmount--;
        reservation.order.receivedAmount++;

        receivedFoods.Add(food);

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
        occupiedSlots.Remove(reservation.slotIndex);
        food.CancelMoveToCustomerPlate();

        OnOrderChanged?.Invoke(this);
        return true;
    }

    private OrderItem FindOrder(PoolType type)
    {
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].typeFood == type)
                return orders[i];
        }

        return null;
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < plateSlots.Count; i++)
        {
            if (plateSlots[i] != null && !occupiedSlots.Contains(i))
                return i;
        }

        return -1;
    }

    private bool IsOrderComplete()
    {
        if (orders.Count == 0)  return false;

        foreach (OrderItem order in orders)
        {
            if (!order.IsComplete)   return false;
        }
        return true;
    }
}