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
    [UnityEditor.MenuItem("SushiKhai/Tests/Check Recipes")]
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
        for (int i = 0; i < 8; i++) Expect(13 + i, PoolType.SushiRice, PoolType.Seaweed, toppings[i]);
        for (int i = 0; i < 8; i++) Expect(21 + i, PoolType.SushiRice, PoolType.Seaweed, toppings[i]);
        for (int i = 0; i < 3; i++) Expect(29 + i, PoolType.SushiRice, PoolType.Seaweed, toppings[9 + i]);
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
        CustomRecipes();
#if UNITY_EDITOR
        UnityEngine.Debug.Log("SUSHI_RECIPES_OK: 48 presets and custom combinations, " + assertions + " assertions passed");
#else
        Console.WriteLine("SUSHI_RECIPES_OK: 48 presets and custom combinations, " + assertions + " assertions passed");
#endif
    }

    private static void CustomRecipes()
    {
        var keys = new HashSet<string>();
        foreach (PoolType first in Enum.GetValues(typeof(PoolType)))
        {
            if (!SushiRecipeCatalog.IsFoodTopping(first)) continue;
            var pair = new DishOrderData { recipeKind = SushiRecipeKind.TwoWithRice, firstIngredient = first };
            SushiRecipe recipe = pair.GetRecipe();
            Check(recipe.Ingredients.Count == 2 && recipe.Ingredients[0] == PoolType.SushiRice &&
                recipe.Ingredients[1] == first && keys.Add(recipe.Key), "Rice pair missing or collides");
            foreach (PoolType second in Enum.GetValues(typeof(PoolType)))
            {
                if (!SushiRecipeCatalog.IsFoodTopping(second) || first >= second) continue;
                foreach (var kind in new[] { SushiRecipeKind.TwoWithoutRice, SushiRecipeKind.ThreeWithRice })
                {
                    var data = new DishOrderData { recipeKind = kind, firstIngredient = first, secondIngredient = second, amount = 2 };
                    recipe = data.GetRecipe();
                    Check(keys.Add(recipe.Key), "Different custom combinations grouped together");
                    Check(recipe.Ingredients.Count == (kind == SushiRecipeKind.ThreeWithRice ? 3 : 2), "Wrong custom ingredient count");
                    var reverse = data.Copy(); reverse.firstIngredient = second; reverse.secondIngredient = first;
                    Check(recipe.Key == reverse.GetRecipe().Key, "Swapped toppings must group together");
                    var customer = new CustomerOrderData { dishes = new List<DishOrderData> { data } };
                    Check(customer.ValidateOrders() == null && customer.CreateFoodList().Count == 2 * recipe.Ingredients.Count,
                        "Custom recipe was not expanded into board ingredients");
                    Check(Count(customer.CreateIngredientOrders(), first) == 2 && Count(customer.CreateIngredientOrders(), second) == 2,
                        "Custom topping quantities wrong");
                    var dish = new SushiDishOrder(0, recipe);
                    if (kind == SushiRecipeKind.ThreeWithRice)
                    {
                        Check(recipe.Ingredients[0] == PoolType.SushiRice && dish.FoundationType == PoolType.SushiRice,
                            "Rice must be first");
                        Check(!dish.CanStartIngredient(first) && !dish.CanStartIngredient(second), "Toppings started before rice");
                        dish.FindAvailableIngredient(PoolType.SushiRice).receivedAmount = 1;
                        Check(dish.CanStartIngredient(first) && dish.CanStartIngredient(second), "Rice did not unlock both toppings");
                    }
                    else Check(!recipe.HasRice && dish.FoundationType == PoolType.None && dish.CanStartIngredient(first) && dish.CanStartIngredient(second),
                        "Rice-free pair should accept either ingredient first");
                    reverse.amount = 9;
                    Check(data.amount == 2 && data.firstIngredient == first, "Duplicated orders share mutable data");
                }
            }
        }
        Check(keys.Count == 289, "Expected 17 rice pairs, 136 rice-free pairs and 136 rice triples");
        foreach (PoolType invalid in new[] { PoolType.None, PoolType.SushiRice, PoolType.TeaPot, PoolType.EmptyCup, PoolType.IceCube, (PoolType)99 })
        {
            Check(!SushiRecipeCatalog.TryCreate(SushiRecipeKind.TwoWithRice, invalid, PoolType.Shrimp, out _, out _), "Invalid food topping accepted");
            Check(!SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithRice, PoolType.Shrimp, invalid, out _, out _), "Invalid second topping accepted");
        }
        Check(!SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithRice, PoolType.Shrimp, PoolType.Shrimp, out _, out _), "Repeated type accepted as two different ingredients");
        Check(!SushiRecipeCatalog.TryCreate((SushiRecipeKind)99, PoolType.Shrimp, PoolType.Avocado, out _, out _), "Unknown recipe kind accepted");
        Check(new DishOrderData().GetRecipe().Id == SushiDishId.NigiriShrimp, "Old serialized dish no longer defaults to catalog mode");
        Check(SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithRice, PoolType.Shrimp, PoolType.Seaweed, out var maki, out _) &&
            ReferenceEquals(maki, SushiRecipeCatalog.Get(SushiDishId.MakiShrimp)), "Mixed custom/catalog orders must share ingredient-slot order");
        Check(SushiRecipeCatalog.TryCreate(SushiRecipeKind.TwoWithoutRice, PoolType.Shrimp, PoolType.Tuna, out var sashimi, out _) &&
            ReferenceEquals(sashimi, SushiRecipeCatalog.Get(SushiDishId.SashimiTunaShrimp)), "Rice-free preset slot order changed in a mixed group");
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
