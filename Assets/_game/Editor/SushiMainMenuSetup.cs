using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit authoring tool: every image, button, text and callback is saved in the prefab.
public static class SushiMainMenuSetup
{
    private const string Art = "Assets/_game/Art/UI/MainMenu/";
    private const string Prefabs = "Assets/_game/Resources/UI/";
    private static readonly Color Cream = new Color(1, 0.95f, 0.82f);
    private static readonly Color Ink = new Color(0.23f, 0.12f, 0.07f);
    private static readonly Color Gold = new Color(1, 0.73f, 0.19f);
    private static readonly Color Green = new Color(0.18f, 0.52f, 0.36f);
    private static readonly Color Navy = new Color(0.14f, 0.27f, 0.45f);
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");
    private static Sprite Circle => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset");
    [Serializable] private class Layout { public Atlas[] atlases; }
    [Serializable] private class Atlas { public string file; public Slice[] sprites; }
    [Serializable] private class Slice { public string name; public int x, y, width, height; }

    public static void RunBatch()
    {
        try { Setup(); Debug.Log("MAIN_MENU_SETUP_OK"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("SushiKhai/UI/Build Main Menu and Tabs")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring prefabs.");
        ImportArt();
        SushiHomePageSetup.Setup();
        BuildMainMenu();
        BuildTransit();
        BindSettings();
        var scene = EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        var manager = Object.FindFirstObjectByType<UIManager>();
        if (manager == null) throw new InvalidOperationException("Loading needs UIManager.");
        var registered = manager.uiResources.Where(p => p != null).ToList();
        foreach (string name in new[] { "UICMainMenu", "UICShop", "UICLeaderBoard", "UICDecor", "UICTransis" })
        {
            var ui = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab").GetComponent<UICanvas>();
            registered.RemoveAll(p => p.GetType() == ui.GetType());
            registered.Add(ui);
        }
        manager.uiResources = registered.ToArray();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ImportArt()
    {
        Sprites.Clear();
        Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Art + "AtlasLayout.json"));
        foreach (Atlas atlas in layout.atlases)
        {
            string path = Art + atlas.file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.userData != "sushi-menu-v1")
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects();
                var rects = atlas.sprites.Select(s => new SpriteRect {
                    name = s.name, rect = new Rect(s.x, s.y, s.width, s.height),
                    pivot = Vector2.one * 0.5f, alignment = SpriteAlignment.Center,
                    spriteID = Array.Find(old, r => r.name == s.name)?.spriteID ?? GUID.Generate()
                }).ToArray();
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
                    rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                provider.Apply(); importer.userData = "sushi-menu-v1"; importer.SaveAndReimport();
            }
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) Sprites[sprite.name] = sprite;
        }
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position, Vector2? anchor = null)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor ?? Vector2.one * 0.5f;
        rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color, Sprite sprite = null)
    {
        var go = Rect(parent, name, size, position).gameObject;
        Image image = sprite == Circle ? go.AddComponent<SushiUICircle>() : go.AddComponent<Image>();
        image.sprite = sprite == Circle ? null : sprite != null ? sprite : Rounded;
        image.type = sprite == null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color; image.raycastTarget = false; return image;
    }
    private static Image Icon(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var image = Panel(parent, name, size, position, Color.white, Sprites[name]);
        image.preserveAspect = true; return image;
    }
    private static TMP_Text Label(Transform parent, string name, string text, Vector2 size, Vector2 pos, float fontSize = 32, Color? color = null)
    {
        var label = Rect(parent, name, size, pos).gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) label.font = Font;
        label.text = text; label.fontSize = label.fontSizeMax = fontSize; label.fontSizeMin = fontSize * 0.72f;
        label.enableAutoSizing = true; label.alignment = TextAlignmentOptions.Center;
        label.color = color ?? Ink; label.raycastTarget = false; return label;
    }
    private static Button Button(Transform parent, string name, Vector2 size, Vector2 position, Color color, UnityAction action, Sprite shape = null)
    {
        var image = Panel(parent, name, size, position, color, shape); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1, 0.96f, 0.88f);
        colors.pressedColor = new Color(0.82f, 0.85f, 0.83f); colors.fadeDuration = 0.08f; button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(button.onClick, action); return button;
    }
    private static void Bind(Object target, string field, Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void BindArray(Object target, string field, Object[] values)
    {
        var data = new SerializedObject(target); var list = data.FindProperty(field); list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }


    private static void BuildMainMenu()
    {
        string path = Prefabs + "UICMainMenu.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasMainMenu>();
            // Rebuild only this explicitly requested menu; keep the prefab GUID and controller.
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = 20;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
            Image backdrop = Icon(root.transform, "RestaurantBackground", Vector2.zero, Vector2.zero);
            Stretch(backdrop.rectTransform); backdrop.raycastTarget = true;
            var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 941f / 1672f;
            RectTransform safe = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
            Stretch(safe); safe.gameObject.AddComponent<SushiUISafeArea>(); view.rootPanel = safe;
            RectTransform viewport = Rect(safe, "Home viewport", Vector2.zero, Vector2.zero);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0, 200); viewport.offsetMax = new Vector2(0, -190);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform home = Rect(viewport, "Home content", Vector2.zero, Vector2.zero); Stretch(home);
            home.gameObject.AddComponent<CanvasGroup>(); Bind(view, "homeContent", home);
            BuildHeader(view, safe);
            BuildActions(view, home);
            BuildTabs(view, safe);
            BuildProfile(view, safe);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildHeader(CanvasMainMenu view, RectTransform safe)
    {
        RectTransform header = Rect(safe, "Header", new Vector2(1030, 126), new Vector2(0, -92), new Vector2(0.5f, 1));
        var avatar = Button(header, "Profile", new Vector2(112, 112), new Vector2(-452, 0), Gold, view.OpenProfile, Circle);
        Panel(avatar.transform, "Ivory rim", new Vector2(100, 100), Vector2.zero, Cream, Circle);
        Icon(avatar.transform, "Avatar", new Vector2(94, 94), Vector2.zero);
        var wallet = Button(header, "Wallet", new Vector2(260, 90), new Vector2(-247, 0), Cream, view.OpenShop);
        Icon(wallet.transform, "Coin", new Vector2(92, 92), new Vector2(-100, 0));
        Bind(view, "coinText", Label(wallet.transform, "Coins", "500", new Vector2(160, 64), new Vector2(35, 0), 34));
        var plus = Panel(wallet.transform, "Plus badge", new Vector2(34, 34), new Vector2(-67, -33), Green, Circle);
        Label(plus.transform, "Plus", "+", new Vector2(30, 32), Vector2.zero, 30, Color.white);
        Image lives = Panel(header, "Lives", new Vector2(257, 90), new Vector2(30, 0), Cream);
        Icon(lives.transform, "Heart", new Vector2(103, 103), new Vector2(-90, 0));
        Label(lives.transform, "Lives count", "5", new Vector2(60, 60), new Vector2(-89, 0), 30, Color.white);
        Label(lives.transform, "Lives status", "Full", new Vector2(130, 55), new Vector2(45, 0), 30);
        Image stars = Panel(header, "Stars", new Vector2(141, 90), new Vector2(250, 0), Cream);
        Icon(stars.transform, "Star", new Vector2(71, 71), new Vector2(-35, 0));
        Bind(view, "starText", Label(stars.transform, "Star count", "0", new Vector2(66, 58), new Vector2(36, 0), 30));
        var settings = Button(header, "Settings", new Vector2(98, 98), new Vector2(441, 0), Green, view.OpenSettings, Circle);
        Icon(settings.transform, "Gear", new Vector2(79, 79), Vector2.zero);
    }

    private static void BuildActions(CanvasMainMenu view, RectTransform home)
    {
        RectTransform actions = Rect(home, "Level and decoration", new Vector2(1000, 225), new Vector2(0, 130), new Vector2(0.5f, 0));
        Panel(actions, "Level shadow", new Vector2(628, 205), new Vector2(-170, -10), new Color(0.08f, 0.28f, 0.18f));
        var play = Button(actions, "Play level", new Vector2(628, 192), new Vector2(-170, 0), Green, view.Play);
        Panel(play.transform, "Top shine", new Vector2(550, 5), new Vector2(0, 78), new Color(0.45f, 0.77f, 0.55f, 0.6f));
        view.playButtonRect = (RectTransform)play.transform; Bind(view, "playButton", play);
        view.txtLevel = Label(play.transform, "Level", "Level 1", new Vector2(570, 100), Vector2.zero, 66, Color.white);
        Panel(actions, "Decoration shadow", new Vector2(304, 179), new Vector2(329, -9), new Color(0.62f, 0.34f, 0.05f));
        var decorate = Button(actions, "Decoration", new Vector2(304, 168), new Vector2(329, 0), Gold, view.OpenDecoration);
        Icon(decorate.transform, "Decoration", new Vector2(52, 52), new Vector2(-103, 31));
        Label(decorate.transform, "Title", "Trang trí", new Vector2(205, 57), new Vector2(26, 31), 30);
        Panel(decorate.transform, "Progress bed", new Vector2(250, 40), new Vector2(0, -40), new Color(0.48f, 0.28f, 0.04f));
        Label(decorate.transform, "Progress", "0/12", new Vector2(220, 36), new Vector2(0, -40), 23, Color.white);
    }

    private static void BuildTabs(CanvasMainMenu view, RectTransform safe)
    {
        RectTransform footer = Rect(safe, "Navigation", new Vector2(1080, 184), new Vector2(0, 92), new Vector2(0.5f, 0));
        Panel(footer, "Navigation background", new Vector2(1080, 184), Vector2.zero, Navy).sprite = null;
        Panel(footer, "Gold rail", new Vector2(1080, 7), new Vector2(0, 92), Gold).sprite = null;
        for (int i = -1; i <= 1; i += 2)
            Panel(footer, "Tab divider", new Vector2(2, 162), new Vector2(i * 180, -8), new Color(0.09f, 0.19f, 0.34f)).sprite = null;
        Image marker = Panel(footer, "Tab selection", new Vector2(153, 153), new Vector2(0, 26), Cream, Circle);
        Panel(marker.transform, "Gold medallion", new Vector2(134, 134), Vector2.zero, Gold, Circle);
        Bind(view, "tabSelection", marker.rectTransform);
        string[] names = { "Shop", "Home", "Leaderboard" }, icons = { "Shop", "Home", "Trophy" }, labels = { "Shop", "Home", "Xếp hạng" };
        UnityAction[] actions = { view.OpenShop, view.OpenHome, view.OpenLeaderboard };
        var tabIcons = new RectTransform[3]; var tabLabels = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            var button = Button(footer, names[i], new Vector2(360, 184), new Vector2((i - 1) * 360, 0), Color.clear, actions[i]);
            tabIcons[i] = Rect(button.transform, "Animated icon", new Vector2(100, 100), Vector2.zero);
            Icon(tabIcons[i], icons[i], new Vector2(91, 91), Vector2.zero);
            tabLabels[i] = Label(button.transform, "Label", labels[i], new Vector2(310, 48), new Vector2(0, -61), 28, Color.white);
        }
        BindArray(view, "tabIcons", tabIcons); BindArray(view, "tabLabels", tabLabels);
    }

    private static void BuildProfile(CanvasMainMenu view, RectTransform safe)
    {
        Image overlay = Panel(safe, "Profile popup", Vector2.zero, Vector2.zero, new Color(0.07f, 0.04f, 0.02f, 0.75f));
        overlay.sprite = null; overlay.raycastTarget = true; Stretch(overlay.rectTransform);
        var popupCanvas = overlay.gameObject.AddComponent<Canvas>();
        popupCanvas.overrideSorting = true; popupCanvas.sortingOrder = 120;
        overlay.gameObject.AddComponent<GraphicRaycaster>();
        Image card = Panel(overlay.transform, "Profile card", new Vector2(810, 750), Vector2.zero, Cream);
        Icon(card.transform, "Avatar", new Vector2(185, 185), new Vector2(0, 205));
        Label(card.transform, "Title", "Bếp trưởng của tôi", new Vector2(730, 70), new Vector2(0, 65), 44);
        Bind(view, "profileLevel", Label(card.transform, "Progress", "Level 1", new Vector2(700, 150), new Vector2(0, -65), 33));
        var close = Button(card.transform, "Close profile", new Vector2(580, 100), new Vector2(0, -245), Green, view.CloseProfile);
        Label(close.transform, "Label", "Đóng", new Vector2(530, 80), Vector2.zero, 36, Color.white);
        Bind(view, "profilePanel", overlay.gameObject); overlay.gameObject.SetActive(false);
    }

    private static void BuildTransit()
    {
        string path = Prefabs + "UICTransis.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasTransit>();
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 32760;
            var group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.AddComponent<CanvasGroup>();
            group.alpha = 1; group.blocksRaycasts = true; group.interactable = true;
            Image backdrop = Icon(root.transform, "RestaurantBackground", Vector2.zero, Vector2.zero);
            Stretch(backdrop.rectTransform);
            var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 941f / 1672f;
            var dim = Panel(root.transform, "Warm shade", Vector2.zero, Vector2.zero, new Color(0.12f, 0.06f, 0.025f, 0.88f));
            dim.sprite = null; Stretch(dim.rectTransform); dim.raycastTarget = true;
            var content = Rect(root.transform, "Loading card", new Vector2(1080, 1450), Vector2.zero);
            content.gameObject.AddComponent<SushiUIFitContent>();
            Icon(content, "Avatar", new Vector2(250, 250), new Vector2(0, 165));
            var title = Label(content, "Level", "Level 1", new Vector2(850, 95), new Vector2(0, -32), 64, Cream);
            Label(content, "Status", "Đang chuẩn bị quầy sushi...", new Vector2(900, 70), new Vector2(0, -125), 31, Cream);
            Panel(content, "Progress track", new Vector2(670, 44), new Vector2(0, -225), new Color(0.3f, 0.2f, 0.12f));
            var progress = Panel(content, "Progress fill", new Vector2(650, 28), new Vector2(0, -225), Gold);
            progress.type = Image.Type.Filled; progress.fillMethod = Image.FillMethod.Horizontal; progress.fillOrigin = 0; progress.fillAmount = 0;
            view.fill = progress; view.tutorials.Clear();
            Bind(view, "fadeGroup", group); Bind(view, "levelLabel", title);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void BindSettings()
    {
        string path = Prefabs + "UICSetting.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasSetting>();
            if (view.homeBt != null)
            {
                view.homeBt.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(view.homeBt.onClick, view.OnBackHome);
                view.homeBt.GetComponentInChildren<TMP_Text>().text = "Về Home";
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
