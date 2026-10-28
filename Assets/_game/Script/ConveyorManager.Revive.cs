using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class ConveyorManager
{
    [Header("Revive blocked conveyor")]
    [SerializeField] private SushiReviveBag reviveBagPrefab;
    [SerializeField, Min(0.1f)] private float bagPackingDuration = 0.35f;
    private const int ReviveBagSize = 5;
    private readonly Dictionary<Transform, SushiReviveBag> slotBags = new Dictionary<Transform, SushiReviveBag>();
    private readonly object bagTweenId = new object();
    public int ReviveBagCount => slotBags.Count;

    public bool CanReviveFullConveyor
    {
        get
        {
            // A linked pair, tray or quantity stack can exceed the remaining free slots.
            // LevelManager decides whether the board is blocked before offering this revive.
            if (reviveBagPrefab == null || Capacity == 0) return false;
            int eligible = 0;
            foreach (FoodShushi food in slotFoods.Values)
                if (food != null && food.Location == FoodLocation.Conveyor && ++eligible >= ReviveBagSize) return true;
            return false;
        }
    }

    public bool TryReviveFullConveyor()
    {
        if (!CanReviveFullConveyor) return false;
        var selectedSlots = new List<Transform>();
        var contents = new List<FoodShushi>();
        foreach (Transform slot in slots)
        {
            if (!slotFoods.TryGetValue(slot, out FoodShushi food) || food.Location != FoodLocation.Conveyor) continue;
            selectedSlots.Add(slot);
            contents.Add(food);
            if (contents.Count == ReviveBagSize) break;
        }

        SushiReviveBag bag = Instantiate(reviveBagPrefab, selectedSlots[0], false);
        bag.gameObject.SetActive(true);
        bag.Pack(contents);
        slotBags.Add(selectedSlots[0], bag);
        for (int i = 0; i < contents.Count; i++)
        {
            FoodShushi food = contents[i];
            slotFoods.Remove(selectedSlots[i]);
            food.transform.DOKill();
            food.transform.SetParent(flyingFoodRoot, true);
            AnimateFlight(food, bag.Opening, bagPackingDuration, 0.35f, 0.12f,
                i * 0.035f, 0, bagTweenId, () => bag.Store(food));
        }
        // Reserve exactly one plate immediately, even while the five ingredients fly into the bag.
        NotifyOccupancy();
        return true;
    }

    private bool IsSlotOccupied(Transform slot) => slotFoods.ContainsKey(slot) || slotBags.ContainsKey(slot);

    private IEnumerable<FoodShushi> BeltIngredients()
    {
        foreach (FoodShushi food in slotFoods.Values) yield return food;
        foreach (SushiReviveBag bag in slotBags.Values)
            foreach (FoodShushi food in bag.Foods) yield return food;
    }

    private bool IsPackingBags()
    {
        foreach (SushiReviveBag bag in slotBags.Values) if (bag.IsPacking) return true;
        return false;
    }

    // Both Magnet implementations use the same sources, including stored bag ingredients.
    private int CollectBeltIngredients(PoolType type, int remaining, List<KeyValuePair<Transform, FoodShushi>> plan)
    {
        foreach (Transform slot in slots)
        {
            if (remaining <= 0) break;
            if (slotFoods.TryGetValue(slot, out FoodShushi food) && food.Type == type &&
                (food.Location == FoodLocation.Conveyor || food.Location == FoodLocation.MovingToConveyor))
            {
                plan.Add(new KeyValuePair<Transform, FoodShushi>(slot, food));
                remaining--;
            }
            if (!slotBags.TryGetValue(slot, out SushiReviveBag bag) || bag.IsPacking) continue;
            foreach (FoodShushi stored in bag.Foods)
            {
                if (remaining <= 0) break;
                if (stored.Type != type) continue;
                plan.Add(new KeyValuePair<Transform, FoodShushi>(slot, stored));
                remaining--;
            }
        }
        return remaining;
    }

    private void RemoveBeltIngredient(Transform slot, FoodShushi food)
    {
        if (slotFoods.TryGetValue(slot, out FoodShushi loose) && loose == food)
        {
            slotFoods.Remove(slot);
            return;
        }
        SushiReviveBag bag = slotBags[slot];
        food.transform.SetParent(flyingFoodRoot, false);
        food.transform.position = bag.Opening.position;
        food.transform.rotation = Quaternion.identity;
        SetWorldScale(food.transform, bag.transform.lossyScale * beltFoodScale);
        food.gameObject.SetActive(true);
        bag.Remove(food);
        if (bag.Foods.Count == 0)
        {
            slotBags.Remove(slot);
            bag.gameObject.SetActive(false);
            Destroy(bag.gameObject);
        }
    }

    private void DeliverBagIngredients()
    {
        if (customerManager == null || slotBags.Count == 0) return;
        foreach (Transform slot in slots)
        {
            if (!slotBags.TryGetValue(slot, out SushiReviveBag bag) || bag.IsPacking) continue;
            for (int i = bag.Foods.Count - 1; i >= 0; i--)
            {
                FoodShushi food = bag.Foods[i];
                foreach (Customer customer in customerManager.FulfillmentPriority())
                {
                    if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) continue;
                    bool reserved = false;
                    if (preparedDishPrefab != null && customer.Dishes.Count > 0)
                    {
                        foreach (SushiDishOrder order in customer.Dishes)
                        {
                            if (!customer.TryReservePreparedIngredient(order, food.Type, out Transform preparedTarget)) continue;
                            RemoveBeltIngredient(slot, food);
                            SushiPreparedDish dish = GetOrCreatePreparedDish(customer, order, preparedTarget);
                            StartPreparedIngredientFlight(dish, food, preparedTarget);
                            reserved = true;
                            break;
                        }
                    }
                    else if (customer.TryReserveFood(food, out Transform target))
                    {
                        RemoveBeltIngredient(slot, food);
                        FlyToCustomer(food, customer, target);
                        reserved = true;
                    }
                    if (!reserved) continue;
                    NotifyOccupancy();
                    break;
                }
            }
        }
    }

    private void ClearReviveBags()
    {
        DOTween.Kill(bagTweenId);
        foreach (SushiReviveBag bag in slotBags.Values)
        {
            foreach (FoodShushi food in bag.Foods)
            {
                food.transform.DOKill();
                food.transform.SetParent(null, true);
                SimplePool.Despawn(food);
            }
            bag.gameObject.SetActive(false);
            Destroy(bag.gameObject);
        }
        slotBags.Clear();
    }
}
