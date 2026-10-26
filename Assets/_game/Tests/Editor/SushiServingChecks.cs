using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Executed by the campaign integration suite against the saved serving prefabs.
public static class SushiServingChecks
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Plated serving: " + message); }
    private static void Set(ConveyorManager belt, string field, object value) => typeof(ConveyorManager)
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(belt, value);
    private static IEnumerator Wait(Func<bool> condition, string operation)
    {
        float deadline = Time.realtimeSinceStartup + 25;
        while (!condition())
        {
            Check(Time.realtimeSinceStartup < deadline, "timeout: " + operation);
            yield return null;
        }
    }
    private static bool Landed(LevelManager level) => level.Board.FoodQueues.All(q => !q.IsPickupPending) &&
        level.Conveyor.PreparingDishCount == 0 && !level.Conveyor.HasCompleteRecipeOnBelt() &&
        level.Conveyor.Slots.All(s => s.GetComponentInChildren<FoodShushi>() == null ||
            s.GetComponentInChildren<FoodShushi>().Location == FoodLocation.Conveyor);

    public static IEnumerator Run(LevelManager level)
    {
        System.IO.Directory.CreateDirectory("Documentation");
        var levels = DataManager.ins.allLevelData.listAllLevel;
        var original = levels[0];
        var data = ScriptableObject.CreateInstance<SushiLevelData>();
        data.number = 20; data.levelName = "Plated serving regression"; data.numberColumns = 3;
        data.beltSlots = 8; data.loopsPerSecond = 0.3f;
        data.customers.Add(Order(SushiDishId.NigiriShrimp, 2));
        data.customers.Add(Order(SushiDishId.NigiriSalmon, 1));
        data.boardColumns.Add(Column(PoolType.SushiRice, 3));
        data.boardColumns.Add(Column(PoolType.Shrimp, 2));
        data.boardColumns.Add(Column(PoolType.Salmon, 1));
        Check(data.ValidateData() == null, data.ValidateData());
        try
        {
            levels[0] = data; level.LoadLevel(0);
            var belt = level.Conveyor;
            Set(belt, "loopsPerSecond", 0f);
            var first = belt.Customers.ActiveCustomers[0];
            var second = belt.Customers.ActiveCustomers[1];
            var recipes = first.GetComponent<CustomerRecipeView>();
            Check(first.transform.Find("Serving slots").Cast<Transform>().Count(slot => slot.gameObject.activeSelf) == 2 &&
                second.transform.Find("Serving slots").Cast<Transform>().Count(slot => slot.gameObject.activeSelf) == 1,
                "each customer should show one circular serving plate per ordered dish portion.");
            Check(recipes.CardAt(0) == recipes.CardAt(1) && recipes.CardAt(0).PortionProgress == "0/2",
                "identical recipe portions must share one 0/2 order icon.");
            // A matching ingredient should start assembling before the full recipe reaches the belt.
            Check(belt.TryTakeFromQueue(level.Board.FoodQueues[0]), "rice pickup rejected.");
            IEnumerator wait = Wait(() => belt.Slots.Any(s =>
                s.GetComponentInChildren<FoodShushi>()?.Location == FoodLocation.Conveyor), "rice landing");
            while (wait.MoveNext()) yield return wait.Current;
            Transform riceSlot = belt.Slots.Single(s =>
                s.GetComponentInChildren<FoodShushi>()?.Type == PoolType.SushiRice);
            int riceSlotIndex = 0;
            while (belt.Slots[riceSlotIndex] != riceSlot) riceSlotIndex++;
            float awayFromRice = Mathf.Repeat((float)riceSlotIndex / belt.Slots.Count + 0.5f, 1f);
            Set(belt, "deliveryProgresses", new[] { awayFromRice, awayFromRice });
            Check(belt.PreparedDishCount == 0 && first.Orders.Single(o =>
                o.typeFood == PoolType.SushiRice).receivedAmount == 0,
                "matching ingredients should stay on the conveyor until they reach the customer's pickup point.");
            Set(belt, "loopsPerSecond", 0.3f);
            wait = Wait(() =>
                first.Dishes[0].Ingredients.Single(i => i.typeFood == PoolType.SushiRice).receivedAmount == 1,
                "first ingredient lands on the customer's plate");
            while (wait.MoveNext()) yield return wait.Current;
            Check(belt.PreparedDishCount == 1 && belt.PlatedDishes.Values.Single().Owner == first &&
                !belt.PlatedDishes.Values.Single().IsReady && !first.Dishes[0].IsOnTable &&
                !first.Dishes[0].IsComplete &&
                first.Orders.Single(o => o.typeFood == PoolType.SushiRice).receivedAmount == 1 &&
                first.Orders.Single(o => o.typeFood == PoolType.Shrimp).receivedAmount == 0,
                "the first matching ingredient should be visible on a partial customer plate without completing the order.");
            Check(belt.TryTakeFromQueue(level.Board.FoodQueues[1]), "shrimp pickup rejected.");
            wait = Wait(() => first.Dishes[0].IsOnTable && belt.PreparingDishCount == 0,
                "ingredients assemble on customer plate");
            while (wait.MoveNext()) yield return wait.Current;
            Check(belt.OccupiedSlotCount == 0 && belt.PreparedDishCount == 1 &&
                belt.PlatedDishes.Values.Single().Owner == first &&
                belt.PlatedDishes.Values.Single().IsReady &&
                belt.PlatedDishes.Keys.Single().IsChildOf(first.transform),
                "ingredients should leave the belt and assemble on the seated guest's circular serving plate.");
            Check(!first.Dishes[0].IsComplete && recipes.CardAt(0).PortionProgress == "0/2", "assembly counted as delivery.");
            SushiDishOrder onTable = first.Dishes[0];
            int occupied = belt.OccupiedSlotCount;
            UIManager.Ins.OpenUI<CanvasSetting>();
            float pauseUntil = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < pauseUntil) yield return null;
            Check(!onTable.IsComplete && onTable.IsOnTable && belt.OccupiedSlotCount == occupied, "popup failed to pause the serving animation.");
            UIManager.Ins.GetUI<CanvasSetting>().OnResume();
            wait = Wait(() => onTable.IsComplete, "finished dish jumps directly to the order");
            while (wait.MoveNext()) yield return wait.Current;
            Check(belt.PreparedDishCount == 0 && recipes.CardAt(0).PortionProgress == "1/2",
                "completed dish remained on the serving plate or did not update its order.");
            wait = Wait(() => Landed(level), "board advances to second portion");
            while (wait.MoveNext()) yield return wait.Current;
            Check(belt.TryTakeFromQueue(level.Board.FoodQueues[0]) &&
                belt.TryTakeFromQueue(level.Board.FoodQueues[1]), "second shrimp portion pickup rejected.");
            wait = Wait(() => first.Dishes[1].IsComplete, "second finished dish reaches order");
            while (wait.MoveNext()) yield return wait.Current;
            Check(recipes.CardAt(0).PortionProgress == "2/2", "second portion did not complete the grouped order.");
            wait = Wait(() => Landed(level), "board advances to salmon");
            while (wait.MoveNext()) yield return wait.Current;
            Check(belt.TryTakeFromQueue(level.Board.FoodQueues[0]) &&
                belt.TryTakeFromQueue(level.Board.FoodQueues[2]), "salmon recipe pickup rejected.");
            SushiDishOrder secondDish = second.Dishes[0];
            wait = Wait(() => level.HasWon, "all plated orders served"); while (wait.MoveNext()) yield return wait.Current;
            Check(belt.OccupiedSlotCount == 0 && belt.DeliveringCount == 0 && belt.Customers.CompletedCustomers == 2 &&
                secondDish.IsComplete,
                "serving chain leaked a plate or failed to finish both seated guests.");

            // Magnet intercepts a raw pickup, assembles it on the selected serving plate,
            // and reserves only one portion of a grouped order.
            level.LoadLevel(0); first = belt.Customers.ActiveCustomers[0];
            Check(belt.TryTakeFromQueue(level.Board.FoodQueues[0]), "magnet pickup rejected.");
            Check(belt.TryTakeMagnet(level.Board, first, first.Dishes[0]), "magnet could not intercept ingredients in flight.");
            Check(!belt.TryTakeMagnet(level.Board, first, first.Dishes[0]), "magnet duplicated an in-flight serving.");
            wait = Wait(() => first.Dishes[0].IsComplete, "magnet via table"); while (wait.MoveNext()) yield return wait.Current;
            Check(!first.IsCompleted && first.GetComponent<CustomerRecipeView>().CardAt(0).PortionProgress == "1/2",
                "one magnet serving must leave the guest at 1/2.");
            Check(belt.TryTakeMagnet(level.Board, first, first.Dishes[1]), "second magnet serving rejected.");
            var cancelled = first.Dishes[1];
            level.LoadLevel(0);
            yield return null;
            Check(!cancelled.IsPrepared && belt.DeliveringCount == 0 && belt.OccupiedSlotCount == 0 &&
                Object.FindObjectsByType<SushiPreparedDish>(FindObjectsSortMode.None).Length == 0,
                "reload during assembly retained food, plates, or reserved orders.");
            Debug.Log("PLATED_SERVING_CHECKS_OK: ingredient-by-ingredient assembly on customer plates, direct order delivery, pause, magnet and cancellation.");
        }
        finally
        {
            levels[0] = original;
            level.LoadLevel(0);
            Object.DestroyImmediate(data);
        }
    }
    private static CustomerOrderData Order(SushiDishId dish, int count)
    {
        var order = new CustomerOrderData(); order.dishes.Add(new DishOrderData { dish = dish, amount = count }); return order;
    }
    private static FoodQueueData Column(PoolType type, int count)
    {
        var column = new FoodQueueData();
        for (int i = 0; i < count; i++) column.foods.Add(new FoodData { typeFood = type, amount = 1 });
        return column;
    }
}
