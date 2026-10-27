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

    public bool CanPrepareDish(SushiDishOrder dish)
    {
        if (dish == null || !dishes.Contains(dish) || completed || RushExpired || dish.IsComplete || dish.IsPrepared) return false;
        foreach (var ingredient in dish.Ingredients)
            if (ingredient.receivedAmount != 0 || ingredient.incomingAmount != 0) return false;
        return true;
    }

    public bool CanAssembleDish(SushiDishOrder dish)
    {
        return IsSeated && !completed && !RushExpired && dish != null && dishes.Contains(dish) &&
            !dish.IsComplete && !dish.IsOnTable;
    }

    public bool CanReceivePreparedIngredient(SushiDishOrder dish, PoolType type, bool includeIncomingFoundation = false)
    {
        if (!CanAssembleDish(dish) || !dish.CanStartIngredient(type, includeIncomingFoundation) ||
            dish.FindAvailableIngredient(type) == null) return false;
        OrderItem order = FindOrder(type);
        return order != null && order.CanReceive &&
            (tableReservations.ContainsKey(dish) || FindFreeServingSlot() >= 0);
    }

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
            tableReservations.Add(dish, slotIndex);
        }

        dish.IsPrepared = true;
        ingredient.incomingAmount++;
        order.incomingAmount++;
        target = servingSlots[slotIndex];
        OnOrderChanged?.Invoke(this);
        return true;
    }

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
        if (!hasAssembly && tableReservations.ContainsKey(dish))
        {
            tableReservations.Remove(dish);
            dish.IsPrepared = false;
        }
        OnOrderChanged?.Invoke(this);
        return true;
    }

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

    public bool TryReserveTable(SushiDishOrder dish, out Transform target)
    {
        target = null;
        if (!IsSeated || completed || RushExpired || !dish.IsPrepared || tableReservations.ContainsKey(dish) || servingSlots == null) return false;
        int slotIndex = FindFreeServingSlot();
        if (slotIndex < 0) return false;
        tableReservations.Add(dish, slotIndex);
        target = servingSlots[slotIndex];
        return true;
    }

    private int FindFreeServingSlot()
    {
        if (servingSlots == null) return -1;
        for (int i = 0; i < servingSlots.Length; i++)
            if (servingSlots[i] != null && !tableReservations.ContainsValue(i)) return i;
        return -1;
    }

    public Transform DishOrderTarget(SushiDishOrder dish) => recipeView.CardAt(dish.Index).DishDeliverySlot;

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
        tableReservations.Remove(dish);
        dish.IsPrepared = dish.IsOnTable = false;
        OnOrderChanged?.Invoke(this);
    }

    public bool CompletePreparedDish(SushiDishOrder dish)
    {
        if (dish == null || !dish.IsPrepared || dish.IsComplete || !dish.HasAllIngredients() ||
            !tableReservations.Remove(dish)) return false;
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
    public Sprite GetIngredientSprite(PoolType type)
    {
        Sprite sprite = ingredientArt != null ? ingredientArt.GetSprite(type) : null;
        return sprite != null ? sprite : SushiArtCatalog.Default != null ? SushiArtCatalog.Default.GetIngredient(type) : null;
    }

    public event Action<Customer> OnCompleted;
    public event Action<Customer> OnOrderChanged;
    public event Action<Customer> OnRushExpired;
    public event Action<Customer, SushiDishOrder> OnDishCompleted;

    public void Setup(int customerId, Sprite avatarSprite, CustomerOrderData data, SushiDishArtLibrary art = null)
    {
        Setup(customerId, avatarSprite, data.CreateFoodList());
        if (data.UsesRecipes)
        {
            foreach (DishOrderData order in data.dishes)
                for (int portion = 0; portion < order.amount; portion++)
                    dishes.Add(new SushiDishOrder(dishes.Count, SushiRecipeCatalog.Get(order.dish)));
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

    public void SitDown()
    {
        IsSeated = true;
        GetComponent<CustomerRecipeView>()?.SetSeated(true);
        if (servingRoot != null) servingRoot.gameObject.SetActive(true);
    }

    public void TickRush(float deltaTime)
    {
        if (!IsRush || !IsSeated || completed || RushExpired || deltaTime <= 0) return;
        RushRemaining = Mathf.Max(0, RushRemaining - deltaTime);
        if (RushRemaining > 0) return;
        RushExpired = true;
        OnRushExpired?.Invoke(this);
    }

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

    public bool CanReceive(PoolType type)
    {
        if (completed || RushExpired || !isActiveAndEnabled)  return false;

        OrderItem order = FindOrder(type);

        return order != null
            && order.CanReceive
            && FindFreeSlot() >= 0;
    }

    public bool TryReserveFood(FoodShushi food, out Transform target) => TryReserveFood(food, null, false, out target);

    internal bool TryReserveFoodForDish(FoodShushi food, SushiDishOrder dish, out Transform target) =>
        TryReserveFood(food, dish, true, out target);

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

    private OrderItem FindOrder(PoolType type)
    {
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].typeFood == type)
                return orders[i];
        }

        return null;
    }

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
