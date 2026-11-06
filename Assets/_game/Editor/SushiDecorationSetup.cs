using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Temporary vector-like art, authored as editable UI layers in the two menu prefabs.
public static class SushiDecorationSetup
{
    private static readonly Color Ink = C(0x49372D), Wood = C(0xB87A47), LightWood = C(0xDFAC70);
    private static readonly Color Cream = C(0xFFF5DE), Green = C(0x476D54), Red = C(0xC95742), Gold = C(0xE9B64F);
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset");
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");
    private static Color C(int rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);

    [MenuItem("SushiKhai/UI/Build Restaurant Decoration")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring decoration.");
        const string folder = "Assets/_game/Resources/Decoration";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_game/Resources", "Decoration");
        string path = folder + "/KaitenCounter.asset";
        if (AssetDatabase.LoadAssetAtPath<SushiDecorationCatalog>(path) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SushiDecorationCatalog>(), path);
        string listPath = folder + "/Restaurants.asset";
        if (AssetDatabase.LoadAssetAtPath<SushiRestaurantCatalog>(listPath) == null)
        {
            var list = ScriptableObject.CreateInstance<SushiRestaurantCatalog>();
            list.decorations = AssetDatabase.LoadAssetAtPath<SushiDecorationCatalog>(path);
            AssetDatabase.CreateAsset(list, listPath);
        }
        Edit("UICMainMenu", BuildHome);
        Edit("UICDecor", root => BuildPage(root.GetComponent<CanvasDecoration>()));
        AssetDatabase.SaveAssets();
    }

    public static void RunBatch()
    {
        try { Setup(); Debug.Log("SUSHI_DECORATION_SETUP_OK"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void Edit(string name, Action<GameObject> build)
    {
        string path = "Assets/_game/Resources/UI/" + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try { build(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildHome(GameObject root)
    {
        var view = root.GetComponent<CanvasMainMenu>();
        var home = root.transform.Find("Safe area/Home viewport/Home content");
        if (home == null) throw new InvalidOperationException("Home content is missing.");
        var stars = root.transform.Find("Safe area/Header/Stars") as RectTransform;
        if (stars != null)
        {
            stars.gameObject.SetActive(true);
            stars.anchoredPosition = new Vector2(332, 0);
            stars.sizeDelta = new Vector2(132, 90);
        }
        Remove(home.Find("Restaurant decoration"));
        Remove(home.Find("Restaurant arrows"));
        var backdrop = root.transform.Find("RestaurantBackground");
        if (backdrop != null)
        {
            for (int i = backdrop.childCount - 1; i >= 0; i--) Remove(backdrop.GetChild(i));
            var image = backdrop.GetComponent<Image>();
            image.sprite = null; image.color = C(0x798E70);
            var fitter = backdrop.GetComponent<AspectRatioFitter>();
            if (fitter != null) Object.DestroyImmediate(fitter);
            Stretch((RectTransform)backdrop);
            var road = Panel(backdrop, "Street", 970, 0, 0, 0, C(0x919B91));
            road.rectTransform.anchorMin = new Vector2(0.5f, 0);
            road.rectTransform.anchorMax = new Vector2(0.5f, 1);
            road.rectTransform.sizeDelta = new Vector2(970, 0);
            Panel(backdrop, "Front pavement", 1080, 110, 0, -530, C(0xBEC4AC));
            for (int side = -1; side <= 1; side += 2)
            {
                Oval(backdrop, "Shrub", 380, 480, side * 610, -440, C(0x56774C));
                Oval(backdrop, "Leaves", 350, 320, side * 590, 450, C(0x648358));
            }
        }
        var stage = Rect(home, "Restaurant decoration", 0, 0, 0, 0);
        Stretch(stage); stage.SetAsFirstSibling();
        var layout = stage.gameObject.AddComponent<SushiHomeRestaurantLayout>();
        view.restaurantLayout = layout;
        var list = AssetDatabase.LoadAssetAtPath<SushiRestaurantCatalog>("Assets/_game/Resources/Decoration/Restaurants.asset");
        if (list.restaurants.Length == 0 || list.restaurants[0].id != SushiRestaurantCatalog.FirstRestaurantId)
            throw new InvalidOperationException("Keep kaiten_counter as the first restaurant for existing saves.");
        var preview = Rect(stage, "Restaurant carousel", 1000, 760, 0, 0);
        var touch = preview.gameObject.AddComponent<Image>(); touch.color = Color.clear; touch.raycastTarget = true;
        preview.gameObject.AddComponent<RectMask2D>();
        layout.preview = preview;
        var carousel = preview.gameObject.AddComponent<SushiRestaurantCarousel>();
        view.restaurantCarousel = carousel; carousel.menu = view; carousel.catalog = list;
        carousel.track = Rect(preview, "Building track", 1000, 760, 0, 0);
        carousel.views = new SushiDecorationRestaurantView[list.restaurants.Length];
        carousel.buildingGroups = new CanvasGroup[list.restaurants.Length];
        carousel.closedSigns = new GameObject[list.restaurants.Length];
        for (int i = 0; i < list.restaurants.Length; i++)
        {
            var info = list.restaurants[i];
            if (string.IsNullOrEmpty(info.id)) throw new InvalidOperationException("Restaurant IDs cannot be empty.");
            for (int j = 0; j < i; j++)
                if (list.restaurants[j].id == info.id) throw new InvalidOperationException("Restaurant IDs must be unique.");
            var building = DrawRestaurant(carousel.track, info.id, i * 1100, 0, 1);
            carousel.views[i] = building;
            carousel.buildingGroups[i] = building.gameObject.AddComponent<CanvasGroup>();
            foreach (var graphic in building.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.name == "Cream facade") graphic.color = info.facade;
                if (graphic.name == "Red awning" || graphic.name == "Red cushion" || graphic.name == "Paper lantern" ||
                    graphic.name == "Planter" || graphic.name == "Gate pillar" || graphic.name == "Torii lintel") graphic.color = info.accent;
                if (graphic.name == "Sign title") { ((TMP_Text)graphic).text = info.sign; graphic.color = info.accent; }
            }
            var closed = Panel(building.transform, "Closed sign", 162, 65, 310, -75, C(0x684D72));
            Text(closed.transform, "Closed label", "ĐÓNG CỬA", 155, 55, 0, 0, 21, Cream);
            carousel.closedSigns[i] = closed.gameObject; closed.gameObject.SetActive(i > 0);
            carousel.buildingGroups[i].alpha = i == 0 ? 1 : 0.58f;
        }
        view.decorationRestaurant = carousel.views[0];
        var chrome = Rect(stage, "Home captions", 0, 0, 0, 0); Stretch(chrome);
        var title = Text(chrome, "Restaurant name", "Quán sushi của bạn", 950, 80, 0, -100, 45, Cream);
        carousel.title = title;
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1);
        var area = Text(chrome, "Area", "NHÀ HÀNG ĐẦU TIÊN", 900, 45, 0, -165, 24, Cream);
        carousel.pageLabel = area; area.text = "NHÀ HÀNG 1 / " + carousel.views.Length;
        area.rectTransform.anchorMin = area.rectTransform.anchorMax = new Vector2(0.5f, 1);
        var badge = Panel(chrome, "Restaurant complete badge", 740, 70, 0, 295, Green);
        badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.5f, 0);
        Text(badge.transform, "Title", "Nhà hàng hoàn thiện", 700, 60, 0, 0, 30, Cream);
        view.restaurantCompleteBadge = badge.gameObject; badge.gameObject.SetActive(false);
        var navigation = Rect(home, "Restaurant arrows", 1000, 100, 0, 0);
        carousel.navigation = navigation;
        carousel.previousButton = Button(navigation, "Previous restaurant", 82, 100, 0, 0, Ink, carousel.Previous);
        var left = (RectTransform)carousel.previousButton.transform; left.anchorMin = left.anchorMax = new Vector2(0, 0.5f);
        Panel(left, "Gold", 72, 90, 0, 0, Gold); Text(left, "Arrow", "<", 70, 90, 0, 2, 64, Cream);
        carousel.nextButton = Button(navigation, "Next restaurant", 82, 100, 0, 0, Ink, carousel.Next);
        var right = (RectTransform)carousel.nextButton.transform; right.anchorMin = right.anchorMax = new Vector2(1, 0.5f);
        Panel(right, "Gold", 72, 90, 0, 0, Gold); Text(right, "Arrow", ">", 70, 90, 0, 2, 64, Cream);
        var locked = Panel(chrome, "Restaurant locked", 1040, 180, 0, 175, C(0x376E89));
        locked.rectTransform.anchorMin = locked.rectTransform.anchorMax = new Vector2(0.5f, 0);
        Panel(locked.transform, "Title band", 1022, 76, 0, 41, C(0x68A7C1));
        Text(locked.transform, "Locked title", "Đã khóa", 960, 70, 0, 41, 46, Cream);
        carousel.lockDescription = Text(locked.transform, "Unlock condition", "", 970, 78, 0, -40, 29, Cream);
        carousel.lockedPanel = locked.gameObject; locked.gameObject.SetActive(false);
        var homeOnly = new System.Collections.Generic.List<GameObject> { chrome.gameObject };
        homeOnly.Add(navigation.gameObject);
        var actions = home.Find("Level and decoration");
        if (actions != null) { homeOnly.Add(actions.gameObject); carousel.homeActions = actions.gameObject; }
        int shortcutIndex = 0;
        foreach (var button in root.GetComponentsInChildren<Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                string method = button.onClick.GetPersistentMethodName(i);
                if (method == "OpenDailYLogin" || method == "OpenDailyReward" || method == "OpenDailySpin")
                {
                    // Leave the central building and both navigation hit areas clear.
                    var shortcut = (RectTransform)button.transform;
                    shortcut.SetParent(home, false);
                    shortcut.anchorMin = shortcut.anchorMax = new Vector2(0, 1);
                    shortcut.anchoredPosition = new Vector2(85, -80 - shortcutIndex++ * 140);
                    shortcut.localScale = Vector3.one * 0.55f;
                    homeOnly.Add(button.gameObject); break;
                }
            }
        layout.homeOnly = homeOnly.ToArray();
        layout.Refresh();
        var progress = home.Find("Level and decoration/Decoration/Progress");
        if (progress != null) view.decorationProgress = progress.GetComponent<TMP_Text>();
    }

    public static void BuildPage(CanvasDecoration view)
    {
        var root = (RectTransform)view.transform;
        for (int i = root.childCount - 1; i >= 0; i--) Remove(root.GetChild(i));
        Stretch(root);
        var canvas = view.GetComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 25;
        var safe = Rect(root, "Safe area", 0, 0, 0, 0); Stretch(safe);
        safe.gameObject.AddComponent<SushiUISafeArea>();
        var viewport = Rect(safe, "Home viewport", 0, 0, 0, 0); Stretch(viewport);
        viewport.offsetMin = new Vector2(0, 200); viewport.offsetMax = new Vector2(0, -190);
        var sheet = Panel(viewport, "Upgrade drawer", 0, 0, 0, 0, C(0x244B60));
        sheet.raycastTarget = true;
        view.pageContent = sheet.rectTransform;
        sheet.rectTransform.anchorMin = Vector2.zero; sheet.rectTransform.anchorMax = new Vector2(1, 0.48f);
        sheet.rectTransform.pivot = new Vector2(0.5f, 0);
        sheet.rectTransform.offsetMin = new Vector2(12, 0); sheet.rectTransform.offsetMax = new Vector2(-12, 0);
        view.sheetGroup = sheet.gameObject.AddComponent<CanvasGroup>();
        var face = Panel(sheet.transform, "Blue panel", 0, 0, 0, 0, C(0x376E89)); Stretch(face.rectTransform);
        face.rectTransform.offsetMin = new Vector2(7, 7); face.rectTransform.offsetMax = new Vector2(-7, -7);
        var layout = Rect(sheet.transform, "Layout", 1080, 760, 0, 0);
        var fit = layout.gameObject.AddComponent<SushiUIFitContent>();
        fit.referenceSize = new Vector2(1080, 760); fit.fillAvailableHeight = true;
        Top(Panel(layout, "Title band", 1050, 120, 0, -70, C(0x68A7C1)).rectTransform);
        Top(Text(layout, "Title shadow", "Nâng cấp công trình", 920, 85, 0, -76, 46, C(0x25485B)).rectTransform);
        Top(Text(layout, "Title", "Nâng cấp công trình", 920, 85, 0, -70, 46, Cream).rectTransform);
        var close = Button(sheet.transform, "Close upgrades", 72, 72, -44, -12, Red, view.ClosePanel);
        ((RectTransform)close.transform).anchorMin = ((RectTransform)close.transform).anchorMax = Vector2.one;
        Text(close.transform, "X", "X", 65, 65, 0, 0, 38, Cream);
        var progress = Panel(layout, "Build progress", 680, 95, 0, -192, C(0xE7E5D9)); Top(progress.rectTransform);
        view.progressText = Text(progress.transform, "Count", "Tiến độ xây  0/12", 620, 44, 0, 20, 29);
        Panel(progress.transform, "Track", 610, 23, 0, -24, C(0x9CABA2));
        view.progressFill = Panel(progress.transform, "Fill", 610, 23, 0, -24, C(0x81C251));
        view.progressFill.type = Image.Type.Filled; view.progressFill.fillMethod = Image.FillMethod.Horizontal;
        view.progressFill.fillAmount = 0;
        view.statusText = Text(layout, "Status", "", 1000, 44, 0, -274, 25, Cream); Top(view.statusText.rectTransform);
        BuildCards(view, layout);
        view.rewardText = Text(layout, "Reward", "", 1000, 40, 0, 30, 23, Cream);
        view.rewardText.rectTransform.anchorMin = view.rewardText.rectTransform.anchorMax = new Vector2(0.5f, 0);
        var complete = Panel(layout, "Completion reward", 990, 44, 0, 30, Green);
        complete.rectTransform.anchorMin = complete.rectTransform.anchorMax = new Vector2(0.5f, 0);
        view.completionPanel = complete.gameObject;
        view.completionRewardText = Text(complete.transform, "Coins", "", 970, 42, 0, 0, 23, Cream);
        complete.gameObject.SetActive(false);
    }

    private static void BuildCards(CanvasDecoration view, Transform parent)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SushiDecorationCatalog>("Assets/_game/Resources/Decoration/KaitenCounter.asset");
        var home = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/UICMainMenu.prefab").GetComponent<CanvasMainMenu>();
        var scroll = Rect(parent, "Items scroll", 1000, 400, 0, -115);
        Stretch(scroll); scroll.offsetMin = new Vector2(40, 65); scroll.offsetMax = new Vector2(-40, -295);
        view.itemScroll = scroll.gameObject.AddComponent<ScrollRect>();
        view.itemScroll.horizontal = false; view.itemScroll.movementType = ScrollRect.MovementType.Clamped;
        var mask = Panel(scroll, "Viewport", 0, 0, 0, 0, Color.clear); Stretch(mask.rectTransform);
        mask.raycastTarget = true; mask.gameObject.AddComponent<RectMask2D>();
        var content = Rect(mask.transform, "Cards", 0, 0, 0, 0);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        grid.cellSize = new Vector2(320, 270); grid.spacing = new Vector2(14, 16);
        grid.padding = new RectOffset(6, 6, 4, 4);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        view.itemScroll.viewport = mask.rectTransform; view.itemScroll.content = content;
        int count = catalog.items.Length;
        view.itemLabels = new TMP_Text[count]; view.itemCards = new Image[count];
        view.itemButtons = new Button[count]; view.itemArt = new CanvasGroup[count];
        for (int i = 0; i < count; i++)
        {
            var card = Panel(content, catalog.items[i].id, 320, 270, 0, 0, C(0x9AA5A8));
            card.raycastTarget = true; view.itemCards[i] = card;
            var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.disabledColor = Color.white; button.colors = colors;
            UnityEventTools.AddIntPersistentListener(button.onClick, view.BuildItem, i);
            view.itemButtons[i] = button;
            Panel(card.transform, "Card face", 306, 256, 0, 0, C(0xE5E7DF));
            var art = Rect(card.transform, "Item illustration", 260, 145, 0, 40);
            view.itemArt[i] = art.gameObject.AddComponent<CanvasGroup>();
            CloneItemIcon(home.decorationRestaurant.itemRoots[i], art, i);
            Text(card.transform, "Name", catalog.items[i].nameVi, 294, 52, 0, -63, 27);
            Panel(card.transform, "Price bed", 294, 43, 0, -111, Cream);
            view.itemLabels[i] = Text(card.transform, "Action", catalog.items[i].starCost + " sao", 288, 40, 0, -111, 25, Green);
        }
        view.buildButton = view.itemButtons[0];
    }

    private static void CloneItemIcon(RectTransform source, RectTransform parent, int index)
    {
        var copy = Object.Instantiate(source.gameObject, parent, false).GetComponent<RectTransform>();
        copy.gameObject.SetActive(true); copy.anchoredPosition = Vector2.zero; copy.localScale = Vector3.one;
        if (index == 2)
            for (int i = copy.childCount - 1; i >= 0; i--)
                if (((RectTransform)copy.GetChild(i)).anchoredPosition.x > 0) Remove(copy.GetChild(i));
        var min = Vector3.one * float.PositiveInfinity;
        var max = Vector3.one * float.NegativeInfinity;
        var corners = new Vector3[4];
        foreach (var graphic in copy.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
            graphic.rectTransform.GetWorldCorners(corners);
            foreach (var point in corners)
            {
                Vector3 local = copy.InverseTransformPoint(point);
                min = Vector3.Min(min, local); max = Vector3.Max(max, local);
            }
        }
        Vector3 size = max - min;
        float scale = Mathf.Min(245 / Mathf.Max(1, size.x), 135 / Mathf.Max(1, size.y));
        copy.localScale = Vector3.one * scale;
        copy.anchoredPosition = -(Vector2)(min + max) * (0.5f * scale);
    }

    private static SushiDecorationRestaurantView DrawRestaurant(Transform parent, string name, float x, float y, float scale)
    {
        var room = Rect(parent, name, 1000, 760, x, y); room.localScale = Vector3.one * scale;
        var view = room.gameObject.AddComponent<SushiDecorationRestaurantView>();
        Oval(room, "Building shadow", 960, 95, 0, -325, C(0x647365));
        Panel(room, "Facade outline", 895, 500, 0, -55, Ink);
        Panel(room, "Cream facade", 865, 475, 0, -50, C(0xF0DFC0));
        Panel(room, "Service window frame", 620, 250, -95, 0, Wood);
        Panel(room, "Service window", 590, 225, -95, 0, C(0x8B8068));
        Panel(room, "Door frame", 188, 255, 310, -135, Wood);
        Panel(room, "Glass door", 162, 235, 310, -135, C(0x788880));
        Panel(room, "Door divider", 8, 235, 310, -135, Ink);
        Panel(room, "Door handle", 9, 48, 291, -140, Cream);
        Panel(room, "Other handle", 9, 48, 329, -140, Cream);
        Panel(room, "Roof beam", 960, 100, 0, 277, Wood);
        Panel(room, "Roof cap", 990, 28, 0, 329, Ink);
        Panel(room, "Awning outline", 910, 90, 0, 190, Ink);
        Panel(room, "Red awning", 890, 80, 0, 197, Red);
        for (int i = 0; i < 4; i++) Panel(room, "Cream awning stripe", 110, 80, -333 + i * 222, 197, C(0xF3D993));
        view.itemRoots = new RectTransform[12];

        var floor = Item(view, room, 1, 0, -306);
        Panel(floor, "Wooden deck edge", 970, 120, 0, 0, Wood);
        Panel(floor, "Deck boards", 960, 100, 0, 12, LightWood);
        for (int i = 0; i < 9; i++) Panel(floor, "Floorboard", 3, 100, -432 + i * 108, 12, Wood);
        Panel(floor, "Plank seam", 960, 2, 0, 20, Wood);

        var mural = Item(view, room, 9, -62, 70);
        Panel(mural, "Mural frame", 210, 135, 0, 0, Wood);
        Panel(mural, "Paper", 194, 118, 0, 0, Cream);
        Oval(mural, "Sun", 45, 45, 49, 20, Red);
        for (int i = 0; i < 3; i++) Bar(mural, "Mountain", 89, 45, -54 + i * 52, -24, Green, i % 2 == 0 ? 25 : -20);

        var window = Item(view, room, 10, 300, 103);
        Panel(window, "Window frame", 192, 145, 0, 0, Wood);
        Panel(window, "Sky", 176, 130, 0, 0, C(0xB8D9D2));
        Bar(window, "Sakura branch", 145, 9, 0, 5, Ink, 28);
        for (int i = 0; i < 7; i++) Flower(window, -65 + i * 20, -20 + i % 3 * 25);
        Panel(window, "Vertical mullion", 8, 130, 0, 0, Wood);
        Panel(window, "Horizontal mullion", 176, 8, 0, 0, Wood);

        var noren = Item(view, room, 5, 310, -12);
        Panel(noren, "Curtain rail", 185, 12, 0, 65, Wood);
        for (int i = 0; i < 3; i++)
        {
            Panel(noren, "Noren panel", 51, 105, -55 + i * 55, 9, C(0x496A78));
            Oval(noren, "White crest", 22, 22, -55 + i * 55, 18, Cream);
        }

        var shelf = Item(view, room, 7, -285, 64);
        Panel(shelf, "Shelf back", 175, 110, 0, 15, Wood);
        for (int row = 0; row < 2; row++)
        {
            Panel(shelf, "Shelf ledge", 190, 12, 0, -32 + row * 62, Ink);
            for (int i = 0; i < 3; i++)
            {
                Oval(shelf, "Bowl", 40, 25, -57 + i * 57, -14 + row * 62, Cream);
                Oval(shelf, "Bowl rim", 40, 9, -57 + i * 57, -4 + row * 62, C(0x86AAA4));
            }
        }

        var counter = Item(view, room, 3, -120, -110);
        Panel(counter, "Counter shadow", 560, 140, 0, -15, Ink);
        Panel(counter, "Counter front", 540, 133, 0, 0, Wood);
        for (int i = 0; i < 7; i++) Panel(counter, "Front slat", 3, 120, -230 + i * 77, -5, LightWood);
        Panel(counter, "Countertop edge", 585, 31, 0, 66, Ink);
        Panel(counter, "Countertop", 580, 25, 0, 75, Cream);

        var belt = Item(view, room, 6, -120, -28);
        Panel(belt, "Belt rim", 560, 50, 0, 0, Ink);
        Panel(belt, "Belt tread", 544, 32, 0, 0, C(0x727B72));
        for (int i = 0; i < 5; i++)
        {
            float px = -213 + i * 106;
            Oval(belt, "Plate", 72, 22, px, 7, Cream);
            Panel(belt, "Sushi rice", 35, 20, px, 18, Color.white);
            Panel(belt, "Salmon", 41, 11, px, 27, Red);
        }

        var stools = Item(view, room, 4, -130, -267);
        for (int i = 0; i < 4; i++)
        {
            float px = -210 + i * 140;
            Oval(stools, "Stool shadow", 95, 22, px, -47, C(0xB18555));
            Bar(stools, "Left leg", 11, 60, px - 22, -16, Ink, -8);
            Bar(stools, "Right leg", 11, 60, px + 22, -16, Ink, 8);
            Oval(stools, "Seat timber", 98, 38, px, 20, Ink);
            Oval(stools, "Red cushion", 89, 29, px, 26, Red);
        }

        var bonsai = Item(view, room, 8, 390, -235);
        Panel(bonsai, "Plant stand", 105, 13, 0, -60, Ink);
        Panel(bonsai, "Planter", 85, 54, 0, -25, Red);
        Bar(bonsai, "Trunk", 18, 110, 0, 46, Wood, -15);
        Bar(bonsai, "Branch", 85, 14, 4, 72, Wood, 20);
        Oval(bonsai, "Leaf crown", 126, 62, -8, 123, Green);
        Oval(bonsai, "Left leaves", 90, 48, -42, 82, C(0x6B8B53));
        Oval(bonsai, "Right leaves", 85, 48, 45, 95, C(0x6B8B53));

        var lantern = Item(view, room, 2, 0, 142);
        for (int side = -1; side <= 1; side += 2)
        {
            float px = side * 380;
            Panel(lantern, "Hanging cord", 4, 85, px, 90, Ink);
            Oval(lantern, "Paper lantern", 91, 117, px, 0, Red);
            Oval(lantern, "Lantern glow", 44, 103, px, 0, C(0xEA9270));
            for (int i = -1; i <= 1; i++) Panel(lantern, "Lantern rib", 85, 3, px, i * 24, C(0xC0654B));
            Panel(lantern, "Top cap", 47, 10, px, 58, Ink);
            Panel(lantern, "Bottom cap", 47, 10, px, -58, Ink);
            Panel(lantern, "Tassel", 7, 28, px, -77, Gold);
        }

        var torii = Item(view, room, 11, 0, 0);
        for (int side = -1; side <= 1; side += 2)
        {
            Panel(torii, "Gate pillar", 27, 650, side * 455, 5, Red);
            Panel(torii, "Pillar foot", 43, 36, side * 455, -325, Ink);
        }
        Panel(torii, "Torii lintel", 965, 32, 0, 310, Red);
        Panel(torii, "Dark cap", 1000, 18, 0, 334, Ink);
        Panel(torii, "Cross beam", 930, 20, 0, 260, Red);

        var sign = Item(view, room, 0, 0, 280);
        Panel(sign, "Sign shadow", 335, 96, 0, -6, Ink);
        Panel(sign, "Sign wood", 325, 90, 0, 0, Wood);
        Panel(sign, "Sign face", 300, 68, 0, 0, Cream);
        Text(sign, "Sign title", "SUSHI", 280, 60, 0, 0, 39, Red);
        view.Show(0);
        return view;
    }

    private static RectTransform Item(SushiDecorationRestaurantView view, Transform room, int index, float x, float y)
    {
        string[] ids = { "sign", "floor", "lantern", "counter", "stools", "noren", "belt", "shelf", "bonsai", "mural", "window", "torii" };
        return view.itemRoots[index] = Rect(room, "deco_" + ids[index], 0, 0, x, y);
    }

    private static void Flower(Transform parent, float x, float y)
    {
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 2 / 5;
            Oval(parent, "Sakura petal", 13, 13, x + Mathf.Cos(angle) * 7, y + Mathf.Sin(angle) * 7, C(0xF1ADC0));
        }
        Oval(parent, "Flower heart", 6, 6, x, y, Cream);
    }

    private static void Remove(Transform target) { if (target != null) Object.DestroyImmediate(target.gameObject); }
    private static RectTransform Rect(Transform parent, string name, float w, float h, float x, float y)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(parent, false); rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y); return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Top(RectTransform rect) => rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
    private static Image Panel(Transform parent, string name, float w, float h, float x, float y, Color color)
    {
        var image = Rect(parent, name, w, h, x, y).gameObject.AddComponent<Image>();
        image.sprite = Rounded; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false; return image;
    }
    private static void Oval(Transform parent, string name, float w, float h, float x, float y, Color color)
    {
        var rect = Rect(parent, name, h, h, x, y);
        rect.localScale = new Vector3(w / h, 1, 1);
        var image = rect.gameObject.AddComponent<SushiUICircle>();
        image.color = color; image.raycastTarget = false;
    }
    private static void Bar(Transform parent, string name, float w, float h, float x, float y, Color color, float angle)
    { Panel(parent, name, w, h, x, y, color).rectTransform.localEulerAngles = new Vector3(0, 0, angle); }
    private static TMP_Text Text(Transform parent, string name, string value, float w, float h, float x, float y, float size, Color? color = null)
    {
        var text = Rect(parent, name, w, h, x, y).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Font; text.text = value; text.fontSize = text.fontSizeMax = size; text.fontSizeMin = size * 0.75f;
        text.enableAutoSizing = true; text.alignment = TextAlignmentOptions.Center; text.color = color ?? Ink;
        text.raycastTarget = false; return text;
    }
    private static Button Button(Transform parent, string name, float w, float h, float x, float y, Color color, UnityAction action)
    {
        var image = Panel(parent, name, w, h, x, y, color); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(button.onClick, action); return button;
    }
}
