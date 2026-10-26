using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit editor authoring. Runtime instantiates these saved prefabs and binds order data.
public static class SushiServingSetup
{
    private const string Prefabs = "Assets/_game/Prefabs/";
    private const string Gameplay = "Assets/_game/Scenes/GamePlay.unity";
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");
    private static readonly Color Ink = new Color(0.27f, 0.13f, 0.08f);
    private static readonly Color Cream = new Color(1, 0.96f, 0.82f);

    public static void RunBatch()
    {
        try { Setup(); Debug.Log("SERVING_PREFABS_OK"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("SushiKhai/Prefabs/Author Plated Dish Delivery")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring serving prefabs.");
        BuildDish(); EditCard(); EditCustomer(); BuildCounter(); EditHUD();
        var scene = EditorSceneManager.OpenScene(Gameplay);
        ConveyorManager conveyor = Object.FindFirstObjectByType<ConveyorManager>();
        Bind(conveyor, "preparedDishPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PreparedDish.prefab").GetComponent<SushiPreparedDish>());
        if (GameObject.Find("Serving counter") == null)
        {
            var counter = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ServingCounter.prefab"), scene);
            counter.name = "Serving counter"; counter.transform.position = new Vector3(0, 3.05f, 0);
        }
        EditorUtility.SetDirty(conveyor);
        EditorSceneManager.SaveScene(scene);
        SushiConveyorCounterSetup.Setup();
        AssetDatabase.SaveAssets();
    }

    private static void Bind(Object target, string field, Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static Transform Node(Transform parent, string name, Vector3 position)
    {
        var node = new GameObject(name).transform; node.SetParent(parent, false); node.localPosition = position; return node;
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    private static Image Image(Transform parent, string name, Vector2 size, Vector2 position, Color color, Sprite sprite = null)
    {
        Image image = Rect(parent, name, size, position).gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : Rounded;
        image.color = color; image.raycastTarget = false; image.type = UnityEngine.UI.Image.Type.Sliced;
        return image;
    }
    private static TMP_Text Text(Transform parent, string name, Vector2 size, Vector2 position, int font, string sample)
    {
        var text = Rect(parent, name, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset");
        text.fontSize = text.fontSizeMax = font; text.fontSizeMin = font * 0.7f; text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center; text.color = Ink; text.raycastTarget = false; text.text = sample;
        return text;
    }
    private static SpriteRenderer World(Transform parent, string name, Sprite sprite, Vector2 size, Color color, int order)
    {
        var image = Node(parent, name, Vector3.zero).gameObject.AddComponent<SpriteRenderer>();
        image.sprite = sprite; image.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_game/Materials/SushiSpriteMaterial.mat");
        image.color = color; image.sortingOrder = order;
        image.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        return image;
    }
    private static void Save(GameObject root, string name)
    { PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + ".prefab"); Object.DestroyImmediate(root); }

    private static void BuildDish()
    {
        var root = new GameObject("PreparedDish"); var view = root.AddComponent<SushiPreparedDish>();
        var plate = World(root.transform, "Slate serving plate", SushiArtCatalog.Default.plate,
            new Vector2(1.03f, 0.82f), new Color(0.24f, 0.35f, 0.45f), 3);
        var dish = World(root.transform, "Finished dish", SushiArtCatalog.Default.GetDish(SushiDishId.NigiriShrimp),
            new Vector2(0.86f, 0.7f), Color.white, 4);
        dish.transform.localPosition = new Vector3(0, 0.07f, 0);
        Bind(view, "plate", plate); Bind(view, "dishImage", dish); Save(root, "PreparedDish");
    }

    private static void EditCard()
    {
        string path = Prefabs + "DishCard.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<SushiDishCardView>();
            var config = new SerializedObject(view);
            var size = root.GetComponent<RectTransform>(); size.sizeDelta = new Vector2(103, 82);
            var layout = root.GetComponent<LayoutElement>(); layout.preferredHeight = 82; layout.minHeight = 45; layout.flexibleHeight = 0;
            var content = root.transform.Find("Content");
            var vertical = content.GetComponent<VerticalLayoutGroup>(); if (vertical != null) Object.DestroyImmediate(vertical);
            var title = config.FindProperty("dishName").objectReferenceValue as TMP_Text; title.gameObject.SetActive(false);
            content.Find("Ingredients").gameObject.SetActive(false); // Ingredient anchors retained for legacy data/booster compatibility.
            var background = config.FindProperty("background").objectReferenceValue as Image;
            background.color = new Color(1, 1, 1, 0);
            config.FindProperty("pendingColor").colorValue = new Color(1, 1, 1, 0);
            config.FindProperty("completedColor").colorValue = new Color(0.7f, 1, 0.6f, 0.2f);
            config.FindProperty("targetingColor").colorValue = new Color(1, 0.75f, 0.2f, 0.45f);
            config.ApplyModifiedPropertiesWithoutUndo();
            var dish = view.DishImage;
            ConfigureDishImage(dish);
            dish.preserveAspect = true;
            Transform oldCount = root.transform.Find("Portion count"); if (oldCount != null) Object.DestroyImmediate(oldCount.gameObject);
            var count = Text(root.transform, "Portion count", new Vector2(68, 29), new Vector2(59, -62), 24, "0/2");
            count.rectTransform.anchorMin = count.rectTransform.anchorMax = new Vector2(0, 1);
            const string countMaterialPath = "Assets/_game/Materials/SushiOrderCount.mat";
            var countMaterial = AssetDatabase.LoadAssetAtPath<Material>(countMaterialPath);
            if (countMaterial == null)
            {
                countMaterial = new Material(count.font.material) { name = "SushiOrderCount" };
                countMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
                countMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
                countMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
                AssetDatabase.CreateAsset(countMaterial, countMaterialPath);
            }
            count.fontSharedMaterial = countMaterial;
            count.color = Color.white;
            Bind(view, "portionCount", count);
            Transform target = dish.transform.Find("Dish arrival");
            if (target == null) target = Rect(dish.transform, "Dish arrival", Vector2.zero, Vector2.zero);
            target.localScale = Vector3.one / 0.013f; Bind(view, "dishDeliverySlot", target);
            var mark = root.transform.Find("Completed mark").GetComponent<RectTransform>();
            mark.anchorMin = mark.anchorMax = new Vector2(0, 1); mark.anchoredPosition = new Vector2(67, -57); mark.localScale = Vector3.one * 1.8f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureDishImage(Image dish)
    {
        dish.rectTransform.anchorMin = dish.rectTransform.anchorMax = new Vector2(0, 1);
        dish.rectTransform.sizeDelta = new Vector2(82, 65);
        dish.rectTransform.anchoredPosition = new Vector2(46, -35);
    }

    private static void EditCustomer()
    {
        string path = Prefabs + "Customer.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform visuals = root.transform.Find("CustomerVisuals");
            // Save overrides on Customer so its visual children stay editable alongside the serving anchors.
            var avatar = visuals.Find("Avatar").GetComponent<RectTransform>();
            avatar.sizeDelta = new Vector2(175, 245); avatar.anchoredPosition = new Vector2(-112, 20);
            var orders = visuals.Find("Orders").GetComponent<RectTransform>();
            orders.pivot = new Vector2(0.5f, 0.5f); orders.anchoredPosition = new Vector2(52, 64); orders.sizeDelta = new Vector2(225, 184);
            var panel = orders.GetComponent<Image>(); if (panel == null) panel = orders.gameObject.AddComponent<Image>();
            panel.sprite = Rounded; panel.type = UnityEngine.UI.Image.Type.Sliced; panel.color = Cream; panel.raycastTarget = false;
            var row = orders.GetComponent<HorizontalLayoutGroup>(); row.padding = new RectOffset(8, 23, 8, 8); row.spacing = 4;
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = true;
            foreach (string columnName in new[] { "Left column", "Right column" })
            {
                var column = orders.Find(columnName).GetComponent<VerticalLayoutGroup>(); column.spacing = 4;
                column.childForceExpandHeight = false;
            }
            // The old nested template contains layout-driven RectTransform overrides.
            // Update its icon too so it inherits the new grid rather than the old ingredient row.
            var recipeSettings = new SerializedObject(root.GetComponent<CustomerRecipeView>());
            var template = (SushiDishCardView)recipeSettings.FindProperty("cardTemplate").objectReferenceValue;
            ConfigureDishImage(template.DishImage);
            Transform indicator = visuals.Find("Rush visuals");
            var track = indicator.Find("Countdown track").GetComponent<RectTransform>();
            track.sizeDelta = new Vector2(9, 154); track.anchoredPosition = new Vector2(154, 64);
            var fill = track.Find("Fill").GetComponent<Image>(); fill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical; fill.fillOrigin = 0;
            var badge = indicator.Find("Timer badge").GetComponent<RectTransform>(); badge.anchoredPosition = new Vector2(65, -47); badge.sizeDelta = new Vector2(92, 28);
            var timer = badge.GetComponentInChildren<TMP_Text>(); timer.fontSize = timer.fontSizeMax = 24;
            Transform serving = root.transform.Find("Serving slots");
            if (serving != null) Object.DestroyImmediate(serving.gameObject);
            serving = Node(root.transform, "Serving slots", new Vector3(0, -1.55f, 0));
            var customer = root.GetComponent<Customer>(); Bind(customer, "servingRoot", serving);
            var settings = new SerializedObject(customer); var slots = settings.FindProperty("servingSlots"); slots.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = Node(serving, "Plate " + (i + 1), new Vector3((i - 1.5f) * 0.98f, 0, 0));
                var ring = anchor.gameObject.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
                ring.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_game/Materials/SushiLineMaterial.mat"); ring.sortingOrder = 2;
                ring.startWidth = ring.endWidth = 0.022f; ring.startColor = ring.endColor = new Color(1, 0.93f, 0.76f, 0.65f);
                ring.positionCount = 40;
                for (int j = 0; j < 40; j++) { float a = j * Mathf.PI * 2 / 40; ring.SetPosition(j, new Vector3(Mathf.Cos(a) * 0.43f, Mathf.Sin(a) * 0.28f, 0)); }
                slots.GetArrayElementAtIndex(i).objectReferenceValue = anchor;
            }
            settings.ApplyModifiedPropertiesWithoutUndo(); serving.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildCounter()
    {
        var root = new GameObject("ServingCounter");
        var shadow = World(root.transform, "Shadow", Rounded, new Vector2(9.4f, 0.94f), new Color(0.18f, 0.07f, 0.03f), -10);
        shadow.transform.localPosition = new Vector3(0, -0.12f, 0);
        World(root.transform, "Wood surface", Rounded, new Vector2(9.4f, 0.76f), new Color(0.78f, 0.40f, 0.17f), -9);
        var edge = World(root.transform, "Front edge", Rounded, new Vector2(9.4f, 0.08f), new Color(1, 0.7f, 0.33f), -8);
        edge.transform.localPosition = new Vector3(0, -0.33f, 0);
        Save(root, "ServingCounter");
    }

    private static void EditHUD()
    {
        string path = "Assets/_game/Resources/UI/UICGamePlay.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasGamePlay>();
            var capacity = root.transform.Find("Content/Header/Belt capacity");
            if (capacity != null) capacity.gameObject.SetActive(false);
            Bind(view, "occupancy", null); // One counter, authored at the centre of the conveyor in GamePlay.
            RectTransform header = root.transform.Find("Content/Header").GetComponent<RectTransform>();
            header.anchoredPosition = new Vector2(0, -80);
            var level = view.txtLevvel.rectTransform; level.anchoredPosition = new Vector2(-450, 0); level.sizeDelta = new Vector2(105, 105);
            view.txtLevvel.color = Ink; view.txtLevvel.fontSize = view.txtLevvel.fontSizeMax = 34;
            Transform old = header.Find("Level badge"); if (old != null) Object.DestroyImmediate(old.gameObject);
            Image badge = Image(header, "Level badge", level.sizeDelta, level.anchoredPosition, Cream); badge.transform.SetAsFirstSibling();
            old = header.Find("Difficulty"); if (old != null) Object.DestroyImmediate(old.gameObject);
            Bind(view, "difficulty", Text(header, "Difficulty", new Vector2(140, 38), new Vector2(-450, -68), 23, "HARD"));
            var speed = header.Find("Speed").GetComponent<RectTransform>(); speed.anchoredPosition = new Vector2(-98, 0); speed.sizeDelta = new Vector2(65, 50);
            old = header.Find("Customers progress"); if (old != null) Object.DestroyImmediate(old.gameObject);
            Image progress = Image(header, "Customers progress", new Vector2(390, 78), new Vector2(155, 0), Cream);
            Text(progress.transform, "Guests", new Vector2(70, 50), new Vector2(-149, 0), 25, "KHÁCH");
            Image track = Image(progress.transform, "Track", new Vector2(190, 26), new Vector2(-5, 0), Ink);
            Image fill = Image(track.transform, "Fill", new Vector2(182, 18), Vector2.zero, new Color(0.42f, 0.78f, 0.18f));
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0;
            Bind(view, "customerProgressFill", fill);
            Bind(view, "customerProgress", Text(progress.transform, "Count", new Vector2(82, 60), new Vector2(144, 0), 31, "0/12"));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        path = "Assets/_game/Resources/UI/UICFloating.prefab"; root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasFloating>(); view.TopGroup.anchoredPosition = new Vector2(0, -80);
            var gold = view.GoldFr.GetComponent<RectTransform>(); gold.anchorMin = gold.anchorMax = new Vector2(0.5f, 0.5f);
            gold.anchoredPosition = new Vector2(-270, 0); gold.sizeDelta = new Vector2(230, 78);
            var settings = view.btnSettingIngame.GetComponent<RectTransform>(); settings.anchorMin = settings.anchorMax = new Vector2(0.5f, 0.5f);
            settings.anchoredPosition = new Vector2(455, 0); settings.sizeDelta = new Vector2(85, 85);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
