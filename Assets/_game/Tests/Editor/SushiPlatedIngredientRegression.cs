#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Run in the isolated test project with -executeMethod SushiPlatedIngredientRegression.RunBatch.
[InitializeOnLoad]
public static class SushiPlatedIngredientRegression
{
    private const string Pending = "SushiPlatedIngredientRegression.Pending";
    private static BoardShishiManager board;
    private static ConveyorManager belt;
    private static CustomerManager customers;
    private static LevelManager level;
    private static int passed;
    private static Action pendingPreview;
    private static int previewFrames;
    private static int startupFrames = 3;
    private static readonly List<string> errors = new List<string>();
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    static SushiPlatedIngredientRegression() { EditorApplication.update += Update; }
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch-mode test project.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void Update()
    {
        if (pendingPreview != null)
        {
            if (--previewFrames > 0) return;
            Action capture = pendingPreview;
            pendingPreview = null;
            try { capture(); CompleteChecks(); }
            catch (Exception error) { FailChecks(error); }
            finally { Application.logMessageReceived -= CaptureError; }
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !SessionState.GetBool(Pending, false)) return;
        // Finish editor startup callbacks before recording errors from the gameplay fixture.
        if (--startupFrames > 0) return;
        SessionState.SetBool(Pending, false);
        Application.logMessageReceived += CaptureError;
        try
        {
            SushiRecipeChecks.Run();
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.defaultUpdateType = UpdateType.Manual;
            Fixture("CreateFixture");
            Fixture("CreateLevel");
            board = Field<BoardShishiManager>("board");
            belt = Field<ConveyorManager>("conveyor");
            customers = Field<CustomerManager>("customers");
            level = Field<LevelManager>("level");
            Run("serialized plated art resolves every ingredient and the new nigiri sprites", ArtBindings);
            Run("all 48 recipes require landed rice or cup before their toppings", FoundationRules);
            Run("normal delivery changes sprites and retains layers under the dish parent", NormalAssembly);
            Run("landed rice serves a later matching order without waiting for earlier cards", UnorderedAssembly);
            Run("maki rice and nori serve the topping that arrives", () => SharedWrappedAssembly(SushiDishId.MakiTuna, SushiDishId.MakiSalmon, false));
            Run("gunkan rice and nori serve the topping that arrives", () => SharedWrappedAssembly(SushiDishId.GunkanTuna, SushiDishId.GunkanSalmon, false));
            Run("a ready maki base replaces a rice-only gunkan plate", () => SharedWrappedAssembly(SushiDishId.MakiTuna, SushiDishId.GunkanSalmon, true));
            Run("a ready gunkan base replaces a rice-only maki plate", () => SharedWrappedAssembly(SushiDishId.GunkanTuna, SushiDishId.MakiSalmon, true));
            Run("custom recipe cards group by ingredients and use distinct plated art", CustomRecipeCards);
            Run("custom rice triples assemble both toppings after rice", CustomTripleAssembly);
            Run("rice-free custom pairs accept either ingredient first", CustomRiceFreeAssembly);
            Run("magnet intercepts rice in flight and waits before launching toppings", () => MagnetAssembly(SushiDishId.NigiriOctopus));
            Run("iced tea magnet puts the cup down before tea and ice", () => MagnetAssembly(SushiDishId.IcedGreenTea));
            Run("missing foundation rejects magnet without consuming any ingredient", MissingFoundation);
            Run("reload cancels queued layers and pooled ingredients recover their raw art", CancellationAndPool);
            Run("loss check distinguishes blocked toppings from an incoming foundation", LossWithFoundation);
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) { CapturePreview(); return; }
            CompleteChecks();
        }
        catch (Exception error)
        {
            FailChecks(error);
        }
        finally { if (pendingPreview == null) Application.logMessageReceived -= CaptureError; }
    }

    private static void CompleteChecks()
    {
        Check(errors.Count == 0, string.Join("\n", errors));
        File.WriteAllText("SushiPlatedIngredientsResults.txt", "PASS: " + passed + " scenarios\n");
        Debug.Log("SUSHI_PLATED_INGREDIENTS_OK: " + passed + " scenarios");
        EditorApplication.Exit(0);
    }

    private static void FailChecks(Exception error)
    {
        File.WriteAllText("SushiPlatedIngredientsResults.txt", "FAIL after " + passed + " scenarios\n" + error);
        Debug.LogException(error);
        EditorApplication.Exit(1);
    }

    private static void CaptureError(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Run(string name, Action test) { test(); passed++; Debug.Log("PASS: " + name); }
    private static object Fixture(string name, params object[] args) =>
        typeof(SushiGameplayRegression).GetMethod(name, PrivateStatic).Invoke(null, args);
    private static T Field<T>(string name) => (T)typeof(SushiGameplayRegression).GetField(name, PrivateStatic).GetValue(null);
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, PrivateInstance).Invoke(target, args);
    private static T Prefab<T>(string name) where T : Component =>
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Prefabs/" + name + ".prefab").GetComponent<T>();
    private static FoodData Food(PoolType type) => new FoodData { typeFood = type };
    private static void Advance(float seconds = 1)
    {
        // DOVirtual.DelayedCall uses a normal-update sequence even in a manual fixture.
        // Move those serving timers onto the same clock as ingredient/dish flights.
        List<Tween> playing = DOTween.PlayingTweens();
        if (playing != null) foreach (Tween tween in playing) tween.SetUpdate(UpdateType.Manual);
        DOTween.ManualUpdate(seconds, seconds);
    }
    private static Sprite Artwork(FoodShushi food) => Get<SpriteRenderer>(food, "foodRenderer").sprite;
    private static bool Tap(int index) => belt.TryTakeFromQueue(board.FoodQueues[index]);
    private static SushiPreparedDish Dish => belt.PlatedDishes.Values.Single();

    private static Customer Prepare(SushiDishId id, int capacity, params FoodData[][] columns)
        => Prepare(new DishOrderData { dish = id }, capacity, columns);

    private static Customer Prepare(DishOrderData order, int capacity, params FoodData[][] columns)
        => Prepare(new[] { order }, capacity, columns);

    private static Customer Prepare(DishOrderData[] orders, int capacity, params FoodData[][] columns)
    {
        level.LoadLevel(0);
        Set(belt, "preparedDishPrefab", Prefab<SushiPreparedDish>("PreparedDish"));
        Set(belt, "tableHoldDuration", 10f);
        Set(belt, "deliveryRange", 100f);
        customers.Init(new List<CustomerOrderData> {
            new CustomerOrderData { dishes = new List<DishOrderData>(orders) }
        });
        Fixture("Load", capacity, columns);
        return customers.ActiveCustomers[0];
    }

    private static void UnorderedAssembly()
    {
        Customer customer = Prepare(new[] {
            new DishOrderData { dish = SushiDishId.NigiriTuna },
            new DishOrderData { dish = SushiDishId.NigiriSalmon },
            new DishOrderData { dish = SushiDishId.NigiriFlounder },
            new DishOrderData { dish = SushiDishId.NigiriMackerel } }, 4,
            new[] { Food(PoolType.SushiRice) }, new[] { Food(PoolType.Salmon) });
        Check(Tap(0), "rice pickup failed");
        Advance();
        var foods = Get<Dictionary<Transform, FoodShushi>>(belt, "slotFoods");
        var pickup = foods.Single();
        SushiDishOrder original = customer.Dishes[0], matching = customer.Dishes[1];
        Check(customer.TryReservePreparedIngredient(original, PoolType.SushiRice, out Transform target), "initial rice reservation failed");
        var plated = (SushiPreparedDish)typeof(ConveyorManager).GetMethod("GetOrCreatePreparedDish", PrivateInstance)
            .Invoke(belt, new object[] { customer, original, target });
        foods.Remove(pickup.Key);
        Invoke(belt, "StartPreparedIngredientFlight", plated, pickup.Value, target);
        Advance();
        Check(plated.AssemblyIngredients.Single() == pickup.Value && !original.IsComplete, "rice did not remain on its serving plate");

        Check(Tap(1), "later order's salmon pickup failed");
        Advance();
        Check(belt.CanMakeProgress, "reusable plated rice was treated as a deadlock");
        Invoke(belt, "Update");
        Advance();
        Check(Dish == plated && plated.Order == matching && plated.IsReady && plated.AssemblyIngredients.Count == 2,
            "salmon waited for the tuna card instead of using the existing rice plate");
        Check(!original.IsPrepared && original.Ingredients.All(item => item.receivedAmount == 0 && item.incomingAmount == 0),
            "the first card retained the transferred rice");
        Check(plated.AssemblyIngredients.Single(food => food.Type == PoolType.SushiRice) == pickup.Value &&
            plated.AssemblyIngredients.All(food => food.transform.IsChildOf(plated.IngredientRoot)),
            "reassigning the recipe lost or duplicated its ingredient objects");
        Check(customer.Orders.Single(item => item.typeFood == PoolType.SushiRice).receivedAmount == 1,
            "aggregate rice progress was counted twice");
        for (int i = 0; i < 130; i++) Advance(0.1f);
        Check(matching.IsComplete && !original.IsComplete && belt.PreparedDishCount == 0 && belt.DeliveringCount == 0,
            "the completed dish flew to an earlier card or leaked its serving plate");
    }

    private static DishOrderData Custom(SushiRecipeKind kind, PoolType first, PoolType second) =>
        new DishOrderData { recipeKind = kind, firstIngredient = first, secondIngredient = second };

    private static void SharedWrappedAssembly(SushiDishId sourceId, SushiDishId targetId, bool hasTargetPlate)
    {
        Customer customer = Prepare(new[] {
            new DishOrderData { dish = sourceId }, new DishOrderData { dish = targetId } }, 5,
            new[] { Food(PoolType.SushiRice) }, new[] { Food(PoolType.Seaweed) },
            new[] { Food(PoolType.Salmon) }, new[] { Food(PoolType.SushiRice) });
        SushiDishOrder original = customer.Dishes[0], matching = customer.Dishes[1];
        SushiPreparedDish Add(SushiDishOrder order, int column)
        {
            Check(Tap(column), "shared-base pickup failed");
            Advance();
            var foods = Get<Dictionary<Transform, FoodShushi>>(belt, "slotFoods");
            var pickup = foods.Single();
            Check(customer.TryReservePreparedIngredient(order, pickup.Value.Type, out Transform target), "shared layer reservation failed");
            var plate = (SushiPreparedDish)typeof(ConveyorManager).GetMethod("GetOrCreatePreparedDish", PrivateInstance)
                .Invoke(belt, new object[] { customer, order, target });
            foods.Remove(pickup.Key);
            Invoke(belt, "StartPreparedIngredientFlight", plate, pickup.Value, target);
            Advance();
            return plate;
        }
        SushiPreparedDish completeBase = Add(original, 0);
        Add(original, 1);
        var sharedFoods = completeBase.AssemblyIngredients.ToArray();
        SushiPreparedDish partialBase = hasTargetPlate ? Add(matching, 3) : null;
        Check(Tap(2), "matching wrapped topping pickup failed");
        Advance();
        Check(belt.CanMakeProgress, "shared rice/nori base was treated as blocked");
        Invoke(belt, "Update");
        Advance();
        Check(completeBase.Order == matching && completeBase.IsReady && completeBase.AssemblyIngredients.Count == 3,
            "the matching topping waited despite a complete rice/nori base");
        Check(sharedFoods.All(food => completeBase.AssemblyIngredients.Contains(food)), "shared rice/nori objects were duplicated or lost");
        Check(completeBase.AssemblyIngredients.All(food => Artwork(food) ==
            SushiArtCatalog.Default.platedIngredientImages.Find(food.Type, matching.Recipe).sprite),
            "reassigning between maki/gunkan kept the wrong layered artwork");
        Check(original.IsPrepared == hasTargetPlate && original.Ingredients.Single(item => item.typeFood == PoolType.Seaweed).receivedAmount == 0,
            "the earlier order retained the shared nori");
        if (partialBase != null)
            Check(partialBase.Order == original && partialBase.AssemblyIngredients.Count == 1,
                "the existing rice-only plate lost its replacement order");
        Check(customer.Orders.Single(item => item.typeFood == PoolType.Seaweed).receivedAmount == 1 &&
            customer.Orders.Single(item => item.typeFood == PoolType.SushiRice).receivedAmount == (hasTargetPlate ? 2 : 1),
            "shared-base reassignment changed aggregate ingredient totals");
        for (int i = 0; i < 130; i++) Advance(0.1f);
        Check(matching.IsComplete && !original.IsComplete && belt.PreparedDishCount == (hasTargetPlate ? 1 : 0),
            "the wrapped dish completed the wrong order or retained its serving slot");
    }

    private static void CustomRecipeCards()
    {
        level.LoadLevel(0);
        var triple = Custom(SushiRecipeKind.ThreeWithRice, PoolType.Shrimp, PoolType.Avocado);
        var reverse = Custom(SushiRecipeKind.ThreeWithRice, PoolType.Avocado, PoolType.Shrimp);
        var other = Custom(SushiRecipeKind.ThreeWithRice, PoolType.Egg, PoolType.Avocado);
        var pair = Custom(SushiRecipeKind.TwoWithRice, PoolType.Shrimp, PoolType.None);
        var noRice = Custom(SushiRecipeKind.TwoWithoutRice, PoolType.Shrimp, PoolType.Avocado);
        customers.Init(new List<CustomerOrderData> {
            new CustomerOrderData { dishes = new List<DishOrderData> { triple, reverse, other, pair, noRice } }
        });
        Customer customer = customers.ActiveCustomers[0];
        customer.SitDown();
        var view = customer.GetComponent<CustomerRecipeView>();
        view.SetSeated(true);
        Check(view.CardAt(0) == view.CardAt(1) && view.CardAt(0).PortionProgress == "0/2", "swapped toppings were not grouped");
        Check(view.CardAt(0) != view.CardAt(2) && view.CardAt(0) != view.CardAt(3) && view.CardAt(3) != view.CardAt(4),
            "different custom recipes shared an order card");
        var plated = SushiArtCatalog.Default.platedIngredientImages;
        Check(plated.Find(PoolType.SushiRice, pair.GetRecipe()).sprite != plated.Find(PoolType.SushiRice, triple.GetRecipe()).sprite,
            "rice pairs and triples reused the same plated rice sprite");
        Check(plated.Find(PoolType.Shrimp, pair.GetRecipe()).sprite != plated.Find(PoolType.Shrimp, triple.GetRecipe()).sprite,
            "shrimp pairs and triples reused the same plated topping sprite");
        var layers = Get<List<UnityEngine.UI.Image>>(view.CardAt(0), "recipeLayers");
        Check(layers.Count == 3 && layers.All(layer => layer.sprite != null && layer.gameObject.activeInHierarchy), "custom order preview is blank");
        view.CardAt(0).SetRecipeExpanded(true);
        Check(layers.All(layer => !layer.gameObject.activeInHierarchy), "composite remains visible over recipe details");
        view.CardAt(0).SetRecipeExpanded(false);
        Check(layers.All(layer => layer.gameObject.activeInHierarchy), "composite did not return after closing details");
    }

    private static void CustomTripleAssembly()
    {
        Customer customer = Prepare(Custom(SushiRecipeKind.ThreeWithRice, PoolType.Shrimp, PoolType.Avocado), 3,
            new[] { Food(PoolType.Shrimp) }, new[] { Food(PoolType.Avocado) }, new[] { Food(PoolType.SushiRice) });
        Check(Tap(0) && Tap(1), "custom toppings pickup failed"); Advance(); Invoke(belt, "Update");
        Check(belt.PreparedDishCount == 0, "custom toppings started without rice");
        Check(Tap(2), "custom rice pickup failed"); Advance(); Invoke(belt, "Update");
        SushiPreparedDish dish = Dish;
        Advance(); Invoke(belt, "Update"); Advance(); Invoke(belt, "Update"); Advance();
        Check(dish.IsReady && dish.AssemblyIngredients.Count == 3, "custom triple did not finish assembly");
        foreach (FoodShushi food in dish.AssemblyIngredients)
        {
            var entry = SushiArtCatalog.Default.platedIngredientImages.Find(food.Type, dish.Order.Recipe);
            Check(Artwork(food) == entry.sprite,
                "custom plate selected the wrong recipe artwork");
            Check((Vector2)dish.IngredientTarget(food.Type).localPosition == entry.position,
                "custom plate ignored the authored ingredient position");
        }
        for (int i = 0; i < 130; i++) Advance(0.1f);
        Check(customer.Dishes[0].IsComplete && belt.PreparedDishCount == 0, "custom triple failed order delivery");
    }

    private static void CustomRiceFreeAssembly()
    {
        Customer customer = Prepare(Custom(SushiRecipeKind.TwoWithoutRice, PoolType.Shrimp, PoolType.Avocado), 2,
            new[] { Food(PoolType.Shrimp) }, new[] { Food(PoolType.Avocado) });
        Check(Tap(1), "rice-free second ingredient pickup failed"); Advance(); Invoke(belt, "Update"); Advance();
        Check(Dish.AssemblyIngredients.Count == 1 && !Dish.IsReady, "rice-free pair did not accept its second ingredient first");
        Check(Tap(0), "rice-free first ingredient pickup failed"); Advance(); Invoke(belt, "Update"); Advance();
        Check(Dish.IsReady && Dish.AssemblyIngredients.Count == 2, "rice-free custom pair did not assemble");
        for (int i = 0; i < 130; i++) Advance(0.1f);
        Check(customer.Dishes[0].IsComplete && belt.PreparedDishCount == 0, "rice-free pair failed order delivery");
    }

    private static void ArtBindings()
    {
        var demo = AssetDatabase.LoadAssetAtPath<SushiLevelData>("Assets/_game/LevelData/SushiCustomRecipesDemo.asset");
        Check(demo != null && demo.ValidateData() == null && demo.GetTotalFoodCount() == 7, "custom recipe demo is invalid");
        var art = SushiArtCatalog.Default.platedIngredientImages;
        Check(art != null && art.ingredients.Count == 21, "plated art list is not assigned or incomplete");
        foreach (SushiRecipe recipe in SushiRecipeCatalog.All)
            foreach (PoolType type in recipe.Ingredients)
            {
                Check(art.Find(type, recipe.Family)?.sprite != null, "missing plated art for " + type);
                var entry = art.Find(type, recipe);
                Check(entry != null && art.GetSortingOrder(type, recipe) == entry.sortingOrder,
                    "authored plated sorting order was ignored for " + recipe.Id + "/" + type);
            }
        Check(art.Find(PoolType.SushiRice, SushiDishFamily.Nigiri).sprite.name == "nigiri_base_0", "wrong rice art");
        Check(art.Find(PoolType.Octopus, SushiDishFamily.Nigiri).sprite.name == "nigiri_top_octopus_0", "wrong octopus art");
        Check(art.Find(PoolType.Octopus, SushiDishFamily.Sashimi).sprite.name == "octopus_0", "sashimi should use its own slice");
        var prefab = Prefab<SushiPreparedDish>("PreparedDish");
        Check(Get<Transform>(prefab, "ingredientRoot") != null &&
            Get<SushiPlatedIngredientArtLibrary>(prefab, "ingredientArt") == art, "prefab has no authored ingredients parent/art");
    }

    private static void FoundationRules()
    {
        foreach (SushiRecipe recipe in SushiRecipeCatalog.All)
        {
            var order = new SushiDishOrder(0, recipe);
            PoolType foundation = recipe.Ingredients.Contains(PoolType.SushiRice) ? PoolType.SushiRice :
                recipe.Ingredients.Contains(PoolType.EmptyCup) ? PoolType.EmptyCup : PoolType.None;
            Check(order.FoundationType == foundation, "wrong foundation for " + recipe.Id);
            foreach (var item in order.Ingredients)
                Check(order.CanStartIngredient(item.typeFood) == (foundation == PoolType.None || item.typeFood == foundation),
                    "topping started without a foundation");
            if (foundation == PoolType.None) continue;
            var baseItem = order.Ingredients.Single(item => item.typeFood == foundation);
            baseItem.incomingAmount = baseItem.requiredAmount;
            foreach (var item in order.Ingredients.Where(item => item.typeFood != foundation))
                Check(!order.CanStartIngredient(item.typeFood) && order.CanStartIngredient(item.typeFood, true),
                    "in-flight foundation was confused with a landed foundation");
            baseItem.receivedAmount = baseItem.requiredAmount; baseItem.incomingAmount = 0;
            Check(order.Ingredients.All(item => order.CanStartIngredient(item.typeFood)), "landed base did not unlock toppings");
        }
    }

    private static void NormalAssembly()
    {
        Customer customer = Prepare(SushiDishId.NigiriOctopus, 3,
            new[] { Food(PoolType.Octopus) }, new[] { Food(PoolType.SushiRice) });
        Check(Tap(0), "octopus pickup failed"); Advance(); Invoke(belt, "Update");
        Check(belt.OccupiedSlotCount == 1 && belt.PreparedDishCount == 0, "octopus left the belt before rice");
        Check(Tap(1), "rice pickup failed"); Advance(); Invoke(belt, "Update");
        SushiPreparedDish dish = Dish;
        Check(dish.IngredientTarget(PoolType.SushiRice).localPosition.x == 0f &&
            dish.IngredientTarget(PoolType.SushiRice).localPosition.y == 0f &&
            dish.IngredientTarget(PoolType.Octopus).localPosition.x == 0f &&
            dish.IngredientTarget(PoolType.Octopus).localPosition.y == 0f,
            "ingredient landing targets are not centered on the plate");
        Check(belt.OccupiedSlotCount == 1 && !customer.CanReceivePreparedIngredient(customer.Dishes[0], PoolType.Octopus),
            "topping reserved before rice landed");
        var rice = Get<Dictionary<SushiPreparedDish, List<FoodShushi>>>(belt, "assemblyFoods")[dish].Single();
        Check(Artwork(rice).name == "nigiri_base_0" && rice.Location == FoodLocation.MovingToCustomerPlate,
            "rice did not change art at takeoff");
        Advance(0.1f); Invoke(belt, "Update");
        Check(belt.OccupiedSlotCount == 1, "topping launched during the base flight");
        Advance();
        Check(dish.AssemblyIngredients.Count == 1 && rice.transform.IsChildOf(dish.IngredientRoot), "base did not join its parent");
        Check(rice.transform.localPosition == Vector3.zero, "landed base is not centered on the plate");
        Invoke(belt, "Update"); Advance();
        Check(dish.IsReady && dish.AssemblyIngredients.Count == 2 && !Get<SpriteRenderer>(dish, "dishImage").enabled,
            "completed composite was replaced by one finished sprite");
        FoodShushi octopus = dish.AssemblyIngredients.Single(food => food.Type == PoolType.Octopus);
        Check(Artwork(octopus).name == "nigiri_top_octopus_0" && octopus.transform.IsChildOf(dish.IngredientRoot),
            "octopus did not retain its plated art/parent");
        Check(octopus.transform.localPosition == Vector3.zero, "landed topping is not centered on the plate");
        Check(Get<SpriteRenderer>(rice, "foodRenderer").sortingOrder < Get<SpriteRenderer>(octopus, "foodRenderer").sortingOrder,
            "rice rendered above its topping");
        Check(rice.transform.localScale == Vector3.one && octopus.transform.localScale == Vector3.one,
            "landing changed the authored composition scale");
        CheckPlatedSizeChanges(dish, rice);
    }

    private static void CheckPlatedSizeChanges(SushiPreparedDish dish, FoodShushi food)
    {
        var originalArt = Get<SushiPlatedIngredientArtLibrary>(dish, "ingredientArt");
        var editedArt = Object.Instantiate(originalArt);
        var renderer = Get<SpriteRenderer>(food, "foodRenderer");
        try
        {
            Set(dish, "ingredientArt", editedArt);
            var entry = editedArt.Find(food.Type, dish.Order.Recipe);
            entry.size = new Vector2(0.7f, 0.3f);
            entry.position = new Vector2(0.2f, -0.1f);
            entry.rotation = 17;
            entry.sortingOrder = 19;
            dish.RefreshArtwork(editedArt);
            Vector3 size = Vector3.Scale(renderer.sprite.bounds.size, renderer.transform.localScale);
            Check(Mathf.Abs(size.x - 0.7f) < 0.001f && Mathf.Abs(size.y - 0.3f) < 0.001f,
                "landed ingredient did not use the edited width and height");
            entry.size.x = 1.4f;
            dish.RefreshArtwork(editedArt);
            size = Vector3.Scale(renderer.sprite.bounds.size, renderer.transform.localScale);
            Check(Mathf.Abs(size.x - 1.4f) < 0.001f && Mathf.Abs(size.y - 0.3f) < 0.001f,
                "editing width changed height or was ignored");
            entry.size.y = 0.6f;
            dish.RefreshArtwork(editedArt);
            size = Vector3.Scale(renderer.sprite.bounds.size, renderer.transform.localScale);
            Check(Mathf.Abs(size.x - 1.4f) < 0.001f && Mathf.Abs(size.y - 0.6f) < 0.001f,
                "editing height changed width or was ignored");
            Transform target = dish.IngredientTarget(food.Type);
            Check((Vector2)target.localPosition == entry.position &&
                Mathf.Abs(Mathf.DeltaAngle(target.localEulerAngles.z, entry.rotation)) < 0.001f,
                "landed ingredient ignored the authored position or rotation");
            Check(food.transform.localScale == Vector3.one && renderer.sortingOrder == 18 + entry.sortingOrder,
                "editing size changed the dish scale or sorting order");
        }
        finally
        {
            Set(dish, "ingredientArt", originalArt);
            dish.RefreshArtwork(originalArt);
            Object.DestroyImmediate(editedArt);
        }
    }

    private static void MagnetAssembly(SushiDishId id)
    {
        SushiRecipe recipe = SushiRecipeCatalog.Get(id);
        var columns = recipe.Ingredients.Select(type => new[] { Food(type) }).ToArray();
        Customer customer = Prepare(id, 4, columns);
        SushiDishOrder order = customer.Dishes[0];
        int foundationColumn = recipe.Ingredients.ToList().IndexOf(order.FoundationType);
        Check(Tap(foundationColumn), "foundation pickup failed");
        Check(belt.CanTargetMagnet(board, customer, order), "Magnet preview missed the available foundation in flight");
        Check(belt.TryTakeMagnet(board, customer, order), "magnet could not intercept the foundation");
        SushiPreparedDish dish = Dish;
        var pending = Get<Dictionary<SushiPreparedDish, List<FoodShushi>>>(belt, "assemblyFoods")[dish];
        var all = pending.ToArray();
        Check(all.Single(food => food.Type == order.FoundationType).Location == FoodLocation.MovingToCustomerPlate,
            "foundation was not the first flight");
        Check(all.Where(food => food.Type != order.FoundationType).All(food => food.Location == FoodLocation.Conveyor),
            "magnet launched toppings alongside the base");
        Check(!belt.TryTakeMagnet(board, customer, order), "duplicate magnet reservation accepted");
        for (int i = 0; i < 30; i++)
        {
            Advance(0.05f);
            var baseItem = order.Ingredients.Single(item => item.typeFood == order.FoundationType);
            foreach (FoodShushi food in all.Where(food => food.Type != order.FoundationType))
                if (food.Location == FoodLocation.MovingToCustomerPlate || food.Location == FoodLocation.CustomerPlate)
                    Check(baseItem.IsComplete, "topping moved before foundation arrived");
        }
        Check(dish.IsReady && dish.AssemblyIngredients.Count == recipe.Ingredients.Count, "magnet composition is incomplete");
        for (int i = 0; i < 130; i++) Advance(0.1f);
        Check(order.IsComplete && belt.PreparedDishCount == 0 && belt.DeliveringCount == 0,
            "composite did not finish delivery");
        Check(all.All(food => !food.gameObject.activeSelf && !food.transform.IsChildOf(dish.transform)),
            "delivered parent retained pooled ingredients");
    }

    private static void MissingFoundation()
    {
        Customer customer = Prepare(SushiDishId.IcedGreenTea, 3,
            new[] { Food(PoolType.TeaPot) }, new[] { Food(PoolType.IceCube) });
        Check(!belt.CanTargetMagnet(board, customer, customer.Dishes[0]), "Magnet preview accepted a recipe without its cup");
        Check(!belt.TryTakeMagnet(board, customer, customer.Dishes[0]), "magnet accepted a recipe without its cup");
        Check(board.FoodQueues.All(column => column.Count == 1) && belt.OccupiedSlotCount == 0 &&
            customer.Dishes[0].Ingredients.All(item => item.incomingAmount == 0), "rejected magnet changed board or reservations");
    }

    private static void CancellationAndPool()
    {
        Customer customer = Prepare(SushiDishId.NigiriOctopus, 2,
            new[] { Food(PoolType.SushiRice) }, new[] { Food(PoolType.Octopus) });
        SushiDishOrder order = customer.Dishes[0];
        Check(belt.TryTakeMagnet(board, customer, order), "cancel fixture failed");
        var all = Get<Dictionary<SushiPreparedDish, List<FoodShushi>>>(belt, "assemblyFoods")[Dish].ToArray();
        belt.Clear();
        Check(all.All(food => !food.gameObject.activeSelf), "queued ingredient was not returned to its pool");
        level.LoadLevel(0);
        Advance(2);
        Check(!order.IsPrepared && order.Ingredients.All(item => item.incomingAmount == 0) &&
            belt.DeliveringCount == 0 && belt.PreparingDishCount == 0 && belt.OccupiedSlotCount == 0,
            "reload retained a queued flight/reservation");
        foreach (PoolType type in new[] { PoolType.SushiRice, PoolType.Octopus })
        {
            var food = SimplePool.Spawn<FoodShushi>(type);
            food.SetupFromData(Food(type));
            Check(Artwork(food) == SushiArtCatalog.Default.GetIngredient(type), "pooled food retained plated artwork");
            SimplePool.Despawn(food);
        }
    }

    private static void LossWithFoundation()
    {
        Prepare(SushiDishId.NigiriOctopus, 1,
            new[] { Food(PoolType.Octopus) }, new[] { Food(PoolType.SushiRice) });
        Check(Tap(0), "blocked topping pickup failed"); Advance();
        Check(!belt.CanMakeProgress, "topping without rice was considered progress");
        Invoke(level, "CheckLevelState", 4f); Check(level.IsPlaying, "loss skipped grace period");
        Invoke(level, "CheckLevelState", 1f); Check(!level.IsPlaying, "permanent foundation deadlock was not detected");
        Prepare(SushiDishId.NigiriOctopus, 2,
            new[] { Food(PoolType.Octopus) }, new[] { Food(PoolType.SushiRice) });
        Check(Tap(0) && Tap(1), "full recipe setup failed"); Advance();
        Check(belt.CanMakeProgress, "available foundation was ignored");
        Invoke(level, "CheckLevelState", 20f); Check(level.IsPlaying, "full recipe caused premature loss");
        Invoke(belt, "Update");
        Invoke(level, "CheckLevelState", 20f); Check(level.IsPlaying, "foundation flight caused premature loss");
    }

    private static void CapturePreview()
    {
        var preview = new GameObject("Plated ingredients preview").transform;
        preview.position = new Vector3(100, 100, 0);
        SushiRecipe[] previewRecipes = {
            Custom(SushiRecipeKind.TwoWithRice, PoolType.Shrimp, PoolType.None).GetRecipe(),
            Custom(SushiRecipeKind.TwoWithoutRice, PoolType.Shrimp, PoolType.Avocado).GetRecipe(),
            Custom(SushiRecipeKind.ThreeWithRice, PoolType.Shrimp, PoolType.Avocado).GetRecipe(),
            Custom(SushiRecipeKind.ThreeWithRice, PoolType.Egg, PoolType.Salmon).GetRecipe(),
            SushiRecipeCatalog.Get(SushiDishId.NigiriOctopus),
            Custom(SushiRecipeKind.TwoWithoutRice, PoolType.Octopus, PoolType.Tuna).GetRecipe(),
            Custom(SushiRecipeKind.ThreeWithRice, PoolType.Octopus, PoolType.Tuna).GetRecipe(),
            Custom(SushiRecipeKind.ThreeWithRice, PoolType.Cucumber, PoolType.CrabStick).GetRecipe()
        };
        for (int i = 0; i < previewRecipes.Length; i++)
        {
            var order = new SushiDishOrder(0, previewRecipes[i]);
            var dish = Object.Instantiate(Prefab<SushiPreparedDish>("PreparedDish"), preview);
            dish.transform.localPosition = new Vector3((i % 4 - 1.5f) * 1.3f, i < 4 ? 0.65f : -0.65f, 0);
            dish.Bind(null, order); dish.BeginAssembly();
            foreach (PoolType type in order.Recipe.Ingredients)
            {
                var food = SimplePool.Spawn<FoodShushi>(type); food.SetupFromData(Food(type));
                food.TryMoveToConveyor(); food.ArriveAtConveyor();
                food.TryMoveToCustomerPlate(); food.ArriveAtCustomerPlate(); dish.AddAssemblyIngredient(food);
            }
            dish.FinishAssembly();
        }
        var cameraObject = new GameObject("Preview camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(100, 100, -10);
        camera.orthographic = true; camera.orthographicSize = 1.55f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.16f, 0.19f, 0.22f);
        camera.allowHDR = false; camera.allowMSAA = false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        // Let Unity initialize the render pipeline and native sprite data before the capture.
        previewFrames = 3;
        pendingPreview = () => CapturePreviewImage(camera, cameraObject, preview);
    }

    private static void CapturePreviewImage(Camera camera, GameObject cameraObject, Transform preview)
    {
        var target = new RenderTexture(1200, 650, 24, RenderTextureFormat.ARGB32);
        target.Create(); camera.targetTexture = target;
        RenderTexture previous = RenderTexture.active;
        var pixels = new Texture2D(1200, 650, TextureFormat.RGB24, false);
        try
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            else camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1200, 650), 0, 0); pixels.Apply();
            Directory.CreateDirectory("Documentation");
            File.WriteAllBytes("Documentation/PlatedIngredientsPreview.png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(preview.gameObject);
        }
    }
}
#endif
