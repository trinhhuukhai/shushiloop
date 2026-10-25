using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Editor-only authoring. The saved prefab supplies all tutorial UI at runtime.
public static class SushiTutorialSetup
{
    private const string PrefabPath = "Assets/_game/Resources/UI/UICTutorial.prefab";
    private const string HandPath = "Assets/_game/Art/UI/Tutorial/TutorialHand.png";
    private const string LoadingPath = "Assets/_game/Scenes/Loading.unity";
    private const string GameplayPath = "Assets/_game/Scenes/GamePlay.unity";
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset");

    [MenuItem("SushiKhai/UI/Build Level One Tutorial")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring tutorial UI.");
        ImportHand();
        BuildPrefab();

        var loading = EditorSceneManager.OpenScene(LoadingPath);
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        DataManager data = Object.FindFirstObjectByType<DataManager>();
        if (ui == null || data == null || data.allLevelData == null || data.allLevelData.listAllLevel.Count == 0)
            throw new InvalidOperationException("Loading needs UIManager and a populated AllLevelData.");
        SushiLevelData first = data.allLevelData.listAllLevel[0];
        ChooseTarget(first, out int column, out PoolType ingredient, out int customer, out int dish);
        var registered = ui.uiResources.Where(item => item != null && !(item is CanvasTutorial)).ToList();
        registered.Add(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<CanvasTutorial>());
        ui.uiResources = registered.ToArray();
        EditorUtility.SetDirty(ui);
        EditorSceneManager.SaveScene(loading);

        var gameplay = EditorSceneManager.OpenScene(GameplayPath);
        LevelManager level = Object.FindFirstObjectByType<LevelManager>();
        if (level == null) throw new InvalidOperationException("GamePlay needs LevelManager.");
        SushiLevelOneTutorial tutorial = level.GetComponent<SushiLevelOneTutorial>();
        if (tutorial == null) tutorial = level.gameObject.AddComponent<SushiLevelOneTutorial>();
        var config = new SerializedObject(tutorial);
        config.FindProperty("targetColumn").intValue = column;
        config.FindProperty("targetIngredient").intValue = (int)ingredient;
        config.FindProperty("targetCustomerIndex").intValue = customer;
        config.FindProperty("targetDishIndex").intValue = dish;
        config.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tutorial);
        EditorSceneManager.SaveScene(gameplay);
        AssetDatabase.SaveAssets();
        Debug.Log($"TUTORIAL_SETUP_OK column={column}, ingredient={ingredient}, customer={customer}, dish={dish}");
    }

    private static void ImportHand()
    {
        AssetDatabase.ImportAsset(HandPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(HandPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing tutorial hand: " + HandPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    private static void BuildPrefab()
    {
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) :
            new GameObject("UICTutorial", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var host = new GameObject("Tutorial authoring canvas", typeof(RectTransform), typeof(Canvas));
        if (existing) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host, root.scene);
        host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        try
        {
            // Nested root authoring keeps the serialized scale at one instead of a screen-derived zero scale.
            root.transform.SetParent(host.transform, false);
            root.layer = 5;
            RectTransform rect = (RectTransform)root.transform;
            Stretch(rect);
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            var view = root.GetComponent<CanvasTutorial>();
            if (view == null) view = root.AddComponent<CanvasTutorial>();

            // Keep the detailed saved layout on repeat authoring runs.
            if (new SerializedObject(view).FindProperty("overlayRoot").objectReferenceValue == null)
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                BuildContent(view);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            if (existing)
            {
                root.transform.SetParent(null, false);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else Object.DestroyImmediate(root);
            if (host != null) Object.DestroyImmediate(host);
        }
    }

    private static void BuildContent(CanvasTutorial view)
    {
        RectTransform overlay = Rect(view.transform, "Tutorial overlay", Vector2.zero, Vector2.zero);
        Stretch(overlay);
        var shades = new RectTransform[4];
        string[] shadeNames = { "Shade left", "Shade right", "Shade top", "Shade bottom" };
        for (int i = 0; i < shades.Length; i++)
        {
            Image shade = Image(overlay, shadeNames[i], new Vector2(540, 960), Vector2.zero, new Color(0.08f, 0.035f, 0.015f, 0.70f), false);
            shade.raycastTarget = true;
            shades[i] = shade.rectTransform;
        }
        // A useful initial preview; CanvasTutorial places these around the live world target.
        shades[0].sizeDelta = shades[1].sizeDelta = new Vector2(240, 1920);
        shades[0].anchoredPosition = new Vector2(-420, 0);
        shades[1].anchoredPosition = new Vector2(420, 0);
        shades[2].sizeDelta = new Vector2(600, 460);
        shades[2].anchoredPosition = new Vector2(0, 730);
        shades[3].sizeDelta = new Vector2(600, 1260);
        shades[3].anchoredPosition = new Vector2(0, -330);

        Image focusTarget = Image(overlay, "Tutorial target", new Vector2(600, 200), new Vector2(0, 400), Color.clear, false);
        focusTarget.raycastTarget = true;
        Button target = focusTarget.gameObject.AddComponent<Button>();
        target.targetGraphic = focusTarget;
        target.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(target.onClick, view.OnTargetClick);
        Image focus = Image(target.transform, "Gold focus frame", Vector2.zero, Vector2.zero, new Color(1f, 0.79f, 0.25f), true);
        Stretch(focus.rectTransform);
        focus.rectTransform.offsetMin = new Vector2(-10, -10);
        focus.rectTransform.offsetMax = new Vector2(10, 10);
        focus.fillCenter = false;

        Image hand = Image(overlay, "Pointing hand", new Vector2(165, 190), new Vector2(160, 330), Color.white, false);
        hand.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HandPath);
        hand.preserveAspect = true;
        hand.rectTransform.pivot = new Vector2(0.16f, 0.88f);

        Image panel = Image(overlay, "Instruction panel", new Vector2(850, 200), new Vector2(0, 75), new Color(1f, 0.94f, 0.80f), true);
        panel.raycastTarget = true;
        Image rim = Image(panel.transform, "Gold panel rim", Vector2.zero, Vector2.zero, new Color(0.96f, 0.69f, 0.25f), true);
        Stretch(rim.rectTransform);
        rim.fillCenter = false;
        TMP_Text title = Label(panel.transform, "Title", "Xem món khách cần", new Vector2(800, 52), new Vector2(0, 58), 40);
        TMP_Text hint = Label(panel.transform, "Hint", "Chạm vào món được đánh dấu để xem nguyên liệu.", new Vector2(785, 72), new Vector2(0, -4), 29);
        TMP_Text step = Label(panel.transform, "Step", "HƯỚNG DẪN · 1/2", new Vector2(760, 30), new Vector2(0, -74), 23);
        step.color = new Color(0.53f, 0.29f, 0.12f);
        Bind(view, "overlayRoot", overlay);
        BindArray(view, "shades", new[] { shades[3], shades[2], shades[0], shades[1] });
        Bind(view, "targetButton", target);
        Bind(view, "focusFrame", focus.rectTransform);
        Bind(view, "hand", hand.rectTransform);
        Bind(view, "instructionPanel", panel.rectTransform);
        Bind(view, "titleText", title);
        Bind(view, "hintText", hint);
        Bind(view, "stepText", step);
    }

    private static void ChooseTarget(SushiLevelData first, out int column, out PoolType ingredient, out int customer, out int dish)
    {
        if (first == null) throw new InvalidOperationException("The first level is missing.");
        List<FoodQueueData> board = first.CreateBoardData();
        IEnumerable<int> candidates = Enumerable.Range(0, board.Count).OrderBy(index => index == 1 ? -1 : index);
        foreach (int index in candidates)
        {
            if (board[index].foods.Count == 0) continue;
            FoodData food = board[index].foods[0];
            if (food.amount != 1 || food.hidden || food.foodState != FoodState.Normal ||
                food.iceLayers != 0 || food.linkId != 0 || food.lockId != 0 || food.trayId != 0) continue;
            for (int customerIndex = 0; customerIndex < Mathf.Min(2, first.customers.Count); customerIndex++)
            {
                CustomerOrderData order = first.customers[customerIndex];
                if (order == null || !order.UsesRecipes) continue;
                int expandedDishIndex = 0;
                foreach (DishOrderData requested in order.dishes)
                {
                    if (SushiRecipeCatalog.Get(requested.dish).Ingredients.Contains(food.typeFood))
                    {
                        column = index; ingredient = food.typeFood; customer = customerIndex; dish = expandedDishIndex;
                        return;
                    }
                    expandedDishIndex += requested.amount;
                }
            }
        }
        throw new InvalidOperationException("Level 1 needs a plain head tile with amount 1 matching one of the first two customers.");
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = Vector2.one * 0.5f;
        rect.localScale = Vector3.one;
    }

    private static Image Image(Transform parent, string name, Vector2 size, Vector2 position, Color color, bool rounded)
    {
        var image = Rect(parent, name, size, position).gameObject.AddComponent<Image>();
        image.sprite = rounded ? Rounded : null;
        image.type = rounded ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.color = color; image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(Transform parent, string name, string text, Vector2 size, Vector2 position, float sizeFont)
    {
        var textLabel = Rect(parent, name, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        textLabel.font = Font;
        textLabel.text = text;
        textLabel.fontSize = textLabel.fontSizeMax = sizeFont;
        textLabel.fontSizeMin = sizeFont * 0.8f;
        textLabel.enableAutoSizing = true;
        textLabel.color = new Color(0.25f, 0.12f, 0.045f);
        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.raycastTarget = false;
        return textLabel;
    }

    private static void Bind(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BindArray(Object target, string field, RectTransform[] values)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
