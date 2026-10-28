#if UNITY_EDITOR || SUSHI_RECIPE_CHECKS
using System;
using System.Collections.Generic;

public static class SushiRecipeChecks
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("SushiKhai/Tests/Check 48 Recipes")]
#endif
    public static void Run()
    {
        assertions = 0;
        Check(SushiRecipeCatalog.All.Count == 48, "Expected 48 recipes");
        var ids = new HashSet<SushiDishId>();
        for (int i = 0; i < 48; i++)
        {
            SushiRecipe recipe = SushiRecipeCatalog.All[i];
            Check((int)recipe.Id == i + 1 && ids.Add(recipe.Id), "Unstable or duplicate recipe ID");
            Check(SushiRecipeCatalog.Get(recipe.Id) == recipe, "Catalog lookup mismatch");
            Check(!string.IsNullOrEmpty(recipe.Name) && !string.IsNullOrEmpty(recipe.BaseName), "Missing recipe presentation");
            foreach (PoolType ingredient in recipe.Ingredients)
                Check(ingredient != PoolType.None && Enum.IsDefined(typeof(PoolType), ingredient), "Invalid ingredient");
        }
        PoolType[] toppings = { PoolType.Tuna, PoolType.Salmon, PoolType.Flounder, PoolType.Mackerel,
            PoolType.Shrimp, PoolType.Octopus, PoolType.Avocado, PoolType.Egg, PoolType.Mayonnaise,
            PoolType.FishRoe, PoolType.BlackFishRoe, PoolType.GreenFishRoe };
        for (int i = 0; i < 12; i++) Expect(i + 1, PoolType.SushiRice, toppings[i]);
        for (int i = 0; i < 8; i++) Expect(13 + i, PoolType.Seaweed, PoolType.SushiRice, toppings[i]);
        for (int i = 0; i < 8; i++) Expect(21 + i, PoolType.Seaweed, PoolType.SushiRice, toppings[i]);
        for (int i = 0; i < 3; i++) Expect(29 + i, PoolType.Seaweed, PoolType.SushiRice, toppings[9 + i]);
        int sashimi = 32;
        for (int first = 0; first < 6; first++)
            for (int second = first + 1; second < 6; second++) Expect(sashimi++, toppings[first], toppings[second]);
        Expect(47, PoolType.TeaPot, PoolType.EmptyCup);
        Expect(48, PoolType.TeaPot, PoolType.EmptyCup, PoolType.IceCube);
        Check(!SushiRecipeCatalog.TryGet(SushiDishId.None, out _) && !SushiRecipeCatalog.TryGet((SushiDishId)49, out _), "Invalid recipe ID accepted");
        Check((int)PoolType.SushiRice == 1 && (int)PoolType.Shrimp == 2 && (int)PoolType.Egg == 3 &&
            (int)PoolType.Tuna == 10 && (int)PoolType.GrilledEel == 11, "Legacy ingredient IDs changed");
        var customer = new CustomerOrderData
        {
            dishes = new List<DishOrderData> { new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 2 },
                new DishOrderData { dish = SushiDishId.MakiShrimp, amount = 1 },
                new DishOrderData { dish = SushiDishId.IcedGreenTea, amount = 1 } }
        };
        Check(customer.ValidateOrders() == null, "Recipe customer rejected");
        List<FoodOrderData> ingredients = customer.CreateIngredientOrders();
        Check(Count(ingredients, PoolType.SushiRice) == 3 && Count(ingredients, PoolType.Shrimp) == 3 &&
            Count(ingredients, PoolType.Seaweed) == 1 && Count(ingredients, PoolType.TeaPot) == 1 &&
            Count(ingredients, PoolType.EmptyCup) == 1 && Count(ingredients, PoolType.IceCube) == 1, "Dish quantity expansion wrong");
        Check(customer.CreateFoodList().Count == 10, "Ingredient total/rush timer input wrong");
        ingredients[0].amount = 99;
        Check(customer.CreateFoodList().Count == 10, "Expanded ingredients alias customer data");
        var firstDish = new SushiDishOrder(0, SushiRecipeCatalog.Get(SushiDishId.NigiriShrimp));
        var secondDish = new SushiDishOrder(1, SushiRecipeCatalog.Get(SushiDishId.NigiriShrimp));
        var rice = firstDish.FindAvailableIngredient(PoolType.SushiRice);
        rice.incomingAmount++;
        Check(firstDish.FindAvailableIngredient(PoolType.SushiRice) == null, "Dish double-booked incoming rice");
        Check(secondDish.FindAvailableIngredient(PoolType.SushiRice) != null, "Repeated dishes share progress");
        Check(!firstDish.IsComplete && !rice.IsComplete, "Incoming ingredient completed a dish too early");
        rice.incomingAmount--; rice.receivedAmount++;
        Check(rice.IsComplete && !firstDish.IsComplete, "Dish completed without shrimp");
        Check(firstDish.FindAvailableIngredient(PoolType.Tuna) == null, "Wrong ingredient accepted");
        customer.dishes[0].dish = (SushiDishId)49;
        Check(customer.ValidateOrders() != null, "Invalid dish accepted");
        customer.dishes[0].dish = SushiDishId.NigiriShrimp; customer.dishes[0].amount = 0;
        Check(customer.ValidateOrders() != null, "Zero portions accepted");
        customer.dishes[0].amount = int.MaxValue;
        Check(customer.ValidateOrders() != null, "Overflowing order accepted");
        var empty = new CustomerOrderData();
        Check(empty.ValidateOrders() != null && empty.CreateFoodList().Count == 0, "Customer without dishes accepted");
        empty.dishes = null;
        Check(empty.ValidateOrders() != null && empty.CreateFoodList().Count == 0, "Null dish list accepted");
#if UNITY_EDITOR
        UnityEngine.Debug.Log("SUSHI_RECIPES_OK: 48 recipes, " + assertions + " assertions passed");
#else
        Console.WriteLine("SUSHI_RECIPES_OK: 48 recipes, " + assertions + " assertions passed");
#endif
    }

    private static void Expect(int id, params PoolType[] expected)
    {
        var actual = SushiRecipeCatalog.Get((SushiDishId)id).Ingredients;
        Check(actual.Count == expected.Length, "Ingredient count mismatch for " + id);
        for (int i = 0; i < expected.Length; i++) Check(actual[i] == expected[i], "Ingredient mismatch for " + id);
    }
    private static int Count(List<FoodOrderData> list, PoolType type) => list.Find(item => item.typeFood == type)?.amount ?? 0;
}
#endif
