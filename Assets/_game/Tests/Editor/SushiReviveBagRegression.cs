#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class SushiGameplayRegression
{
    public static void RunReviveBagBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run in an isolated batch project.");
        SushiReviveBagSetup.CreatePrefab();
        SessionState.SetBool(PendingKey + ".ReviveBag", true);
        RunBatch();
    }

    private static void RunReviveBagChecks()
    {
        try
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.defaultUpdateType = UpdateType.Manual;
            CreateFixture();
            CreateLevel();
            Set(conveyor, "reviveBagPrefab", Prefab<SushiReviveBag>("ReviveBag"));
            Run("full belt offers paid revive after the grace period and frees exactly four slots", FullBeltRevive);
            Run("pickup flights and fewer than five eligible ingredients cannot be packed", BagEligibility);
            Run("quantity, linked pairs and trays offer bag revive when remaining slots are insufficient", PartialBeltRevive);
            Run("available moves skip bag revive and frozen heads retain the ice revive", PartialBeltReviveGuards);
            Run("a bag serves directly to customer plates, respecting foundations and quantities", BagDelivery);
            Run("Magnet includes bag contents and rejects incomplete recipes atomically", BagMagnet);
            Run("repeated revives preserve existing bags and do not overlap reserved slots", BagRepeatedRevive);
            Run("replay clears stored food and cancels packing flights", BagReload);
            Debug.Log("SUSHI_REVIVE_BAG_OK: " + passed + " scenarios passed");
            System.IO.File.WriteAllText("SushiReviveBagResults.txt", "PASS: " + passed + " scenarios\n");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            System.IO.File.WriteAllText("SushiReviveBagResults.txt", "FAIL after " + passed + " scenarios\n" + error);
            EditorApplication.Exit(1);
        }
    }

    private static Dictionary<Transform, SushiReviveBag> Bags =>
        Get<Dictionary<Transform, SushiReviveBag>>(conveyor, "slotBags");
    private static SushiReviveBag FirstBag => new List<SushiReviveBag>(Bags.Values)[0];

    private static void FillBagBelt(bool settle = true)
    {
        Set(conveyor, "preparedDishPrefab", null);
        level.LoadLevel(0);
        Load(8, new[] { Food(PoolType.Shrimp, 3), Food(), Food(PoolType.Egg) },
            new[] { Food(PoolType.Shrimp, 3) }, new[] { Food(PoolType.Shrimp, 2) });
        Check(Tap(0) && Tap(1) && Tap(2), "could not fill eight belt plates");
        if (settle) Advance(2);
    }

    private static void FullBeltRevive()
    {
        FillBagBelt();
        var payment = Node("Bag payment").gameObject.AddComponent<SushiRegressionPayment>();
        Set(level, "reviveProvider", payment);
        CheckLevelState(4);
        Check(level.PendingRevive == SushiReviveReason.None, "revive skipped the existing grace period");
        CheckLevelState(1);
        Check(level.IsPlaying && level.PendingRevive == SushiReviveReason.FullConveyor && Time.timeScale == 0,
            "full conveyor did not open the separate revive reason");
        payment.balance = 299;
        Check(!level.TryReviveWithCoins() && conveyor.OccupiedSlotCount == 8 && payment.balance == 299,
            "unpaid revive changed ingredients or wallet");
        payment.balance = 1000;
        Check(level.TryReviveWithCoins() && payment.balance == 700 && level.ReviveCost == 600,
            "revive cost or success state incorrect");
        Check(conveyor.OccupiedSlotCount == 4 && conveyor.FreeSlotCount == 4 && conveyor.ReviveBagCount == 1 &&
            FirstBag.Foods.Count == 5 && FirstBag.IsPacking && Time.timeScale == 1 && level.PendingRevive == SushiReviveReason.None,
            "five ingredients did not reserve exactly one plate");
        Check(!level.TryReviveWithCoins() && payment.balance == 700, "double click spent coins twice");
        Advance(2);
        foreach (FoodShushi food in FirstBag.Foods)
            Check(!food.gameObject.activeSelf && food.Amount == 1, "stored ingredient lost quantity or stayed visible");
        Check(!FirstBag.IsPacking && !conveyor.CanMakeProgress, "irrelevant stored toppings hid a real deadlock");
        FillBagBelt();
        CheckLevelState(5);
        //level.TryReviveWithAd();
        Check(level.PendingRevive == SushiReviveReason.None && conveyor.FreeSlotCount == 4 && payment.balance == 700,
            "rewarded revive did not pack a bag or charged coins");
        FillBagBelt();
        CheckLevelState(5);
        level.DeclineRevive();
        Check(!level.IsPlaying && conveyor.ReviveBagCount == 0 && payment.balance == 700,
            "declined revive changed the belt or wallet");
    }

    private static void BagEligibility()
    {
        FillBagBelt(false);
        Check(!conveyor.CanReviveFullConveyor && !conveyor.TryReviveFullConveyor(), "packing stole in-flight reservations");
        Advance(2);
        Check(conveyor.CanReviveFullConveyor && conveyor.TryReviveFullConveyor(), "settled ingredients were not eligible");
        Check(!conveyor.TryReviveFullConveyor() && conveyor.ReviveBagCount == 1, "less than five loose ingredients created another bag");
        Load(4, new[] { Food(PoolType.Shrimp, 3) }, new[] { Food(PoolType.Shrimp) });
        Check(Tap(0) && Tap(1), "small belt setup failed");
        Advance(2);
        Check(!conveyor.CanReviveFullConveyor, "less than five plates offered a four-slot revive");
    }

    private static void PartialBeltRevive()
    {
        var cases = new[]
        {
            new[] { new[] { Food(PoolType.Shrimp, 3), Food(PoolType.Egg, 3) },
                new[] { Food(PoolType.Shrimp, 3), Food(PoolType.Egg, 3) } },
            new[] { new[] { Food(PoolType.Shrimp, 3), Food(PoolType.Egg, 2, link: 1) },
                new[] { Food(PoolType.Shrimp, 3), Food(PoolType.SushiRice, 2, link: 1) } },
            new[] { new[] { Food(PoolType.Shrimp, 3), Food(PoolType.SushiRice, 2, tray: 1), Food(PoolType.Egg, 2, tray: 1) },
                new[] { Food(PoolType.Shrimp, 3), Food(PoolType.Egg, 3) } }
        };
        foreach (FoodData[][] columns in cases)
        {
            Set(conveyor, "preparedDishPrefab", null);
            level.LoadLevel(0);
            Load(8, columns);
            var payment = Node("Partial belt payment").gameObject.AddComponent<SushiRegressionPayment>();
            Set(level, "reviveProvider", payment);
            Check(Tap(0) && Tap(1), "partial belt setup failed");
            Advance(2);
            Check(conveyor.FreeSlotCount == 2 && !board.HasAvailableTap(2) && board.HasAvailableTap(6),
                "fixture must be blocked by the group's required slot count");
            CheckLevelState(4);
            Check(level.PendingRevive == SushiReviveReason.None, "partial belt skipped the deadlock grace period");
            CheckLevelState(1);
            Check(level.IsPlaying && level.PendingRevive == SushiReviveReason.FullConveyor,
                "partially occupied belt lost instead of offering bag revive");
            Check(level.TryReviveWithCoins() && payment.balance == 700 && conveyor.FreeSlotCount == 6 &&
                FirstBag.Foods.Count == 5, "partial belt revive did not free four slots");
            Advance(2);
            Check(Tap(0), "revive did not make the previously blocked ingredient group collectible");
            Advance(2);
        }
    }

    private static void PartialBeltReviveGuards()
    {
        Set(conveyor, "preparedDishPrefab", null);
        level.LoadLevel(0);
        Load(8, new[] { Food(PoolType.Shrimp, 3), Food(PoolType.Egg) },
            new[] { Food(PoolType.Shrimp, 3), Food() });
        Check(Tap(0) && Tap(1), "playable partial belt setup failed");
        Advance(2);
        CheckLevelState(20);
        Check(level.PendingRevive == SushiReviveReason.None && level.IsPlaying && conveyor.ReviveBagCount == 0,
            "a playable board offered bag revive");

        Load(8, new[] { Food(PoolType.Shrimp, 3), Food(PoolType.Egg, ice: 2) },
            new[] { Food(PoolType.Shrimp, 3), Food(ice: 2) });
        Check(Tap(0) && Tap(1), "frozen partial belt setup failed");
        Advance(2);
        Check(conveyor.FreeSlotCount == 2 && board.AllRemainingHeadsFrozen(), "frozen fixture is not blocked by ice");
        CheckLevelState(5);
        Check(level.PendingRevive == SushiReviveReason.FrozenHeads,
            "extra space replaced the required ice revive with a bag");
        level.LoadLevel(0);
    }

    private static void BagDelivery()
    {
        FillBagBelt();
        Check(conveyor.TryReviveFullConveyor(), "bag setup failed");
        Advance(2);
        customers.Init(new List<CustomerOrderData> { Order(SushiDishId.NigiriShrimp, 5), Order(SushiDishId.NigiriEgg, 1) });
        Set(conveyor, "preparedDishPrefab", Prefab<SushiPreparedDish>("PreparedDish"));
        Customer customer = customers.ActiveCustomers[0];
        Invoke(conveyor, "DeliverBagIngredients");
        Check(FirstBag.Foods.Count == 5 && conveyor.PreparingDishCount == 0, "toppings left before their foundations");
        foreach (SushiDishOrder order in customer.Dishes)
        {
            Check(customer.TryReservePreparedIngredient(order, PoolType.SushiRice, out Transform target), "rice reservation failed");
            FoodShushi rice = SimplePool.Spawn<FoodShushi>(PoolType.SushiRice);
            rice.SetupFromData(Food()); rice.TryMoveToConveyor(); rice.ArriveAtConveyor();
            var plate = (SushiPreparedDish)typeof(ConveyorManager).GetMethod("GetOrCreatePreparedDish", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(conveyor, new object[] { customer, order, target });
            typeof(ConveyorManager).GetMethod("StartPreparedIngredientFlight", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(conveyor, new object[] { plate, rice, target });
        }
        Advance(2);
        SushiReviveBag bag = FirstBag;
        var contents = new List<FoodShushi>(bag.Foods);
        Vector3 origin = bag.Opening.position;
        Check(conveyor.CanMakeProgress, "needed bag ingredients were not counted as progress");
        Invoke(conveyor, "DeliverBagIngredients");
        Check(conveyor.ReviveBagCount == 0 && conveyor.OccupiedSlotCount == 3 && conveyor.PreparingDishCount == 5,
            "empty bag did not release its plate or deliveries were duplicated");
        foreach (FoodShushi food in contents)
            Check(food.gameObject.activeSelf && Vector3.Distance(food.transform.position, origin) < 0.001f &&
                food.Location == FoodLocation.MovingToCustomerPlate, "food did not depart directly from the pouch opening");
        Invoke(conveyor, "DeliverBagIngredients");
        Check(conveyor.PreparingDishCount == 5, "same contents were reserved twice");
        AdvanceBagDelivery();
        foreach (SushiDishOrder order in customer.Dishes)
            Check(order.IsComplete, "bag delivery did not finish the order: prepared=" + order.IsPrepared +
                " table=" + order.IsOnTable + " ingredients=" + order.Ingredients.All(item => item.IsComplete) +
                " flights=" + conveyor.PreparingDishCount + " serving=" + conveyor.DeliveringCount);
    }

    private static void BagMagnet()
    {
        FillBagBelt();
        Check(conveyor.TryReviveFullConveyor(), "bag setup failed");
        Advance(2);
        customers.Init(new List<CustomerOrderData> { Order(SushiDishId.NigiriShrimp, 1), Order(SushiDishId.NigiriTuna, 1) });
        Set(conveyor, "preparedDishPrefab", Prefab<SushiPreparedDish>("PreparedDish"));
        Customer missing = customers.ActiveCustomers[1];
        Check(!conveyor.TryTakeMagnet(board, missing, missing.Dishes[0]) && FirstBag.Foods.Count == 5 && Column(0).Count == 2,
            "missing Magnet recipe partially consumed stored ingredients");
        Customer customer = customers.ActiveCustomers[0];
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[0]) && FirstBag.Foods.Count == 4 &&
            conveyor.ReviveBagCount == 1 && Column(0).Count == 1, "Magnet did not combine bag toppings with board rice");
        AdvanceBagDelivery();
        Check(customer.Dishes[0].IsComplete, "Magnet from a bag did not complete the dish");
    }

    private static void AdvanceBagDelivery()
    {
        for (int i = 0; i < 80; i++)
        {
            // DOVirtual.DelayedCall uses Normal update; drive table holds with the manual fixture as well.
            var tweens = DOTween.TweensById(Get<object>(conveyor, "dishTweenId"), false);
            if (tweens != null) foreach (Tween tween in tweens) tween.SetUpdate(UpdateType.Manual);
            Advance(0.05f);
        }
    }

    private static void BagRepeatedRevive()
    {
        FillBagBelt();
        Check(conveyor.TryReviveFullConveyor(), "first bag failed");
        Advance(2);
        SushiReviveBag original = FirstBag;
        var originalFoods = new List<FoodShushi>(original.Foods);
        board.Init(new List<FoodQueueData> {
            new FoodQueueData { foods = new List<FoodData> { Food(PoolType.Shrimp, 3), Food(PoolType.Shrimp) } }
        });
        Check(Tap(0), "refill three slots failed"); Advance(2);
        Check(Tap(0), "refill fourth slot failed"); Advance(2);
        var loose = Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods");
        foreach (Transform slot in Bags.Keys) Check(!loose.ContainsKey(slot), "pickup reused the bag's occupied plate");
        Check(conveyor.TryReviveFullConveyor() && conveyor.ReviveBagCount == 2 && conveyor.FreeSlotCount == 4,
            "second full-belt revive did not free four slots");
        Advance(2);
        var unique = new HashSet<FoodShushi>(loose.Values);
        foreach (var bag in Bags.Values)
            foreach (var food in bag.Foods) Check(unique.Add(food), "ingredient appears in multiple bags/slots");
        Check(unique.Count == 12 && original.Foods.Count == 5, "repeated revive lost or repacked existing bag contents");
        foreach (var food in originalFoods) Check(original.Foods.Contains(food), "existing bag changed");
    }

    private static void BagReload()
    {
        foreach (bool duringPacking in new[] { true, false })
        {
            FillBagBelt();
            Check(conveyor.TryReviveFullConveyor(), "reload bag setup failed");
            if (!duringPacking) Advance(2);
            var contents = new List<FoodShushi>(FirstBag.Foods);
            level.LoadLevel(0);
            Advance(3);
            Check(conveyor.ReviveBagCount == 0 && conveyor.OccupiedSlotCount == 0 && conveyor.DeliveringCount == 0,
                "replay kept a bag, flight or reservation");
            foreach (FoodShushi food in contents)
                Check(food == null || !food.gameObject.activeSelf || food.Owner != null,
                    "stored food was orphaned or a packing callback changed the new level");
        }
    }
}
#endif
