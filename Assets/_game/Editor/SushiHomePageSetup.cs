using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Author these tab layouts once in the Editor. Runtime controllers only refresh saved values.
public static class SushiHomePageSetup
{
    private const string Root = "Assets/_game/Resources/UI/";
    private static readonly Color Cream = new Color(0.98f, 0.95f, 0.87f);
    private static readonly Color Paper = new Color(1f, 0.99f, 0.95f);
    private static readonly Color Ink = new Color(0.13f, 0.22f, 0.27f);
    private static readonly Color Muted = new Color(0.39f, 0.45f, 0.43f);
    private static readonly Color Sage = new Color(0.33f, 0.48f, 0.37f);
    private static readonly Color Gold = new Color(0.93f, 0.72f, 0.34f);
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset");

    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing menu prefabs.");
        Create<CanvasShop>("UICShop", BuildShop);
        Create<CanvasLeadBoard>("UICLeaderBoard", BuildLeaderboard);
        Create<CanvasDecoration>("UICDecor", BuildDecoration);
        AssetDatabase.SaveAssets();
    }

    private static void Create<T>(string name, Action<T> build) where T : CanvasHomePage
    {
        string path = Root + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            throw new InvalidOperationException("Missing provided menu prefab: " + path);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            T view = root.GetComponent<T>();
            if (view == null) view = root.AddComponent<T>();
            ConfigureRoot(root);
            // Preserve detailed layouts on repeat runs; complete the supplied empty prefab once.
            if (view.pageContent != null)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return;
            }
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            RectTransform safe = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<SushiUISafeArea>();
            RectTransform viewport = Rect(safe, "Viewport", Vector2.zero, Vector2.zero);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0, 200); viewport.offsetMax = new Vector2(0, -190);
            viewport.gameObject.AddComponent<RectMask2D>();
            view.pageContent = Rect(viewport, "Page content", Vector2.zero, Vector2.zero);
            Stretch(view.pageContent);
            view.pageContent.gameObject.AddComponent<CanvasGroup>();
            Image background = Panel(view.pageContent, "Background", Vector2.zero, Vector2.zero, Cream);
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.raycastTarget = true;
            Stretch(background.rectTransform);
            RectTransform outer = view.pageContent;
            RectTransform layout = Rect(outer, "Layout", new Vector2(1080, 1450), Vector2.zero);
            layout.gameObject.AddComponent<SushiUIFitContent>();
            view.pageContent = layout;
            build(view);
            view.pageContent = outer;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureRoot(GameObject root)
    {
        var rect = (RectTransform)root.transform;
        Stretch(rect);
        rect.pivot = Vector2.one * 0.5f;
        rect.localScale = Vector3.one;
        root.GetComponent<Canvas>().overrideSorting = true;
        root.GetComponent<Canvas>().sortingOrder = 25;
        var viewport = root.transform.Find("Viewport") as RectTransform;
        if (viewport != null)
        {
            var safe = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<SushiUISafeArea>();
            viewport.SetParent(safe, false);
        }
        var visible = root.transform.Find("Safe area/Viewport") as RectTransform;
        if (visible != null)
        {
            visible.anchorMin = Vector2.zero; visible.anchorMax = Vector2.one;
            visible.offsetMin = new Vector2(0, 200); visible.offsetMax = new Vector2(0, -190);
        }
    }

    private static void BuildShop(CanvasShop view)
    {
        Heading(view, "Shop", "Sẵn sàng cho lượt phục vụ tiếp theo");
        TMP_Text section = Label(view.pageContent, "Inventory heading", "BOOSTER CỦA BẠN", new Vector2(880, 50), new Vector2(0, 440), 29);
        section.color = Sage;
        string[] titles = { "Cây gắp", "Đổi hàng", "Nam châm" };
        string[] descriptions = { "Gắp một nguyên liệu trên board.", "Sắp xếp lại các hàng nguyên liệu.", "Hút nguyên liệu cho món đang chọn." };
        string[] marks = { "GẮP", "ĐỔI", "HÚT" };
        view.inventoryCounts = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            float y = 250 - i * 230;
            Image card = Card(view.pageContent, titles[i], new Vector2(920, 200), new Vector2(0, y));
            Image badge = Panel(card.transform, "Tool badge", new Vector2(120, 120), new Vector2(-360, 0), new Color(0.88f, 0.91f, 0.82f));
            TMP_Text mark = Label(badge.transform, "Tool", marks[i], new Vector2(108, 58), Vector2.zero, 24);
            mark.color = Sage;
            TMP_Text title = Label(card.transform, "Name", titles[i], new Vector2(490, 60), new Vector2(-25, 36), 35);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text description = Label(card.transform, "Description", descriptions[i], new Vector2(490, 80), new Vector2(-25, -32), 25);
            description.alignment = TextAlignmentOptions.MidlineLeft;
            description.color = Muted;
            Image stock = Panel(card.transform, "Owned", new Vector2(125, 125), new Vector2(350, 0), Sage);
            view.inventoryCounts[i] = Label(stock.transform, "Count", "0", new Vector2(110, 65), new Vector2(0, 16), 43);
            view.inventoryCounts[i].color = Color.white;
            TMP_Text owned = Label(stock.transform, "Caption", "HIỆN CÓ", new Vector2(110, 30), new Vector2(0, -36), 18);
            owned.color = Cream;
        }
        Footer(view, "Cửa hàng sẽ sớm có thêm vật phẩm.");
    }

    private static void BuildLeaderboard(CanvasLeadBoard view)
    {
        Heading(view, "Xếp hạng", "Hành trình của một đầu bếp");
        Image card = Card(view.pageContent, "Personal progress", new Vector2(920, 720), new Vector2(0, 105));
        TMP_Text overline = Label(card.transform, "Section", "THÀNH TÍCH CỦA BẠN", new Vector2(800, 60), new Vector2(0, 255), 29);
        overline.color = Sage;
        Image medallion = Panel(card.transform, "Progress medallion", new Vector2(260, 245), new Vector2(0, 75), Sage);
        view.progressValue = Label(medallion.transform, "Completed levels", "0", new Vector2(230, 120), new Vector2(0, 15), 86);
        view.progressValue.color = Color.white;
        TMP_Text caption = Label(medallion.transform, "Caption", "MÀN", new Vector2(220, 45), new Vector2(0, -70), 24);
        caption.color = Cream;
        Label(card.transform, "Completed caption", "Màn đã hoàn thành", new Vector2(790, 65), new Vector2(0, -110), 34);
        TMP_Text detail = Label(card.transform, "Saved progress", "Thành tích được lưu sau mỗi màn chơi.", new Vector2(790, 70), new Vector2(0, -195), 27);
        detail.color = Muted;
        TMP_Text note = Label(view.pageContent, "Coming soon", "Bảng xếp hạng sẽ sớm ra mắt.", new Vector2(920, 70), new Vector2(0, -365), 32);
        note.color = Sage;
        Footer(view, "Tiếp tục phục vụ để đạt cột mốc mới nhé!");
    }

    private static void BuildDecoration(CanvasDecoration view)
    {
        Heading(view, "Trang trí", "Góc nhỏ dành riêng cho quán của bạn");
        Image card = Card(view.pageContent, "Restaurant card", new Vector2(920, 770), new Vector2(0, 80));
        Image accent = Panel(card.transform, "Art backdrop", new Vector2(820, 395), new Vector2(0, 120), new Color(0.90f, 0.92f, 0.84f));
        Art(accent.transform, "Tea", "Assets/_game/Art/Dishes/GreenTea.png", new Vector2(235, 235), new Vector2(-245, -30));
        Art(accent.transform, "Sushi", "Assets/_game/Art/Dishes/NigiriSalmon.png", new Vector2(335, 295), new Vector2(0, 20));
        Art(accent.transform, "Avocado", "Assets/_game/Art/Dishes/NigiriAvocado.png", new Vector2(235, 235), new Vector2(250, -30));
        Label(card.transform, "Card title", "Quán sushi của bạn", new Vector2(820, 85), new Vector2(0, -150), 42);
        TMP_Text detail = Label(card.transform, "Description", "Các hạng mục trang trí mới\nsẽ sớm được mở.", new Vector2(790, 115), new Vector2(0, -260), 29);
        detail.color = Muted;
        Footer(view, "Một góc quán ấm áp đang chờ bạn.");
    }

    private static void Heading(CanvasHomePage view, string title, string subtitle)
    {
        TMP_Text heading = Label(view.pageContent, "Title", title, new Vector2(960, 90), new Vector2(0, -95), 57);
        heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f, 1);
        TMP_Text description = Label(view.pageContent, "Subtitle", subtitle, new Vector2(940, 65), new Vector2(0, -178), 27);
        description.rectTransform.anchorMin = description.rectTransform.anchorMax = new Vector2(0.5f, 1);
        description.color = Muted;
    }

    private static void Footer(CanvasHomePage view, string message)
    {
        TMP_Text label = Label(view.pageContent, "Status", message, new Vector2(940, 70), new Vector2(0, 190), 27);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0);
        label.color = Muted;
        Image image = Panel(view.pageContent, "Back to Home", new Vector2(380, 98), new Vector2(0, 84), Gold);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        UnityEventTools.AddPersistentListener(button.onClick, view.GoHome);
        Label(image.transform, "Label", "Về Home", new Vector2(350, 70), Vector2.zero, 30);
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        Image image = Rect(parent, name, size, position).gameObject.AddComponent<Image>();
        image.sprite = Rounded;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image Card(Transform parent, string name, Vector2 size, Vector2 position)
    {
        Panel(parent, name + " shadow", size + new Vector2(6, 6), position + new Vector2(0, -8), new Color(0.23f, 0.22f, 0.17f, 0.10f));
        return Panel(parent, name, size, position, Paper);
    }

    private static TMP_Text Label(Transform parent, string name, string text, Vector2 size, Vector2 position, float fontSize)
    {
        TMP_Text label = Rect(parent, name, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        if (Font != null) label.font = Font;
        label.fontSize = label.fontSizeMax = fontSize;
        label.fontSizeMin = fontSize * 0.8f;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Ink;
        label.raycastTarget = false;
        return label;
    }

    private static void Art(Transform parent, string name, string path, Vector2 size, Vector2 position)
    {
        Image image = Rect(parent, name, size, position).gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }
}