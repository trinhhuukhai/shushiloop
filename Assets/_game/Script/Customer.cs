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
    [SerializeField] private SushiIngredientArtLibrary ingredientArt;

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
    public float ReceivingFoodScale => dishes.Count > 0 ? CustomerRecipeView.IngredientSize(dishes.Count) : 0.35f;
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
            int slotIndex = 0;
            foreach (var dish in dishes)
            {
                for (int i = 0; i < dish.Recipe.Ingredients.Count; i++)
                {
                    var slot = new GameObject("Dish ingredient " + slotIndex++).transform;
                    slot.SetParent(transform, false);
                    slot.localPosition = CustomerRecipeView.IngredientPosition(dish.Index, dishes.Count,
                        i, dish.Recipe.Ingredients.Count);
                    recipeSlots.Add(slot);
                }
            }
            var view = GetComponent<CustomerRecipeView>();
            if (view == null) view = gameObject.AddComponent<CustomerRecipeView>();
            view.Setup(this, art);
        }
        ConfigureRush(data);
        OnOrderChanged?.Invoke(this);
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
        if (IsRush && GetComponent<RushCustomerView>() == null) gameObject.AddComponent<RushCustomerView>();
    }

    public void SitDown()
    {
        IsSeated = true;
        GetComponent<CustomerRecipeView>()?.SetSeated(true);
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
        foreach (var slot in recipeSlots) if (slot != null) Destroy(slot.gameObject);
        recipeSlots.Clear();
        settledFoods.Clear();
        receivedFoods.Clear();
        reservations.Clear();
        occupiedSlots.Clear();

        if (avartar != null)
        {
            avartar.sprite = avatarSprite;
            avartar.sortingOrder = 2;
            if (avatarSprite != null)
            {
                float scale = 1.05f / Mathf.Max(0.01f, avatarSprite.bounds.size.y);
                avartar.transform.localScale = Vector3.one * scale;
                avartar.transform.localPosition = new Vector3(0, 0.65f, 0) - avatarSprite.bounds.center * scale;
            }
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
        food.transform.localScale = Vector3.one * ReceivingFoodScale;
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
