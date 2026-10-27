#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Chạy trong project test riêng: Unity -batchmode -nographics -projectPath ...
// -executeMethod SushiGameplayRegression.RunBatch -logFile ... (không thêm -quit).
[InitializeOnLoad]
public static class SushiGameplayRegression
{
    private const string PendingKey = "SushiGameplayRegression.Pending";
    private static int passed;
    private static BoardShishiManager board;
    private static ConveyorManager conveyor;
    private static CustomerManager customers;
    private static Transform fixture;
    private static LevelManager level;

    static SushiGameplayRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += RunWhenPlaying;
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run gameplay checks in a separate batch-mode test project.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    public static void RunNoMovesBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run gameplay checks in a separate batch-mode test project.");
        SessionState.SetBool(PendingKey + ".NoMoves", true);
        RunBatch();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        // Editor delayCall có thể bị callback package khác ngắt. Chạy bằng update.
    }

    private static void RunWhenPlaying()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        if (SessionState.GetBool(PendingKey + ".NoMoves", false))
        {
            SessionState.SetBool(PendingKey + ".NoMoves", false);
            RunNoMovesChecks();
        }
        else RunAll();
    }

    private static void RunNoMovesChecks()
    {
        try
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.defaultUpdateType = UpdateType.Manual;
            CreateFixture();
            Run("quantity reserves separate slots and rejects overflow atomically", Quantity);
            Run("rapid picks include in-flight reservations", RapidPicks);
            Run("linked pair waits for both heads and takes total quantity", Linked);
            Run("linked pair rejects insufficient capacity", LinkedOverflow);
            Run("available taps count quantities, trays and ready links without feedback", AvailableTapRules);
            Run("frozen heads require adjacent column taps", Frozen);
            Run("rigid tray takes mixed quantities as one action", RigidTray);
            Run("rush and popup pause gameplay", RushAndPopup);
            Run("frozen revive waits for the deadlock grace period", Revive);
            Run("deadlock delay resets for pause, valid moves and reload", NoMovesDelay);
            Run("deliveries and customer transitions prevent premature failure", NoMovesProgress);
            Run("partial plates cannot hide a permanent deadlock", PartialDishDeadlock);
            Run("reload clears in-flight food and reservations", Reload);
            Debug.Log("SUSHI_NO_MOVES_REGRESSION_OK: " + passed + " scenarios passed");
            System.IO.File.WriteAllText("SushiNoMovesRegressionResults.txt", "PASS: " + passed + " scenarios\n");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            System.IO.File.WriteAllText("SushiNoMovesRegressionResults.txt", "FAIL after " + passed + " scenarios\n" + exception);
            EditorApplication.Exit(1);
        }
    }

    private static void RunAll()
    {
        try
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.defaultUpdateType = UpdateType.Manual;
            CreateFixture();
            SushiRecipeChecks.Run();
            Run("prefab visuals are serialized, reusable and preserve authored text layout", PrefabVisuals);
            Run("board trays are tight and only the outside rails remain visible", BoardOuterRails);
            Run("customer UI binds data without rewriting prefab layout and fits large orders", CustomerPrefabBinding);
            Run("all 21 ingredient types share one prefab and select their own art", SharedIngredientArt);
            Run("two art lists resolve all dishes, ingredients and element sprites before serving", ArtCatalogImages);
            Run("quantity reserves separate slots and rejects overflow atomically", Quantity);
            Run("rapid picks include in-flight reservations", RapidPicks);
            Run("pooled foods reset quantity, location and elements", PoolReset);
            Run("linked pair waits for both heads and takes total quantity", Linked);
            Run("linked pair rejects insufficient capacity without changing either column", LinkedOverflow);
            Run("available taps count quantities, whole trays and ready links without click feedback", AvailableTapRules);
            Run("frozen tile loses one layer per adjacent column tap, only visible rows", Frozen);
            Run("chopsticks select visible non-frozen cells, reveal hidden food and release links", Chopsticks);
            Run("magnet completes the selected dish from a full belt and deep board ingredients", Magnet);
            Run("magnet accounts for incoming food and rejects missing ingredients atomically", MagnetReservations);
            Run("magnet leaves empty tray compartments until whole tray is picked", EmptyTray);
            Run("rigid tray takes mixed quantities as one action", RigidTray);
            Run("empty tray slides upward and fades without scaling while remaining rows advance", TrayDeparture);
            Run("empty base moves and fades together before hiding while the column keeps advancing", NormalBaseDeparture);
            Run("chopsticks collapse only rows behind the removed base and partial magnet keeps its base", BoosterBaseDeparture);
            Run("clearing a departing tray cancels its delayed collapse and frozen deadlock check", TrayCancellation);
            Run("adjacent ice keeps its visible row while a departing tray delays column collapse", TrayAdjacentIce);
            Run("pickup waits in world space, follows moving slots, and lands with stable size and rotation", PickupFlight);
            Run("Swap Row moves 2/3 compartments and crossing-boundary trays as blocks", SwapTrays);
            Run("Swap Row starts immediately during column movement and rotates four ordinary rows", SwapDuringMovement);
            Run("hidden top reveals after pickup and Swap Row; ice layers survive Swap Row", RevealAndSwap);
            Run("level quantities, element gates, groups, ice feasibility and copies validate", LevelValidation);
            Run("shared ingredients reserve one portion and complete each dish independently", RecipePortions);
            Run("recipe orders exceeding six ingredients cannot deadlock on plate capacity", LargeRecipeOrder);
            Run("iced tea requires all three ingredients and rejects unrelated food", IcedTea);
            Run("customer icons, waiting layout and deliveries follow the correct seat and moving target", CustomerDeliveryLayout);
            Run("rush timer starts at seat, continues during targeting and pauses in popup", RushAndPopup);
            Run("revive restores only expired customer, prices 300/600, or clears head ice", Revive);
            Run("deadlock waits five seconds and resets for pause, valid moves and reload", NoMovesDelay);
            Run("automatic deliveries and customer transitions prevent premature failure", NoMovesProgress);
            Run("an incomplete serving plate cannot hide a full-belt deadlock", PartialDishDeadlock);
            Run("customer arrival order wins over rush status and seat index", ArrivalPriority);
            Run("level reload clears in-flight food and reservations", Reload);
            Run("booster selection, cancel, dish hit targets, immediate swap and reload follow the guide", BoosterControls);
            Debug.Log("SUSHI_REGRESSION_OK: " + passed + " scenarios passed");
            System.IO.File.WriteAllText("SushiRegressionResults.txt", "PASS: " + passed + " scenarios\n");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            System.IO.File.WriteAllText("SushiRegressionResults.txt", "FAIL after " + passed + " scenarios\n" + exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Run(string name, Action test)
    {
        test(); passed++; Debug.Log("PASS: " + name);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Set(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null) throw new Exception("Missing field: " + name);
        field.SetValue(target, value);
    }
    private static T Get<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static Transform Node(string name)
    {
        var node = new GameObject(name).transform;
        if (fixture != null) node.SetParent(fixture, false);
        return node;
    }
    private static FoodData Food(PoolType type = PoolType.SushiRice, int amount = 1, int ice = 0, int link = 0, int tray = 0, bool hidden = false)
        => new FoodData { typeFood = type, amount = amount, iceLayers = ice, linkId = link, trayId = tray, hidden = hidden };
    private static CustomerOrderData Order(PoolType type, int amount, bool rush = false, float seconds = 0)
        => new CustomerOrderData { rushCustomer = rush, rushSeconds = seconds, orders = new List<FoodOrderData> { new FoodOrderData { typeFood = type, amount = amount } } };
    private static void CreateFixture()
    {
        fixture = Node("Fixture");
        Transform pool = Node("Pool");
        typeof(SimplePool).GetField("root", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, pool);
        FoodShushi prefab = Prefab<FoodShushi>("Ingredient");
        SimplePool.Preload(prefab, 2, pool, true, false);
        FoodQueue columnPrefab = Prefab<FoodQueue>("FoodQueue");
        board = Node("Board").gameObject.AddComponent<BoardShishiManager>();
        Set(board, "tf", board.transform); Set(board, "columnPrefab", columnPrefab);
        Set(board, "ingredientPrefab", prefab);
        conveyor = Node("Conveyor").gameObject.AddComponent<ConveyorManager>();
        Set(board, "conveyorManager", conveyor);
        customers = Node("Customers").gameObject.AddComponent<CustomerManager>();
        Customer customerPrefab = Prefab<Customer>("Customer");
        Set(customers, "customerPrefab", customerPrefab);
        Set(customers, "firstSeat", Node("Seat 0")); Set(customers, "secondSeat", Node("Seat 1"));
        var waiting = new List<Transform>();
        for (int i = 0; i < 8; i++) waiting.Add(Node("Waiting " + i));
        Set(customers, "waitingPositions", waiting);
        Set(conveyor, "customerManager", customers);
        Set(conveyor, "spline", Node("Spline").gameObject.AddComponent<ConveyorSpline>());
        Set(conveyor, "slotPrefab", Prefab<Transform>("Dia")); Set(conveyor, "slotRoot", Node("Slots"));
        Set(conveyor, "flyingFoodRoot", Node("Flying food"));
    }
    private static T Prefab<T>(string name) where T : Component =>
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Prefabs/" + name + ".prefab").GetComponent<T>();

    private static void PrefabVisuals()
    {
        FoodShushi prefab = Prefab<FoodShushi>("Ingredient");
        foreach (string field in new[] { "foodRenderer", "quantityLabel", "stateLabel", "cover", "tileBackground" })
            Check(Get<Component>(prefab, field) != null, "Ingredient prefab is missing " + field);
        Check(Prefab<Customer>("Customer").GetComponent<RushCustomerView>() != null &&
            Get<CustomerRecipeView>(Prefab<Customer>("Customer"), "recipeView") != null, "customer visual components are not authored");
        Check(Get<List<Transform>>(Prefab<FoodQueue>("FoodQueue"), "emptyBases").Count == 4,
            "four empty conveyor bases are missing from the prefab");
        Check(Get<SpriteRenderer>(Prefab<FoodQueue>("FoodQueue"), "lowerRowsShade") != null,
            "lower row shade must be authored and referenced by the FoodQueue prefab");
        Check(Prefab<Transform>("Dia").Find("Ceramic plate") != null, "plate image is not authored");
        var instance = Object.Instantiate(prefab, fixture);
        instance.gameObject.SetActive(false);
        var quantity = Get<TMPro.TextMeshPro>(instance, "quantityLabel");
        int childCount = instance.GetComponentsInChildren<Transform>(true).Length;
        float fontSize = quantity.fontSize;
        Vector3 position = quantity.transform.localPosition;
        instance.gameObject.SetActive(true);
        instance.SetupFromData(Food(PoolType.SushiRice, 3));
        Check(quantity.text == "×3" && quantity.gameObject.activeSelf, "authored quantity label was not updated");
        instance.SetupFromData(Food(PoolType.Salmon, 2, hidden: true));
        Check(!quantity.gameObject.activeSelf && instance.transform.Find("Cover").gameObject.activeSelf, "hidden prefab state wrong");
        instance.SetupFromData(Food(PoolType.Shrimp, 2));
        Check(instance.GetComponentsInChildren<Transform>(true).Length == childCount &&
            quantity.fontSize == fontSize && quantity.transform.localPosition == position, "setup rebuilt visuals or changed authored text layout");
        Object.Destroy(instance.gameObject);
    }

    private static void BoardOuterRails()
    {
        board.Init(new List<FoodQueueData>
        {
            new FoodQueueData(), new FoodQueueData(), new FoodQueueData()
        });
        Transform first = board.FoodQueues[0].transform.Find("Column conveyor");
        Transform middle = board.FoodQueues[1].transform.Find("Column conveyor");
        Transform last = board.FoodQueues[2].transform.Find("Column conveyor");
        Check(first.Find("Left rail").gameObject.activeSelf && !first.Find("Right rail").gameObject.activeSelf &&
            !middle.Find("Left rail").gameObject.activeSelf && !middle.Find("Right rail").gameObject.activeSelf &&
            !last.Find("Left rail").gameObject.activeSelf && last.Find("Right rail").gameObject.activeSelf,
            "board did not restrict rails to its two outer edges");
        Transform leftRail = first.Find("Left rail");
        Transform rightRail = last.Find("Right rail");
        Check(Mathf.Abs(leftRail.localPosition.x + 0.83f) < 0.001f &&
            Mathf.Abs(rightRail.localPosition.x - 0.83f) < 0.001f &&
            Mathf.Abs(leftRail.localScale.x - 0.8f) < 0.001f &&
            Mathf.Abs(rightRail.localScale.x - 0.8f) < 0.001f,
            "outer rails should be offset from trays with x scale 0.8");
        SpriteRenderer shade = board.FoodQueues[0].transform.Find("Lower rows shade").GetComponent<SpriteRenderer>();
        Check(shade != null && shade.color.r == 0f && shade.color.g == 0f && shade.color.b == 0f &&
            shade.color.a > 0f && shade.color.a < 1f && shade.sortingOrder > 1 &&
            Mathf.Abs(shade.transform.localPosition.y + 2.6f) < 0.001f &&
            Mathf.Abs(shade.transform.localScale.y - 3.9f) < 0.001f,
            "lower rows should have a translucent black overlay that leaves the top row uncovered");
        Check(Mathf.Abs(Vector3.Distance(board.FoodQueues[0].transform.position,
                board.FoodQueues[1].transform.position) - 1.4f) < 0.001f &&
            Mathf.Abs(Vector3.Distance(board.FoodQueues[1].transform.position,
                board.FoodQueues[2].transform.position) - 1.4f) < 0.001f,
            "adjacent food trays should have a small separation");
    }

    private static void Load(int slots, params FoodData[][] columns)
    {
        conveyor.Clear();
        var data = new List<FoodQueueData>();
        foreach (FoodData[] foods in columns) data.Add(new FoodQueueData { foods = new List<FoodData>(foods) });
        board.Init(data); conveyor.Init(slots, 0.05f);
    }
    private static FoodQueue Column(int index) => board.FoodQueues[index];
    private static bool Tap(int index) => conveyor.TryTakeFromQueue(Column(index));
    private static void Advance(float seconds = 1) { DOTween.ManualUpdate(seconds, seconds); }

    private static void CustomerPrefabBinding()
    {
        foreach (int count in new[] { 1, 2, 4, 8 })
        {
            Customer customer = RecipeCustomer(new DishOrderData { dish = SushiDishId.MakiAvocado, amount = count });
            customer.SitDown();
            CustomerRecipeView view = customer.GetComponent<CustomerRecipeView>();
            RectTransform orderRoot = Get<RectTransform>(view, "orderRoot");
            Image orderBoard = orderRoot.GetComponent<Image>();
            Image avatar = Get<Image>(customer, "avatarImage");
            Canvas avatarCanvas = avatar.GetComponentInParent<Canvas>();
            Canvas orderCanvas = orderRoot.GetComponent<Canvas>();
            Check(avatarCanvas != null && orderCanvas != null &&
                avatarCanvas.overrideSorting &&
                avatarCanvas.sortingOrder < orderCanvas.sortingOrder &&
                orderCanvas.overrideSorting &&
                avatar.transform.parent.GetComponent<RectMask2D>() != null,
                "customer portrait should be masked and render below every order board");
            Check(orderBoard.sprite.name == "order_board_1x2_0" &&
                Mathf.Abs(orderRoot.rect.height - 184f) < 0.1f &&
                Mathf.Abs(orderRoot.rect.width - 184f * 188f / 252f) < 0.1f,
                "one recipe group should use the compact 1x2 board");
            for (int i = 0; i < count; i++)
            {
                SushiDishCardView card = view.CardAt(i);
                Vector3 center = card.Rect.TransformPoint(card.Rect.rect.center);
                Check(card.Rect.rect.width > 0 && card.Rect.rect.height > 0 && view.DishAtWorldPosition(center) == customer.Dishes[i],
                    "prefab layout or hit area failed for " + count + " dishes");
                GameObject ingredientDetails = card.IngredientAt(0).transform.parent.gameObject;
                Check(!ingredientDetails.activeSelf &&
                    view.ToggleRecipeDetailsAtWorldPosition(center) && ingredientDetails.activeSelf,
                    "tapping the order did not toggle its ingredient recipe");
                center = card.Rect.TransformPoint(card.Rect.rect.center);
                Check(view.ToggleRecipeDetailsAtWorldPosition(center) && !ingredientDetails.activeSelf,
                    "tapping the expanded order did not close its ingredient recipe");
                var corners = new Vector3[4]; card.Rect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = orderRoot.InverseTransformPoint(corner);
                    Check(local.y >= orderRoot.rect.yMin - 0.1f && local.y <= orderRoot.rect.yMax + 0.1f,
                        "large order extends outside the customer panel");
                }
                Check((card.DeliverySlot(1).position - card.IngredientAt(1).Image.transform.position).sqrMagnitude < 0.00001f &&
                    card.IngredientAt(1).ReceivingFoodScale > 0, "prefab delivery anchor does not follow the image layout");
            }
            SushiDishCardView first = view.CardAt(0);
            first.DishImage.transform.localScale = new Vector3(0.93f, 0.93f, 1);
            var label = Get<TMPro.TMP_Text>(first, "dishName");
            label.fontSizeMax = 17;
            var rects = first.GetComponentsInChildren<RectTransform>(true);
            var positions = new Vector3[rects.Length]; var sizes = new Vector2[rects.Length];
            for (int i = 0; i < rects.Length; i++) { positions[i] = rects[i].localPosition; sizes[i] = rects[i].sizeDelta; }
            for (int iteration = 0; iteration < 3; iteration++) first.Bind(customer, customer.Dishes[0], SushiArtCatalog.Default.dishImages);
            for (int i = 0; i < rects.Length; i++)
                Check(rects[i].localPosition == positions[i] && rects[i].sizeDelta == sizes[i], "binding rewrote an authored RectTransform");
            Check(label.fontSizeMax == 17 && first.DishImage.transform.localScale.x == 0.93f,
                "binding rewrote authored font settings or image scale");
            Object.Destroy(customer.gameObject);
        }
        Customer multiDishCustomer = RecipeCustomer(
            new DishOrderData { dish = SushiDishId.MakiAvocado },
            new DishOrderData { dish = SushiDishId.NigiriShrimp },
            new DishOrderData { dish = SushiDishId.GunkanSalmon });
        multiDishCustomer.SitDown();
        CustomerRecipeView multiDishView = multiDishCustomer.GetComponent<CustomerRecipeView>();
        RectTransform multiDishOrderRoot = Get<RectTransform>(multiDishView, "orderRoot");
        SushiDishCardView firstDishCard = multiDishView.CardAt(0);
        SushiDishCardView secondDishCard = multiDishView.CardAt(1);
        SushiDishCardView thirdDishCard = multiDishView.CardAt(2);
        Vector3 firstDishCenter = firstDishCard.Rect.TransformPoint(firstDishCard.Rect.rect.center);
        float collapsedOrderHeight = multiDishOrderRoot.rect.height;
        Check(multiDishOrderRoot.GetComponent<Image>().sprite.name == "order_board_2x2_0" &&
            Mathf.Abs(collapsedOrderHeight - 184f) < 0.1f &&
            firstDishCard.transform.parent == thirdDishCard.transform.parent &&
            firstDishCard.transform.parent != secondDishCard.transform.parent &&
            Get<Transform>(multiDishView, "rightColumn").gameObject.activeSelf,
            "three recipe groups should use a two-column 2x2 board");
        Transform[] servingSlots = Get<Transform[]>(multiDishCustomer, "servingSlots");
        Check(Mathf.Abs(servingSlots[1].localPosition.x - servingSlots[0].localPosition.x - 1.2f) < 0.001f &&
            Mathf.Abs(servingSlots[2].localPosition.x - servingSlots[1].localPosition.x - 1.2f) < 0.001f,
            "serving plates should have a visible gap between them");
        Check(multiDishView.ToggleRecipeDetailsAtWorldPosition(firstDishCenter) && firstDishCard.IsRecipeExpanded,
            "first recipe did not expand");
        Vector3 secondDishCenter = secondDishCard.Rect.TransformPoint(secondDishCard.Rect.rect.center);
        Check(multiDishView.ToggleRecipeDetailsAtWorldPosition(secondDishCenter) && !firstDishCard.IsRecipeExpanded &&
            secondDishCard.IsRecipeExpanded, "opening a second recipe did not close the first");
        Vector3 thirdDishCenter = thirdDishCard.Rect.TransformPoint(thirdDishCard.Rect.rect.center);
        Check(multiDishView.ToggleRecipeDetailsAtWorldPosition(thirdDishCenter) &&
            !firstDishCard.IsRecipeExpanded && !secondDishCard.IsRecipeExpanded && thirdDishCard.IsRecipeExpanded,
            "opening a third recipe did not close the others");
        Check(!thirdDishCard.DishImage.enabled &&
            !Get<TMPro.TMP_Text>(thirdDishCard, "dishName").gameObject.activeSelf &&
            !Get<TMPro.TMP_Text>(thirdDishCard, "portionCount").gameObject.activeSelf &&
            thirdDishCard.IngredientAt(0).transform.parent.GetComponent<VerticalLayoutGroup>().enabled &&
            !thirdDishCard.IngredientAt(0).transform.parent.GetComponent<HorizontalLayoutGroup>().enabled &&
            multiDishOrderRoot.rect.height > collapsedOrderHeight,
            "expanded recipe should hide the dish art and show a vertical ingredient list without overlap");
        SushiOrderIngredientView expandedIngredient = thirdDishCard.IngredientAt(0);
        Check(!Get<TMPro.TMP_Text>(expandedIngredient, "progress").gameObject.activeSelf &&
            Mathf.Approximately(expandedIngredient.Image.color.a, 1) &&
            expandedIngredient.Image.GetComponent<LayoutElement>().preferredHeight > 30,
            "expanded recipe should show only full-opacity, enlarged ingredient images");
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            (RectTransform)thirdDishCard.IngredientAt(0).transform.parent);
        float firstIngredientY = thirdDishCard.IngredientAt(0).transform.localPosition.y;
        float secondIngredientY = thirdDishCard.IngredientAt(1).transform.localPosition.y;
        float thirdIngredientY = thirdDishCard.IngredientAt(2).transform.localPosition.y;
        Check(Mathf.Abs(firstIngredientY - secondIngredientY) > 1 &&
            Mathf.Abs(secondIngredientY - thirdIngredientY) > 1 &&
            Mathf.Abs(firstIngredientY - thirdIngredientY) > 1 &&
            Mathf.Abs(firstIngredientY - secondIngredientY) >= 52 &&
            Mathf.Abs(secondIngredientY - thirdIngredientY) >= 52,
            "expanded ingredient icons should have visible vertical spacing");
        Vector3 expandedDishCenter = thirdDishCard.Rect.TransformPoint(thirdDishCard.Rect.rect.center);
        Check(multiDishView.ToggleRecipeDetailsAtWorldPosition(expandedDishCenter) &&
            !thirdDishCard.IsRecipeExpanded && thirdDishCard.DishImage.enabled &&
            Get<TMPro.TMP_Text>(thirdDishCard, "portionCount").gameObject.activeSelf &&
            Get<TMPro.TMP_Text>(expandedIngredient, "progress").gameObject.activeSelf &&
            expandedIngredient.Image.GetComponent<LayoutElement>().preferredHeight == 30 &&
            Mathf.Approximately(multiDishOrderRoot.rect.height, collapsedOrderHeight),
            "closing the recipe should restore the order card layout");
        Object.Destroy(multiDishCustomer.gameObject);

        SushiDishId[] uniqueDishes =
        {
            SushiDishId.NigiriTuna, SushiDishId.NigiriSalmon, SushiDishId.NigiriFlounder,
            SushiDishId.NigiriMackerel, SushiDishId.NigiriShrimp
        };
        foreach (int count in new[] { 2, 4, 5 })
        {
            var orders = new List<DishOrderData>();
            for (int i = 0; i < count; i++) orders.Add(new DishOrderData { dish = uniqueDishes[i] });
            Customer customer = RecipeCustomer(orders.ToArray());
            customer.SitDown();
            CustomerRecipeView view = customer.GetComponent<CustomerRecipeView>();
            RectTransform orderRoot = Get<RectTransform>(view, "orderRoot");
            string expectedBoard = count < 5 ? "order_board_2x2_0" : "order_board_2x3_0";
            float expectedHeight = count == 5 ? 248f : 184f;
            Check(orderRoot.GetComponent<Image>().sprite.name == expectedBoard &&
                Mathf.Abs(orderRoot.rect.height - expectedHeight) < 0.1f &&
                Get<Transform>(view, "rightColumn").gameObject.activeSelf &&
                view.CardAt(0).transform.parent != view.CardAt(1).transform.parent,
                count + " recipe groups did not use the expected two-column order board");
            Object.Destroy(customer.gameObject);
        }

        Customer completedCustomer = RecipeCustomer(new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 1 });
        completedCustomer.SitDown();
        SushiDishOrder completedOrder = completedCustomer.Dishes[0];
        foreach (OrderItem ingredient in completedOrder.Ingredients)
            for (int i = 0; i < ingredient.requiredAmount; i++) Serve(completedCustomer, ingredient.typeFood);
        GameObject completedMark = Get<GameObject>(completedCustomer.GetComponent<CustomerRecipeView>().CardAt(0), "completedMark");
        Check(completedOrder.IsComplete && completedMark.activeSelf &&
            completedMark.transform.Find("Tick art").gameObject.activeSelf,
            "completed dish did not display its checkmark");
        Object.Destroy(completedCustomer.gameObject);

        Customer partiallyCompletedCustomer = RecipeCustomer(
            new DishOrderData { dish = SushiDishId.MakiAvocado },
            new DishOrderData { dish = SushiDishId.NigiriShrimp });
        partiallyCompletedCustomer.SitDown();
        CustomerRecipeView partiallyCompletedView = partiallyCompletedCustomer.GetComponent<CustomerRecipeView>();
        SushiDishCardView completedCard = partiallyCompletedView.CardAt(0);
        Vector3 completedCardCenter = completedCard.Rect.TransformPoint(completedCard.Rect.rect.center);
        Check(partiallyCompletedView.ToggleRecipeDetailsAtWorldPosition(completedCardCenter) && completedCard.IsRecipeExpanded,
            "incomplete dish recipe did not open before completion");
        SushiDishOrder firstOrder = partiallyCompletedCustomer.Dishes[0];
        foreach (OrderItem ingredient in firstOrder.Ingredients)
            for (int i = 0; i < ingredient.requiredAmount; i++) Serve(partiallyCompletedCustomer, ingredient.typeFood);
        Check(firstOrder.IsComplete && !partiallyCompletedCustomer.IsCompleted && !completedCard.IsRecipeExpanded &&
            completedCard.DishImage.gameObject.activeSelf && completedCard.DishImage.enabled &&
            !completedCard.IngredientAt(0).transform.parent.gameObject.activeSelf &&
            completedCard.Rect.GetComponent<LayoutElement>().preferredHeight == 82,
            "completing an expanded dish did not collapse its ingredients to the finished dish view");
        completedCardCenter = completedCard.Rect.TransformPoint(completedCard.Rect.rect.center);
        Check(!partiallyCompletedView.ToggleRecipeDetailsAtWorldPosition(completedCardCenter) && !completedCard.IsRecipeExpanded,
            "completed dish recipe opened again after completion");
        Object.Destroy(partiallyCompletedCustomer.gameObject);

        Customer normalCustomer = RecipeCustomer(new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 1 });
        normalCustomer.SitDown();
        RushCustomerView normalView = normalCustomer.GetComponent<RushCustomerView>();
        Invoke(normalView, "Update");
        Check(!Get<GameObject>(normalView, "indicator").activeSelf,
            "normal customer displayed a rush timer/progress indicator");
        Object.Destroy(normalCustomer.gameObject);

        Customer rushCustomer = RecipeCustomer(new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 1 });
        rushCustomer.ConfigureRush(new CustomerOrderData { rushCustomer = true, rushSeconds = 30 });
        rushCustomer.SitDown();
        RushCustomerView rush = rushCustomer.GetComponent<RushCustomerView>();
        int childCount = rushCustomer.GetComponentsInChildren<Transform>(true).Length;
        Invoke(rush, "Update");
        Check(Get<GameObject>(rush, "indicator").activeSelf && Get<TMPro.TMP_Text>(rush, "timer").text == "00:30",
            "rush prefab did not bind its initial state");
        Check(Get<UnityEngine.UI.Image>(rush, "countdown").sprite == Get<Sprite>(rush, "normalFill"),
            "rush timer did not start with the green art");
        rushCustomer.TickRush(15); Invoke(rush, "Update");
        Check(Get<UnityEngine.UI.Image>(rush, "countdown").fillAmount == 0.5f &&
            rushCustomer.GetComponentsInChildren<Transform>(true).Length == childCount,
            "rush display created visual objects or failed to bind remaining time");
        rushCustomer.TickRush(0.2f); Invoke(rush, "Update");
        Check(Get<UnityEngine.UI.Image>(rush, "countdown").sprite == Get<Sprite>(rush, "warningFill"),
            "rush timer did not switch to orange below the half-time mark");
        rushCustomer.TickRush(9); Invoke(rush, "Update");
        Check(Get<UnityEngine.UI.Image>(rush, "countdown").sprite == Get<Sprite>(rush, "urgentFill"),
            "rush timer did not switch to red below the final-time mark");
        Object.Destroy(rushCustomer.gameObject);
    }

    private static void SharedIngredientArt()
    {
        FoodShushi previousPrefab = Get<FoodShushi>(board, "ingredientPrefab");
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Prefabs/Ingredient.prefab");
        FoodShushi shared = prefabAsset != null ? prefabAsset.GetComponent<FoodShushi>() : null;
        var library = AssetDatabase.LoadAssetAtPath<SushiIngredientArtLibrary>("Assets/_game/Art/Ingredients/SushiIngredientArt.asset");
        Check(shared != null && library != null, "shared prefab/art library missing");
        Set(board, "ingredientPrefab", shared);
        var allTypes = new List<FoodData>();
        foreach (PoolType type in Enum.GetValues(typeof(PoolType)))
            if (type != PoolType.None) allTypes.Add(Food(type));
        Load(24, allTypes.ToArray());
        var sprites = new HashSet<Sprite>();
        var registrations = (Dictionary<PoolType, GameUnit>)typeof(SimplePool)
            .GetField("poolTypes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        foreach (FoodShushi food in Column(0).Foods)
        {
            Sprite expected = library.GetSprite(food.Type);
            Check(expected != null && sprites.Add(expected), "missing/duplicate sprite for " + food.Type);
            Check(Get<SpriteRenderer>(food, "foodRenderer").sprite == expected, "wrong art for " + food.Type);
            Check(registrations[food.Type] == shared, "per-type prefab still used: " + food.Type);
        }
        Check(sprites.Count == 21, "ingredient count changed unexpectedly");
        FoodShushi reused = Column(0).GetTopFood();
        reused.SetupFromData(Food(PoolType.TeaPot, 2));
        Check(reused.Type == PoolType.TeaPot && Get<SpriteRenderer>(reused, "foodRenderer").sprite == library.GetSprite(PoolType.TeaPot), "reused prefab retained old art");
        board.ClearBoard();
        Set(board, "ingredientPrefab", previousPrefab);
    }

    private static void Quantity()
    {
        Load(3, new[] { Food(amount: 2), Food(PoolType.Egg, 2) });
        Check(Tap(0), "×2 rejected");
        Check(conveyor.OccupiedSlotCount == 2 && conveyor.FreeSlotCount == 1, "×2 did not occupy 2 slots");
        var original = Column(0).GetTopFood();
        Check(!Tap(0) && Column(0).GetTopFood() == original && original.Amount == 2, "overflow changed board");
        var units = Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods");
        var unique = new HashSet<FoodShushi>(units.Values);
        Check(unique.Count == 2, "slot units share one object");
        foreach (var unit in unique) Check(unit.Amount == 1, "belt unit retained ×n");
        Advance();
        Check(!Tap(0) && Column(0).GetTopFood() == original && original.Amount == 2,
            "overflow accepted after the column settled");
        foreach (var unit in unique) Check(unit.Location == FoodLocation.Conveyor, "jump did not complete");
    }

    private static void ArtCatalogImages()
    {
        SushiArtCatalog catalog = SushiArtCatalog.Default;
        Check(catalog != null && catalog.ingredientImages != null && catalog.dishImages != null, "default art catalog or either image list missing");
        Check(catalog.ingredientImages.ingredients.Count == 21 && catalog.dishImages.dishes.Count == 48, "art lists do not cover all types");
        var sprites = new HashSet<Sprite>();
        foreach (SushiRecipe recipe in SushiRecipeCatalog.All)
            Check(catalog.GetDish(recipe.Id) != null && sprites.Add(catalog.GetDish(recipe.Id)), "missing/duplicate dish image " + recipe.Id);
        Check(catalog.hiddenTile != null && catalog.frozenTile != null && catalog.linkedTile != null && catalog.rigidTray != null,
            "element illustration missing");
        Customer customer = RecipeCustomer(new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 1 });
        customer.SitDown();
        SushiDishCardView card = customer.GetComponent<CustomerRecipeView>().CardAt(0);
        UnityEngine.UI.Image dish = card.DishImage;
        Check(!customer.IsCompleted && dish.enabled && dish.sprite == catalog.GetDish(SushiDishId.NigiriShrimp), "dish image waits for completion");
        UnityEngine.UI.Image icon = card.IngredientAt(1).Image;
        Check(icon.sprite == catalog.GetIngredient(PoolType.Shrimp) && icon.enabled, "order ingredient does not resolve image by type");
        dish.sprite = icon.sprite = null;
        Vector3 dishScale = dish.transform.localScale, iconScale = icon.transform.localScale;
        customer.GetComponent<CustomerRecipeView>().RefreshArtwork();
        Check(dish.sprite == catalog.GetDish(SushiDishId.NigiriShrimp) && icon.sprite == catalog.GetIngredient(PoolType.Shrimp),
            "refresh did not restore order images");
        Check(dish.preserveAspect && icon.preserveAspect && dish.transform.localScale == dishScale && icon.transform.localScale == iconScale,
            "data binding changed prefab image fitting or scale");
        Load(4, new[] { Food(hidden: true) }, new[] { Food(PoolType.Egg, ice: 2) });
        Check(Column(0).GetTopFood().transform.Find("Cover").GetComponent<SpriteRenderer>().sprite == catalog.hiddenTile,
            "question mark tile did not use its illustration");
        Check(Column(1).GetTopFood().transform.Find("Cover").GetComponent<SpriteRenderer>().sprite == catalog.frozenTile,
            "frozen tile did not use its illustration");
        var frozen = Column(1).GetTopFood();
        Check(frozen.transform.Find("Quantity").localPosition.y > 0 && frozen.transform.Find("Element").localPosition.y < 0,
            "frozen layer count overlaps ingredient quantity");
        Check(dish.material != null && icon.material != null && dish.material.shader != null && icon.material.shader != null,
            "order UI images have missing materials");
        Load(8, new[] { Food(link: 1) }, new[] { Food(PoolType.Egg), Food(PoolType.Shrimp, link: 1) },
            new[] { Food(tray: 1), Food(PoolType.Egg, tray: 1) });
        Invoke(board, "LateUpdate");
        var groups = Get<Dictionary<string, SpriteRenderer>>(board, "groupArt");
        Check(groups["link:1"].sprite == catalog.linkedTile && groups["link:1"].gameObject.activeSelf,
            "linked pair did not show rope illustration");
        Check(groups["tray:1"].sprite == catalog.rigidTray && groups["tray:1"].gameObject.activeSelf,
            "rigid tray did not show wooden illustration");
    }
    private static void RapidPicks()
    {
        Load(3, new[] { Food(amount: 2) }, new[] { Food(amount: 2) });
        Check(Tap(0) && !Tap(1) && Column(1).Count == 1 && conveyor.OccupiedSlotCount == 2, "in-flight reservation lost");
    }
    private static void PoolReset()
    {
        Load(3, new[] { Food(amount: 3) }); Check(Tap(0), "setup failed"); Advance();
        Load(3, new[] { Food(amount: 2) });
        var food = Column(0).GetTopFood();
        Check(food.Location == FoodLocation.Board && food.Amount == 2 && food.CanPick(), "pooled state not reset");
    }
    private static void Linked()
    {
        Load(6, new[] { Food(amount: 2, link: 1) }, new[] { Food(PoolType.Egg), Food(PoolType.Shrimp, 3, link: 1) });
        Check(!Tap(0) && Tap(1) && !Tap(0), "linked head rule failed during column motion");
        Advance();
        Check(Tap(0), "linked heads could not be taken after advancing");
        Advance();
        Check(conveyor.OccupiedSlotCount == 6 && board.IsEmpty(), "linked quantities wrong");
    }
    private static void LinkedOverflow()
    {
        Load(5, new[] { Food(amount: 2, link: 1) }, new[] { Food(PoolType.Egg), Food(PoolType.Shrimp, 3, link: 1) });
        Check(Tap(1), "linked overflow setup failed");
        Advance();
        Check(!Tap(0), "linked overflow accepted");
        Check(Column(0).Count == 1 && Column(1).Count == 1 && conveyor.OccupiedSlotCount == 1, "partial linked pickup");
    }
    private static void AvailableTapRules()
    {
        Load(6, new[] { Food(amount: 3) }, new[] { Food(ice: 1) });
        Check(!board.HasAvailableTap(2) && board.HasAvailableTap(3), "quantity or ice ignored by move check");
        Check(Get<Tween>(Column(1), "shakeTween") == null, "move check shook frozen column");

        Load(6, new[] { Food(amount: 2, link: 1) }, new[] { Food(ice: 1), Food(amount: 3, link: 1) });
        Check(!board.HasAvailableTap(6), "buried link partner counted as a move");
        Check(Get<Tween>(Column(0), "shakeTween") == null &&
            Get<Tween>(Column(1), "shakeTween") == null, "move check shook linked columns");

        Load(6, new[] { Food(amount: 2, link: 1) }, new[] { Food(amount: 3, link: 1) });
        Check(!board.HasAvailableTap(4) && board.HasAvailableTap(5), "link did not require both quantities");

        Load(6, new[] { Food(amount: 2, tray: 1), Food(amount: 3, tray: 1) });
        Check(!board.HasAvailableTap(4) && board.HasAvailableTap(5), "tray did not require the whole group");
        foreach (FoodShushi food in Column(0).Foods) Set(food, "amount", 0);
        Check(board.HasAvailableTap(0) && Tap(0), "empty booster tray could not be cleared");
        Check(board.HasPendingMovement, "departing tray was treated as a settled board");
        Advance();
        Check(!board.HasPendingMovement && board.IsEmpty(), "empty tray failed to settle");

        Load(1, new[] { Food(hidden: true) });
        Check(board.HasPendingMovement && !board.HasAvailableTap(1), "reveal was treated as a ready move");
        Advance();
        Check(!board.HasPendingMovement && board.HasAvailableTap(1), "revealed head stayed blocked");
    }

    private static void Frozen()
    {
        Load(8, new[] { Food(amount: 3), Food(PoolType.Egg), Food(), Food(), Food() },
            new[] { Food(PoolType.Shrimp, ice: 2), Food(), Food(), Food(), Food(ice: 2) });
        FoodShushi ice = Column(1).Foods[0], deepIce = Column(1).Foods[4];
        Check(!Tap(1) && Tap(0) && ice.IceLayers == 1 && deepIce.IceLayers == 2, "×3/visible ice rule failed");
        Check(!Tap(0), "column accepted a second pickup before its belt advanced");
        Advance();
        Check(Tap(0) && ice.IceLayers == 0, "ice did not break");
    }
    private static void Chopsticks()
    {
        Load(8, new[] { Food(), Food(PoolType.Shrimp, 2, hidden: true), Food(ice: 2), Food(link: 1), Food(tray: 1) },
            new[] { Food(PoolType.Egg, ice: 3) }, new[] { Food(PoolType.Egg, link: 1) });
        FoodShushi frozen = Column(0).Foods[2], linked = Column(0).Foods[3], tray = Column(0).Foods[4];
        Check(!conveyor.TryTakeChopsticks(tray), "chopsticks reached the hidden fifth row");
        Check(conveyor.TryTakeChopsticks(Column(0).Foods[1]), "hidden chopsticks failed");
        Check(conveyor.OccupiedSlotCount == 2 && Column(1).GetTopFood().IceLayers == 3, "booster damaged ice");
        Check(!conveyor.TryTakeChopsticks(frozen) && !conveyor.TryTakeChopsticks(linked), "chopsticks reached a frozen or moving cell");
        Advance();
        foreach (var unit in Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods").Values)
            Check(!unit.IsHidden && unit.Location == FoodLocation.Conveyor, "hidden unit did not reveal then arrive");
        Check(!conveyor.TryTakeChopsticks(frozen) && conveyor.TryTakeChopsticks(linked) &&
            Column(2).GetTopFood().LinkId == 0, "chopsticks failed to reject ice or release a linked cell");
        Advance();
        Check(conveyor.TryTakeChopsticks(tray) && tray.Amount == 0 && Column(0).IndexOf(tray) >= 0,
            "chopsticks did not leave an empty tray compartment");
    }
    private static void Magnet()
    {
        Load(1, new[] { Food() }, new[] { Food(amount: 3), Food(PoolType.Egg) },
            new[] { Food(PoolType.Egg), Food(PoolType.Seaweed, ice: 3, hidden: true) }, new[] { Food(PoolType.Avocado, 2) });
        Customer customer = SetRecipeOrders(new DishOrderData { dish = SushiDishId.NigiriAvocado },
            new DishOrderData { dish = SushiDishId.MakiAvocado });
        Check(Tap(0) && conveyor.FreeSlotCount == 0, "full-belt magnet setup failed");
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[1]) && conveyor.OccupiedSlotCount == 0 &&
            conveyor.DeliveringCount == 3, "magnet did not send all three ingredients straight to the selected dish");
        Check(Column(1).GetTopFood().Amount == 3 && Column(3).GetTopFood().Amount == 1 &&
            customer.Dishes[0].Ingredients[0].incomingAmount == 0 && customer.Dishes[1].Ingredients[0].incomingAmount == 1,
            "magnet took extra rice or reserved an earlier dish sharing its ingredients");
        Check(!conveyor.TryTakeMagnet(board, customer, customer.Dishes[1]), "magnet duplicated ingredients already incoming");
        Advance();
        Check(customer.Dishes[1].IsComplete && !customer.Dishes[0].IsComplete && conveyor.DeliveringCount == 0 &&
            conveyor.OccupiedSlotCount == 0, "selected dish did not complete or an old pickup callback restored its belt food");
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[0]) && Column(1).GetTopFood().Amount == 2,
            "magnet failed to preserve a partially consumed board cell");
        Advance();
    }
    private static Customer SetRecipeOrders(params DishOrderData[] dishes)
    {
        customers.Init(new List<CustomerOrderData> { new CustomerOrderData { dishes = new List<DishOrderData>(dishes) } });
        return customers.ActiveCustomers[0];
    }
    private static void MagnetReservations()
    {
        Load(2, new[] { Food(amount: 2) }, new[] { Food(PoolType.Egg, 2) });
        Customer customer = SetRecipeOrders(new DishOrderData { dish = SushiDishId.NigiriEgg, amount = 2 });
        FoodShushi incoming = ReserveIngredient(customer, PoolType.SushiRice);
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[0]) && conveyor.DeliveringCount == 1 &&
            Column(0).GetTopFood().Amount == 2, "magnet duplicated an ingredient already reserved for the dish");
        Check(customer.CompleteReceive(incoming), "existing incoming ingredient could not finish");
        Advance();
        Check(customer.Dishes[0].IsComplete && !customer.Dishes[1].IsComplete, "magnet changed a different portion");
        Load(1, new[] { Food() }, new[] { Food(PoolType.Avocado) });
        customer = SetRecipeOrders(new DishOrderData { dish = SushiDishId.MakiAvocado });
        Check(Tap(0), "atomic magnet setup failed");
        FoodShushi beltFood = new List<FoodShushi>(Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods").Values)[0];
        Check(!conveyor.TryTakeMagnet(board, customer, customer.Dishes[0]) && conveyor.OccupiedSlotCount == 1 &&
            conveyor.DeliveringCount == 0 && beltFood.Location == FoodLocation.MovingToConveyor && Column(1).Count == 1 &&
            customer.Dishes[0].Ingredients[0].incomingAmount == 0, "missing seaweed partially changed the board, belt or reservations");
        Customer other = RecipeCustomer(new DishOrderData { dish = SushiDishId.NigiriAvocado });
        Check(!conveyor.TryTakeMagnet(board, customer, other.Dishes[0]) &&
            !conveyor.TryTakeMagnet(board, other, other.Dishes[0]), "magnet accepted another customer's dish or a waiting customer");
        Object.DestroyImmediate(other.gameObject);
    }
    private static void EmptyTray()
    {
        Load(2, new[] { Food(tray: 1), Food(tray: 1) }, new[] { Food(PoolType.Egg, 2) }, new[] { Food(PoolType.Avocado, 2) });
        Customer customer = SetRecipeOrders(new DishOrderData { dish = SushiDishId.NigiriEgg, amount = 2 });
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[0]) &&
            conveyor.TryTakeMagnet(board, customer, customer.Dishes[1]) && Tap(2), "tray magnet failed");
        Check(Column(0).Count == 2 && Column(0).Foods[0].Amount == 0 && Column(0).Foods[1].Amount == 0, "tray compartments removed");
        Check(Tap(0) && Column(0).IsPickupPending && conveyor.OccupiedSlotCount == 2, "empty tray cannot clear full belt");
        Advance();
        Check(board.IsEmpty(), "empty tray did not finish clearing");
    }
    private static void RigidTray()
    {
        Load(6, new[] { Food(amount: 2, tray: 1), Food(PoolType.Egg, 3, tray: 1) }, new[] { Food(ice: 2) });
        Check(Tap(0) && Column(0).Count == 0 && conveyor.OccupiedSlotCount == 5 && Column(1).GetTopFood().IceLayers == 1, "tray count/ice damage wrong");
        Load(4, new[] { Food(amount: 2, tray: 1), Food(PoolType.Egg, 3, tray: 1) });
        Check(!Tap(0) && Column(0).Count == 2 && !Column(0).IsPickupPending && conveyor.OccupiedSlotCount == 0, "partial tray on overflow");
    }
    private static void TrayDeparture()
    {
        Load(8, new[] { Food(amount: 2, tray: 1), Food(PoolType.Egg, tray: 1), Food(PoolType.Shrimp) },
            new[] { Food(ice: 1) }, new[] { Food(PoolType.Egg) });
        FoodQueue column = Column(0);
        FoodShushi behind = column.Foods[2];
        Vector3 originalPosition = behind.transform.position;
        GameObject pickedBase = Get<SpriteRenderer>(column.GetTopFood(), "tileBackground").gameObject;
        Invoke(board, "LateUpdate");
        GameObject trayView = Get<Dictionary<string, SpriteRenderer>>(board, "groupArt")["tray:1"].gameObject;
        Check(Tap(0) && column.IsPickupPending && column.Count == 1 && conveyor.OccupiedSlotCount == 3, "tray pickup was not reserved atomically");
        float duration = Get<Tween>(column, "columnMoveTween").Duration();
        GameObject view = Get<GameObject>(column, "departingTray");
        Transform frame = view.transform.GetChild(0);
        Vector3 frameOrigin = frame.position, frameScale = frame.localScale;
        SpriteRenderer frameRenderer = frame.GetComponent<SpriteRenderer>();
        Check(!pickedBase.activeInHierarchy && !trayView.activeSelf && view.activeInHierarchy && view.transform.childCount == 3,
            "picked tray did not preserve its empty frame and compartments for the animation");
        Check(!Tap(0) && !conveyor.TryTakeChopsticks(behind) &&
            board.BuildMagnet(PoolType.Shrimp, 1).Count == 0, "pickup reached food behind the departing tray");
        Check(Tap(2), "tray animation blocked another column");
        Advance(0.2f);
        Check(column.IsPickupPending && behind.transform.position == originalPosition,
            "belt moved before the last takeoff");
        Advance(duration - 0.35f);
        Check(column.IsPickupPending && behind.transform.position.y > originalPosition.y && view.activeSelf &&
            frame.position.y > frameOrigin.y && frame.localScale == frameScale &&
            frameRenderer.color.a > 0 && frameRenderer.color.a < 1,
            "empty tray did not slide and fade together while preserving its scale");
        Advance(0.16f);
        Check(!column.IsPickupPending && !view.activeSelf && behind.transform.localPosition.sqrMagnitude < 0.00001f &&
            column.CanTakeTopFood(), "column did not settle after tray departure");
    }

    private static void NormalBaseDeparture()
    {
        Load(8, new[] { Food(amount: 2), Food(PoolType.Egg), Food(), Food(PoolType.Shrimp), Food() },
            new[] { Food(PoolType.Egg) });
        FoodQueue column = Column(0);
        FoodShushi next = column.Foods[1], entering = column.Foods[4];
        GameObject pickedBase = Get<SpriteRenderer>(column.GetTopFood(), "tileBackground").gameObject;
        Vector3 original = next.transform.localPosition;
        var blanks = Get<List<Transform>>(column, "emptyBases");
        Check(blanks.Count == 4 && column.transform.Find("Column conveyor") != null, "column belt was not created");
        Check(Tap(0) && !Tap(0) && column.IsPickupPending && conveyor.OccupiedSlotCount == 2,
            "normal pickup did not reserve quantity or protect the moving column");
        GameObject view = Get<GameObject>(column, "departingTray");
        Transform emptyBase = view.transform.GetChild(0);
        Vector3 baseOrigin = emptyBase.position, baseScale = emptyBase.localScale;
        SpriteRenderer baseRenderer = emptyBase.GetComponent<SpriteRenderer>();
        Check(!pickedBase.activeInHierarchy && view.activeInHierarchy && view.transform.childCount == 1 &&
            view.GetComponentsInChildren<FoodShushi>().Length == 0,
            "empty base was not separated from the picked ingredient");
        Advance(0.1f);
        Check(next.transform.localPosition == original && emptyBase.position == baseOrigin && emptyBase.localScale == baseScale &&
            baseRenderer.color.a == 1,
            "remaining food or the empty base moved before takeoff");
        Check(Tap(1), "one moving belt blocked another column");
        Advance(0.15f);
        Check(next.transform.localPosition.y > original.y && next.transform.localPosition.y < 0 && view.activeSelf &&
            emptyBase.position.y > baseOrigin.y && emptyBase.localScale == baseScale &&
            baseRenderer.color.a > 0 && baseRenderer.color.a < 1,
            "empty base did not slide and fade together, or remaining food failed to advance");
        float previousY = emptyBase.position.y, previousAlpha = baseRenderer.color.a;
        Advance(0.055f);
        Check(view.activeSelf && emptyBase.position.y > previousY && baseRenderer.color.a < previousAlpha &&
            baseRenderer.color.a > 0 && emptyBase.localScale == baseScale,
            "empty base stopped sliding or fading, or its scale changed");
        foreach (SpriteRenderer part in view.GetComponentsInChildren<SpriteRenderer>())
            Check(Mathf.Abs(part.color.a - baseRenderer.color.a) < 0.001f, "nested base pieces did not fade together");
        Advance(0.15f);
        Check(!view.activeSelf, "empty base did not hide after sliding and fading");
        Advance();
        Check(!column.IsPickupPending && next.transform.localPosition == Vector3.zero &&
            Get<SpriteRenderer>(entering, "foodRenderer").enabled, "next row did not settle or hidden fifth row did not enter");
        Load(4, new[] { Food() });
        Check(Tap(0) && !board.IsEmpty(), "last base caused premature board completion");
        Advance();
        foreach (Transform blank in Get<List<Transform>>(Column(0), "emptyBases"))
            Check(blank.gameObject.activeSelf, "empty column lost its remaining belt bases");
        Check(board.IsEmpty() && !Column(0).GetComponent<BoxCollider2D>().enabled, "empty belt remained interactive");
    }

    private static void BoosterBaseDeparture()
    {
        Load(8, new[] { Food(), Food(PoolType.Egg), Food(PoolType.Shrimp), Food() });
        FoodQueue column = Column(0);
        FoodShushi head = column.Foods[0], behind = column.Foods[2];
        Check(conveyor.TryTakeChopsticks(column.Foods[1]) && column.IsPickupPending, "chopsticks did not start advancing the column");
        Advance();
        Check(head.transform.localPosition == Vector3.zero && Mathf.Abs(behind.transform.localPosition.y + Get<float>(column, "rowSpace")) < 0.001f,
            "chopsticks moved rows ahead of the gap or failed to collapse rows behind it");
        Load(8, new[] { Food(amount: 3), Food(PoolType.Egg) }, new[] { Food(PoolType.Egg, 3) });
        column = Column(0);
        Customer customer = SetRecipeOrders(new DishOrderData { dish = SushiDishId.NigiriEgg, amount = 3 });
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[0]) && column.GetTopFood().Amount == 2 &&
            !column.IsPickupPending && Get<Tween>(column, "columnMoveTween") == null && Get<GameObject>(column, "departingTray") == null,
            "partial magnet removed a base that still contains ingredients");
        Check(conveyor.TryTakeMagnet(board, customer, customer.Dishes[1]) &&
            conveyor.TryTakeMagnet(board, customer, customer.Dishes[2]) && column.IsPickupPending,
            "magnet did not advance the column after taking the final ingredients");
        Tween oldTween = Get<Tween>(column, "columnMoveTween");
        GameObject oldView = Get<GameObject>(column, "departingTray");
        column.gameObject.SetActive(false);
        Advance();
        column.gameObject.SetActive(true);
        Check(!column.IsPickupPending && !oldTween.IsActive() && !oldView.activeSelf && column.GetTopFood().transform.localPosition == Vector3.zero,
            "disabling a moving column left a pending tween or a gap");
    }
    private static void TrayCancellation()
    {
        Load(8, new[] { Food(tray: 1), Food(PoolType.Egg, tray: 1) }, new[] { Food(ice: 1) });
        Check(Tap(0) && !board.AllRemainingHeadsFrozen() && !board.IsEmpty(), "departing tray caused a premature deadlock or win");
        Tween oldTween = Get<Tween>(Column(0), "columnMoveTween");
        GameObject oldView = Get<GameObject>(Column(0), "departingTray");
        Load(8, new[] { Food(PoolType.Shrimp) });
        Advance(3);
        Check(!oldTween.IsActive() && !oldView.activeSelf && !Column(0).IsPickupPending && Column(0).Count == 1 &&
            Column(0).GetTopFood().Type == PoolType.Shrimp && conveyor.OccupiedSlotCount == 0,
            "departing tray callback changed a reloaded board");
    }
    private static void PickupFlight()
    {
        Load(4, new[] { Food(amount: 3) });
        Vector3 origin = Column(0).GetTopFood().transform.position;
        Check(Tap(0), "pickup flight failed");
        var units = Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods");
        Set(conveyor, "progress", 0.13f); Invoke(conveyor, "UpdateSlotPositions"); Advance(0.03f);
        foreach (var pair in units)
            Check(pair.Value.transform.parent == Get<Transform>(conveyor, "flyingFoodRoot") &&
                (pair.Value.transform.position - origin).sqrMagnitude < 0.00001f, "waiting ingredient drifted with its reserved plate");
        Advance(0.24f);
        bool airborne = false;
        foreach (var pair in units)
            airborne |= pair.Value.Location == FoodLocation.MovingToConveyor && (pair.Value.transform.position - origin).sqrMagnitude > 0.001f;
        Check(airborne, "ingredients did not follow an animated pickup path");
        for (int i = 0; i < 8; i++)
        {
            Set(conveyor, "progress", 0.15f + 0.02f * i); Invoke(conveyor, "UpdateSlotPositions"); Advance(0.15f);
        }
        foreach (var pair in units)
            Check(pair.Value.Location == FoodLocation.Conveyor && pair.Value.transform.parent == pair.Key &&
                pair.Value.transform.localPosition.sqrMagnitude < 0.00001f &&
                Quaternion.Angle(pair.Value.transform.localRotation, Quaternion.identity) < 0.001f &&
                Mathf.Abs(pair.Value.transform.localScale.x - Get<float>(conveyor, "beltFoodScale")) < 0.001f,
                "ingredient missed a moving plate or retained its flight squash/rotation");
    }
    private static void TrayAdjacentIce()
    {
        Load(12, new[] { Food(tray: 1), Food(PoolType.Egg, tray: 1), Food(PoolType.Shrimp, ice: 2) }, new[] { Food() });
        Check(Tap(0) && Tap(1) && Column(0).GetTopFood().IceLayers == 2, "pending column ice was damaged using its future row");
        Load(12, new[] { Food(tray: 1), Food(PoolType.Egg, tray: 1), Food(PoolType.Shrimp, ice: 2) },
            new[] { Food(), Food(), Food() });
        Check(Tap(0) && Tap(1) && Column(0).GetTopFood().IceLayers == 1, "ice at visible row 3 did not receive adjacent damage");
    }
    private static void SwapTrays()
    {
        Load(6, new[] { Food(tray: 1), Food(tray: 1), Food(tray: 1), Food(PoolType.Egg) }); board.SwapRow();
        Check(Column(0).Foods[1].TrayId == 1 && Column(0).Foods[3].TrayId == 1, "3-tray did not move to row 2");
        Load(6, new[] { Food(tray: 1), Food(tray: 1), Food(PoolType.Egg), Food(PoolType.Shrimp) }); board.SwapRow();
        Check(Column(0).Foods[2].TrayId == 1 && Column(0).Foods[3].TrayId == 1, "2-tray did not move to row 3");
        Load(6, new[] { Food(PoolType.Egg), Food(), Food(), Food(tray: 1), Food(tray: 1) }); board.SwapRow();
        Check(Column(0).Foods[2].TrayId == 1 && Column(0).Foods[3].TrayId == 1 && Column(0).Foods[4].Type == PoolType.Egg, "crossing tray split");
    }
    private static void SwapDuringMovement()
    {
        Load(8, new[] { Food(), Food(PoolType.Egg), Food(PoolType.Shrimp), Food(PoolType.Tuna), Food(PoolType.Avocado), Food() });
        Check(Tap(0) && Column(0).IsPickupPending, "moving swap setup failed");
        board.SwapRow();
        Check(!Column(0).IsPickupPending && Column(0).Foods[0].Type == PoolType.Shrimp &&
            Column(0).Foods[1].Type == PoolType.Tuna && Column(0).Foods[2].Type == PoolType.Avocado &&
            Column(0).Foods[3].Type == PoolType.Egg && Column(0).Foods[4].Type == PoolType.SushiRice,
            "Swap Row waited for targeting or failed to rotate the four visible rows");
        Advance();
        Check(Column(0).GetTopFood().transform.localPosition == Vector3.zero, "Swap Row left an old column tween running");
    }
    private static void BoosterControls()
    {
        level.LoadLevel(0);
        Load(8, new[] { Food(), Food(PoolType.Egg), Food(PoolType.Tuna), Food(PoolType.Avocado) });
        var input = Node("Booster input").gameObject.AddComponent<FoodInputController>();
        Set(input, "conveyorManager", conveyor);
        input.SelectMagnet(); input.CancelBooster();
        Check(input.PickMode == FoodPickMode.Tap && Column(0).Count == 4 && conveyor.OccupiedSlotCount == 0,
            "cancel changed food or failed to leave targeting");
        input.SelectChopsticks();
        Check(input.TryUseChopsticks(Column(0).Foods[1]) && input.PickMode == FoodPickMode.Tap,
            "successful chopsticks did not finish targeting");
        input.SelectMagnet(); input.UseSwapRow();
        Check(input.PickMode == FoodPickMode.Tap && !Column(0).IsPickupPending && Column(0).GetTopFood().Type == PoolType.Tuna,
            "Swap Row required a target or failed to cancel the previous booster");
        level.SetPopupOpen(true); input.SelectMagnet();
        Check(input.PickMode == FoodPickMode.Tap, "booster selection ignored the popup");
        level.SetPopupOpen(false);
        Load(2, new[] { Food(amount: 2) }, new[] { Food(PoolType.Egg, 2) });
        Customer customer = SetRecipeOrders(new DishOrderData { dish = SushiDishId.NigiriEgg, amount = 2 });
        var view = customer.GetComponent<CustomerRecipeView>();
        Vector3 point = view.CardAt(1).Rect.TransformPoint(view.CardAt(1).Rect.rect.center);
        Check(view.DishAtWorldPosition(point) == customer.Dishes[1] &&
            view.DishAtWorldPosition(customer.transform.TransformPoint(new Vector3(8, 8, 0))) == null,
            "order hit testing did not resolve the exact dish card");
        input.SelectMagnet();
        Check(input.TryUseMagnet(customer, view.DishAtWorldPosition(point)) && input.PickMode == FoodPickMode.Tap &&
            conveyor.DeliveringCount == 2, "Magnet did not use the selected card or finish targeting");
        level.LoadLevel(0); Advance();
        Check(conveyor.DeliveringCount == 0 && conveyor.OccupiedSlotCount == 0 && input.PickMode == FoodPickMode.Tap,
            "reloading kept magnet flights or stale booster selection");
        Object.DestroyImmediate(input.gameObject);
    }
    private static void RevealAndSwap()
    {
        Load(6, new[] { Food(), Food(hidden: true), Food(ice: 2) });
        var hidden = Column(0).Foods[1]; Check(Tap(0) && Column(0).IsPickupPending && hidden.IsHidden, "top hidden revealed before reaching the belt head");
        Advance(); Advance(0.3f);
        Check(!hidden.IsHidden && hidden.CanPick(), "top reveal failed");
        Load(6, new[] { Food(), Food(hidden: true), Food(ice: 2) }); board.SwapRow(); Advance();
        Check(!Column(0).Foods[0].IsHidden && Column(0).Foods[1].IceLayers == 2, "Swap changed ice or failed reveal");
    }
    private static SushiLevelData ValidLevel()
    {
        var data = ScriptableObject.CreateInstance<SushiLevelData>(); data.number = 46; data.numberColumns = 2;
        data.customers = new List<CustomerOrderData> { Order(PoolType.SushiRice, 2), Order(PoolType.Egg, 2) };
        data.boardColumns = new List<FoodQueueData> { new FoodQueueData { foods = new List<FoodData> { Food(amount: 2) } },
            new FoodQueueData { foods = new List<FoodData> { Food(PoolType.Egg, 2) } } };
        return data;
    }
    private static void LevelValidation()
    {
        ProgressionGates();
        var data = ValidLevel(); Check(data.ValidateData() == null, data.ValidateData());
        var copy = data.CreateBoardData(); copy[0].foods[0].amount = 1;
        Check(data.boardColumns[0].foods[0].amount == 2, "board data aliases asset");
        data.boardColumns[0].foods[0].amount = 3; Check(data.ValidateData() != null, "quantity mismatch allowed");
        data.boardColumns[0].foods[0].amount = 2;
        data.boardColumns[0].foods[0].linkId = 1; Check(data.ValidateData() != null, "unpaired link allowed");
        data.boardColumns[1].foods[0].linkId = 1; Check(data.ValidateData() != null, "both linked heads initially allowed");
        data.boardColumns[0].foods[0].linkId = data.boardColumns[1].foods[0].linkId = 0;
        data.boardColumns[0].foods[0].iceLayers = 5; Check(data.ValidateData() != null, "impossible ice allowed");
        data.boardColumns[0].foods[0].iceLayers = 0; data.number = 19; data.customers[0].rushCustomer = true;
        Check(data.ValidateData() != null, "rush unlock gate ignored");
        Object.DestroyImmediate(data);
    }
    private static SushiLevelData GateFixture(int number, params FoodData[][] columns)
    {
        var data = ScriptableObject.CreateInstance<SushiLevelData>();
        data.number = number;
        data.numberColumns = columns.Length;
        data.boardColumns = new List<FoodQueueData>();
        data.customers = new List<CustomerOrderData>();
        var totals = new Dictionary<PoolType, int>();
        foreach (FoodData[] column in columns)
        {
            data.boardColumns.Add(new FoodQueueData { foods = new List<FoodData>(column) });
            foreach (FoodData food in column)
            {
                totals.TryGetValue(food.typeFood, out int amount);
                totals[food.typeFood] = amount + food.amount;
            }
        }
        foreach (var ingredient in totals)
            for (int remaining = ingredient.Value; remaining > 0; remaining -= 6)
                data.customers.Add(Order(ingredient.Key, Mathf.Min(6, remaining)));
        return data;
    }

    private static void ProgressionGates()
    {
        Check(SushiLevelData.HiddenUnlockLevel == 5 && SushiLevelData.FrozenUnlockLevel == 7 &&
            SushiLevelData.LinkedUnlockLevel == 9 && SushiLevelData.TrayUnlockLevel == 11,
            "campaign mechanic constants changed from the requested 5/7/9/11 milestones");

        var hidden = GateFixture(4, new[] { Food(), Food(hidden: true) }, new[] { Food(PoolType.Egg) });
        Check(hidden.ValidateData() != null, "hidden tile allowed before level 5");
        hidden.number = 5;
        Check(hidden.ValidateData() == null, "hidden tile rejected at level 5: " + hidden.ValidateData());
        hidden.boardColumns[0].foods[0].hidden = true;
        Check(hidden.ValidateData() != null, "hidden tile allowed in the initial head row");
        hidden.boardColumns[0].foods[0].hidden = false;
        hidden.boardColumns[0].foods[1].hidden = false;
        hidden.boardColumns[0].foods[1].foodState = FoodState.HoiCham;
        hidden.number = 4;
        Check(hidden.ValidateData() != null, "legacy HoiCham state bypassed level 5");
        hidden.number = 5;
        Check(hidden.ValidateData() == null, "legacy HoiCham state rejected at level 5");
        Object.DestroyImmediate(hidden);

        var frozen = GateFixture(6,
            new[] { Food(), Food(), Food(), Food(), Food(), Food() },
            new[] { Food(PoolType.Egg, ice: 1) },
            new[] { Food(), Food(), Food(), Food(), Food(), Food() });
        Check(frozen.ValidateData() != null && SushiLevelData.GetMaximumIceLayers(6) == 0,
            "ice allowed before level 7");
        frozen.boardColumns[1].foods[0].foodState = FoodState.Ice;
        int[] stages = { 7, 8, 9, 13, 14, 17, 18, 20, 29, 30, 38, 39, 40, 48, 49, 50 };
        int[] limits = { 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 5, 4, 4, 5, 4 };
        for (int i = 0; i < stages.Length; i++)
        {
            frozen.number = stages[i];
            Check(SushiLevelData.GetMaximumIceLayers(stages[i]) == limits[i],
                "wrong ice limit at level " + stages[i]);
            frozen.boardColumns[1].foods[0].iceLayers = limits[i];
            Check(frozen.ValidateData() == null,
                "valid ice rejected at level " + stages[i] + ": " + frozen.ValidateData());
            frozen.boardColumns[1].foods[0].iceLayers = limits[i] + 1;
            Check(frozen.ValidateData() != null, "ice cap bypassed at level " + stages[i]);
        }
        frozen.number = 7;
        frozen.boardColumns[1].foods[0].iceLayers = 1;
        frozen.boardColumns[0].foods[0].iceLayers = 1;
        Check(frozen.ValidateData() != null, "ice intro accepted fewer than two unfrozen heads");
        Object.DestroyImmediate(frozen);
        var impossibleIce = GateFixture(7, new[] { Food() },
            new[] { Food(PoolType.Egg), Food(PoolType.Egg, ice: 1) }, new[] { Food() });
        Check(impossibleIce.ValidateData() != null, "ice without adjacent moves at its row was allowed");
        Object.DestroyImmediate(impossibleIce);

        var linked = GateFixture(8, new[] { Food(), Food(link: 1) },
            new[] { Food(PoolType.Egg), Food(PoolType.Egg, link: 1) });
        Check(linked.ValidateData() != null, "link allowed before level 9");
        linked.number = 9;
        Check(linked.ValidateData() == null, "valid pair rejected at level 9: " + linked.ValidateData());
        foreach (FoodQueueData column in linked.boardColumns)
        {
            column.foods[1].lockId = 1;
            column.foods[1].linkId = 0;
            column.foods[1].foodState = FoodState.Lock;
        }
        linked.number = 8;
        Check(linked.ValidateData() != null, "legacy lockId bypassed level 9");
        linked.number = 9;
        Check(linked.ValidateData() == null, "legacy lockId rejected at level 9");
        Object.DestroyImmediate(linked);
        var twoPairs = GateFixture(9, new[] { Food(), Food(link: 1), Food(link: 2) },
            new[] { Food(PoolType.Egg), Food(PoolType.Egg, link: 1), Food(PoolType.Egg, link: 2) });
        Check(twoPairs.ValidateData() != null, "intro level 9 allowed two link pairs");
        twoPairs.number = 10;
        Check(twoPairs.ValidateData() == null, "two pairs rejected after introduction: " + twoPairs.ValidateData());
        Object.DestroyImmediate(twoPairs);

        var tray = GateFixture(10, new[] { Food(), Food(tray: 1), Food(PoolType.Egg, tray: 1) },
            new[] { Food(PoolType.Egg) });
        Check(tray.ValidateData() != null, "tray allowed before level 11");
        tray.number = 11;
        Check(tray.ValidateData() == null, "valid tray rejected at level 11: " + tray.ValidateData());
        tray.boardColumns[0].foods[1].foodState = FoodState.RigidTray;
        tray.boardColumns[0].foods[2].foodState = FoodState.RigidTray;
        Check(tray.ValidateData() == null, "explicit rigid tray states rejected at level 11");
        Object.DestroyImmediate(tray);
    }
    private static void CreateLevel()
    {
        var obj = Node("Level").gameObject; obj.SetActive(false);
        level = obj.AddComponent<LevelManager>(); Set(level, "showGameplayHud", false);
        Set(level, "boardManager", board); Set(level, "customerManager", customers); Set(level, "conveyorManager", conveyor);
        obj.SetActive(true);
        var data = ValidLevel(); data.number = 20; data.customers[0].rushCustomer = true;
        var allLevels = ScriptableObject.CreateInstance<AllLevelData>();
        allLevels.listAllLevel = new List<SushiLevelData> { data };
        Set(level, "allLevelData", allLevels); level.LoadLevel(0);
    }

    private static Customer RecipeCustomer(params DishOrderData[] dishes)
    {
        var customer = Object.Instantiate(Prefab<Customer>("Customer"), fixture);
        customer.Setup(0, null, new CustomerOrderData { dishes = new List<DishOrderData>(dishes) });
        return customer;
    }

    private static FoodShushi ReserveIngredient(Customer customer, PoolType type)
    {
        FoodShushi unit = SimplePool.Spawn<FoodShushi>(type);
        unit.SetupFromData(Food(type));
        Check(unit.TryMoveToConveyor(), "unit could not move to belt");
        unit.ArriveAtConveyor();
        Check(customer.TryReserveFood(unit, out _), "dish rejected required " + type);
        return unit;
    }

    private static void Serve(Customer customer, PoolType type)
    {
        Check(customer.CompleteReceive(ReserveIngredient(customer, type)), "recipe delivery failed");
    }

    private static void RecipePortions()
    {
        var customer = RecipeCustomer(new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 2 },
            new DishOrderData { dish = SushiDishId.NigiriEgg, amount = 1 });
        int completedDishes = 0; customer.OnDishCompleted += (_, __) => completedDishes++;
        var firstRice = ReserveIngredient(customer, PoolType.SushiRice);
        var secondRice = ReserveIngredient(customer, PoolType.SushiRice);
        Check(customer.Dishes[0].Ingredients[0].incomingAmount == 1 && customer.Dishes[1].Ingredients[0].incomingAmount == 1,
            "rice reserved for wrong portion");
        Check(completedDishes == 0 && !customer.IsCompleted, "incoming ingredients completed order");
        Check(customer.CancelReceive(secondRice), "recipe reservation cancellation failed");
        SimplePool.Despawn(secondRice);
        Check(customer.Dishes[1].Ingredients[0].incomingAmount == 0, "dish retained cancelled incoming");
        Check(customer.CompleteReceive(firstRice), "rice delivery failed"); Serve(customer, PoolType.Shrimp);
        Check(completedDishes == 1 && customer.Dishes[0].IsComplete && !customer.IsCompleted, "portion completion wrong");
        Serve(customer, PoolType.SushiRice); Serve(customer, PoolType.Shrimp);
        Check(completedDishes == 2 && !customer.IsCompleted, "whole order completed before final dish");
        Serve(customer, PoolType.Egg); Serve(customer, PoolType.SushiRice);
        Check(completedDishes == 3 && customer.IsCompleted && customer.ReceivedFoods.Count == 0, "assembled ingredients not consumed");
    }

    private static void LargeRecipeOrder()
    {
        Load(24, new[] { Food(PoolType.Seaweed), Food(PoolType.TeaPot), Food(PoolType.EmptyCup), Food(PoolType.IceCube) });
        var customer = RecipeCustomer(new DishOrderData { dish = SushiDishId.MakiShrimp, amount = 4 });
        for (int i = 0; i < 4; i++) Serve(customer, PoolType.SushiRice);
        for (int i = 0; i < 4; i++) Serve(customer, PoolType.Seaweed);
        Check(!customer.IsCompleted, "Maki completed without topping");
        for (int i = 0; i < 4; i++) Serve(customer, PoolType.Shrimp);
        Check(customer.IsCompleted && customer.Dishes.Count == 4, "large recipe order blocked by legacy plate capacity");
    }

    private static void IcedTea()
    {
        var customer = RecipeCustomer(new DishOrderData { dish = SushiDishId.IcedGreenTea, amount = 1 });
        Check(!customer.CanReceive(PoolType.SushiRice), "tea accepted rice");
        Serve(customer, PoolType.TeaPot); Serve(customer, PoolType.EmptyCup);
        Check(!customer.IsCompleted, "iced tea completed without ice");
        Serve(customer, PoolType.IceCube); Check(customer.IsCompleted, "iced tea did not complete");
    }
    private static void CustomerDeliveryLayout()
    {
        Customer originalPrefab = Get<Customer>(customers, "customerPrefab");
        Customer actualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Prefabs/Customer.prefab").GetComponent<Customer>();
        Transform firstSeat = customers.GetSeat(0), secondSeat = customers.GetSeat(1);
        Vector3 firstPosition = firstSeat.position, secondPosition = secondSeat.position;
        firstSeat.position = new Vector3(-2.15f, 4.45f, 0);
        secondSeat.position = new Vector3(2.15f, 4.45f, 0);
        Set(customers, "customerPrefab", actualPrefab);
        var orderData = new List<CustomerOrderData>
        {
            new CustomerOrderData { dishes = new List<DishOrderData> { new DishOrderData { dish = SushiDishId.NigiriSalmon, amount = 1 } } },
            new CustomerOrderData { dishes = new List<DishOrderData> { new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 1 } } },
            new CustomerOrderData { dishes = new List<DishOrderData> { new DishOrderData { dish = SushiDishId.IcedGreenTea, amount = 1 } } }
        };
        customers.Init(orderData);
        Load(8, new[] { Food(PoolType.Shrimp) });
        Customer first = customers.ActiveCustomers[0], second = customers.ActiveCustomers[1];
        var waiting = Get<List<Customer>>(customers, "waitingCustomers")[0];
        Check(!waiting.IsSeated && Mathf.Abs(waiting.transform.localScale.x - 0.6f) < 0.001f, "waiting customer is not smaller");
        Check(!waiting.GetComponent<CustomerRecipeView>().CardAt(0).gameObject.activeSelf, "waiting order card is visible");
        var recipeSlots = Get<List<Transform>>(second, "recipeSlots");
        var icon = second.GetComponent<CustomerRecipeView>().CardAt(0).IngredientAt(1).Image;
        Check((recipeSlots[1].position - icon.transform.position).sqrMagnitude < 0.00001f,
            "delivery target does not match ingredient icon");
        Sprite expected = second.GetIngredientSprite(PoolType.Shrimp);
        Check(expected != null, "customer ingredient art missing");
        Check(icon.sprite == expected && icon.gameObject.activeInHierarchy, "customer does not display ingredient sprite");
        var gates = Get<float[]>(conveyor, "deliveryProgresses");
        var spline = Get<ConveyorSpline>(conveyor, "spline");
        Check(Mathf.Abs(spline.AtDistance(gates[1]).x - secondSeat.position.x) < 0.01f &&
            Mathf.Abs(gates[0] - gates[1]) > 0.05f, "conveyor gates do not follow seats");
        Check(Tap(0), "delivery pickup failed"); Advance();
        var slotFoods = Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods");
        Transform occupied = null; FoodShushi unit = null;
        foreach (var pair in slotFoods) { occupied = pair.Key; unit = pair.Value; break; }
        int slotIndex = 0;
        while (conveyor.Slots[slotIndex] != occupied) slotIndex++;
        float previous = Mathf.Repeat(gates[1] - (float)slotIndex / conveyor.Capacity - 0.005f, 1);
        Set(conveyor, "progress", Mathf.Repeat(previous + 0.01f, 1));
        Invoke(conveyor, "UpdateSlotPositions");
        typeof(ConveyorManager).GetMethod("CheckCustomerOrders", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(conveyor, new object[] { previous, 0.01f });
        var deliveries = Get<Dictionary<FoodShushi, Customer>>(conveyor, "deliveringFoods");
        Check(deliveries.TryGetValue(unit, out Customer receiver) && receiver == second && first.ReceivedFoods.Count == 0,
            "ingredient jumped to wrong customer");
        Vector3 deliveryScale = unit.transform.lossyScale;
        Advance(0.2f);
        Check((unit.transform.lossyScale - deliveryScale).sqrMagnitude < 0.0001f,
            "ingredient changed scale while flying to the order card");
        second.transform.position += Vector3.right * 0.25f;
        Advance();
        Check(second.ReceivedFoods.Count == 1 && first.ReceivedFoods.Count == 0 && unit.transform.parent == recipeSlots[1] &&
            (unit.transform.position - recipeSlots[1].position).sqrMagnitude < 0.00001f, "jump did not follow customer's current position");
        Check((unit.transform.lossyScale - deliveryScale).sqrMagnitude < 0.0001f,
            "ingredient changed scale when it landed in the order card");
        conveyor.Clear(); customers.ClearCustomers();
        firstSeat.position = firstPosition; secondSeat.position = secondPosition;
        Set(customers, "customerPrefab", originalPrefab);
    }
    private static void RushAndPopup()
    {
        CreateLevel();
        Customer first = customers.ActiveCustomers[0];
        Check(first.RushRemaining == 30, "rush formula wrong");
        customers.TickRush(2); Check(first.RushRemaining == 28, "seated timer did not run");
        var targeting = Node("Input").gameObject.AddComponent<FoodInputController>(); targeting.SelectMagnet();
        customers.TickRush(2); Check(first.RushRemaining == 26, "targeting paused timer");
        level.SetPopupOpen(true); customers.TickRush(10);
        Check(first.RushRemaining == 26 && Time.timeScale == 0, "popup did not pause timer");
        level.SetPopupOpen(false); Check(Time.timeScale == 1, "popup did not restore time");
        Customer waiting = Object.Instantiate(Prefab<Customer>("Customer"), fixture);
        var order = Order(PoolType.SushiRice, 2, true); waiting.Setup(4, null, order.CreateFoodList()); waiting.ConfigureRush(order);
        waiting.TickRush(10); Check(waiting.RushRemaining == 30, "waiting timer started early");
    }
    private static void Revive()
    {
        Customer first = customers.ActiveCustomers[0], second = customers.ActiveCustomers[1];
        customers.TickRush(100);
        Check(level.PendingRevive == SushiReviveReason.RushCustomer && level.ExpiredCustomer == first, "timeout did not open correct revive");
        float secondTime = second.RushRemaining;
        var provider = Node("Payment").gameObject.AddComponent<SushiRegressionPayment>(); Set(level, "reviveProvider", provider);
        Check(level.ReviveCost == 300 && level.TryReviveWithCoins(), "first revive failed");
        Check(provider.balance == 700 && first.RushRemaining == 30 && second.RushRemaining == secondTime && !level.IsPopupOpen, "revive target/payment wrong");
        customers.TickRush(40); Check(level.ReviveCost == 600 && level.TryReviveWithCoins() && provider.balance == 100, "second revive price wrong");
        customers.TickRush(40); Check(!level.TryReviveWithCoins() && level.IsPopupOpen, "unpaid revive allowed");
        level.TryReviveWithAd(); Check(!level.IsPopupOpen && first.RushRemaining == 30, "rewarded revive failed");
        Load(6, new[] { Food(ice: 3), Food(ice: 4) }, new[] { Food(ice: 2) });
        CheckLevelState(4);
        Check(level.PendingRevive == SushiReviveReason.None, "frozen deadlock skipped the grace period");
        CheckLevelState(1);
        Check(level.PendingRevive == SushiReviveReason.FrozenHeads, "frozen deadlock undetected");
        level.TryReviveWithAd();
        Check(Column(0).Foods[0].IceLayers == 0 && Column(1).Foods[0].IceLayers == 0 && Column(0).Foods[1].IceLayers == 4, "revive cleared non-head ice");
    }
    private static void CheckLevelState(float seconds) => typeof(LevelManager)
        .GetMethod("CheckLevelState", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(level, new object[] { seconds });

    private static void NoMovesDelay()
    {
        level.LoadLevel(0);
        Check(Get<float>(level, "noMovesLoseDelay") == 5f, "default loss delay is not five seconds");
        Load(1, new[] { Food(PoolType.Shrimp), Food(PoolType.Shrimp) });
        Check(Tap(0), "full belt setup failed");
        CheckLevelState(20);
        Check(level.IsPlaying && Get<float>(level, "noMovesElapsed") == 0, "pickup flight triggered loss");
        Advance();
        Check(!conveyor.CanMakeProgress && !board.HasAvailableTap(conveyor.FreeSlotCount), "belt is not blocked");
        CheckLevelState(4);
        Check(level.IsPlaying, "lost before five seconds");
        level.SetPopupOpen(true);
        CheckLevelState(20);
        Check(level.IsPlaying && Get<float>(level, "noMovesElapsed") == 0, "popup did not reset countdown");
        level.SetPopupOpen(false);
        CheckLevelState(4);
        Check(level.IsPlaying, "pause retained the previous countdown");
        CheckLevelState(1);
        Check(!level.IsPlaying && GameManager.CurrentGameState == GameState.Fail, "full belt did not lose at five seconds");
        CheckLevelState(20);
        Check(!level.IsPlaying, "failure resumed gameplay");

        level.LoadLevel(0);
        Load(3, new[] { Food(PoolType.Shrimp, 2) }, new[] { Food(PoolType.Egg, 2), Food() });
        Check(Tap(0), "partial capacity setup failed");
        Advance();
        CheckLevelState(4);
        Check(level.IsPlaying && Get<float>(level, "noMovesElapsed") == 4, "quantity overflow was not counted as blocked");
        Check(board.SwapRow(), "swap did not expose a smaller pickup");
        Advance();
        CheckLevelState(1);
        Check(level.IsPlaying && Get<float>(level, "noMovesElapsed") == 0, "legal booster move did not cancel loss");

        Load(1, new[] { Food(PoolType.Shrimp), Food(PoolType.Shrimp) });
        Check(Tap(0), "reload countdown setup failed");
        Advance();
        CheckLevelState(4);
        level.ReplayLevel();
        Check(Get<float>(level, "noMovesElapsed") == 0, "replay retained the old countdown");
        Load(1, new[] { Food(PoolType.Shrimp), Food(PoolType.Shrimp) });
        Check(Tap(0), "reloaded belt setup failed");
        Advance();
        CheckLevelState(4);
        Check(level.IsPlaying, "old countdown leaked into replay");
        CheckLevelState(1);
        Check(!level.IsPlaying, "reloaded deadlock did not lose");
        level.LoadLevel(0);
    }

    private static void NoMovesProgress()
    {
        level.LoadLevel(0);
        Load(1, new[] { Food(), Food(PoolType.Shrimp) });
        Check(Tap(0), "matching food setup failed");
        Advance();
        Check(conveyor.FreeSlotCount == 0 && conveyor.CanMakeProgress, "matching belt ingredient cannot progress");
        CheckLevelState(20);
        Check(level.IsPlaying && Get<float>(level, "noMovesElapsed") == 0,
            "lost while matching food was waiting to reach its customer");

        conveyor.Clear();
        customers.Init(new List<CustomerOrderData> {
            Order(PoolType.Shrimp, 1), Order(PoolType.Egg, 1), Order(PoolType.Salmon, 1)
        });
        Load(1, new[] { Food(PoolType.Salmon), Food(PoolType.Salmon) });
        Check(Tap(0), "customer transition setup failed");
        Advance();
        Serve(customers.ActiveCustomers[0], PoolType.Shrimp);
        Check(customers.HasPendingTransitions && conveyor.CanMakeProgress, "departing customer was not considered");
        CheckLevelState(20);
        Check(level.IsPlaying, "lost while a customer was leaving");
        Advance();
        Check(customers.HasPendingTransitions && conveyor.CanMakeProgress, "arriving customer was not considered");
        CheckLevelState(20);
        Check(level.IsPlaying, "lost while a customer was walking to a seat");
        Advance();
        Check(conveyor.CanMakeProgress, "new customer's matching food was not considered");
        level.LoadLevel(0);
    }

    private static void PartialDishDeadlock()
    {
        level.LoadLevel(0);
        Set(conveyor, "preparedDishPrefab", Prefab<SushiPreparedDish>("PreparedDish"));
        try
        {
            customers.Init(new List<CustomerOrderData> {
                new CustomerOrderData { dishes = new List<DishOrderData> {
                    new DishOrderData { dish = SushiDishId.NigiriShrimp, amount = 1 }
                } }
            });
            Load(1, new[] { Food(), Food(PoolType.Salmon), Food(PoolType.Shrimp) });
            Check(Tap(0), "partial dish rice setup failed");
            Advance();
            var foods = Get<Dictionary<Transform, FoodShushi>>(conveyor, "slotFoods");
            var pickup = new List<KeyValuePair<Transform, FoodShushi>>(foods)[0];
            Customer customer = customers.ActiveCustomers[0];
            SushiDishOrder order = customer.Dishes[0];
            Check(customer.CanReceivePreparedIngredient(order, pickup.Value.Type), "matching partial ingredient was rejected");
            Check(customer.TryReservePreparedIngredient(order, pickup.Value.Type, out Transform target), "rice reservation failed");
            var dish = (SushiPreparedDish)typeof(ConveyorManager)
                .GetMethod("GetOrCreatePreparedDish", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(conveyor, new object[] { customer, order, target });
            foods.Remove(pickup.Key);
            typeof(ConveyorManager).GetMethod("StartPreparedIngredientFlight", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(conveyor, new object[] { dish, pickup.Value, target });
            Check(conveyor.CanMakeProgress, "ingredient flight was not considered progress");
            Advance();
            Check(conveyor.DeliveringCount > 0 && !dish.IsReady && !conveyor.CanMakeProgress,
                "partial plate was incorrectly counted as an active delivery");
            Check(Tap(0), "unrelated ingredient setup failed");
            Advance();
            CheckLevelState(4);
            Check(level.IsPlaying, "partial plate deadlock lost too soon");
            CheckLevelState(1);
            Check(!level.IsPlaying, "partial plate hid a permanent full-belt deadlock");
        }
        finally
        {
            level.LoadLevel(0);
            Set(conveyor, "preparedDishPrefab", null);
        }
    }

    private static void ArrivalPriority()
    {
        customers.Init(new List<CustomerOrderData> { Order(PoolType.SushiRice, 2), Order(PoolType.SushiRice, 2, true) });
        Check(customers.FirstCustomerFor(PoolType.SushiRice).id == 0, "rush customer overtook old customer");
        var seats = Get<Customer[]>(customers, "activeCustomers");
        Customer old = seats[0]; seats[0] = seats[1]; seats[1] = old;
        Check(customers.FirstCustomerFor(PoolType.SushiRice) == old, "seat index overrode arrival order");
    }
    private static void Reload()
    {
        level.LoadLevel(0); Check(Tap(0), "reload pickup failed");
        level.LoadLevel(0);
        Check(conveyor.OccupiedSlotCount == 0 && conveyor.DeliveringCount == 0 && Column(0).GetTopFood().Amount == 2, "reload retained food/slots");
        Advance(); Check(conveyor.OccupiedSlotCount == 0 && Column(0).GetTopFood().Location == FoodLocation.Board, "stale tween changed reloaded food");
    }
}

public class SushiRegressionPayment : SushiReviveProvider
{
    public int balance = 1000;
    public override bool TrySpendCoins(int amount) { if (balance < amount) return false; balance -= amount; return true; }
    public override void ShowRewardedAd(Action<bool> onRewarded) { onRewarded(true); }
}
#endif
