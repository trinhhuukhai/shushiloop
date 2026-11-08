using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public partial class ConveyorManager
{
    [Header("Ghép món và giao qua bàn")]
    [SerializeField] private SushiPreparedDish preparedDishPrefab;
    [SerializeField, Min(0.05f)] private float assemblyDuration = 0.3f;
    [SerializeField, Min(0f)] private float assemblyIngredientStagger = 0.12f;
    [SerializeField, Min(0f)] private float tableHoldDuration = 0.3f;
    [SerializeField, Min(0.05f)] private float orderJumpDuration = 0.38f;
    private readonly Dictionary<Transform, SushiPreparedDish> slotDishes = new Dictionary<Transform, SushiPreparedDish>();
    private readonly HashSet<SushiPreparedDish> servingDishes = new HashSet<SushiPreparedDish>();
    private readonly Dictionary<SushiPreparedDish, List<FoodShushi>> assemblyFoods = new Dictionary<SushiPreparedDish, List<FoodShushi>>();
    private readonly object dishTweenId = new object();
    public int PreparedDishCount => slotDishes.Count;
    public int PreparingDishCount => assemblyFoods.Values.Count(foods => foods.Count > 0);
    public IReadOnlyDictionary<Transform, SushiPreparedDish> PlatedDishes => slotDishes;

    // Kiểm tra băng chuyền và túi có đủ nguyên liệu để bắt đầu một món của khách đang ngồi.
    public bool HasCompleteRecipeOnBelt()
    {
        if (preparedDishPrefab == null) return false;
        foreach (Customer customer in customerManager.FulfillmentPriority())
        {
            if (customer == null || !customer.IsSeated) continue;
            foreach (var dish in customer.Dishes)
            {
                if (!customer.CanPrepareDish(dish)) continue;
                bool enough = true;
                foreach (var need in dish.Ingredients)
                {
                    int count = 0;
                    foreach (var food in BeltIngredients())
                        if (food.Type == need.typeFood && food.Location == FoodLocation.Conveyor) count++;
                    enough &= count >= need.requiredAmount;
                }
                if (enough) return true;
            }
        }
        return false;
    }

    // Chọn nguyên liệu đi qua điểm giao, giữ chỗ món phù hợp và đưa lên đĩa ghép.
    private void AssembleAvailableDishes(float previousProgress)
    {
        if (preparedDishPrefab == null || slotFoods.Count == 0) return;
        foreach (Transform slot in slots)
        {
            if (!slotFoods.TryGetValue(slot, out FoodShushi food) || food == null ||
                food.Location != FoodLocation.Conveyor) continue;
            foreach (Customer customer in customerManager.FulfillmentPriority())
            {
                if (customer == null || !customer.IsSeated) continue;
                if (!IsAtCustomerDeliveryPoint(customer, slot, previousProgress)) continue;
                if (!TryReserveAvailablePreparedIngredient(customer, food.Type, out SushiDishOrder order, out Transform destination)) continue;
                SushiPreparedDish plated = GetOrCreatePreparedDish(customer, order, destination);
                slotFoods.Remove(slot);
                StartPreparedIngredientFlight(plated, food, destination);
                NotifyOccupancy();
                break;
            }
        }
    }

    // Tìm đĩa cùng khách có phần nền chuyển được sang món đích, ưu tiên tiến độ cao hơn.
    private SushiPreparedDish FindReusableFoundation(Customer customer, SushiDishOrder target, PoolType type)
    {
        if (type == target.FoundationType || target.FindAvailableIngredient(type) == null) return null;
        SushiPreparedDish best = null;
        int bestProgress = -1, bestLanded = -1;
        foreach (SushiPreparedDish plated in slotDishes.Values)
        {
            if (plated.Owner != customer || !customer.CanTransferPreparedFoundation(plated.Order, target)) continue;
            OrderItem projected = target.ProjectedIngredient(type, plated.Order);
            if (!projected.CanReceive) continue;
            int landed = plated.Order.Ingredients.Sum(item => item.receivedAmount);
            int progress = landed + plated.Order.Ingredients.Sum(item => item.incomingAmount);
            if (progress < bestProgress || (progress == bestProgress && landed <= bestLanded)) continue;
            best = plated;
            bestProgress = progress;
            bestLanded = landed;
        }
        return best;
    }

    // Chọn món nhận nguyên liệu theo nguồn đang có, chuyển phần nền nếu cần rồi giữ chỗ nhận.
    private bool TryReserveAvailablePreparedIngredient(Customer customer, PoolType type,
        out SushiDishOrder order, out Transform destination)
    {
        destination = null;
        var available = new Dictionary<PoolType, int>();
        foreach (FoodShushi food in BeltIngredients())
        {
            if (food == null || (food.Location != FoodLocation.Conveyor && food.Location != FoodLocation.MovingToConveyor)) continue;
            available.TryGetValue(food.Type, out int count);
            available[food.Type] = count + 1;
        }
        // Đếm lượng nguyên liệu cùng loại đang có trên băng chuyền hoặc trong túi cho bước chọn món.
        int Count(PoolType ingredient) => available.TryGetValue(ingredient, out int count) ? count : 0;
        var reusable = new Dictionary<SushiDishOrder, SushiPreparedDish>();
        foreach (SushiDishOrder dish in customer.Dishes)
        {
            SushiPreparedDish plated = FindReusableFoundation(customer, dish, type);
            if (plated != null) reusable.Add(dish, plated);
        }
        // Lấy đơn món đang giữ phần nền có thể tái sử dụng cho món được xét.
        SushiDishOrder Foundation(SushiDishOrder dish) => reusable.TryGetValue(dish, out var plated) ? plated.Order : null;
        int start = Random.Range(0, customer.Dishes.Count);
        order = SushiDishSelection.Select(customer.Dishes, type,
            dish => reusable.ContainsKey(dish) || customer.CanReceivePreparedIngredient(dish, type), Count, start, Foundation);
        if (order == null) return false;
        if (reusable.TryGetValue(order, out SushiPreparedDish sourcePlate))
        {
            SushiDishOrder sourceOrder = sourcePlate.Order;
            SushiPreparedDish targetPlate = null;
            foreach (SushiPreparedDish plated in slotDishes.Values)
                if (plated.Owner == customer && plated.Order == order) { targetPlate = plated; break; }
            if (!customer.TryTransferPreparedFoundation(sourceOrder, order)) return false;
            if (targetPlate != null) ReassignPreparedPlate(targetPlate, sourceOrder);
            ReassignPreparedPlate(sourcePlate, order);
        }
        return customer.TryReservePreparedIngredient(order, type, out destination);
    }

    // Gán đĩa sang đơn món mới và đồng bộ hình ảnh nguyên liệu đang bay tới.
    private void ReassignPreparedPlate(SushiPreparedDish plated, SushiDishOrder order)
    {
        plated.ReassignOrder(order);
        if (assemblyFoods.TryGetValue(plated, out var flights))
            foreach (FoodShushi food in flights) plated.ApplyIngredientArtwork(food, true);
    }

    // Kiểm tra đoạn di chuyển vừa qua của vị trí băng chuyền có chạm vùng giao cho khách.
    private bool IsAtCustomerDeliveryPoint(Customer customer, Transform slot, float previousProgress)
    {
        int customerIndex = -1;
        IReadOnlyList<Customer> customers = customerManager.ActiveCustomers;
        for (int i = 0; i < customers.Count; i++)
        {
            if (customers[i] != customer) continue;
            customerIndex = i;
            break;
        }
        int slotIndex = slots.IndexOf(slot);
        if (deliveryProgresses == null || customerIndex < 0 ||
            customerIndex >= deliveryProgresses.Length || slotIndex < 0) return false;

        float slotOffset = (float)slotIndex / slots.Count;
        float previousSlotProgress = Mathf.Repeat(previousProgress + slotOffset, 1f);
        Vector2 gatePosition = spline.AtDistance(deliveryProgresses[customerIndex]);
        Vector2 previousPosition = spline.AtDistance(previousSlotProgress);
        Vector2 currentPosition = slot.position;
        return DistanceToSegment(gatePosition, previousPosition, currentPosition) <= deliveryRange;
    }

    // Tính khoảng cách ngắn nhất từ một điểm tới đoạn thẳng chuyển động.
    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon) return Vector2.Distance(point, start);
        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }

    // Lấy đĩa đã giữ chỗ hoặc tạo đĩa mới trên bàn và bắt đầu theo dõi ghép món.
    private SushiPreparedDish GetOrCreatePreparedDish(Customer customer, SushiDishOrder order, Transform destination)
    {
        if (slotDishes.TryGetValue(destination, out SushiPreparedDish existing)) return existing;
        SushiPreparedDish plated = Instantiate(preparedDishPrefab, destination, false);
        plated.transform.localPosition = Vector3.zero;
        plated.Bind(customer, order);
        plated.BeginAssembly();
        slotDishes.Add(destination, plated);
        assemblyFoods.Add(plated, new List<FoodShushi>());
        servingDishes.Add(plated);
        return plated;
    }

    // Tách nguyên liệu khỏi băng chuyền, thêm vào danh sách chờ và thử khởi động chuyến bay.
    private void StartPreparedIngredientFlight(SushiPreparedDish plated, FoodShushi food, Transform destination)
    {
        if (food.Location == FoodLocation.MovingToConveyor) food.ArriveAtConveyor();
        List<FoodShushi> flights = assemblyFoods[plated];
        flights.Add(food);
        food.transform.DOKill();
        food.transform.SetParent(flyingFoodRoot, true);
        LaunchReadyPreparedIngredients(plated);
    }

    // Cho các nguyên liệu đủ điều kiện thứ tự ghép bay lên đĩa với độ trễ so le.
    private void LaunchReadyPreparedIngredients(SushiPreparedDish plated)
    {
        if (!assemblyFoods.TryGetValue(plated, out var flights)) return;
        int launched = 0;
        foreach (FoodShushi food in flights)
        {
            // Magnet reserves the whole recipe atomically, but toppings wait for the base to land.
            if (food.Location != FoodLocation.Conveyor || !plated.Order.CanStartIngredient(food.Type)) continue;
            LaunchPreparedIngredient(plated, food, flights, launched++ * assemblyIngredientStagger);
        }
    }

    // Đưa một nguyên liệu lên đĩa, chốt lượt nhận và chuyển món đủ thành phần sang bước phục vụ.
    private void LaunchPreparedIngredient(SushiPreparedDish plated, FoodShushi food, List<FoodShushi> flights, float delay)
    {
        if (!food.TryMoveToCustomerPlate()) return;
        plated.ApplyIngredientArtwork(food, true);
        // Nguyên liệu đầu tiên (bát cơm) bay được nửa đường thì đĩa đen mờ dần hiện ra để đón.
        plated.RevealPlate(delay + assemblyDuration * 0.4f);
        Transform destination = plated.IngredientTarget(food.Type);
        AnimateFlight(food, destination, assemblyDuration, 0.3f, 1f, delay, 0, dishTweenId, () =>
        {
            flights.Remove(food);
            food.ArriveAtCustomerPlate();
            plated.AddAssemblyIngredient(food);
            if (!plated.Owner.CompletePreparedIngredient(plated.Order, food.Type, out bool complete))
            {
                Debug.LogError($"Nguyên liệu {food.Type} đáp lên đĩa nhưng reservation không hợp lệ.", plated);
                return;
            }
            if (!complete)
            {
                // Món 3 nguyên liệu: đủ 2 nguyên liệu trên đĩa thì nảy lên ghép thành món dở (vd. cơm + rong biển).
                plated.CombineLandedIngredients();
                LaunchReadyPreparedIngredients(plated);
                return;
            }
            assemblyFoods.Remove(plated);
            plated.Order.IsOnTable = true;
            // Nguyên liệu gốc biến thành món ngay trên đĩa, giữ một nhịp rồi mới bay tới khách.
            float transformTime = plated.FinishAssembly();
            DOVirtual.DelayedCall(transformTime + tableHoldDuration, () => SendDishToOrder(plated), false).SetId(dishTweenId);
        });
    }

    // Giải phóng chỗ trên bàn và cho món hoàn chỉnh bay tới thẻ đơn để xác nhận giao xong.
    private void SendDishToOrder(SushiPreparedDish plated)
    {
        if (plated == null || plated.Owner == null) return;
        if (plated.transform.parent != null) slotDishes.Remove(plated.transform.parent);
        plated.LeaveTable();
        plated.SetFlying();
        plated.transform.SetParent(flyingFoodRoot, true);
        NotifyOccupancy();
        JumpDish(plated, plated.Owner.DishOrderTarget(plated.Order), orderJumpDuration, 0.5f, 0.7f, () =>
        {
            servingDishes.Remove(plated);
            plated.Owner.CompletePreparedDish(plated.Order);
            plated.ClearAssemblyIngredients();
            Destroy(plated.gameObject);
        });
    }

    // Chạy chuyển động vòng cung và đổi kích thước đĩa món tới điểm nhận.
    private void JumpDish(SushiPreparedDish plated, Transform target, float duration, float height, float scale, System.Action complete)
    {
        Transform subject = plated.transform;
        Vector3 origin = subject.position, size = subject.lossyScale;
        DOVirtual.Float(0, 1, Mathf.Max(0.05f, duration), t =>
        {
            if (subject == null || target == null) return;
            subject.position = Vector3.Lerp(origin, target.position, t) + Vector3.up * (4 * height * t * (1 - t));
            SetWorldScale(subject, Vector3.Lerp(size, target.lossyScale * scale, t));
        }).SetEase(Ease.InOutSine).SetId(dishTweenId).OnComplete(() => complete());
    }

    // Hủy hiệu ứng món, thu hồi nguyên liệu đang ghép và hủy các đĩa cùng lượt giữ chỗ.
    private void ClearPreparedDishes()
    {
        DOTween.Kill(dishTweenId);
        foreach (var foods in assemblyFoods.Values)
            foreach (var food in foods)
                if (food != null) { food.transform.SetParent(null, true); SimplePool.Despawn(food); }
        assemblyFoods.Clear();
        var all = new HashSet<SushiPreparedDish>(slotDishes.Values);
        all.UnionWith(servingDishes);
        foreach (var plated in all)
        {
            if (plated == null) continue;
            if (plated.Owner != null) plated.Owner.CancelPreparedDish(plated.Order);
            plated.ClearAssemblyIngredients();
            plated.gameObject.SetActive(false);
            Destroy(plated.gameObject);
        }
        slotDishes.Clear(); servingDishes.Clear();
    }

    // Dùng nam châm giao đĩa sẵn có hoặc gom đủ nguyên liệu để hoàn thành món được chọn.
    private bool TryMagnetPreparedDish(BoardShishiManager board, Customer customer, SushiDishOrder order)
    {
        SushiPreparedDish existingDish = null;
        foreach (var pair in slotDishes)
        {
            SushiPreparedDish plated = pair.Value;
            if (plated.Owner != customer || plated.Order != order) continue;
            existingDish = plated;
            break;
        }
        if (existingDish != null && existingDish.IsReady)
        {
            SendDishToOrder(existingDish);
            return true;
        }
        if (!customer.CanAssembleDish(order)) return false;
        if (!TryBuildMagnetPlan(board, order, out var boardPlan, out var beltPlan)) return false;
        var units = new List<FoodShushi>();
        try
        {
            foreach (var pickup in boardPlan)
                for (int i = 0; i < pickup.amount; i++)
                {
                    var food = SimplePool.Spawn<FoodShushi>(pickup.food.Type);
                    if (food == null) throw new System.InvalidOperationException("Missing ingredient prefab: " + pickup.food.Type);
                    units.Add(food);
                    food.SetupFromData(new FoodData { typeFood = pickup.food.Type });
                    food.transform.SetParent(flyingFoodRoot, false);
                    food.transform.position = pickup.food.transform.position;
                    food.BeginConveyorMove(false);
                }
        }
        catch (System.Exception error)
        {
            foreach (var food in units) SimplePool.Despawn(food);
            Debug.LogException(error, this); return false;
        }

        var selectedFoods = new List<FoodShushi>(units);
        foreach (var pickup in beltPlan) selectedFoods.Add(pickup.Value);
        PoolType foundation = order.FoundationType;
        selectedFoods.Sort((a, b) => (a.Type == foundation ? 0 : 1).CompareTo(b.Type == foundation ? 0 : 1));
        var newlyReserved = new List<FoodShushi>();
        Transform target = null;
        foreach (FoodShushi food in selectedFoods)
        {
            if (!customer.TryReservePreparedIngredient(order, food.Type, out Transform reservedTarget, includeIncomingFoundation: true))
            {
                foreach (FoodShushi reservedFood in newlyReserved)
                    customer.CancelPreparedIngredientReservation(order, reservedFood.Type);
                foreach (FoodShushi spawned in units) SimplePool.Despawn(spawned);
                return false;
            }
            target = reservedTarget;
            newlyReserved.Add(food);
        }

        foreach (var pickup in beltPlan)
        {
            RemoveBeltIngredient(pickup.Key, pickup.Value);
            pickup.Value.transform.DOKill();
            pickup.Value.RevealForMagnet();
            pickup.Value.transform.SetParent(flyingFoodRoot, true);
        }
        if (boardPlan.Count > 0) board.CommitPickup(boardPlan, FoodPickMode.Magnet, 0);
        SushiPreparedDish dish = GetOrCreatePreparedDish(customer, order, target);
        foreach (FoodShushi food in selectedFoods) StartPreparedIngredientFlight(dish, food, target);
        NotifyOccupancy();
        return true;
    }
}
