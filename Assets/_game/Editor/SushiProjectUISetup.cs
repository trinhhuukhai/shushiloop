using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit editor migration only. The saved prefabs are the source of all runtime UI.
public static class SushiProjectUISetup
{
    private const string Root = "Assets/_game/Resources/UI/";
    private const string Loading = "Assets/_game/Scenes/Loading.unity";
    private const string Gameplay = "Assets/_game/Scenes/GamePlay.unity";
    private static readonly Color Ink = new Color(0.22f, 0.12f, 0.07f);
    private static readonly Color Cream = new Color(1f, 0.94f, 0.80f);
    private static readonly Color Orange = new Color(1f, 0.71f, 0.28f);
    private static readonly Color Green = new Color(0.20f, 0.43f, 0.30f);
    private static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Materials/SushiRoundedPanel.png");

    public static void RunBatch()
    {
        try { Setup(); Debug.Log("PROJECT_UI_SETUP_OK"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("SushiKhai/UI/Complete Project UI Prefabs")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing UI prefabs.");
        Edit<CanvasGamePlay>("UICGamePlay", 50, BuildGameplay);
        Edit<CanvasVictory>("UICVictory", 100, BuildVictory);
        Edit<CanvasFail>("UICLose", 100, BuildLose);
        Edit<CanvasSetting>("UICSetting", 110, BuildSettings);
        Edit<CanvasFloating>("UICFloating", 60, BindFloating);
        var levelData = AssetDatabase.LoadAssetAtPath<AllLevelData>("Assets/_game/LevelData/_AllLevelData.asset");
        if (levelData == null || levelData.listAllLevel == null || levelData.listAllLevel.Count == 0)
            throw new InvalidOperationException("_AllLevelData must contain the game's levels.");
        var scene = EditorSceneManager.OpenScene(Loading);
        DataManager data = Object.FindFirstObjectByType<DataManager>();
        data.allLevelData = levelData;
        var manager = Object.FindFirstObjectByType<UIManager>();
        string[] names = { "UICFloating", "UICGamePlay", "UICVictory", "UICLose", "UICSetting" };
        var prefabs = manager.uiResources.Where(p => p != null).ToList();
        foreach (string name in names)
        {
            var ui = AssetDatabase.LoadAssetAtPath<GameObject>(Root + name + ".prefab").GetComponent<UICanvas>();
            prefabs.RemoveAll(p => p.GetType() == ui.GetType()); prefabs.Add(ui);
        }
        manager.uiResources = prefabs.ToArray();
        EditorUtility.SetDirty(manager); EditorUtility.SetDirty(data);
        EditorSceneManager.SaveScene(scene);
        scene = EditorSceneManager.OpenScene(Gameplay);
        LevelManager level = Object.FindFirstObjectByType<LevelManager>();
        Bind(level, "allLevelData", levelData);
        var settings = new SerializedObject(level);
        settings.FindProperty("showGameplayHud").boolValue = true;
        settings.FindProperty("showLevelSelector").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (settings.FindProperty("reviveProvider").objectReferenceValue == null)
        {
            var provider = level.GetComponent<SushiProfileReviveProvider>();
            if (provider == null) provider = level.gameObject.AddComponent<SushiProfileReviveProvider>();
            Bind(level, "reviveProvider", provider);
        }
        foreach (SushiGameplayHud legacy in Object.FindObjectsByType<SushiGameplayHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(legacy);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Loading, true), new EditorBuildSettingsScene(Gameplay, true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != Loading && s.path != Gameplay)).ToArray();
        AssetDatabase.SaveAssets();
    }

    private static void Edit<T>(string name, int order, Action<T> build) where T : UICanvas
    {
        string path = Root + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = order;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 0.5f;
            build(root.GetComponent<T>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchor)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var image = Rect(parent, name, size, position, Vector2.one * 0.5f).gameObject.AddComponent<Image>();
        image.sprite = Rounded; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
        return image;
    }
    private static TMP_Text Label(Transform parent, string name, string text, Vector2 size, Vector2 position, float font = 32)
    {
        var label = Rect(parent, name, size, position, Vector2.one * 0.5f).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text; label.fontSize = font; label.fontSizeMax = font; label.fontSizeMin = font * 0.7f;
        label.enableAutoSizing = true; label.alignment = TextAlignmentOptions.Center; label.color = Ink; label.raycastTarget = false;
        return label;
    }
    private static Button Button(Transform parent, string name, string title, Vector2 size, Vector2 position, UnityAction click)
    {
        Image image = Panel(parent, name, size, position, Orange); image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        UnityEventTools.AddPersistentListener(button.onClick, click);
        TMP_Text text = Label(image.transform, "Label", title, size - new Vector2(20, 10), Vector2.zero);
        Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(10, 5); text.rectTransform.offsetMax = new Vector2(-10, -5);
        return button;
    }
    private static void Bind(Object target, string field, Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void BindArray(Object target, string field, Object[] values)
    {
        var data = new SerializedObject(target); var property = data.FindProperty(field); property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static RectTransform Content(UICanvas view, bool modal)
    {
        Transform old = view.transform.Find("bg");
        if (old != null)
        {
            old.gameObject.SetActive(modal);
            Stretch((RectTransform)old);
            var image = old.GetComponent<Image>(); image.color = new Color(0.08f, 0.045f, 0.025f, 0.75f); image.raycastTarget = modal;
        }
        RectTransform root = Rect(view.transform, "Content", Vector2.zero, Vector2.zero, Vector2.one * 0.5f);
        Stretch(root); return root;
    }

    private static void BuildGameplay(CanvasGamePlay view)
    {
        // Migrate an already authored HUD without rebuilding the user's controls.
        Transform oldCapacity = view.transform.Find("Content/Header/Belt capacity");
        if (oldCapacity != null) Object.DestroyImmediate(oldCapacity.gameObject);
        Bind(view, "occupancy", null);
        if (view.transform.Find("Content") != null) return;
        RectTransform root = Content(view, false); Bind(view, "rootPanel", root);
        RectTransform header = Rect(root, "Header", new Vector2(1000, 100), new Vector2(0, -60), new Vector2(0.5f, 1));
        Bind(view, "TopGroup", header);
        TMP_Text level = Label(header, "Level", "Level 1", new Vector2(300, 65), Vector2.zero, 38); level.color = Color.white;
        Bind(view, "txtLevvel", level);
        Button speed = Button(header, "Speed", "1×", new Vector2(100, 64), new Vector2(345, 0), view.OnClickChangeSpeed);
        Bind(view, "speedText", speed.GetComponentInChildren<TMP_Text>());
        RectTransform footer = Rect(root, "Boosters", new Vector2(990, 120), new Vector2(0, 83), new Vector2(0.5f, 0));
        var actions = new UnityAction[] { view.BoosterClick, view.BoosterSuffle, view.BoosterHut };
        string[] names = { "Cây gắp", "Swap Row", "Magnet" };
        Button[] buttons = new Button[3]; TMP_Text[] counts = new TMP_Text[3]; GameObject[] selected = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 320;
            Image highlight = Panel(footer, names[i] + " selected", new Vector2(306, 106), new Vector2(x, 0), Cream);
            selected[i] = highlight.gameObject; selected[i].SetActive(false);
            buttons[i] = Button(footer, names[i], names[i], new Vector2(290, 90), new Vector2(x, 0), actions[i]);
            TMP_Text name = buttons[i].GetComponentInChildren<TMP_Text>();
            name.rectTransform.offsetMin = new Vector2(10, 25); name.rectTransform.offsetMax = new Vector2(-10, -5);
            counts[i] = Label(buttons[i].transform, "Count", "2", new Vector2(260, 28), new Vector2(0, -28), 24);
        }
        BindArray(view, "boosterButtons", buttons); BindArray(view, "boosterCounts", counts); BindArray(view, "selectedBoosters", selected);
        TMP_Text hint = Label(footer, "Booster hint", "", new Vector2(800, 60), new Vector2(-60, 100), 25); hint.color = Cream;
        Bind(view, "boosterHint", hint);
        Button cancel = Button(footer, "Cancel booster", "Hủy", new Vector2(125, 58), new Vector2(430, 100), view.CancelBooster);
        cancel.gameObject.SetActive(false); Bind(view, "cancelBooster", cancel);
        RectTransform selector = Rect(root, "Level selector", new Vector2(840, 60), new Vector2(0, -175), new Vector2(0.5f, 1));
        Bind(view, "levelSelector", selector.gameObject);
        Bind(view, "previousLevel", Button(selector, "Previous", "Màn trước", new Vector2(250, 55), new Vector2(-275, 0), view.PreviousLevel));
        Button(selector, "Replay", "Chơi lại", new Vector2(250, 55), Vector2.zero, view.ReplayLevel);
        Bind(view, "nextLevel", Button(selector, "Next", "Màn sau", new Vector2(250, 55), new Vector2(275, 0), view.NextLevel));
        selector.gameObject.SetActive(false);
    }

    private static void BuildVictory(CanvasVictory view)
    {
        if (view.transform.Find("Content") != null) return;
        RectTransform root = Content(view, true);
        Image card = Panel(root, "Victory card", new Vector2(820, 660), Vector2.zero, Cream);
        TMP_Text title = Label(card.transform, "Title", "Hoàn thành!", new Vector2(730, 110), new Vector2(0, 210), 54); title.color = Green;
        Bind(view, "title", title);
        Label(card.transform, "Message", "Tất cả khách đã nhận đủ món", new Vector2(700, 70), new Vector2(0, 115), 30);
        Bind(view, "goldRwTxt", Label(card.transform, "Reward", "+100 xu", new Vector2(680, 110), new Vector2(0, 10), 54));
        Button next = Button(card.transform, "Claim and next", "Nhận & màn tiếp", new Vector2(650, 105), new Vector2(0, -200), view.BtnNext);
        Bind(view, "btnGoldRW", next); Bind(view, "btnNormal", next.gameObject); Bind(view, "nextLabel", next.GetComponentInChildren<TMP_Text>());
    }

    private static void BuildLose(CanvasFail view)
    {
        if (view.transform.Find("Content") != null) return;
        RectTransform root = Content(view, true);
        Image card = Panel(root, "Lose and revive card", new Vector2(840, 750), Vector2.zero, Cream);
        Bind(view, "grandCg", card.gameObject.AddComponent<CanvasGroup>());
        Bind(view, "title", Label(card.transform, "Title", "Tiếp tục phục vụ?", new Vector2(760, 100), new Vector2(0, 255), 48));
        Bind(view, "description", Label(card.transform, "Description", "", new Vector2(710, 110), new Vector2(0, 140), 30));
        RectTransform actions = Rect(card.transform, "Revive actions", new Vector2(700, 130), new Vector2(0, -25), Vector2.one * 0.5f);
        Bind(view, "reviveActions", actions.gameObject);
        Button coins = Button(actions, "Revive with coins", "300 xu", new Vector2(570, 100), Vector2.zero, view.BtnRevive);
        Bind(view, "coinButton", coins); Bind(view, "coinPrice", coins.GetComponentInChildren<TMP_Text>());
        Button ad = Button(actions, "Revive with ad", "Quảng cáo", new Vector2(300, 80), new Vector2(0, -100), view.ReviveWithAd);
        Bind(view, "adButton", ad); ad.gameObject.SetActive(false);
        Bind(view, "feedback", Label(card.transform, "Feedback", "", new Vector2(710, 45), new Vector2(0, -138), 25));
        Button retry = Button(card.transform, "Retry or decline", "Chơi lại", new Vector2(570, 90), new Vector2(0, -255), view.BtnNext);
        retry.image.color = Green; retry.GetComponentInChildren<TMP_Text>().color = Color.white;
        Bind(view, "retryLabel", retry.GetComponentInChildren<TMP_Text>());
    }

    private static void BuildSettings(CanvasSetting view)
    {
        if (view.transform.Find("Content") != null) return;
        RectTransform root = Content(view, true);
        Image card = Panel(root, "Pause card", new Vector2(800, 650), Vector2.zero, Cream);
        Bind(view, "mainPanelRect", card.rectTransform);
        Label(card.transform, "Title", "Tạm dừng", new Vector2(700, 100), new Vector2(0, 220), 50);
        Bind(view, "resumeBt", Button(card.transform, "Resume", "Tiếp tục", new Vector2(620, 95), new Vector2(0, 80), view.OnResume));
        Bind(view, "back_Bt", Button(card.transform, "Replay", "Chơi lại", new Vector2(620, 95), new Vector2(0, -55), view.OnReplay));
        Bind(view, "homeBt", Button(card.transform, "Give up", "Dừng màn chơi", new Vector2(620, 95), new Vector2(0, -190), view.OnGiveUp));
    }

    private static void BindFloating(CanvasFloating view)
    {
        // Retain the user's coin artwork, effects and settings controls.
        view.HeartFr.SetActive(false);
        view.FrmHeartUnlimited.SetActive(false);
        view.TopGroup.anchoredPosition = new Vector2(0, -60);
        view.TopGroup.sizeDelta = new Vector2(0, 100);
        RectTransform gold = (RectTransform)view.GoldFr.transform;
        gold.SetParent(view.TopGroup, false);
        gold.anchorMin = gold.anchorMax = new Vector2(0, 0.5f); gold.anchoredPosition = new Vector2(165, 0);
        gold.sizeDelta = new Vector2(260, 80);
        view.txtGold.fontSize = view.txtGold.fontSizeMax = 36; view.txtGold.fontSizeMin = 22; view.txtGold.enableAutoSizing = true;
        foreach (GameObject obj in new[] { view.settingBt.gameObject, view.btnSettingIngame })
        {
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f); rect.anchoredPosition = new Vector2(-65, 0); rect.sizeDelta = new Vector2(72, 72);
            obj.GetComponent<Image>().enabled = true;
            obj.GetComponent<Button>().enabled = true;
        }
        view.settingBt.gameObject.SetActive(false); view.btnSettingIngame.SetActive(true);
        // Only actual buttons intercept input; decorative full-screen images must allow board taps.
        foreach (Graphic graphic in view.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = graphic.GetComponent<Button>() != null;
    }
}
