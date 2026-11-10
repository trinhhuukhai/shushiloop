using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Áp art UI mới (Art/UI/Home, Shop, Ranking) theo các ảnh demo UI_home.png, shop1-3.png, leader_board1-2.png.
// - UICMainMenu: sửa tại chỗ (giữ các chỉnh tay khác), ẩn decor + slide map, xếp lại header/nút/thanh tab.
// - UICShop, UICLeaderBoard: dựng lại toàn bộ nội dung trang.
// Toạ độ trong file lấy trực tiếp từ ảnh demo 1080x1920 (gốc trên-trái), art dùng đúng kích thước gốc.
public static class SushiHomeArtSetup
{
    private const string Art = "Assets/_game/Art/UI/";
    private const string Prefabs = "Assets/_game/Resources/UI/";
    private const string ImportMark = "sushi-home-art-v1";
    private const float NavHeight = 222f;
    private const float PageBottom = 1920f - NavHeight; // đáy vùng trang trong toạ độ demo
    private const float PlayerRowHeight = 190f;          // dòng xanh của người chơi ghim cuối trang Ranking

    private static readonly Color Brown = new Color(0.36f, 0.18f, 0.09f);
    private static readonly Color TextBrown = new Color(0.45f, 0.24f, 0.11f);
    private static readonly Color ShopBackground = new Color(0.31f, 0.19f, 0.16f);
    private static readonly Color Popular = new Color(1f, 0.86f, 0.18f);

    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_game/Font/for localize sub/NotoSans-Black SDF.asset");
    private static Material outline;

    [MenuItem("SushiKhai/UI/Apply New Home Art (Home, Shop, Ranking)")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi dựng prefab.");
        ImportArt();
        outline = OutlineMaterial();
        UpdateProfileData();
        EditMainMenu();
        BuildShop();
        BuildRanking();
        BuildProfile();
        AssetDatabase.SaveAssets();
        Debug.Log("Đã áp art UI mới cho Home, Shop, Ranking.");
    }

    // Chỉ dựng lại Shop (không đụng Home/Ranking/Profile, giữ các chỉnh tay ở những prefab đó).
    [MenuItem("SushiKhai/UI/Rebuild Shop Only")]
    public static void SetupShopOnly()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi dựng prefab.");
        ImportArt();
        outline = OutlineMaterial();
        BuildShop();
        AssetDatabase.SaveAssets();
        Debug.Log("Đã dựng lại UICShop.");
    }

    // Vá tại chỗ (giữ chỉnh tay): ảnh header Shop/Ranking chạm đỉnh màn hình, nút né tai thỏ.
    [MenuItem("SushiKhai/UI/Patch Shop and Ranking Safe Area")]
    public static void PatchSafeArea()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi sửa prefab.");
        Patch("UICShop", root =>
        {
            Transform page = root.Find("Safe area/Viewport/Page content");
            Inset(page.Find("Header/Wallet"));
            Inset(page.Find("Header/Title"));
            Inset(page.Find("Header/Settings"));
        });
        Patch("UICLeaderBoard", root =>
        {
            Transform page = root.Find("Safe area/Viewport/Page content");
            foreach (string name in new[] { "Wallet", "Title", "Settings", "Weekly tab", "Rank tab", "Timer" }) Inset(page.Find(name));
            Inset(page.Find("Weekly"), SushiUISafeTopInset.Mode.ShrinkTop);
            Inset(page.Find("Rank"), SushiUISafeTopInset.Mode.ShrinkTop);
            if (page.Find("Sky") is RectTransform sky)
            {
                sky.pivot = new Vector2(0.5f, 1f);
                sky.anchoredPosition = Vector2.zero;
                Inset(sky, SushiUISafeTopInset.Mode.Grow);
            }
        });
        AssetDatabase.SaveAssets();
        Debug.Log("Đã vá safe area cho UICShop và UICLeaderBoard.");
    }

    private static void Patch(string prefab, Action<Transform> edit)
    {
        string path = Prefabs + prefab + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var safe = root.transform.Find("Safe area")?.GetComponent<SushiUISafeArea>();
            if (safe != null)
            {
                var data = new SerializedObject(safe);
                data.FindProperty("applyTop").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            edit(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Dựng lại Shop + Ranking (không đụng Home/Profile).
    [MenuItem("SushiKhai/UI/Rebuild Shop and Ranking")]
    public static void SetupPages()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi dựng prefab.");
        ImportArt();
        outline = OutlineMaterial();
        BuildShop();
        BuildRanking();
        AssetDatabase.SaveAssets();
        Debug.Log("Đã dựng lại UICShop và UICLeaderBoard.");
    }

    // ---------------------------------------------------------------- Import

    private static void ImportArt()
    {
        foreach (string folder in new[] { "Home", "Shop", "Ranking" })
            foreach (string file in Directory.GetFiles(Art + folder, "*.png", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.userData == ImportMark) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = path.EndsWith("Home/background.png") ? 4096 : 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.userData = ImportMark;
                importer.SaveAndReimport();
            }
    }

    private static Sprite S(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
        if (sprite == null) throw new InvalidOperationException("Thiếu sprite: " + Art + path);
        return sprite;
    }

    // Material chữ trắng viền nâu dùng chung (tạo một lần cạnh font).
    private static Material OutlineMaterial()
    {
        const string path = "Assets/_game/Font/for localize sub/NotoSans-Black SDF Outline.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Font.material) { name = "NotoSans-Black SDF Outline" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.CopyPropertiesFromMaterial(Font.material);
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetColor(ShaderUtilities.ID_OutlineColor, Brown);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------------------------------------------------------------- Helpers

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position, Vector2? anchor = null)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect, float bottom = 0, float top = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(0, bottom); rect.offsetMax = new Vector2(0, -top);
    }

    // Toạ độ demo (x, y tính từ mép trên) -> anchoredPosition khi neo ở giữa mép trên.
    private static Vector2 Top(float x, float y) => new Vector2(x - 540f, -y);
    private static readonly Vector2 TopAnchor = new Vector2(0.5f, 1f);

    private static Image Img(Transform parent, string name, Sprite sprite, Vector2 position, Vector2? size = null, Vector2? anchor = null)
    {
        var image = Rect(parent, name, size ?? sprite.rect.size, position, anchor).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Txt(Transform parent, string name, string text, Vector2 size, Vector2 position, float fontSize,
        Color color, bool outlined = false, Vector2? anchor = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var label = Rect(parent, name, size, position, anchor).gameObject.AddComponent<TextMeshProUGUI>();
        Style(label, text, fontSize, color, outlined, align);
        return label;
    }

    private static void Style(TMP_Text label, string text, float fontSize, Color color, bool outlined,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        if (Font != null) label.font = Font;
        if (outlined && outline != null) label.fontSharedMaterial = outline;
        else if (Font != null) label.fontSharedMaterial = Font.material;
        if (text != null) label.text = text;
        label.fontSize = label.fontSizeMax = fontSize;
        label.fontSizeMin = fontSize * 0.6f;
        label.enableAutoSizing = true;
        label.alignment = align;
        label.color = color;
        label.lineSpacing = -18f;
        label.raycastTarget = false;
    }

    private static Button MakeButton(Image image, UnityAction action)
    {
        image.raycastTarget = true;
        var button = image.GetComponent<Button>();
        if (button == null) button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick = new Button.ButtonClickedEvent();
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform found = root.Find(path);
        if (found == null) Debug.LogWarning("[SushiHomeArtSetup] Không tìm thấy " + path);
        return found;
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child : Rect(parent, name, Vector2.zero, Vector2.zero);
    }

    // Đặt sprite cho Image có sẵn, giữ đúng kích thước gốc.
    private static Image Skin(Transform target, Sprite sprite, Vector2 position, Vector2? size = null)
    {
        var image = target.GetComponent<Image>();
        if (image == null) image = target.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size ?? sprite.rect.size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return image;
    }

    private static void Place(Transform target, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void Hide(Transform root, string path)
    {
        Transform target = root.Find(path);
        if (target != null) target.gameObject.SetActive(false);
    }

    private static void HideChildren(Transform target)
    {
        foreach (Transform child in target) child.gameObject.SetActive(false);
    }

    private static void Bind(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        var property = data.FindProperty(field);
        if (property == null) { Debug.LogWarning("[SushiHomeArtSetup] Không có field " + field); return; }
        property.objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BindArray(Object target, string field, Object[] values)
    {
        var data = new SerializedObject(target);
        var list = data.FindProperty(field);
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetValue(Object target, string field, float value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string field, Color value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).colorValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    // Ví xu dùng chung cho header trang Shop/Ranking.
    private static TMP_Text Wallet(Transform parent, Vector2 coinCenter, UnityAction openShop)
    {
        Image frame = Img(parent, "Wallet", S("Home/fr_coin.png"), Top(coinCenter.x + 110, coinCenter.y), null, TopAnchor);
        MakeButton(frame, openShop);
        Img(frame.transform, "Coin", S("Home/ic_coin.png"), new Vector2(-105, 0));
        Img(frame.transform, "Plus", S("Home/bt_plus.png"), new Vector2(-65, -33));
        return Txt(frame.transform, "Coins", "0", new Vector2(130, 60), new Vector2(18, 0), 40, Brown);
    }

    // Gắn né tai thỏ cho phần tử (mặc định: dời xuống).
    private static void Inset(Component target, SushiUISafeTopInset.Mode mode = SushiUISafeTopInset.Mode.Move)
    {
        if (target == null) return;
        var inset = target.GetComponent<SushiUISafeTopInset>();
        if (inset == null) inset = target.gameObject.AddComponent<SushiUISafeTopInset>();
        inset.mode = mode;
    }

    private static void SettingsButton(Transform parent, Vector2 center, UnityAction open)
    {
        Image icon = Img(parent, "Settings", S("Home/bt_setting.png"), Top(center.x, center.y), null, TopAnchor);
        MakeButton(icon, open);
    }

    // Avatar = khung xanh + ảnh hamster.
    private static Image Avatar(Transform parent, string name, Vector2 position, float size, Sprite avatar)
    {
        RectTransform root = Rect(parent, name, Vector2.one * size, position);
        Img(root, "Frame", S("Home/avatar/fr_avt.png"), Vector2.zero, Vector2.one * size);
        return Img(root, "Avatar", avatar, Vector2.zero, Vector2.one * size);
    }

    // Trang con: Safe area / Viewport (chừa thanh tab) / Page content.
    private static RectTransform PageRoot(GameObject root, CanvasHomePage view, Color background, Sprite backgroundSprite = null)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        var rootRect = (RectTransform)root.transform;
        Stretch(rootRect);
        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = 25;
        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
        }
        RectTransform safe = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
        Stretch(safe);
        // Ảnh nền/header chạm đỉnh màn hình kể cả máy có tai thỏ; nút tự né bằng SushiUISafeTopInset.
        var safeArea = safe.gameObject.AddComponent<SushiUISafeArea>();
        var safeData = new SerializedObject(safeArea);
        safeData.FindProperty("applyTop").boolValue = false;
        safeData.ApplyModifiedPropertiesWithoutUndo();
        RectTransform viewport = Rect(safe, "Viewport", Vector2.zero, Vector2.zero);
        Stretch(viewport, NavHeight);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect(viewport, "Page content", Vector2.zero, Vector2.zero);
        Stretch(content);
        content.gameObject.AddComponent<CanvasGroup>();
        // Cắt mọi thứ tràn ngoài trang (vd. mái hiên rộng 2161px) để khi trượt tab không lộ sang trang bên cạnh.
        content.gameObject.AddComponent<RectMask2D>();
        Image back = Rect(content, "Background", Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        back.sprite = backgroundSprite;
        back.color = backgroundSprite != null ? Color.white : background;
        back.raycastTarget = true;
        Stretch(back.rectTransform);
        view.pageContent = content;
        return content;
    }

    // ScrollRect dọc; nội dung neo mép trên, toạ độ con theo demo (y tính từ mép trên của nội dung).
    private static RectTransform Scroll(Transform parent, string name, float top, float bottom, float contentHeight)
    {
        RectTransform area = Rect(parent, name, Vector2.zero, Vector2.zero);
        Stretch(area, bottom, top);
        var hit = area.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        area.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect(area, "Content", new Vector2(1080, contentHeight), Vector2.zero, TopAnchor);
        content.pivot = new Vector2(0.5f, 1f);
        var scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = area;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30f;
        return content;
    }

    // ---------------------------------------------------------------- Main menu

    private static void EditMainMenu()
    {
        string path = Prefabs + "UICMainMenu.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasMainMenu>();
            Transform t = root.transform;

            // Nền mới, ẩn các lớp phố cũ.
            Transform backdrop = Find(t, "RestaurantBackground");
            if (backdrop != null)
            {
                var image = backdrop.GetComponent<Image>();
                image.sprite = S("Home/background.png");
                image.type = Image.Type.Simple;
                image.color = Color.white;
                // AspectRatioFitter tự phủ kín màn hình; ảnh không tự giữ tỉ lệ (tránh dải trống trên/dưới).
                image.preserveAspect = false;
                var fit = backdrop.GetComponent<AspectRatioFitter>();
                if (fit != null) fit.aspectRatio = 2100f / 3150f;
                var backdropRect = (RectTransform)backdrop;
                backdropRect.anchorMin = backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
                backdropRect.sizeDelta = new Vector2(1280, 1920);
                HideChildren(backdrop);
            }

            // Trang chủ chiếm toàn bộ chiều cao trừ thanh tab; header nằm đè lên trên.
            var viewport = Find(t, "Safe area/Home viewport") as RectTransform;
            if (viewport != null) Stretch(viewport, NavHeight);

            // Tạm ẩn phần decor (toà nhà) và slide chuyển map.
            Hide(t, "Safe area/Home viewport/Home content/Restaurant decoration");
            Hide(t, "Safe area/Home viewport/Home content/Restaurant arrows");

            EditHeader(view, Find(t, "Safe area/Header"));
            EditSideButtons(view, Find(t, "Safe area/Home viewport/Home content"));
            EditActions(view, Find(t, "Safe area/Home viewport/Home content/Level and decoration"));
            EditNavigation(view, Find(t, "Safe area/Navigation"));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void EditHeader(CanvasMainMenu view, Transform header)
    {
        if (header == null) return;
        var rect = (RectTransform)header;
        rect.anchorMin = rect.anchorMax = TopAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080, 180);
        rect.anchoredPosition = new Vector2(0, -90);
        var group = header.GetComponent<CanvasGroup>();
        if (group == null) group = header.gameObject.AddComponent<CanvasGroup>();
        Bind(view, "headerGroup", group);
        // Toạ độ con: (x demo - 540, 90 - y demo).
        Vector2 H(float x, float y) => new Vector2(x - 540f, 90f - y);

        Transform profile = header.Find("Profile");
        if (profile != null)
        {
            profile.GetComponent<Graphic>().color = Color.clear;
            Place(profile, H(105, 100), new Vector2(153, 150));
            Hide(profile, "Ivory rim");
            Transform frame = FindOrCreate(profile, "Frame");
            frame.SetAsFirstSibling();
            Skin(frame, S("Home/avatar/fr_avt.png"), Vector2.zero, new Vector2(153, 150));
            Transform avatar = profile.Find("Avatar");
            if (avatar != null) { Skin(avatar, S("Home/avatar/avt1.png"), Vector2.zero, new Vector2(153, 150)); avatar.SetAsLastSibling(); }
            // ProfileHome đổi avatar lúc chạy: trỏ đúng vào khung/ảnh mới.
            var home = profile.GetComponent<ProfileHome>();
            if (home != null)
            {
                home.img_frame = frame.GetComponent<Image>();
                if (avatar != null) home.img_avatar = avatar.GetComponent<Image>();
                EditorUtility.SetDirty(home);
            }
        }

        Transform wallet = header.Find("Wallet");
        if (wallet != null)
        {
            Skin(wallet, S("Home/fr_coin.png"), H(355, 92));
            if (wallet.Find("Coin") is Transform coin) Skin(coin, S("Home/ic_coin.png"), new Vector2(-103, 0));
            if (wallet.Find("Coins") is Transform coins) { Place(coins, new Vector2(22, 0), new Vector2(130, 60)); Style(coins.GetComponent<TMP_Text>(), null, 40, Brown, false); }
            PlusBadge(wallet.Find("plus"), new Vector2(-62, -35));
        }

        foreach (string lives in new[] { "LivesNormal", "LivesUnlimitTime" })
        {
            Transform panel = header.Find(lives);
            if (panel == null) continue;
            Skin(panel, S("Home/fr_coin.png"), H(615, 92));
            bool unlimited = lives == "LivesUnlimitTime";
            if (panel.Find("Heart") is Transform heart)
                Skin(heart, unlimited ? S("Shop/ic_unlimited_heart.png") : S("Home/ic_heart.png"), new Vector2(-95, 0), new Vector2(105, 91));
            if (panel.Find("Lives count") is Transform count)
            {
                Place(count, new Vector2(-95, 2), new Vector2(70, 60));
                Style(count.GetComponent<TMP_Text>(), null, 44, Color.white, true);
                count.SetAsLastSibling();
            }
            if (panel.Find("Lives status") is Transform status)
            {
                Place(status, new Vector2(30, 0), new Vector2(125, 60));
                Style(status.GetComponent<TMP_Text>(), null, 38, Brown, false);
            }
            PlusBadge(panel.Find("plus"), new Vector2(-58, -35));
        }

        Transform stars = header.Find("Stars");
        if (stars != null)
        {
            Skin(stars, S("Home/fr_star.png"), H(858, 92));
            if (stars.Find("Star") is Transform star) Skin(star, S("Home/profile/ic_star.png"), new Vector2(-76, 2), new Vector2(98, 95));
            if (stars.Find("Star count") is Transform count)
            {
                Place(count, new Vector2(22, 0), new Vector2(100, 60));
                Style(count.GetComponent<TMP_Text>(), null, 40, Brown, false);
            }
        }

        Transform settings = header.Find("Settings");
        if (settings != null)
        {
            settings.GetComponent<Graphic>().color = Color.clear;
            Place(settings, H(1003, 90), new Vector2(95, 99));
            HideChildren(settings);
            Transform icon = FindOrCreate(settings, "Icon");
            icon.gameObject.SetActive(true);
            Skin(icon, S("Home/bt_setting.png"), Vector2.zero);
        }
    }

    private static void PlusBadge(Transform plus, Vector2 position)
    {
        if (plus == null) return;
        Skin(plus, S("Home/bt_plus.png"), position);
        foreach (Transform child in plus)
            if (child.GetComponent<Graphic>() != null) child.gameObject.SetActive(false);
        plus.SetAsLastSibling();
    }

    // Nút tròn hai bên: Daily Reward / Lucky Spin bên trái, Remove Ads / Daily login bên phải.
    private static void EditSideButtons(CanvasMainMenu view, Transform home)
    {
        if (home == null) return;
        Transform removeAds = home.Find("RemoveAds");
        Transform login = home.Find("DailyLogin");
        if (removeAds == null && login != null)
        {
            removeAds = Object.Instantiate(login.gameObject, home).transform;
            removeAds.name = "RemoveAds";
        }
        if (removeAds != null)
        {
            MakeButton(removeAds.GetComponent<Image>(), view.OpenShop);
            Transform icon = FindOrCreate(removeAds, "Icon");
            Skin(icon, S("Home/ic_no_ads.png"), new Vector2(0, 8), new Vector2(118, 126));
            Hide(removeAds, "noti");
        }
        SideButton(home.Find("DailyReward"), 117, 438, "Daily\nReward", "Home/fr_icon/ic_daily_reward_top.png");
        SideButton(home.Find("Spin"), 117, 655, "Lucky Spin", "Home/fr_icon/ic_lucky_spin_top.png");
        SideButton(removeAds, 965, 438, "Remove\nAds", null);
        SideButton(login, 965, 655, "Daily login", "Home/fr_icon/ic_daily_login_top.png");
    }

    // iconPath: icon đã cắt bỏ tấm giấy ở dưới; đáy icon giấu sau ribbon.
    private static void SideButton(Transform button, float x, float y, string title, string iconPath)
    {
        if (button == null) return;
        if (iconPath != null)
        {
            Sprite cropped = S(iconPath);
            Transform holder = FindOrCreate(button, "Icon");
            float width = 172f;
            Vector2 size = new Vector2(width, width * cropped.rect.height / cropped.rect.width);
            var iconImage = Skin(holder, cropped, Vector2.zero, size);
            var iconRect = iconImage.rectTransform;
            iconRect.pivot = new Vector2(0.5f, 0f);
            iconRect.anchoredPosition = new Vector2(0, -44);
        }
        var image = button.GetComponent<Image>();
        Sprite round = S("Home/fr_icon/round.png");
        Transform icon = button.Find("Icon");
        // Lần đầu: ảnh gốc của nút chính là icon -> chuyển xuống con "Icon".
        if (icon == null)
        {
            Sprite original = image.sprite;
            icon = Rect(button, "Icon", Vector2.zero, Vector2.zero);
            Skin(icon, original, new Vector2(0, 8), new Vector2(130, 130));
        }
        image.sprite = round;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        var rect = (RectTransform)button;
        rect.anchorMin = rect.anchorMax = TopAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = round.rect.size;
        rect.anchoredPosition = Top(x, y);
        rect.localScale = Vector3.one;
        icon.SetAsFirstSibling();
        Transform ribbon = FindOrCreate(button, "Ribbon");
        Skin(ribbon, S("Home/fr_icon/ribbon.png"), new Vector2(0, -59));
        ribbon.SetAsLastSibling();
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.transform.parent != button) continue;
            Place(label.transform, new Vector2(0, -59), new Vector2(170, 62));
            Style(label, title, 30, Color.white, true);
            label.lineSpacing = -30f;
            label.transform.SetAsLastSibling();
        }
        if (button.Find("noti") is Transform noti)
        {
            var notiRect = (RectTransform)noti;
            notiRect.anchorMin = notiRect.anchorMax = new Vector2(0.5f, 0.5f);
            notiRect.anchoredPosition = new Vector2(70, 62);
            noti.SetAsLastSibling();
        }
    }

    private static void EditActions(CanvasMainMenu view, Transform actions)
    {
        if (actions == null) return;
        var rect = (RectTransform)actions;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080, 250);
        rect.anchoredPosition = new Vector2(0, PageBottom - 1522f);
        Hide(actions, "Level shadow");
        Hide(actions, "Decoration shadow");

        Transform play = actions.Find("Play level");
        if (play != null)
        {
            Skin(play, S("Home/bt_green_big2.png"), new Vector2(272 - 540, 0));
            Hide(play, "Top shine");
            if (play.Find("Level") is Transform level)
            {
                Place(level, new Vector2(0, 6), new Vector2(400, 130));
                Style(level.GetComponent<TMP_Text>(), null, 84, Color.white, true);
            }
        }

        Transform decorate = actions.Find("Decoration");
        if (decorate == null) return;
        // Toạ độ con theo tâm nút Decorate (808, 1522) trong demo.
        Vector2 D(float x, float y) => new Vector2(x - 808f, 1522f - y);
        Skin(decorate, S("Home/bt_orange_big2.png"), new Vector2(808 - 540, 0));
        if (decorate.Find("Decoration") is Transform icon) Skin(icon, S("Home/ic_home_decor.png"), D(676, 1485));
        if (decorate.Find("Title") is Transform title)
        {
            Place(title, D(845, 1485), new Vector2(290, 80));
            Style(title.GetComponent<TMP_Text>(), "Decorate", 58, Color.white, true);
        }
        if (decorate.Find("Progress bed") is Transform bed) Skin(bed, S("Home/progress1.png"), D(813, 1558));
        Transform fill = FindOrCreate(decorate, "Progress fill");
        var fillImage = Skin(fill, S("Home/progress2.png"), D(813, 1558));
        fillImage.preserveAspect = false;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f / 12f;
        Bind(view, "decorationFill", fillImage);
        if (decorate.Find("Progress") is Transform progress)
        {
            Place(progress, D(813, 1558), new Vector2(300, 46));
            var text = progress.GetComponent<TMP_Text>();
            Style(text, null, 34, Color.white, true);
            progress.SetAsLastSibling();
            Bind(view, "decorationProgress", text);
        }
        // Thanh fill vẽ ngay trên nền thanh tiến độ.
        if (decorate.Find("Progress bed") is Transform bedAgain) fill.SetSiblingIndex(bedAgain.GetSiblingIndex() + 1);
        if (decorate.Find("Progress") is Transform progressAgain) progressAgain.SetAsLastSibling();
        Transform badge = FindOrCreate(decorate, "Badge");
        Skin(badge, S("Home/fr_number.png"), D(1008, 1432));
        Transform count = badge.Find("Count");
        TMP_Text badgeText = count != null ? count.GetComponent<TMP_Text>()
            : Txt(badge, "Count", "0", new Vector2(70, 70), new Vector2(0, 3), 50, Color.white, true);
        badge.SetAsLastSibling();
        Bind(view, "decorationBadge", badge.gameObject);
        Bind(view, "decorationBadgeText", badgeText);
    }

    private static void EditNavigation(CanvasMainMenu view, Transform nav)
    {
        if (nav == null) return;
        var rect = (RectTransform)nav;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080, NavHeight);
        rect.anchoredPosition = new Vector2(0, NavHeight * 0.5f);
        // Thanh tab nằm trên các trang Shop/Ranking (order 25) nhưng dưới popup (>= 100).
        var navCanvas = nav.GetComponent<Canvas>();
        if (navCanvas == null) navCanvas = nav.gameObject.AddComponent<Canvas>();
        navCanvas.overrideSorting = true;
        navCanvas.sortingOrder = 30;
        if (nav.GetComponent<GraphicRaycaster>() == null) nav.gameObject.AddComponent<GraphicRaycaster>();
        if (nav.Find("Navigation background") is Transform background)
            Skin(background, S("Home/home_task_bar/fr.png"), Vector2.zero, new Vector2(1080, NavHeight)).preserveAspect = false;
        Hide(nav, "Gold rail");
        foreach (Transform child in nav)
            if (child.name == "Tab divider") child.gameObject.SetActive(false);
        Hide(nav, "Tab selection");

        string[] names = { "Shop", "Home", "Leaderboard" };
        string[] icons = { "ic_shop.png", "ic_home.png", "ic_ranking.png" };
        string[] labels = { "Shop", "Home", "Ranking" };
        var buttons = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            Transform tab = nav.Find(names[i]);
            if (tab == null) return;
            var image = Skin(tab, S("Home/home_task_bar/fr2.png"), new Vector2((i - 1) * 360f, -(NavHeight - 211f) * 0.5f), new Vector2(336, 211));
            image.preserveAspect = false;
            image.raycastTarget = true;
            buttons[i] = (RectTransform)tab;
            if (tab.Find("Animated icon") is Transform animated)
            {
                Place(animated, Vector2.zero, new Vector2(230, 200));
                foreach (Transform child in animated)
                    Skin(child, S("Home/home_task_bar/" + icons[i]), Vector2.zero);
            }
            if (tab.Find("Label") is Transform label)
            {
                Place(label, new Vector2(0, -64), new Vector2(330, 70));
                Style(label.GetComponent<TMP_Text>(), labels[i], 54, Color.white, true);
                label.SetAsLastSibling();
            }
        }
        BindArray(view, "tabButtons", buttons);
        Bind(view, "selectedTabSprite", S("Home/home_task_bar/fr_1.png"));
        Bind(view, "idleTabSprite", S("Home/home_task_bar/fr2.png"));
        SetValue(view, "selectedTabWidth", 408f);
        SetValue(view, "selectedIconY", 70f);
        SetValue(view, "selectedIconScale", 1f);
        SetValue(view, "idleIconScale", 0.78f);
        SetColor(view, "selectedLabelColor", Color.white);
        SetColor(view, "idleLabelColor", new Color(1, 1, 1, 0));
    }

    // ---------------------------------------------------------------- Shop

    private static void BuildShop()
    {
        string path = Prefabs + "UICShop.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasShop>();
            if (view == null) view = root.AddComponent<CanvasShop>();
            RectTransform page = PageRoot(root, view, ShopBackground);

            const float contentHeight = 4560f;
            RectTransform list = Scroll(page, "Packs", 0, 0, contentHeight);
            var prices = new System.Collections.Generic.List<TMP_Text>();
            var packs = new System.Collections.Generic.List<CanvasShop.Pack>();
            CanvasShop.Pack Add(string id, string title, int coins, int boosters, int hours, bool ads)
            {
                var pack = new CanvasShop.Pack { id = id, title = title, coins = coins, tongs = boosters, magnet = boosters,
                    swap = boosters, unlimitedHours = hours, removeAds = ads, price = "6.99$" };
                packs.Add(pack);
                return pack;
            }

            // 0. Remove Ads Pack
            Add("remove_ads", "Remove Ads Pack", 0, 0, 0, true);
            Image removeAds = Img(list, "Remove Ads Pack", S("Shop/remove_ads_pack.png"), Top(540, 450), null, TopAnchor);
            Txt(list, "Title", "Remove\nAds Pack", new Vector2(320, 160), Top(520, 455), 62, Color.white, true, TopAnchor);
            prices.Add(BuyButton(view, list, Top(838, 463), 0));
            Nest(removeAds.rectTransform);

            // 1. Starter Pack (khung vàng)
            Add("starter", "Starter Pack", 500, 1, 0, false);
            Image starter = Img(list, "Starter Pack", S("Shop/starter_pack.png"), Top(540, 910), null, TopAnchor);
            SaleTag(list, Top(98, 700));
            Img(list, "Coins", S("Shop/img_coin2.png"), Top(255, 815), null, TopAnchor);
            Txt(list, "Coin amount", "500", new Vector2(220, 80), Top(243, 905), 64, Color.white, true, TopAnchor);
            Img(list, "Rewards", S("Shop/fr_reward1.png"), Top(713, 810), null, TopAnchor);
            Booster(list, "Shop/ic_tongs.png", Top(535, 805), Top(575, 838));
            Booster(list, "Shop/ic_magnet.png", Top(710, 800), Top(748, 838));
            Booster(list, "Shop/ic_swap.png", Top(885, 805), Top(933, 838));
            Title(list, "Starter Pack", Top(330, 1043));
            prices.Add(BuyButton(view, list, Top(800, 1040), 1));
            Nest(starter.rectTransform);

            // 2-5. Bundle khung đỏ / tím
            Bundle(view, list, prices, Add("remove_ads_bundle", "Remove Ads Bundle", 600, 1, 6, true), 2, 1205, false, "img_coin1.png", true, true, null);
            Bundle(view, list, prices, Add("professional", "Professional Bundle", 1500, 1, 6, false), 3, 1775, false, "img_coin3.png", false, false, null);
            Bundle(view, list, prices, Add("master", "Master Bundle", 1500, 1, 6, false), 4, 2345, true, "img_coin4.png", false, false, "Most Popular");
            Bundle(view, list, prices, Add("mega", "Mega Bundle", 1500, 1, 6, false), 5, 2915, true, "img_coin5.png", false, false, "Best Value");

            // 6-11. Gói xu: lưới 3 cột (GridLayoutGroup), mua lại được nhiều lần nên không ẩn sau khi mua.
            RectTransform grid = Rect(list, "Coin packs", new Vector2(1014, 984), Top(540, 3988), TopAnchor);
            string[] coins = { "img_coin1.png", "img_coin2.png", "img_coin3.png", "img_coin4.png", "img_coin5.png", "img_coin6.png" };
            for (int i = 0; i < coins.Length; i++)
            {
                float x = 197 + (i % 3) * 343f;
                float y = 3735 + (i / 3) * 506f;
                Add("coins_" + (i + 1), "Coin pack " + (i + 1), 1500, 0, 0, false).hideAfterPurchase = false;
                Image card = Img(grid, "Coin pack " + (i + 1), S("Shop/coin_pack.png"), Top(x, y), null, TopAnchor);
                Img(grid, "Coins", S("Shop/" + coins[i]), Top(x, y - 80), new Vector2(230, 190), TopAnchor);
                Txt(grid, "Coin amount", "1500", new Vector2(250, 80), Top(x, y - 5), 64, Color.white, true, TopAnchor);
                prices.Add(BuyButton(view, grid, Top(x, y + 128), 6 + i, 0.88f));
                Nest(card.rectTransform);
            }
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(327, 478);
            gridLayout.spacing = new Vector2(16.5f, 28);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            // Danh sách xếp dọc bằng VerticalLayoutGroup: ẩn một gói thì các gói sau tự đẩy lên.
            var views = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in list)
            {
                if (child == grid) continue;
                views.Add(child.gameObject);
                child.gameObject.AddComponent<LayoutElement>().preferredHeight = ((RectTransform)child).sizeDelta.y;
            }
            foreach (Transform child in grid) views.Add(child.gameObject);
            var vertical = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(0, 0, 279, 60);
            vertical.spacing = 19;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = false;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
            var fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            view.packViews = views.ToArray();
            view.listContent = list;
            view.coinGrid = grid.gameObject;

            // Header: mái hiên + ví xu + tiêu đề + cài đặt (đè lên danh sách khi cuộn), gom trong một nhóm.
            RectTransform header = Rect(page, "Header", new Vector2(1080, 260), Top(540, 130), TopAnchor);
            Img(header, "Roof", S("Shop/roof.png"), Top(540, 118), null, TopAnchor);
            view.coinText = Wallet(header, new Vector2(80, 75), view.OpenShop);
            Inset(view.coinText.transform.parent);
            Inset(Img(header, "Title", S("Shop/Shop.png"), Top(540, 92), null, TopAnchor));
            SettingsButton(header, new Vector2(1003, 75), view.OpenSettings);
            Inset(header.Find("Settings"));

            view.packs = packs.ToArray();
            view.priceLabels = prices.ToArray();
            EditorUtility.SetDirty(view);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static TMP_Text BuyButton(CanvasShop view, Transform parent, Vector2 position, int index, float scale = 1f)
    {
        Sprite sprite = S("Shop/bt_buy.png");
        Image image = Img(parent, "Buy " + index, sprite, position, sprite.rect.size * scale, TopAnchor);
        Button button = MakeButton(image, null);
        UnityEventTools.AddIntPersistentListener(button.onClick, view.Buy, index);
        return Txt(image.transform, "Price", "6.99$", new Vector2(240 * scale, 100 * scale), new Vector2(0, 6 * scale), 58 * scale, Color.white, true);
    }

    private static void SaleTag(Transform parent, Vector2 position)
    {
        Image tag = Img(parent, "Sale tag", S("Shop/tag_sale.png"), position, null, TopAnchor);
        TMP_Text text = Txt(tag.transform, "Off", "30%\nOFF", new Vector2(150, 120), new Vector2(0, 4), 46, Color.white, true);
        text.lineSpacing = -35f;
        text.rectTransform.localRotation = Quaternion.Euler(0, 0, 18f);
    }

    private static void Booster(Transform parent, string sprite, Vector2 position, Vector2 countPosition, float scale = 0.85f)
    {
        Sprite icon = S(sprite);
        Img(parent, Path.GetFileNameWithoutExtension(sprite), icon, position, icon.rect.size * scale, TopAnchor);
        Txt(parent, "x1", "x1", new Vector2(110, 70), countPosition, 52, Color.white, true, TopAnchor);
    }

    private static void Title(Transform parent, string text, Vector2 position)
    {
        Txt(parent, "Title", text, new Vector2(470, 150), position, 62, Color.white, true, TopAnchor);
    }

    // Bundle cao 551: toạ độ con tính theo mép trên `top` của khung (lấy từ demo).
    private static void Bundle(CanvasShop view, Transform list, System.Collections.Generic.List<TMP_Text> prices, CanvasShop.Pack pack,
        int index, float top, bool purple, string coinSprite, bool sale, bool removeAds, string ribbon)
    {
        Vector2 P(float x, float y) => Top(x, top + y);
        Image frame = Img(list, pack.title, S(purple ? "Shop/purple_pack.png" : "Shop/red_pack.png"), P(540, 275), null, TopAnchor);
        if (sale) SaleTag(list, P(98, 60));
        if (removeAds)
        {
            Sprite coins = S("Shop/" + coinSprite);
            Img(list, "Coins", coins, P(165, 185), coins.rect.size * 0.8f, TopAnchor);
            Txt(list, "Coin amount", pack.coins.ToString(), new Vector2(200, 80), P(165, 268), 64, Color.white, true, TopAnchor);
            Txt(list, "Plus", "+", new Vector2(70, 80), P(275, 200), 70, Color.white, true, TopAnchor);
            Img(list, "No ads", S("Shop/ic_no_ads.png"), P(367, 200), new Vector2(118, 125), TopAnchor);
        }
        else
        {
            Img(list, "Coins", S("Shop/" + coinSprite), P(255, 170), null, TopAnchor);
            Txt(list, "Coin amount", pack.coins.ToString(), new Vector2(240, 80), P(255, 262), 64, Color.white, true, TopAnchor);
        }
        Img(list, "Rewards", S("Shop/fr_reward2.png"), P(597, 175), null, TopAnchor);
        Booster(list, "Shop/ic_tongs.png", P(515, 115), P(550, 153), 0.75f);
        Booster(list, "Shop/ic_magnet.png", P(675, 115), P(705, 153), 0.75f);
        Booster(list, "Shop/ic_swap.png", P(598, 225), P(640, 263), 0.75f);
        Img(list, "Heart reward", S("Shop/fr_reward3.png"), P(880, 175), null, TopAnchor);
        Img(list, "Unlimited heart", S("Shop/ic_unlimited_heart.png"), P(885, 150), null, TopAnchor);
        Txt(list, "Hours", pack.unlimitedHours + "h", new Vector2(150, 70), P(880, 265), 54, Color.white, true, TopAnchor);
        if (purple)
        {
            Image tag = Img(list, "Tag", S("Shop/tag_popular.png"), P(222, 350), null, TopAnchor);
            Txt(tag.transform, "Text", ribbon, new Vector2(330, 70), new Vector2(-5, 8), 40, Popular, true);
            Txt(list, "Title", pack.title, new Vector2(520, 100), P(345, 435), 62, Color.white, true, TopAnchor);
            prices.Add(BuyButton(view, list, P(812, 402), index));
        }
        else
        {
            Title(list, pack.title.Replace(" Bundle", "\nBundle"), P(305, 410));
            prices.Add(BuyButton(view, list, P(800, 407), index));
        }
        Nest(frame.rectTransform);
    }

    // Chuyển mọi phần tử tạo sau khung pack (cùng cha, cùng neo mép trên) vào làm con của khung,
    // đổi toạ độ sang tương đối với tâm khung; vị trí trên màn hình giữ nguyên.
    private static void Nest(RectTransform pack)
    {
        Transform list = pack.parent;
        var items = new System.Collections.Generic.List<RectTransform>();
        for (int i = pack.GetSiblingIndex() + 1; i < list.childCount; i++) items.Add((RectTransform)list.GetChild(i));
        foreach (RectTransform item in items)
        {
            Vector2 local = item.anchoredPosition - pack.anchoredPosition;
            item.SetParent(pack, false);
            item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
            item.anchoredPosition = local;
        }
    }

    // ---------------------------------------------------------------- Ranking

    private static void BuildRanking()
    {
        string path = Prefabs + "UICLeaderBoard.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasLeadBoard>();
            if (view == null) view = root.AddComponent<CanvasLeadBoard>();
            RectTransform page = PageRoot(root, view, Color.white, S("Ranking/bg.png"));
            // Trời chỉ hiện phía trên dải cam (tab Rank có dải thấp, không che hết ảnh trời).
            RectTransform skyMask = Rect(page, "Sky", new Vector2(1080, 330), Vector2.zero, TopAnchor);
            skyMask.pivot = new Vector2(0.5f, 1f);
            skyMask.gameObject.AddComponent<RectMask2D>();
            Img(skyMask, "Clouds", S("Ranking/bg_top.png"), new Vector2(0, -328), null, TopAnchor);
            Sprite[] avatars = Enumerable.Range(1, 9).Select(i => S($"Home/avatar/avt{i}.png")).ToArray();

            // Tab Weekly / Rank (neo đáy tab tại y = 310 của demo).
            Image weekly = Tab(page, "Weekly tab", 285, "Weekly", view.ShowWeekly);
            Image rank = Tab(page, "Rank tab", 795, "Rank", view.ShowRank);

            // --- Weekly
            RectTransform weeklyView = Rect(page, "Weekly", Vector2.zero, Vector2.zero);
            Stretch(weeklyView);
            Img(weeklyView, "Band", S("Ranking/fr_mid1.png"), Top(540, 703), null, TopAnchor);
            Img(weeklyView, "Podium panel", S("Ranking/fr_mid.png"), Top(540, 757), null, TopAnchor);
            Img(weeklyView, "Light", S("Ranking/light.png"), Top(540, 640), null, TopAnchor);
            Img(weeklyView, "Info", S("Ranking/noti.png"), Top(98, 488), null, TopAnchor);
            // Thứ tự mảng: hạng 1, 2, 3.
            float[] xs = { 540, 233, 855 };
            float[] flagY = { 640, 690, 690 };
            string[] flags = { "flag_gold.png", "flag_silver.png", "flag_bronze.png" };
            string[] medals = { "m_gold1.png", "m_silver1.png", "m_bronze1.png" };
            float[] medalY = { 497, 555, 552 };
            var podiumAvatars = new Image[3];
            var podiumNames = new TMP_Text[3];
            var podiumScores = new TMP_Text[3];
            for (int i = 2; i >= 0; i--)
            {
                Img(weeklyView, "Flag " + (i + 1), S("Ranking/" + flags[i]), Top(xs[i], flagY[i]), null, TopAnchor);
                RectTransform holder = Rect(weeklyView, "Podium " + (i + 1), new Vector2(130, 130), Top(xs[i], flagY[i] - 25), TopAnchor);
                podiumAvatars[i] = Avatar(holder, "Avatar", Vector2.zero, 128, avatars[0]);
                podiumNames[i] = Txt(weeklyView, "Name " + (i + 1), "William", new Vector2(220, 60), Top(xs[i] + 4, flagY[i] + 70), 42, TextBrown, false, TopAnchor);
                Img(weeklyView, "Medal " + (i + 1), S("Ranking/" + medals[i]), Top(xs[i], medalY[i]), null, TopAnchor);
            }
            Img(weeklyView, "Chests", S("Ranking/chest.png"), Top(540, 950), null, TopAnchor);
            float[] scoreX = { 540, 233, 847 };
            for (int i = 0; i < 3; i++)
                podiumScores[i] = Txt(weeklyView, "Score " + (i + 1), "Score\n0", new Vector2(220, 120), Top(scoreX[i], 1025), 48, TextBrown, false, TopAnchor);
            Image ribbon = Img(weeklyView, "Ribbon", S("Ranking/ribbon.png"), Top(540, 405), null, TopAnchor);
            Txt(ribbon.transform, "Title", "Weekly Contest", new Vector2(520, 90), new Vector2(0, 12), 60, Color.white, true);
            RectTransform weeklyList = Scroll(weeklyView, "List", 1140, PlayerRowHeight, 600);
            SushiLeaderboardRow weeklyMe = Row(weeklyView, "Player row", S("Ranking/fr_green.png"), avatars[0]);

            // --- Rank (World / Country)
            RectTransform rankView = Rect(page, "Rank", Vector2.zero, Vector2.zero);
            Stretch(rankView);
            Img(rankView, "Band", S("Ranking/Rank/fr_mid.png"), Top(540, 394), null, TopAnchor);
            RectTransform rankList = Scroll(rankView, "List", 525, PlayerRowHeight, 600);
            Image toggle = Rect(rankView, "Scope", new Vector2(615, 100), Top(535, 413), TopAnchor).gameObject.AddComponent<Image>();
            toggle.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");
            toggle.type = Image.Type.Sliced;
            toggle.color = new Color(0.69f, 0.42f, 0.22f);
            toggle.raycastTarget = false;
            Image world = Img(rankView, "World selected", S("Ranking/Rank/bt_yellow1.png"), Top(382, 413), null, TopAnchor);
            Image country = Img(rankView, "Country selected", S("Ranking/Rank/bt_yellow2.png"), Top(688, 413), null, TopAnchor);
            ScopeButton(rankView, "World", 382, view.ShowWorld);
            ScopeButton(rankView, "Country", 688, view.ShowCountry);
            SushiLeaderboardRow rankMe = Row(rankView, "Player row", S("Ranking/fr_green.png"), avatars[0]);

            // Thời gian còn lại của tuần, nằm đè lên tab Weekly.
            Image timer = Img(page, "Timer", S("Ranking/time.png"), Top(287, 315), null, TopAnchor);
            TMP_Text timerText = Txt(timer.transform, "Time", "7d0h", new Vector2(130, 56), new Vector2(22, 0), 36, Color.white, false);

            // Header
            view.coinText = Wallet(page, new Vector2(80, 65), view.OpenShop);
            Inset(view.coinText.transform.parent);
            Inset(Img(page, "Title", S("Ranking/Ranking.png"), Top(540, 80), null, TopAnchor));
            SettingsButton(page, new Vector2(1003, 65), view.OpenSettings);
            Inset(page.Find("Settings"));
            // Tai thỏ: tab, đồng hồ và phần nội dung tụt xuống; bầu trời kéo dài xuống để vẫn che kín phía sau tab.
            Inset(weekly); Inset(rank); Inset(timer);
            Inset(weeklyView, SushiUISafeTopInset.Mode.ShrinkTop);
            Inset(rankView, SushiUISafeTopInset.Mode.ShrinkTop);
            Inset(skyMask, SushiUISafeTopInset.Mode.Grow);

            SushiLeaderboardRow template = Row(page, "Row template", S("Ranking/fr_white.png"), avatars[0]);
            template.gameObject.SetActive(false);
            foreach (SushiLeaderboardRow pinned in new[] { weeklyMe, rankMe })
            {
                var rect = (RectTransform)pinned.transform;
                // Dòng của người chơi nằm sát ngay trên thanh tab.
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0, 2f);
            }

            view.weeklyTab = weekly; view.rankTab = rank;
            view.tabSelected = S("Ranking/fr1.png"); view.tabIdle = S("Ranking/fr2.png");
            view.weeklyView = weeklyView.gameObject; view.rankView = rankView.gameObject;
            view.timerText = timerText;
            view.worldSelected = world.gameObject; view.countrySelected = country.gameObject;
            view.podiumAvatars = podiumAvatars; view.podiumNames = podiumNames; view.podiumScores = podiumScores;
            view.rowTemplate = template;
            view.weeklyList = weeklyList; view.rankList = rankList;
            view.weeklyPlayerRow = weeklyMe; view.rankPlayerRow = rankMe;
            view.rowSpacing = 192f;
            view.medals = new[] { S("Ranking/Rank/m_gold2.png"), S("Ranking/Rank/m_silver2.png"), S("Ranking/Rank/m_bronze2.png") };
            view.avatars = avatars;
            weeklyView.gameObject.SetActive(true);
            rankView.gameObject.SetActive(false);
            EditorUtility.SetDirty(view);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ---------------------------------------------------------------- Profile

    // Avatar hồ sơ dùng bộ art mới (avt1..9) và một khung xanh chung.
    private static void UpdateProfileData()
    {
        var data = AssetDatabase.LoadAssetAtPath<ProfileDataFragment>("Assets/_game/DataSO/Profile Fragment.asset");
        if (data == null) { Debug.LogWarning("[SushiHomeArtSetup] Không tìm thấy Profile Fragment.asset"); return; }
        var serialized = new SerializedObject(data);
        var avatars = serialized.FindProperty("spr_avatars");
        avatars.arraySize = 9;
        for (int i = 0; i < 9; i++) avatars.GetArrayElementAtIndex(i).objectReferenceValue = S($"Home/avatar/avt{i + 1}.png");
        var frames = serialized.FindProperty("spr_frames");
        frames.arraySize = 1;
        frames.GetArrayElementAtIndex(0).objectReferenceValue = S("Home/avatar/fr_avt.png");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    // Toạ độ demo profile (1080x1920) -> vị trí trong khung 1080x1920 neo giữa.
    private static Vector2 C(float x, float y) => new Vector2(x - 540f, 960f - y);

    private static void BuildProfile()
    {
        string path = Prefabs + "UICProfile.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<CanvasProfile>();
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            Stretch((RectTransform)root.transform);
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0;
            }
            Image dim = Rect(root.transform, "bg", Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            dim.color = new Color(0.08f, 0.04f, 0.02f, 0.78f);
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            RectTransform safe = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
            Stretch(safe);
            safe.gameObject.AddComponent<SushiUISafeArea>();
            Sprite avatar = S("Home/avatar/avt1.png");

            // ===== Bảng xem (profile1.png)
            RectTransform main = Rect(safe, "normal panel", new Vector2(1080, 1920), Vector2.zero);
            main.gameObject.AddComponent<SushiUIFitContent>();
            Img(main, "Popup", S("Home/profile/fr_popup.png"), C(540, 1015));
            PopupTitle(main, 420);
            CloseButton(main, view, 490);
            Img(main, "Info card", S("Home/profile/fr1.png"), C(540, 685));
            view.avatarImage = Avatar(main, "Avatar", C(275, 680), 178, avatar);
            view.frameImage = view.avatarImage.transform.parent.Find("Frame").GetComponent<Image>();
            MakeButton(Img(main, "Edit", S("Home/profile/bt_edit.png"), C(346, 748)), view.OnOpenEditProfile);
            view.userNameText = Txt(main, "Name", "William", new Vector2(310, 70), C(540, 635), 54, Color.white, true,
                null, TextAlignmentOptions.MidlineLeft);
            Img(main, "Calendar", S("Home/profile/ic_calendar.png"), C(412, 708));
            view.joinDateText = Txt(main, "Join date", "10/2026", new Vector2(220, 50), C(550, 712), 34, TextBrown, false,
                null, TextAlignmentOptions.MidlineLeft);
            view.idText = Txt(main, "ID", "ID: 000000000", new Vector2(240, 50), C(510, 752), 32, TextBrown, false);
            MakeButton(Img(main, "Copy", S("Home/profile/bt_copy.png"), C(662, 750)), view.CopyId);
            Image level = Img(main, "Level", S("Home/profile/fr_level.png"), C(806, 650));
            view.levelText = Txt(level.transform, "Level text", "Level\n1", new Vector2(160, 120), new Vector2(0, 20), 50, TextBrown);
            view.levelText.lineSpacing = -30f;

            Txt(main, "Stats title", "General Stats", new Vector2(420, 80), C(540, 866), 58, TextBrown);
            Img(main, "Line left", S("Home/profile/line1.png"), C(238, 864));
            Img(main, "Line right", S("Home/profile/line1.png"), C(842, 864));
            string[] icons = { "ic_first_try_win.png", "ic_star.png", "ic_restaurant.png" };
            string[] titles = { "First try wins", "Star", "Restaurant" };
            float[] xs = { 275, 537, 800 };
            var values = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                Img(main, titles[i] + " card", S("Home/profile/fr2.png"), C(xs[i], 1100));
                Img(main, titles[i] + " icon", S("Home/profile/" + icons[i]), C(xs[i], 1025));
                Txt(main, titles[i] + " title", titles[i], new Vector2(225, 50), C(xs[i], 1127), 34, TextBrown);
                Img(main, titles[i] + " line", S("Home/profile/line2.png"), C(xs[i], 1158));
                values[i] = Txt(main, titles[i] + " value", "0", new Vector2(220, 80), C(xs[i], 1212), 60, Color.white, true);
            }
            view.firstTryText = values[0]; view.starText = values[1]; view.restaurantText = values[2];
            Image google = Img(main, "Google", S("Home/profile/bt_blue_big3.png"), C(540, 1418));
            MakeButton(google, view.OnGoogleButton);
            view.googleButton = google;
            view.googleLabel = Img(google.transform, "Label", S("Home/profile/Link_Google_Play_Games.png"), new Vector2(0, 4));
            view.googleLinkButton = S("Home/profile/bt_blue_big3.png");
            view.googleLinkedButton = S("Home/profile/bt_gray_big3.png");
            view.googleLinkLabel = S("Home/profile/Link_Google_Play_Games.png");
            view.googleLinkedLabel = S("Home/profile/Linked_Google_Play_Games.png");

            // ===== Bảng sửa (profile2.png)
            RectTransform edit = Rect(safe, "Edit Panel", new Vector2(1080, 1920), Vector2.zero);
            edit.gameObject.AddComponent<SushiUIFitContent>();
            Img(edit, "Popup", S("Home/profile/edit/fr_popup.png"), C(540, 1005));
            PopupTitle(edit, 290);
            CloseButton(edit, view, 360);
            Img(edit, "Top card", S("Home/profile/edit/fr1.png"), C(540, 557));
            view.img_avatar = Avatar(edit, "Preview", C(275, 550), 178, avatar);
            view.img_frame = view.img_avatar.transform.parent.Find("Frame").GetComponent<Image>();
            view.nameInput = NameInput(edit, C(640, 557));
            MakeButton(Img(edit, "Edit name", S("Home/profile/bt_edit.png"), C(842, 557)), view.FocusName);
            Img(edit, "Grid card", S("Home/profile/edit/fr2.png"), C(540, 1048));
            var choices = new UIChooseAvatar[9];
            for (int i = 0; i < 9; i++)
                choices[i] = AvatarChoice(edit, i, C(293 + (i % 3) * 249.5f, 830 + (i / 3) * 207.5f));
            view.uIChooseAvatars = choices;
            Image save = Img(edit, "Save Button", S("Home/profile/edit/bt_green.png"), C(540, 1530));
            MakeButton(save, view.OnSave);
            Txt(save.transform, "Text", "Save", new Vector2(280, 110), new Vector2(0, 6), 78, Color.white, true);

            view.mainPanelObject = main.gameObject; view.mainPanelRect = main;
            view.editPanelObject = edit.gameObject; view.editPanelRect = edit;
            edit.gameObject.SetActive(false);
            EditorUtility.SetDirty(view);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void PopupTitle(Transform panel, float y)
    {
        Image title = Img(panel, "Title", S("Home/profile/fr_title.png"), C(540, y));
        Txt(title.transform, "Text", "Profile", new Vector2(420, 120), new Vector2(0, 12), 96, Color.white, true);
    }

    private static void CloseButton(Transform panel, CanvasProfile view, float y)
    {
        MakeButton(Img(panel, "Close", S("Home/profile/X.png"), C(940, y)), view.OnClose);
    }

    // Ô nhập tên (khung fr_name) dùng TMP_InputField.
    private static TMP_InputField NameInput(Transform parent, Vector2 position)
    {
        Image frame = Img(parent, "Name field", S("Home/profile/edit/fr_name.png"), position);
        frame.raycastTarget = true;
        var field = frame.gameObject.AddComponent<TMP_InputField>();
        RectTransform area = Rect(frame.transform, "Text Area", new Vector2(360, 80), new Vector2(-50, 0));
        area.gameObject.AddComponent<RectMask2D>();
        TMP_Text text = Txt(area, "Text", "", Vector2.zero, Vector2.zero, 52, TextBrown, false, null, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);
        text.enableAutoSizing = false;
        TMP_Text placeholder = Txt(area, "Placeholder", "Name", Vector2.zero, Vector2.zero, 52, new Color(TextBrown.r, TextBrown.g, TextBrown.b, 0.4f),
            false, null, TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform);
        placeholder.enableAutoSizing = false;
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.targetGraphic = frame;
        field.fontAsset = Font;
        field.pointSize = 52;
        field.characterLimit = 12;
        field.lineType = TMP_InputField.LineType.SingleLine;
        return field;
    }

    // Ô chọn avatar: khung xám/xanh + avatar + dấu tick.
    private static UIChooseAvatar AvatarChoice(Transform parent, int index, Vector2 position)
    {
        RectTransform root = Rect(parent, "Avatar " + (index + 1), new Vector2(175, 185), position);
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        var choice = root.gameObject.AddComponent<UIChooseAvatar>();
        Button button = MakeButton(hit, null);
        UnityEventTools.AddPersistentListener(button.onClick, choice.Btn_clickChooseThis);
        choice.idleFrame = S("Home/avatar/fr_avt_gray.png");
        choice.selectedFrame = S("Home/avatar/fr_avt_green.png");
        choice.img_frame = Img(root, "Frame", choice.idleFrame, Vector2.zero, new Vector2(160, 157));
        choice.img_avatar = Img(root, "Avatar", S($"Home/avatar/avt{index + 1}.png"), Vector2.zero, new Vector2(160, 157));
        choice.img_selecting = Img(root, "Tick", S("Home/profile/edit/tick.png"), new Vector2(60, -50));
        choice.img_selecting.gameObject.SetActive(index == 0);
        choice.id = index;
        choice.idAvatar = index;
        return choice;
    }

    private static Image Tab(Transform page, string name, float x, string title, UnityAction action)
    {
        Sprite sprite = S("Ranking/fr2.png");
        Image tab = Img(page, name, sprite, Top(x, 310), null, TopAnchor);
        tab.preserveAspect = false;
        tab.rectTransform.pivot = new Vector2(0.5f, 0f);
        MakeButton(tab, action);
        Txt(tab.transform, "Label", title, new Vector2(420, 90), new Vector2(0, 62), 66, TextBrown, false, new Vector2(0.5f, 0f));
        return tab;
    }

    private static void ScopeButton(Transform parent, string title, float x, UnityAction action)
    {
        Image hit = Rect(parent, title + " button", new Vector2(305, 100), Top(x, 413), TopAnchor).gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        MakeButton(hit, action);
        Txt(hit.transform, "Label", title, new Vector2(280, 80), Vector2.zero, 54, TextBrown);
    }

    // Dòng bảng xếp hạng (khung 1016x189): hạng/huy chương, avatar, tên, điểm.
    private static SushiLeaderboardRow Row(Transform parent, string name, Sprite frame, Sprite avatar)
    {
        Image background = Img(parent, name, frame, Vector2.zero, null, TopAnchor);
        background.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var row = background.gameObject.AddComponent<SushiLeaderboardRow>();
        row.medal = Img(background.transform, "Medal", S("Ranking/Rank/m_gold2.png"), new Vector2(-415, 0));
        row.rankText = Txt(background.transform, "Rank", "4", new Vector2(170, 110), new Vector2(-415, 0), 72, Color.white, true);
        row.avatar = Avatar(background.transform, "Avatar", new Vector2(-265, 0), 128, avatar);
        row.nameText = Txt(background.transform, "Name", "William", new Vector2(420, 80), new Vector2(15, 0), 58, TextBrown, false,
            null, TextAlignmentOptions.MidlineLeft);
        row.scoreText = Txt(background.transform, "Score", "Score\n0", new Vector2(230, 140), new Vector2(367, 0), 50, TextBrown);
        row.medal.gameObject.SetActive(false);
        return row;
    }
}
