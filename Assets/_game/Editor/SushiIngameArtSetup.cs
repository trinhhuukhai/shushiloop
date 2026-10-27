using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit authoring command. Never runs on import or in a player build.
public static class SushiIngameArtSetup
{
    private const string Art = "Assets/_game/Art/ingame/";
    private const string Prefabs = "Assets/_game/Prefabs/";
    private static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Art + name + ".png").OfType<Sprite>().First();
    private static Material WorldMaterial => AssetDatabase.LoadAssetAtPath<Material>("Assets/_game/Materials/SushiSpriteMaterial.mat");
    private static void Bind(Object owner, string field, Object value)
    {
        var data = new SerializedObject(owner); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Edit(string path, Action<GameObject> action)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { action(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void Fit(SpriteRenderer renderer, string art, Vector2 size)
    {
        renderer.sprite = Sprite(art); renderer.color = Color.white; renderer.sharedMaterial = WorldMaterial;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.transform.localScale = new Vector3(size.x / renderer.sprite.bounds.size.x, size.y / renderer.sprite.bounds.size.y, 1);
    }
    private static SpriteRenderer World(Transform parent, string name, string art, Vector2 size, Vector2 position, int order)
    {
        Transform child = parent.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        Fit(renderer, art, size); child.localPosition = position; renderer.sortingOrder = order; return renderer;
    }
    private static Image Picture(Transform parent, string name, string art, Vector2 size, Vector2 position)
    {
        Transform child = parent.Find(name);
        if (child == null) { child = new GameObject(name, typeof(RectTransform)).transform; child.SetParent(parent, false); }
        var image = child.GetComponent<Image>();
        if (image == null) image = child.gameObject.AddComponent<Image>();
        image.sprite = Sprite(art); image.color = Color.white; image.type = Image.Type.Simple; image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(.5f, .5f);
        image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = position; return image;
    }
    public static void RunBatch()
    {
        try { Setup(); Debug.Log("INGAME_ART_AUTHORED_OK"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    [MenuItem("SushiKhai/Art/Apply Ingame Art To Scene And Prefabs")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring the scene.");
        var catalog = SushiArtCatalog.Default;
        catalog.boardTile = Sprite("tray"); catalog.plate = Sprite("plate");
        catalog.orderCard = Sprite("order_board"); catalog.conveyor = Sprite("conveyor");
        catalog.customerAvatars = new[] { Sprite("char2"), Sprite("char1"), Sprite("char3") };
        EditorUtility.SetDirty(catalog);
        EditFood(); EditCustomer(); EditHUD();
        Edit(Prefabs + "Dia.prefab", root => Fit(root.transform.Find("Ceramic plate").GetComponent<SpriteRenderer>(), "plate", new Vector2(1.03f, .85f)));
        Edit(Prefabs + "PreparedDish.prefab", root =>
        {
            var view = root.GetComponent<SushiPreparedDish>();
            var plate = (SpriteRenderer)new SerializedObject(view).FindProperty("plate").objectReferenceValue;
            Fit(plate, "serve_plate", new Vector2(1.03f, .72f));
        });
        Edit(Prefabs + "ServingCounter.prefab", root =>
        {
            foreach (Transform child in root.transform) child.gameObject.SetActive(false);
            var counter = World(root.transform, "Serving table art", "serve_table", new Vector2(9.6f, 6.04f), new Vector2(0, -2.72f), -25);
            counter.gameObject.SetActive(true);
        });
        AuthorScene(); AssetDatabase.SaveAssets();
    }
    private static void EditFood()
    {
        Edit(Prefabs + "Ingredient.prefab", root =>
        {
            var food = root.GetComponent<FoodShushi>(); var data = new SerializedObject(food);
            Fit((SpriteRenderer)data.FindProperty("tileBackground").objectReferenceValue, "tray", new Vector2(1.26f, 1.09f));
            var badge = World(root.transform, "Quantity badge", "x2", new Vector2(.43f, .46f), new Vector2(.4f, -.4f), 6);
            Bind(food, "quantityBadge", badge); Bind(food, "quantityTwo", Sprite("x2")); Bind(food, "quantityThree", Sprite("x3"));
            badge.gameObject.SetActive(false);
        });
        Edit(Prefabs + "FoodQueue.prefab", root =>
        {
            Transform belt = root.transform.Find("Column conveyor");
            Fit(belt.Find("Belt").GetComponent<SpriteRenderer>(), "conveyor1", new Vector2(1.42f, 5.2f));
            Fit(belt.Find("Left rail").GetComponent<SpriteRenderer>(), "line2", new Vector2(.09f, 5.2f));
            Fit(belt.Find("Right rail").GetComponent<SpriteRenderer>(), "line2", new Vector2(.09f, 5.2f));
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name.StartsWith("Empty belt base")) Fit(renderer, "tray", new Vector2(1.26f, 1.09f));
            World(belt, "Head selection", "tray_selecct", new Vector2(1.32f, 1.15f), Vector2.zero, 0);
            for (int row = 0; row < 4; row++)
                World(belt, "Belt seam " + row, "conveyor1_line", new Vector2(1.4f, .045f), new Vector2(0, -.65f - row * 1.3f), -3);
        });
        Edit(Prefabs + "DishCard.prefab", root =>
        {
            Transform mark = root.transform.Find("Completed mark");
            foreach (Transform child in mark) child.gameObject.SetActive(false);
            var tick = Picture(mark, "Tick art", "tick", new Vector2(30, 28), Vector2.zero); tick.preserveAspect = true;
        });
    }
    private static void EditCustomer()
    {
        Edit(Prefabs + "Customer.prefab", root =>
        {
            var visual = root.transform.Find("CustomerVisuals");
            // Keep this Customer-specific layout editable without reparenting a locked nested instance.
            if (PrefabUtility.IsAnyPrefabInstanceRoot(visual.gameObject))
                PrefabUtility.UnpackPrefabInstance(visual.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var orders = visual.Find("Orders").GetComponent<Image>();
            orders.sprite = Sprite("order_board_2x2"); orders.type = Image.Type.Simple; orders.color = Color.white;
            var recipeSettings = new SerializedObject(root.GetComponent<CustomerRecipeView>());
            recipeSettings.FindProperty("oneColumnTwoRowsBoard").objectReferenceValue = Sprite("order_board_1x2");
            recipeSettings.FindProperty("twoColumnTwoRowsBoard").objectReferenceValue = Sprite("order_board_2x2");
            recipeSettings.FindProperty("twoColumnThreeRowsBoard").objectReferenceValue = Sprite("order_board_2x3");
            recipeSettings.ApplyModifiedPropertiesWithoutUndo();
            // Crop the supplied full-body characters at the serving table using an authored viewport.
            var avatar = (Image)new SerializedObject(root.GetComponent<Customer>()).FindProperty("avatarImage").objectReferenceValue;
            Transform viewport = visual.Find("Portrait viewport");
            if (viewport == null)
            {
                viewport = new GameObject("Portrait viewport", typeof(RectTransform), typeof(RectMask2D)).transform;
                viewport.SetParent(visual, false); viewport.SetAsFirstSibling();
            }
            var rect = (RectTransform)viewport; rect.sizeDelta = new Vector2(175, 240); rect.anchoredPosition = new Vector2(-92, 16);
            Canvas avatarCanvas = viewport.GetComponent<Canvas>();
            if (avatarCanvas == null) avatarCanvas = viewport.gameObject.AddComponent<Canvas>();
            avatarCanvas.renderMode = RenderMode.WorldSpace;
            avatarCanvas.overrideSorting = true;
            avatarCanvas.sortingOrder = 9;
            Canvas ordersCanvas = orders.GetComponent<Canvas>();
            if (ordersCanvas == null) ordersCanvas = orders.gameObject.AddComponent<Canvas>();
            ordersCanvas.renderMode = RenderMode.WorldSpace;
            ordersCanvas.overrideSorting = true;
            ordersCanvas.sortingOrder = 11;
            avatar.transform.SetParent(viewport, false); avatar.preserveAspect = true;
            avatar.rectTransform.anchorMin = avatar.rectTransform.anchorMax = new Vector2(.5f, 1);
            avatar.rectTransform.pivot = new Vector2(.5f, 1); avatar.rectTransform.sizeDelta = new Vector2(205, 610);
            avatar.rectTransform.anchoredPosition = new Vector2(0, 0);
            var track = visual.Find("Rush visuals/Countdown track").GetComponent<Image>(); track.sprite = Sprite("fr_time"); track.type = Image.Type.Simple; track.color = Color.white;
            var fill = track.transform.Find("Fill").GetComponent<Image>(); fill.sprite = Sprite("time_green"); fill.color = Color.white;
            var rush = new SerializedObject(root.GetComponent<RushCustomerView>());
            rush.FindProperty("normalColor").colorValue = Color.white;
            rush.FindProperty("warningColor").colorValue = new Color(1, .7f, .25f);
            rush.FindProperty("urgentColor").colorValue = new Color(1, .25f, .2f);
            rush.FindProperty("normalFill").objectReferenceValue = Sprite("time_green");
            rush.FindProperty("warningFill").objectReferenceValue = Sprite("time_orange");
            rush.FindProperty("urgentFill").objectReferenceValue = Sprite("time_red");
            rush.ApplyModifiedPropertiesWithoutUndo();
            Transform serving = root.transform.Find("Serving slots"); serving.localPosition = new Vector3(0, -1.85f, 0);
            foreach (Transform slot in serving)
            {
                var line = slot.GetComponent<LineRenderer>(); if (line != null) Object.DestroyImmediate(line);
                World(slot, "Empty slot art", "serve_empty_slot", new Vector2(.88f, .57f), Vector2.zero, 2);
            }
        });
    }
    private static void EditHUD()
    {
        Edit("Assets/_game/Resources/UI/UICGamePlay.prefab", root =>
        {
            var view = root.GetComponent<CanvasGamePlay>(); var header = root.transform.Find("Content/Header");
            root.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var badge = Picture(header, "Level badge", "fr_level_normal", new Vector2(126, 122), new Vector2(-450, -5)); badge.transform.SetAsFirstSibling();
            Bind(view, "levelBadge", badge); Bind(view, "normalLevelBadge", Sprite("fr_level_normal"));
            Bind(view, "hardLevelBadge", Sprite("fr_level_hard")); Bind(view, "superHardLevelBadge", Sprite("fr_level_sphard"));
            var progress = Picture(header, "Customers progress", "fr_progress", new Vector2(440, 80), new Vector2(150, 0));
            var title = progress.transform.Find("Guests"); if (title != null) title.gameObject.SetActive(false);
            Picture(progress.transform, "People icon", "ic_people", new Vector2(65, 66), new Vector2(-167, 5));
            var track = Picture(progress.transform, "Track", "progress1", new Vector2(245, 27), new Vector2(-5, 0));
            var fill = Picture(track.transform, "Fill", "progress2", new Vector2(239, 21), Vector2.zero);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            Bind(view, "customerProgressFill", fill);
            ((RectTransform)progress.transform.Find("Count")).anchoredPosition = new Vector2(167, 0);
            var speed = header.Find("Speed").GetComponent<RectTransform>(); speed.anchoredPosition = new Vector2(-102, 0); speed.sizeDelta = new Vector2(50, 44);
        });
        Edit("Assets/_game/Resources/UI/UICFloating.prefab", root =>
        {
            var view = root.GetComponent<CanvasFloating>(); root.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var gold = view.GoldFr.GetComponent<Image>(); gold.sprite = Sprite("fr_coin"); gold.color = Color.white; gold.type = Image.Type.Simple;
            gold.rectTransform.anchoredPosition = new Vector2(-255, 0); gold.rectTransform.sizeDelta = new Vector2(204, 82);
            var coin = view.GoldFr.transform.Find("gold").GetComponent<Image>(); coin.sprite = Sprite("coin"); coin.color = Color.white; coin.preserveAspect = true;
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(.5f, .5f);
            coin.rectTransform.sizeDelta = new Vector2(82, 89); coin.rectTransform.anchoredPosition = new Vector2(-84, 0);
            var plus = view.btnPlus.GetComponent<Image>(); plus.sprite = Sprite("plus"); plus.color = Color.white;
            var settings = view.btnSettingIngame.GetComponent<Image>(); settings.sprite = Sprite("bt_setting"); settings.color = Color.white; settings.type = Image.Type.Simple;
            foreach (var child in view.btnSettingIngame.GetComponentsInChildren<Image>(true)) if (child != settings) child.gameObject.SetActive(false);
        });
    }
    private static void AuthorScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_game/Scenes/GamePlay.unity");
        foreach (var old in Object.FindObjectsByType<SushiRestaurantView>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(old);
        Transform art = GameObject.Find("Ingame art")?.transform;
        if (art == null) art = new GameObject("Ingame art").transform;
        World(art, "Background", "background", new Vector2(14, 30), Vector2.zero, -60);
        World(art, "Restaurant backdrop", "top_bg", new Vector2(9.6f, 5.6f), new Vector2(0, 5.8f), -50);
        World(art, "Conveyor table", "conveyor_table", new Vector2(9.6f, 4.89f), new Vector2(0, -.4f), -24);
        World(art, "Conveyor frame", "conveyor", new Vector2(8.6f, 3.93f), new Vector2(0, .35f), -16);
        var counter = GameObject.Find("Serving counter");
        if (counter == null) counter = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ServingCounter.prefab"), scene);
        counter.name = "Serving counter"; counter.transform.SetParent(art, true); counter.transform.position = new Vector3(0, 3.0f, 0);
        var manager = Object.FindFirstObjectByType<CustomerManager>(); var data = new SerializedObject(manager);
        var avatars = data.FindProperty("avatars"); avatars.arraySize = 3;
        for (int i = 0; i < 3; i++) avatars.GetArrayElementAtIndex(i).objectReferenceValue = SushiArtCatalog.Default.customerAvatars[i];
        var campaign = AssetDatabase.LoadAssetAtPath<AllLevelData>("Assets/_game/LevelData/_AllLevelData.asset");
        int requiredWaiting = campaign.listAllLevel.Max(level => level.CustomerCount) - 2;
        var waiting = data.FindProperty("waitingPositions");
        while (waiting.arraySize < requiredWaiting)
        {
            int index = waiting.arraySize;
            var marker = new GameObject("Authored waiting " + (index + 1)).transform;
            marker.SetParent(manager.transform, false); marker.localPosition = new Vector3(6 + index * 1.2f, 5.83f, 0);
            waiting.arraySize++; waiting.GetArrayElementAtIndex(index).objectReferenceValue = marker;
        }
        // Existing overflow markers are off the phone viewport; keep them off tablets too.
        for (int i = 0; i < waiting.arraySize; i++)
        {
            var marker = (Transform)waiting.GetArrayElementAtIndex(i).objectReferenceValue;
            if (marker.position.x > 4.5f && marker.position.x < 14f)
                marker.position = new Vector3(14 + i * 1.2f, marker.position.y, marker.position.z);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        var board = Object.FindFirstObjectByType<BoardShishiManager>(); board.transform.position = new Vector3(0, -2.15f, 0);
        var boardData = new SerializedObject(board); boardData.FindProperty("firstRowOffset").floatValue = 0; boardData.ApplyModifiedPropertiesWithoutUndo();
        Transform ui = GameObject.Find("Ingame UI")?.transform;
        if (ui == null) ui = new GameObject("Ingame UI").transform;
        //var binding = ui.GetComponent<SushiSceneUI>();
        //if (binding == null) binding = ui.gameObject.AddComponent<SushiSceneUI>();
        //foreach (string name in new[] { "UICGamePlay", "UICFloating" })
        //{
        //    var instance = ui.Find(name)?.gameObject;
        //    if (instance == null)
        //    {
        //        instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/" + name + ".prefab"), scene);
        //        instance.transform.SetParent(ui, false);
        //    }
        //    instance.SetActive(true); Bind(binding, name == "UICGamePlay" ? "gameplay" : "floating", instance.GetComponent<UICanvas>());
        //}
        EditorSceneManager.SaveScene(scene);
    }
}
