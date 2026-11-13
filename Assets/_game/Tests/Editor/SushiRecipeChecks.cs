#if UNITY_EDITOR || SUSHI_RECIPE_CHECKS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        Check(SushiRecipeCatalog.All.Count == 68, "Expected exactly the 68 supplied dishes");
        var ids = new HashSet<SushiDishId>();
        for (int i = 0; i < SushiRecipeCatalog.All.Count; i++)
        {
            SushiRecipe recipe = SushiRecipeCatalog.All[i];
            Check((int)recipe.Id == i + 1 && ids.Add(recipe.Id), "Unstable or duplicate recipe ID");
            Check(SushiRecipeCatalog.Get(recipe.Id) == recipe, "Catalog lookup mismatch");
            Check(!string.IsNullOrEmpty(recipe.Name) && !string.IsNullOrEmpty(recipe.BaseName), "Missing recipe presentation");
            foreach (PoolType ingredient in recipe.Ingredients)
                Check(ingredient != PoolType.None && Enum.IsDefined(typeof(PoolType), ingredient), "Invalid ingredient");
        }
        PoolType[] toppings = { PoolType.Tuna, PoolType.Salmon, PoolType.Mackerel, PoolType.Flounder,
            PoolType.Shrimp, PoolType.Octopus, PoolType.Avocado, PoolType.Egg, PoolType.Mayonnaise,
            PoolType.FishRoe, PoolType.BlackFishRoe, PoolType.GreenFishRoe };
        // IDs 9–12 (nigiri mayonnaise và ba loại trứng cá) dùng lại mã cũ theo bộ ảnh fix.
        for (int i = 0; i < 12; i++) Expect(i + 1, PoolType.SushiRice, toppings[i]);
        Garnishes();
        for (int i = 0; i < 8; i++) Expect(13 + i, PoolType.SushiRice, PoolType.Seaweed, toppings[i]);
        for (int i = 0; i < 8; i++) Expect(21 + i, PoolType.SushiRice, PoolType.Seaweed, toppings[i]);
        for (int i = 0; i < 3; i++) Expect(29 + i, PoolType.SushiRice, PoolType.Seaweed, toppings[9 + i]);
        int sashimi = 32;
        for (int first = 0; first < 6; first++)
            for (int second = first + 1; second < 6; second++) Expect(sashimi++, toppings[first], toppings[second]);
        Expect(47, PoolType.TeaPot, PoolType.EmptyCup);
        Expect(48, PoolType.TeaPot, PoolType.EmptyCup, PoolType.IceCube);
        Check(!SushiRecipeCatalog.TryGet(SushiDishId.None, out _) && !SushiRecipeCatalog.TryGet((SushiDishId)69, out _) &&
            !SushiRecipeCatalog.TryGet((SushiDishId)(-1), out _), "Invalid recipe ID accepted");
        SashimiTriples();
        Check(SushiRecipeCatalog.Get(SushiDishId.NigiriOctopus).Name == "Nigiri mực" &&
            SushiRecipeCatalog.Get(SushiDishId.NigiriEgg).Name == "Nigiri trứng" &&
            SushiRecipeCatalog.Get(SushiDishId.GunkanOrangeRoe).Name == "Gunkan trứng cá vàng" &&
            SushiRecipeCatalog.Get(SushiDishId.GreenTea).Name == "Trà nóng" &&
            SushiRecipeCatalog.Get(SushiDishId.IcedGreenTea).Name == "Trà đá", "Dish names do not match source filenames");
        Check(SushiRecipeCatalog.AssetName(SushiDishId.NigiriTuna) == "nigiri_ca_ngu" &&
            SushiRecipeCatalog.AssetName(PoolType.Tuna) == "tuna" && SushiRecipeCatalog.AssetName(PoolType.SushiRice) == "rice",
            "Inspector dropdowns still use the old enum labels");
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
        customer.dishes[0].dish = (SushiDishId)69;
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
        CustomTriples();
        UnorderedServing();
        SharedWrappedBases();
#if UNITY_EDITOR
        SourceArtwork();
        UnityEngine.Debug.Log("SUSHI_RECIPES_OK: 68 presets and custom combinations, " + assertions + " assertions passed");
#else
        Console.WriteLine("SUSHI_RECIPES_OK: 68 presets and custom combinations, " + assertions + " assertions passed");
#endif
    }

#if UNITY_EDITOR
    private static void SourceArtwork()
    {
        var art = UnityEditor.AssetDatabase.LoadAssetAtPath<SushiArtCatalog>("Assets/_game/Resources/SushiArtCatalog.asset");
        Check(art != null && art.ingredientImages != null && art.dishImages != null && art.platedIngredientImages != null,
            "Missing art libraries");
        var artworkIds = art.dishImages.dishes.Where(entry => entry != null && SushiRecipeCatalog.TryGet(entry.dish, out _))
            .Select(entry => entry.dish).ToArray();
        Check(artworkIds.Length == SushiRecipeCatalog.All.Count && artworkIds.Distinct().Count() == SushiRecipeCatalog.All.Count,
            "Dish artwork IDs are missing or duplicated");
        foreach (PoolType type in Enum.GetValues(typeof(PoolType)))
            if (type != PoolType.None)
            {
                var sprite = art.GetIngredient(type);
                string path = SushiArtSetup.IngredientPath(type);
                Check(sprite != null, "Missing ingredient artwork: " + type);
                if (!path.StartsWith(SushiArtSetup.SourceArtRoot, StringComparison.Ordinal)) continue;
                string spritePath = UnityEditor.AssetDatabase.GetAssetPath(sprite);
                Check(spritePath == path &&
                    SushiRecipeCatalog.AssetName(type) == System.IO.Path.GetFileNameWithoutExtension(path),
                    "Ingredient source/name mismatch: " + type);
            }
        // Bộ ảnh fix: nigiri/maki/gunkan/sashimi dựng từ đĩa riêng và các lớp ảnh; chỉ trà còn dùng ảnh món hoàn chỉnh.
        var plated = art.platedIngredientImages;
        var buffer = new List<SushiPlatedIngredientArtLibrary.Layer>();
        var garnishes = new[] { PoolType.Lemon, PoolType.Wasabi, PoolType.Perilla };
        int layered = 0;
        foreach (SushiRecipe recipe in SushiRecipeCatalog.All)
        {
            var plate = plated.GetPlate(recipe);
            if (recipe.Family == SushiDishFamily.Tea)
            {
                Check(plate == null && !plated.IsLayered(recipe) && art.dishImages.GetSprite(recipe) != null,
                    "Tea must keep its finished dish artwork: " + recipe.Name);
                continue;
            }
            layered++;
            Check(plated.IsLayered(recipe) && plate != null && plate.size.x > 0 && plate.size.y > 0 &&
                UnityEditor.AssetDatabase.GetAssetPath(plate.sprite).StartsWith(SushiArtSetup.FixArtRoot, StringComparison.Ordinal),
                "Missing finished plate for " + recipe.Name);
            string plateName = System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GetAssetPath(plate.sprite));
            Check(plateName == (recipe.Family == SushiDishFamily.Sashimi ? "ice_tray" : "plate"), "Wrong plate kind for " + recipe.Name);
            SushiRecipe full = SushiRecipeCatalog.WithGarnish(recipe, true, true, true);
            foreach (PoolType type in full.Ingredients)
            {
                Check(plated.HasLayers(full, type), "Missing layer artwork: " + recipe.Name + "/" + type);
                buffer.Clear();
                plated.GetLayers(full, type, null, buffer);
                // Trong maki/gunkan hoàn chỉnh, cơm nằm sẵn trong ảnh cuộn rong biển nên không có lớp riêng.
                bool merged = type == PoolType.SushiRice && full.Ingredients.Contains(PoolType.Seaweed);
                Check(merged ? buffer.Count == 0 : buffer.Count > 0, "Wrong layer count: " + recipe.Name + "/" + type);
                foreach (var layer in buffer)
                    Check(layer.sprite != null && layer.size.x > 0 && layer.size.y > 0 &&
                        UnityEditor.AssetDatabase.GetAssetPath(layer.sprite).StartsWith(SushiArtSetup.FixArtRoot, StringComparison.Ordinal),
                        "Layer does not use the fix artwork: " + recipe.Name + "/" + type);
            }
            foreach (PoolType type in garnishes)
                Check(!plated.HasLayers(recipe, type) || recipe.ToppingSlot(type) == 0, "Garnish took a topping position");
        }
        Check(layered == 66, "Every non-tea dish must use the layered artwork");
        Check(plated.FindStyle(SushiRecipeCatalog.Get(SushiDishId.SashimiTunaSalmon)).plate !=
            plated.FindStyle(SushiRecipeCatalog.Get(SushiDishId.SashimiTunaSalmonShrimp)).plate ||
            plated.FindStyle(SushiRecipeCatalog.Get(SushiDishId.SashimiTunaSalmon)).style !=
            plated.FindStyle(SushiRecipeCatalog.Get(SushiDishId.SashimiTunaSalmonShrimp)).style, "The two sashimi styles were merged");
    }
#endif

    private static void SashimiTriples()
    {
        var keys = new HashSet<string>();
        for (int id = 49; id <= 68; id++)
        {
            var recipe = SushiRecipeCatalog.Get((SushiDishId)id);
            Check(recipe.Family == SushiDishFamily.Sashimi && recipe.Kind == SushiRecipeKind.ThreeWithoutRice &&
                !recipe.HasRice && recipe.Ingredients.Count == 3 && keys.Add(recipe.Key), "Invalid or duplicate sashimi triple");
            Check(recipe.Ingredients.Distinct().Count() == 3 && recipe.Ingredients.All(type =>
                new[] { PoolType.Tuna, PoolType.Salmon, PoolType.Flounder, PoolType.Mackerel, PoolType.Shrimp, PoolType.Octopus }.Contains(type)),
                "Sashimi triple has an ingredient outside the supplied seafood set");
            var dish = new SushiDishOrder(0, recipe);
            Check(dish.FoundationType == PoolType.None && recipe.Ingredients.All(type => dish.CanStartIngredient(type)),
                "Sashimi should accept any ingredient first");
        }
        // Source images 49 and 53 have a deliberately different order from the old pair IDs (ca_bon art is mackerel).
        Expect(49, PoolType.Tuna, PoolType.Salmon, PoolType.Flounder);
        Expect(53, PoolType.Tuna, PoolType.Salmon, PoolType.Mackerel);
        Expect(68, PoolType.Flounder, PoolType.Shrimp, PoolType.Octopus);
        var customer = new CustomerOrderData { dishes = new List<DishOrderData> {
            new DishOrderData { dish = SushiDishId.SashimiTunaSalmonMackerel, amount = 2 },
            new DishOrderData { dish = SushiDishId.SashimiSalmonShrimpOctopus, amount = 1 } } };
        Check(customer.CreateFoodList().Count == 9 && Count(customer.CreateIngredientOrders(), PoolType.Salmon) == 3 &&
            Count(customer.CreateIngredientOrders(), PoolType.SushiRice) == 0, "Sashimi portions expanded incorrectly");
    }

    private static void CustomTriples()
    {
        var toppings = Enum.GetValues(typeof(PoolType)).Cast<PoolType>().Where(SushiRecipeCatalog.IsFoodTopping).ToArray();
        var keys = new HashSet<string>();
        int presets = 0;
        for (int a = 0; a < toppings.Length; a++)
            for (int b = a + 1; b < toppings.Length; b++)
                for (int c = b + 1; c < toppings.Length; c++)
                {
                    var data = new DishOrderData { recipeKind = SushiRecipeKind.ThreeWithoutRice,
                        firstIngredient = toppings[a], secondIngredient = toppings[b], thirdIngredient = toppings[c], amount = 2 };
                    var recipe = data.GetRecipe();
                    Check(recipe.Kind == SushiRecipeKind.ThreeWithoutRice && recipe.Ingredients.Count == 3 &&
                        !recipe.HasRice && keys.Add(recipe.Key), "Rice-free triples collided or added rice");
                    var reverse = data.Copy();
                    reverse.firstIngredient = toppings[c]; reverse.thirdIngredient = toppings[a];
                    Check(reverse.GetRecipe().Key == recipe.Key, "Reversed triple did not group with the same dish");
                    if (!recipe.IsCustom) { presets++; Check(ReferenceEquals(recipe, reverse.GetRecipe()), "Preset slot order changed"); }
                    var customer = new CustomerOrderData { dishes = new List<DishOrderData> { data } };
                    Check(customer.ValidateOrders() == null && customer.CreateFoodList().Count == 6 &&
                        Count(customer.CreateIngredientOrders(), toppings[c]) == 2, "Third topping or portion count was lost");
                    var pair = new DishOrderData { recipeKind = SushiRecipeKind.TwoWithoutRice,
                        firstIngredient = toppings[a], secondIngredient = toppings[b] }.GetRecipe();
                    Check(pair.Key != recipe.Key, "Triple grouped with a pair");
                }
        Check(keys.Count == 680 && presets == 20, "Expected all 680 triples including the 20 supplied sashimi dishes");
        foreach (PoolType invalid in new[] { PoolType.None, PoolType.SushiRice, PoolType.EmptyCup, PoolType.TeaPot, PoolType.IceCube, (PoolType)99 })
            Check(!SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithoutRice, PoolType.Tuna, PoolType.Salmon, invalid, out _, out _),
                "Invalid third ingredient accepted");
        Check(!SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithoutRice, PoolType.Tuna, PoolType.Salmon, PoolType.Tuna, out _, out _) &&
            !SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithoutRice, PoolType.Tuna, PoolType.Salmon, PoolType.Salmon, out _, out _),
            "Repeated third ingredient accepted");
    }

    private static void CustomRecipes()
    {
        var keys = new HashSet<string>();
        foreach (PoolType first in Enum.GetValues(typeof(PoolType)))
        {
            if (!SushiRecipeCatalog.IsFoodTopping(first)) continue;
            var pair = new DishOrderData { recipeKind = SushiRecipeKind.TwoWithRice, firstIngredient = first };
            SushiRecipe recipe;
            if (SushiRecipeCatalog.IsNigiriTopping(first))
            {
                recipe = pair.GetRecipe();
                Check(recipe.Ingredients.Count == 2 && recipe.Ingredients[0] == PoolType.SushiRice &&
                    recipe.Ingredients[1] == first && keys.Add(recipe.Key), "Rice pair missing or collides");
            }
            else Check(!pair.TryGetRecipe(out _, out _), "A retired dish was recreated as a custom rice pair");
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
                        bool nori = first == PoolType.Seaweed || second == PoolType.Seaweed;
                        PoolType topping = first == PoolType.Seaweed ? second : first;
                        if (nori)
                        {
                            // Maki có rong biển: rong biển lên đĩa trước, rồi cơm, cuối cùng là topping.
                            Check(dish.LeadType == PoolType.Seaweed && dish.CanStartIngredient(PoolType.Seaweed) &&
                                !dish.CanStartIngredient(PoolType.SushiRice) && !dish.CanStartIngredient(topping),
                                "Nori must be plated before rice and topping");
                            dish.FindAvailableIngredient(PoolType.Seaweed).receivedAmount = 1;
                            Check(dish.CanStartIngredient(PoolType.SushiRice) && !dish.CanStartIngredient(topping),
                                "Nori did not unlock rice, or topping skipped the rice");
                        }
                        else Check(!dish.CanStartIngredient(first) && !dish.CanStartIngredient(second), "Toppings started before rice");
                        dish.FindAvailableIngredient(PoolType.SushiRice).receivedAmount = 1;
                        if (!nori && SushiRecipeCatalog.IsRoe(first) != SushiRecipeCatalog.IsRoe(second))
                        {
                            // Nigiri phủ trứng cá: cơm -> lát chính -> trứng cá.
                            PoolType roe = SushiRecipeCatalog.IsRoe(first) ? first : second;
                            PoolType main = roe == first ? second : first;
                            Check(dish.CanStartIngredient(main) && !dish.CanStartIngredient(roe), "Roe started before the slice under it");
                            dish.FindAvailableIngredient(main).receivedAmount = 1;
                            Check(dish.CanStartIngredient(roe), "Slice did not unlock the roe on top");
                        }
                        else Check(dish.CanStartIngredient(first) && dish.CanStartIngredient(second), "Rice did not unlock both toppings");
                    }
                    else Check(!recipe.HasRice && dish.FoundationType == PoolType.None && dish.CanStartIngredient(first) && dish.CanStartIngredient(second),
                        "Rice-free pair should accept either ingredient first");
                    reverse.amount = 9;
                    Check(data.amount == 2 && data.firstIngredient == first, "Duplicated orders share mutable data");
                }
            }
        }
        Check(keys.Count == 288, "Expected 16 rice pairs, 136 rice-free pairs and 136 rice triples");
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

    // Đồ trang trí (chanh, wasabi, lá tía tô) và nigiri phủ trứng cá.
    private static void Garnishes()
    {
        Check((int)PoolType.IceCube == 21 && (int)PoolType.Lemon == 22 && (int)PoolType.Wasabi == 23 &&
            (int)PoolType.Perilla == 24, "Garnish ingredients must be appended after the legacy IDs");
        var plain = new DishOrderData { dish = SushiDishId.NigiriSalmon };
        var full = new DishOrderData { dish = SushiDishId.NigiriSalmon, lemon = true, wasabi = true, perilla = true, amount = 2 };
        SushiRecipe recipe = full.GetRecipe();
        Check(ReferenceEquals(plain.GetRecipe(), SushiRecipeCatalog.Get(SushiDishId.NigiriSalmon)), "Plain order changed its catalog recipe");
        Check(recipe.Id == SushiDishId.NigiriSalmon && recipe.Ingredients.Count == 5 && recipe.CoreCount == 2 &&
            recipe.Kind == SushiRecipeKind.TwoWithRice && recipe.Key != plain.GetRecipe().Key &&
            ReferenceEquals(recipe, full.GetRecipe()), "Garnished dish must keep its base recipe and group separately");
        Check(recipe.ToppingSlot(PoolType.Salmon) == 1 && recipe.ToppingSlot(PoolType.Lemon) == 0 &&
            recipe.ToppingSlot(PoolType.SushiRice) == 0, "Garnish or base took a topping position");
        var customer = new CustomerOrderData { dishes = new List<DishOrderData> { plain, full } };
        List<FoodOrderData> totals = customer.CreateIngredientOrders();
        Check(customer.ValidateOrders() == null && Count(totals, PoolType.SushiRice) == 3 && Count(totals, PoolType.Salmon) == 3 &&
            Count(totals, PoolType.Lemon) == 2 && Count(totals, PoolType.Wasabi) == 2 && Count(totals, PoolType.Perilla) == 2,
            "Garnish quantities were not added to the board requirement");
        var dish = new SushiDishOrder(0, recipe);
        Check(!dish.CanStartIngredient(PoolType.Lemon) && dish.CanStartIngredient(PoolType.SushiRice), "Garnish started before rice");
        dish.FindAvailableIngredient(PoolType.SushiRice).receivedAmount = 1;
        Check(dish.CanStartIngredient(PoolType.Lemon) && dish.CanStartIngredient(PoolType.Salmon), "Rice did not unlock garnish");
        Check(!new DishOrderData { dish = SushiDishId.GreenTea, lemon = true }.TryGetRecipe(out _, out string teaError) &&
            teaError != null, "Tea accepted a garnish");
        var sashimi = new DishOrderData { dish = SushiDishId.SashimiTunaSalmonShrimp, perilla = true }.GetRecipe();
        Check(sashimi.Kind == SushiRecipeKind.ThreeWithoutRice && sashimi.ToppingSlot(PoolType.Shrimp) == 3, "Garnish changed sashimi positions");

        Check(SushiRecipeCatalog.TryCreate(SushiRecipeKind.ThreeWithRice, PoolType.BlackFishRoe, PoolType.Tuna, out var roeTop, out _) &&
            roeTop.Family == SushiDishFamily.Nigiri && roeTop.Ingredients[1] == PoolType.Tuna &&
            roeTop.ToppingSlot(PoolType.Tuna) == 1 && roeTop.ToppingSlot(PoolType.BlackFishRoe) == 2,
            "Rice + fish + roe must be a roe-topped nigiri with the fish first");
        foreach (PoolType type in new[] { PoolType.Lemon, PoolType.Wasabi, PoolType.Perilla })
            Check(SushiRecipeCatalog.IsGarnish(type) && !SushiRecipeCatalog.IsFoodTopping(type) &&
                !SushiRecipeCatalog.TryCreate(SushiRecipeKind.TwoWithRice, type, PoolType.None, out _, out _),
                "A garnish was accepted as a main topping");
    }

    private static void Expect(int id, params PoolType[] expected)
    {
        var actual = SushiRecipeCatalog.Get((SushiDishId)id).Ingredients;
        Check(actual.Count == expected.Length, "Ingredient count mismatch for " + id);
        for (int i = 0; i < expected.Length; i++) Check(actual[i] == expected[i], "Ingredient mismatch for " + id);
    }
    private static int Count(List<FoodOrderData> list, PoolType type) => list.Find(item => item.typeFood == type)?.amount ?? 0;

    private static void UnorderedServing()
    {
        MethodInfo select = typeof(SushiDishOrder).Assembly.GetType("SushiDishSelection")
            .GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic);
        SushiDishOrder Select(IReadOnlyList<SushiDishOrder> options, PoolType type,
            Predicate<SushiDishOrder> canReceive, Func<PoolType, int> available, int start) =>
            (SushiDishOrder)select.Invoke(null, new object[] { options, type, canReceive, available, start, null });
        bool CanTransfer(SushiDishOrder sourceDish, SushiDishOrder targetDish) => (bool)typeof(SushiDishOrder)
            .GetMethod("CanTransferFoundationTo", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(sourceDish, new object[] { targetDish });
        bool Transfer(SushiDishOrder sourceDish, SushiDishOrder targetDish) => (bool)typeof(SushiDishOrder)
            .GetMethod("TryTransferFoundationTo", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(sourceDish, new object[] { targetDish });
        void State(SushiDishOrder dish, string property, bool value) => typeof(SushiDishOrder).GetProperty(property).SetValue(dish, value);
        var dishes = new[] {
            new SushiDishOrder(0, SushiRecipeCatalog.Get(SushiDishId.NigiriTuna)),
            new SushiDishOrder(1, SushiRecipeCatalog.Get(SushiDishId.NigiriSalmon)),
            new SushiDishOrder(2, SushiRecipeCatalog.Get(SushiDishId.NigiriFlounder)),
            new SushiDishOrder(3, SushiRecipeCatalog.Get(SushiDishId.NigiriMackerel)) };
        var belt = new Dictionary<PoolType, int> { [PoolType.SushiRice] = 1, [PoolType.Salmon] = 1 };
        int Available(PoolType type) => belt.TryGetValue(type, out int count) ? count : 0;
        bool CanStart(SushiDishOrder dish) => dish.CanStartIngredient(PoolType.SushiRice) &&
            dish.FindAvailableIngredient(PoolType.SushiRice) != null;
        for (int start = 0; start < dishes.Length; start++)
        {
            Check(Select(dishes, PoolType.SushiRice, CanStart, Available, start) == dishes[1],
                "Rice followed card order instead of the salmon available on the conveyor");
            Check(Select(dishes.Reverse().ToArray(), PoolType.SushiRice, CanStart, Available, start) == dishes[1],
                "Rearranging cards changed ingredient-based recipe selection");
        }
        belt.Remove(PoolType.Salmon);
        belt[PoolType.Mackerel] = 1;
        Check(Select(dishes, PoolType.SushiRice, CanStart, Available, 0) == dishes[3],
            "The fourth card had to wait for the earlier cards");
        Check(Select(dishes, PoolType.SushiRice, _ => false, Available, 0) == null,
            "An unavailable plate was selected");

        var source = dishes[0];
        var target = dishes[1];
        State(source, "IsPrepared", true);
        OrderItem sourceRice = source.Ingredients.First(item => item.typeFood == PoolType.SushiRice);
        sourceRice.incomingAmount = 1;
        Check(!CanTransfer(source, target), "A foundation changed recipe during its flight");
        sourceRice.incomingAmount = 0;
        sourceRice.receivedAmount = 1;
        Check(CanTransfer(source, target), "Landed rice could not serve a later matching recipe");
        source.Ingredients.First(item => item.typeFood == PoolType.Tuna).incomingAmount = 1;
        Check(!CanTransfer(source, target), "A reserved topping lost its recipe");
        source.Ingredients.First(item => item.typeFood == PoolType.Tuna).incomingAmount = 0;
        source.Ingredients.First(item => item.typeFood == PoolType.Tuna).receivedAmount = 1;
        Check(!CanTransfer(source, target), "A plated topping lost its recipe");
        source.Ingredients.First(item => item.typeFood == PoolType.Tuna).receivedAmount = 0;
        State(target, "IsPrepared", true);
        Check(!CanTransfer(source, target), "A reserved recipe was overwritten");
        State(target, "IsPrepared", false);
        State(source, "IsOnTable", true);
        Check(!CanTransfer(source, target), "A finished serving dish changed recipes");
        State(source, "IsOnTable", false);
        Check(!CanTransfer(source, new SushiDishOrder(4, SushiRecipeCatalog.Get(SushiDishId.GreenTea))),
            "Rice was reused as a tea cup");
        Check(Transfer(source, target) && !source.IsPrepared && target.IsPrepared &&
            sourceRice.receivedAmount == 0 && target.CanStartIngredient(PoolType.Salmon) &&
            source.Ingredients.Sum(item => item.receivedAmount) + target.Ingredients.Sum(item => item.receivedAmount) == 1,
            "Moving a rice-only plate duplicated or lost its ingredient progress");
        Check(!Transfer(source, dishes[2]), "The same landed rice was reused twice");

        var portions = new[] {
            new SushiDishOrder(0, SushiRecipeCatalog.Get(SushiDishId.NigiriSalmon)),
            new SushiDishOrder(1, SushiRecipeCatalog.Get(SushiDishId.NigiriSalmon)) };
        Check(Select(portions, PoolType.SushiRice, CanStart, Available, 1) == portions[0],
            "Identical portions were filled inconsistently inside their shared card");
    }

    private static void SharedWrappedBases()
    {
        MethodInfo canTransfer = typeof(SushiDishOrder).GetMethod("CanTransferFoundationTo", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo transfer = typeof(SushiDishOrder).GetMethod("TryTransferFoundationTo", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo select = typeof(SushiDishOrder).Assembly.GetType("SushiDishSelection")
            .GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic);
        var wrapped = SushiRecipeCatalog.All.Where(recipe => recipe.Family == SushiDishFamily.Maki ||
            recipe.Family == SushiDishFamily.Gunkan).ToArray();
        foreach (SushiRecipe recipe in wrapped)
            foreach (SushiRecipe other in wrapped)
                for (int state = 0; state < 4; state++)
                {
                    var source = new SushiDishOrder(0, recipe);
                    var target = new SushiDishOrder(1, other);
                    typeof(SushiDishOrder).GetProperty("IsPrepared").SetValue(source, true);
                    // Rong biển lên đĩa trước; cơm đã đáp hoặc đang bay tới.
                    OrderItem nori = source.Ingredients.First(item => item.typeFood == PoolType.Seaweed);
                    nori.receivedAmount = 1;
                    OrderItem rice = source.Ingredients.First(item => item.typeFood == PoolType.SushiRice);
                    if (state % 2 == 0) rice.receivedAmount = 1;
                    else rice.incomingAmount = 1;
                    if (state >= 2)
                    {
                        typeof(SushiDishOrder).GetProperty("IsPrepared").SetValue(target, true);
                        target.Ingredients.First(item => item.typeFood == PoolType.Seaweed).receivedAmount = 1;
                    }
                    PoolType topping = other.Ingredients.First(type => type != PoolType.SushiRice && type != PoolType.Seaweed);
                    // Cơm đã đáp lên rong biển = đĩa đã thành maki/gunkan, không đổi sang kiểu món khác nữa.
                    if (state % 2 == 0 && recipe.Family != other.Family)
                    {
                        Check(!(bool)canTransfer.Invoke(source, new object[] { target }),
                            "A combined " + recipe.Family + " base turned into " + other.Name);
                        continue;
                    }
                    Check((bool)canTransfer.Invoke(source, new object[] { target }), "Rice and nori could not serve " + other.Name);
                    var noriTotals = source.Ingredients.Concat(target.Ingredients).Where(item => item.typeFood == PoolType.Seaweed)
                        .Sum(item => item.receivedAmount + item.incomingAmount);
                    int riceReceived = rice.receivedAmount, riceIncoming = rice.incomingAmount;
                    Check((bool)transfer.Invoke(source, new object[] { target }) && target.IsPrepared &&
                        target.CanStartIngredient(topping, riceIncoming > 0), "Shared base did not unlock its matching topping");
                    OrderItem targetRice = target.Ingredients.First(item => item.typeFood == PoolType.SushiRice);
                    Check(targetRice.receivedAmount == riceReceived && targetRice.incomingAmount == riceIncoming &&
                        rice.receivedAmount == 0 && rice.incomingAmount == 0 && source.IsPrepared == (state >= 2) &&
                        source.Ingredients.Concat(target.Ingredients).Where(item => item.typeFood == PoolType.Seaweed)
                            .Sum(item => item.receivedAmount + item.incomingAmount) == noriTotals,
                        "Sharing the base lost or duplicated rice/nori or an incoming reservation");
                    target.FindAvailableIngredient(topping).incomingAmount = 1;
                    // Topping đã giữ chỗ chỉ theo đĩa sang món cũng dùng topping đó.
                    if (!recipe.Ingredients.Contains(topping))
                        Check(!(bool)canTransfer.Invoke(target, new object[] { source }), "A reserved topping changed its dish");
                }

        var readyBase = new SushiDishOrder(0, SushiRecipeCatalog.Get(SushiDishId.MakiTuna));
        var matching = new SushiDishOrder(1, SushiRecipeCatalog.Get(SushiDishId.MakiSalmon));
        var partial = new SushiDishOrder(2, SushiRecipeCatalog.Get(SushiDishId.GunkanSalmon));
        readyBase.Ingredients.First(item => item.typeFood == PoolType.SushiRice).receivedAmount = 1;
        readyBase.Ingredients.First(item => item.typeFood == PoolType.Seaweed).receivedAmount = 1;
        partial.Ingredients.First(item => item.typeFood == PoolType.Seaweed).receivedAmount = 1;
        var choices = new[] { partial, matching };
        Func<SushiDishOrder, SushiDishOrder> project = dish => dish == matching ? readyBase : null;
        Predicate<SushiDishOrder> eligible = _ => true;
        Func<PoolType, int> belt = type => type == PoolType.Salmon ? 1 : 0;
        for (int start = 0; start < choices.Length; start++)
            Check((SushiDishOrder)select.Invoke(null, new object[] { choices, PoolType.Salmon, eligible, belt, start, project }) == matching,
                "A nori-only dish took the topping while another plate had a complete shared base");
    }
}
#endif
