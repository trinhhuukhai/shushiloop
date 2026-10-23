using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-time editor migration. Runtime views only bind data to the saved prefab components.
public static class SushiCustomerPrefabSetup
{
    private const string Root = "Assets/_game/Prefabs/";
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Resources/SushiRoundedPanel.png");

    [MenuItem("SushiKhai/Prefabs/Create Customer UI Prefabs")]
    public static void EnsurePrefabs()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing prefabs.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "OrderIngredient.prefab") == null) BuildIngredient();
        GameObject card = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "DishCard.prefab");
        if (card == null || card.GetComponent<RectTransform>() == null) BuildCard();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "CustomerVisuals.prefab") == null) BuildVisuals();
        BindCustomer();
        AssetDatabase.SaveAssets();
        Debug.Log("CUSTOMER_UI_PREFABS_OK: authored avatar, order cards, ingredient slots and rush display.");
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image Image(Transform parent, string name, Vector2 size, Sprite sprite, Color color)
    {
        Image image = Rect(parent, name, size).gameObject.AddComponent<Image>();
        image.sprite = sprite; image.color = color;
        image.preserveAspect = true; image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, Vector2 size, float fontSize, string sample)
    {
        var label = Rect(parent, name, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = sample; label.fontSize = fontSize;
        label.enableAutoSizing = true; label.fontSizeMin = 8; label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.color = new Color(0.2f, 0.1f, 0.04f);
        return label;
    }

    private static LayoutElement Layout(GameObject obj, float preferredHeight, float minimumHeight, float flexibleHeight = 0)
    {
        var element = obj.AddComponent<LayoutElement>();
        element.preferredHeight = preferredHeight; element.minHeight = minimumHeight;
        element.flexibleHeight = flexibleHeight; element.minWidth = 0; element.flexibleWidth = 1;
        return element;
    }

    private static VerticalLayoutGroup Vertical(GameObject obj, float spacing)
    {
        var group = obj.AddComponent<VerticalLayoutGroup>();
        group.childAlignment = TextAnchor.UpperCenter; group.spacing = spacing;
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = true; group.childForceExpandHeight = false;
        return group;
    }

    private static HorizontalLayoutGroup Horizontal(GameObject obj, float spacing)
    {
        var group = obj.AddComponent<HorizontalLayoutGroup>();
        group.childAlignment = TextAnchor.UpperCenter; group.spacing = spacing;
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = group.childForceExpandHeight = true;
        return group;
    }

    private static void Bind(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ColorField(Object target, string field, Color value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).colorValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Nested(string name, Transform parent) =>
        (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + name + ".prefab"), parent);

    private static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, Root + name + ".prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildIngredient()
    {
        RectTransform root = Rect(null, "OrderIngredient", new Vector2(44, 44));
        var view = root.gameObject.AddComponent<SushiOrderIngredientView>();
        Layout(root.gameObject, 44, 18, 1);
        Vertical(root.gameObject, 1);
        Image image = Image(root, "Image", new Vector2(34, 30), SushiArtCatalog.Default.GetIngredient(PoolType.SushiRice), Color.white);
        Layout(image.gameObject, 30, 8, 1);
        var progress = Text(root, "Progress", new Vector2(44, 13), 16, "0/1");
        Layout(progress.gameObject, 13, 8);
        RectTransform delivery = Rect(image.transform, "Delivery slot", Vector2.zero);
        // Food remains a world sprite. This anchor cancels the Canvas's pixels-to-world scale.
        delivery.localScale = Vector3.one * 100;
        Bind(view, "image", image); Bind(view, "progress", progress); Bind(view, "deliverySlot", delivery);
        ColorField(view, "pendingImageColor", new Color(1, 1, 1, 0.65f));
        ColorField(view, "incomingImageColor", new Color(1, 1, 1, 0.85f));
        ColorField(view, "completedImageColor", Color.white);
        ColorField(view, "pendingTextColor", new Color(0.3f, 0.24f, 0.18f));
        ColorField(view, "incomingTextColor", new Color(0.68f, 0.36f, 0.05f));
        ColorField(view, "completedTextColor", new Color(0.1f, 0.55f, 0.22f));
        Save(root.gameObject, "OrderIngredient");
    }

    private static void BuildCard()
    {
        RectTransform root = Rect(null, "DishCard", new Vector2(153, 138));
        var view = root.gameObject.AddComponent<SushiDishCardView>();
        Layout(root.gameObject, 138, 40);
        Image background = Image(root, "Background", root.sizeDelta, Rounded, new Color(1, 0.92f, 0.76f));
        background.preserveAspect = false; background.type = UnityEngine.UI.Image.Type.Sliced;
        Stretch(background.rectTransform);
        RectTransform content = Rect(root, "Content", root.sizeDelta);
        Stretch(content);
        VerticalLayoutGroup layout = Vertical(content.gameObject, 0);
        layout.padding = new RectOffset(6, 6, 4, 6);
        var title = Text(content, "Dish name", new Vector2(141, 20), 18, "Maki bơ");
        Layout(title.gameObject, 20, 8);
        Image dish = Image(content, "Dish image", new Vector2(66, 64), SushiArtCatalog.Default.GetDish(SushiDishId.MakiAvocado), Color.white);
        Layout(dish.gameObject, 64, 8, 1);
        RectTransform row = Rect(content, "Ingredients", new Vector2(141, 44));
        Layout(row.gameObject, 44, 18, 1);
        Horizontal(row.gameObject, 2);
        var data = new SerializedObject(view);
        var items = data.FindProperty("ingredients");
        items.arraySize = 3;
        for (int i = 0; i < items.arraySize; i++)
        {
            GameObject ingredient = Nested("OrderIngredient", row);
            ingredient.name = "Ingredient " + (i + 1);
            items.GetArrayElementAtIndex(i).objectReferenceValue = ingredient.GetComponent<SushiOrderIngredientView>();
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        RectTransform mark = Rect(root, "Completed mark", new Vector2(16, 16));
        mark.anchorMin = mark.anchorMax = Vector2.one;
        mark.anchoredPosition = new Vector2(-10, -27);
        Image shortStroke = Image(mark, "Short stroke", new Vector2(3, 7), null, new Color(0.1f, 0.55f, 0.22f));
        shortStroke.rectTransform.anchoredPosition = new Vector2(-3, -1);
        shortStroke.rectTransform.localRotation = Quaternion.Euler(0, 0, 40);
        Image longStroke = Image(mark, "Long stroke", new Vector2(3, 12), null, shortStroke.color);
        longStroke.rectTransform.anchoredPosition = new Vector2(2, 1);
        longStroke.rectTransform.localRotation = Quaternion.Euler(0, 0, -40);
        mark.gameObject.SetActive(false);
        Bind(view, "background", background); Bind(view, "dishName", title); Bind(view, "finished", dish);
        Bind(view, "completedMark", mark.gameObject);
        ColorField(view, "pendingColor", background.color);
        ColorField(view, "completedColor", new Color(0.79f, 0.94f, 0.75f));
        ColorField(view, "targetingColor", new Color(1, 0.76f, 0.3f));
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Save(root.gameObject, "DishCard");
    }

    private static void BuildVisuals()
    {
        RectTransform root = Rect(null, "CustomerVisuals", new Vector2(324, 212));
        root.localScale = Vector3.one * 0.01f;
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 10;
        Image avatar = Image(root, "Avatar", new Vector2(105, 105), SushiArtCatalog.Default.customerAvatars[0], Color.white);
        avatar.rectTransform.anchoredPosition = new Vector2(0, 65);
        RectTransform orders = Rect(root, "Orders", new Vector2(324, 212));
        orders.pivot = new Vector2(0.5f, 1); orders.anchoredPosition = new Vector2(0, 11);
        Horizontal(orders.gameObject, 10);
        foreach (string name in new[] { "Left column", "Right column" })
        {
            RectTransform column = Rect(orders, name, new Vector2(157, 212));
            Layout(column.gameObject, 212, 0, 1);
            Vertical(column.gameObject, 8);
        }
        RectTransform rush = Rect(root, "Rush visuals", root.sizeDelta);
        Image badge = Image(rush, "Timer badge", new Vector2(103, 40), Rounded, new Color(0.2f, 0.14f, 0.1f));
        badge.rectTransform.anchoredPosition = new Vector2(105, 65);
        badge.preserveAspect = false; badge.type = UnityEngine.UI.Image.Type.Sliced;
        var timer = Text(badge.transform, "Timer", badge.rectTransform.sizeDelta, 32, "00:30");
        Stretch(timer.rectTransform);
        Image track = Image(rush, "Countdown track", new Vector2(316, 5), Rounded, new Color(0.2f, 0.14f, 0.1f));
        track.rectTransform.anchoredPosition = new Vector2(0, 17);
        track.preserveAspect = false; track.type = UnityEngine.UI.Image.Type.Sliced;
        Image fill = Image(track.transform, "Fill", track.rectTransform.sizeDelta, Rounded, Color.green);
        Stretch(fill.rectTransform); fill.preserveAspect = false;
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin = 0; fill.fillAmount = 1;
        rush.gameObject.SetActive(false);
        GameObject template = Nested("DishCard", root);
        template.name = "Dish card template";
        template.SetActive(false);
        Save(root.gameObject, "CustomerVisuals");
    }

    private static void BindCustomer()
    {
        string path = Root + "Customer.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Keep existing UI and overrides on subsequent runs.
            if (root.transform.Find("CustomerVisuals") != null) return;
            foreach (string name in new[] { "Customer (1)", "Dish card template", "Rush timer", "Timer badge", "Order countdown border" })
            {
                Transform old = root.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Transform visuals = Nested("CustomerVisuals", root.transform).transform;
            Customer customer = root.GetComponent<Customer>();
            CustomerRecipeView recipes = root.GetComponent<CustomerRecipeView>();
            RushCustomerView rush = root.GetComponent<RushCustomerView>();
            if (recipes == null) recipes = root.AddComponent<CustomerRecipeView>();
            if (rush == null) rush = root.AddComponent<RushCustomerView>();
            Bind(customer, "recipeView", recipes);
            Bind(customer, "avatarImage", visuals.Find("Avatar").GetComponent<Image>());
            Bind(recipes, "cardTemplate", visuals.Find("Dish card template").GetComponent<SushiDishCardView>());
            Bind(recipes, "orderRoot", visuals.Find("Orders"));
            Bind(recipes, "leftColumn", visuals.Find("Orders/Left column"));
            Bind(recipes, "rightColumn", visuals.Find("Orders/Right column"));
            Transform indicator = visuals.Find("Rush visuals");
            Bind(rush, "indicator", indicator.gameObject);
            Bind(rush, "timer", indicator.Find("Timer badge/Timer").GetComponent<TMP_Text>());
            Bind(rush, "countdown", indicator.Find("Countdown track/Fill").GetComponent<Image>());
            AudioSource audio = root.GetComponent<AudioSource>();
            if (audio == null) audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 0;
            Bind(rush, "audioSource", audio);
            ColorField(rush, "normalColor", Color.green);
            ColorField(rush, "warningColor", Color.yellow);
            ColorField(rush, "urgentColor", Color.red);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
