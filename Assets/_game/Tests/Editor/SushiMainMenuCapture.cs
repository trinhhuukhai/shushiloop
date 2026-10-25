#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Isolated project only. Use -batchmode -executeMethod SushiMainMenuCapture.RunBatch,
// keep graphics enabled, omit -quit. productName must be "Sushi UI Verification".
[InitializeOnLoad]
public static class SushiMainMenuCapture
{
    private const string Key = "SushiMainMenuCapture.Phase";
    private const string Started = "SushiMainMenuCapture.Started";
    private static readonly StringBuilder Report = new StringBuilder();
    private static double readyAt, transitionAt;
    private static int transitionFrame;
    private static bool sawIntermediate;
    private static float initialShopX, initialMarkerX, initialIconY, initialIconX;

    static SushiMainMenuCapture() => EditorApplication.update += Update;

    public static void RunBatch()
    {
        if (!Application.isBatchMode || PlayerSettings.productName != "Sushi UI Verification")
            throw new InvalidOperationException("Run only in a separate Sushi UI Verification project.");
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Main menu capture requires graphics; omit -nographics.");
        try
        {
            SushiProjectUISetup.Setup();
            SushiMainMenuSetup.Setup();
            EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Documentation");
            SessionState.SetFloat(Started, (float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(Key, 1);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Update()
    {
        int phase = SessionState.GetInt(Key, 0);
        if (phase == 0 || !Application.isBatchMode || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Started, 0) > 180)
                throw new TimeoutException("Main menu startup, capture or animation verification timed out.");
            if (!Application.isPlaying || UIManager.Ins == null || GameManager.ins == null ||
                DataManager.ins == null || !DataManager.ins.isLoaded || !UIManager.Ins.IsOpened<CanvasMainMenu>()) return;
            var menu = UIManager.Ins.GetUI<CanvasMainMenu>();
            if (phase == 1)
            {
                Check(Application.persistentDataPath.Contains("Sushi UI Verification"), "Capture must not use the user's save directory.");
                readyAt = EditorApplication.timeSinceStartup + 0.5;
                SetPhase(2);
                return;
            }
            if (phase == 2)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                CheckColdMenu();
                CheckCallbacks(menu);
                CheckTab(menu, 1);
                Capture("MainMenu", 720, 1280);
                Capture("MainMenu", 1080, 2400);
                Capture("MainMenu", 1536, 2048);
                // Let the next player frame recover from synchronous GPU readbacks before measuring animation.
                readyAt = EditorApplication.timeSinceStartup + 0.25;
                SetPhase(3);
                return;
            }
            if (phase == 3)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                Click(menu, "OpenShop");
                Check(menu.SelectedTab == 0 && menu.IsTabAnimating, "Shop click did not start a tab animation.");
                initialShopX = UIManager.Ins.GetUI<CanvasShop>().pageContent.anchoredPosition.x;
                initialMarkerX = Field<RectTransform>(menu, "tabSelection").anchoredPosition.x;
                initialIconY = Icon(menu, 0).anchoredPosition.y;
                initialIconX = Icon(menu, 0).anchoredPosition.x;
                Check(Mathf.Abs(initialIconX) > 10, "Selected icon must enter sideways with the tab.");
                Check(Mathf.Abs(initialShopX) > 1, "Shop appeared at its final position instead of sliding in.");
                transitionAt = EditorApplication.timeSinceStartup;
                transitionFrame = Time.frameCount;
                sawIntermediate = false;
                Record("Shop animation start", menu);
                SetPhase(4);
                return;
            }
            if (phase == 4)
            {
                RectTransform shop = UIManager.Ins.GetUI<CanvasShop>().pageContent;
                RectTransform marker = Field<RectTransform>(menu, "tabSelection");
                RectTransform icon = Icon(menu, 0);
                if (!sawIntermediate && Time.frameCount > transitionFrame && menu.IsTabAnimating &&
                    Mathf.Abs(shop.anchoredPosition.x) > 0.5f &&
                    Mathf.Abs(shop.anchoredPosition.x) < Mathf.Abs(initialShopX) - 0.5f &&
                    Mathf.Abs(marker.anchoredPosition.x - initialMarkerX) > 0.1f &&
                    Mathf.Abs(icon.anchoredPosition.y - initialIconY) > 0.1f && Mathf.Abs(icon.anchoredPosition.x - initialIconX) > 0.1f)
                {
                    sawIntermediate = true;
                    Record("Shop animation intermediate", menu);
                }
                if (EditorApplication.timeSinceStartup - transitionAt < 0.55 || menu.IsTabAnimating) return;
                Check(sawIntermediate, "No intermediate slide/marker/icon movement was observed between real player frames.");
                CheckTab(menu, 0);
                Record("Shop animation end", menu);
                Capture("Shop", 720, 1280);
                readyAt = EditorApplication.timeSinceStartup + 0.25;
                SetPhase(5);
                return;
            }
            if (phase == 5)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                Click(menu, "OpenLeaderboard");
                readyAt = EditorApplication.timeSinceStartup + 0.55;
                SetPhase(6);
                return;
            }
            if (phase == 6)
            {
                if (EditorApplication.timeSinceStartup < readyAt || menu.IsTabAnimating) return;
                CheckTab(menu, 2);
                Capture("Leaderboard", 720, 1280);
                Click(menu, "OpenProfile");
                var profile = Field<GameObject>(menu, "profilePanel");
                Check(profile.activeInHierarchy && profile.GetComponent<Canvas>().sortingOrder > 25, "Profile must cover every menu tab.");
                Capture("Profile", 720, 1280);
                Click(menu, "CloseProfile");
                readyAt = EditorApplication.timeSinceStartup + 0.25;
                SetPhase(7);
                return;
            }
            if (phase == 7)
            {
                if (EditorApplication.timeSinceStartup < readyAt) return;
                Click(menu, "OpenShop");
                Click(menu, "OpenLeaderboard");
                Click(menu, "OpenHome");
                readyAt = EditorApplication.timeSinceStartup + 0.55;
                SetPhase(8);
                return;
            }
            if (EditorApplication.timeSinceStartup < readyAt || menu.IsTabAnimating) return;
            if (phase == 8)
            {
                CheckTab(menu, 1);
                CheckColdMenu();
                Record("Rapid Shop Leaderboard Home settled", menu);
                Capture("HomeAfterRapidTabs", 720, 1280);
                Click(menu, "OpenDecoration");
                readyAt = EditorApplication.timeSinceStartup + 0.55;
                SetPhase(9);
                return;
            }
            Check(menu.SelectedTab == 3 && UIManager.Ins.IsOpened<CanvasDecoration>(), "Decoration button did not open its provided prefab.");
            Capture("Decoration", 720, 1280);
            Report.AppendLine("PASS: Loading enters GamePlay and opens only MainMenu; no level, ingredients, customers or conveyor slots initialized.");
            Report.AppendLine("PASS: Seven authored button callbacks are present; navigation tested through real pointer-click handlers.");
            Report.AppendLine("PASS: Page slide, marker translation, sideways icon entry and lift observed between real player frames, then correct final transforms.");
            Report.AppendLine("PASS: Rapid Shop > Leaderboard > Home leaves only home visible and selected.");
            Report.AppendLine("PASS: Actual MainMenu captured at 720x1280, 1080x2400, 1536x2048; Shop, Leaderboard and post-rapid Home at 720x1280.");
            File.WriteAllText("Documentation/MainMenuCaptureValidation.txt", Report.ToString());
            Debug.Log("SUSHI_MAIN_MENU_CAPTURE_OK");
            SessionState.SetInt(Key, 0);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }

    private static void SetPhase(int phase) => SessionState.SetInt(Key, phase);

    private static void CheckColdMenu()
    {
        Check(SceneManager.GetActiveScene().name == "GamePlay", "Startup must load the GamePlay scene before opening Home.");
        Check(LevelManager.Ins != null && LevelManager.Ins.CurrentLevel == null && !LevelManager.Ins.IsPlaying
            && LevelManager.Ins.Board.FoodQueues.Count == 0 && LevelManager.Ins.Conveyor.Slots.Count == 0,
            "Home must not initialize a playable level.");
        Check(GameManager.IsScene(GameScene.Home) && GameManager.IsState(GameState.MainMenu), "Wrong home navigation state.");
        Check(!UIManager.Ins.IsOpened<CanvasGamePlay>() && !UIManager.Ins.IsOpened<CanvasFloating>(), "Gameplay HUD is open in home.");
        Check(!GameManager.ins.IsLoadingLevel && Mathf.Approximately(Time.timeScale, 1), "Home retains loading/pause state.");
        Check(LoadingScreen.ins == null || !LoadingScreen.ins.gameObject.activeInHierarchy, "Loading still obscures the main menu.");
    }

    private static T Field<T>(Object owner, string field) where T : Object
    {
        var property = new SerializedObject(owner).FindProperty(field);
        T value = property?.objectReferenceValue as T;
        Check(value != null, owner.name + " missing serialized field " + field);
        return value;
    }

    private static RectTransform Icon(CanvasMainMenu menu, int index)
    {
        var icons = new SerializedObject(menu).FindProperty("tabIcons");
        Check(icons != null && icons.arraySize == 3, "Menu must serialize three navigation icons.");
        var icon = icons.GetArrayElementAtIndex(index).objectReferenceValue as RectTransform;
        Check(icon != null, "Missing navigation icon " + index);
        return icon;
    }

    private static Button ButtonFor(CanvasMainMenu menu, string method)
    {
        foreach (Button button in menu.GetComponentsInChildren<Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == menu && button.onClick.GetPersistentMethodName(i) == method)
                    return button;
        throw new InvalidOperationException("Missing authored MainMenu button callback: " + method);
    }

    private static void CheckCallbacks(CanvasMainMenu menu)
    {
        foreach (string method in new[] { "Play", "OpenShop", "OpenHome", "OpenLeaderboard", "OpenDecoration", "OpenSettings", "OpenProfile" })
        {
            Button button = ButtonFor(menu, method);
            Check(button.targetGraphic != null, method + " has no visible button graphic.");
            Report.AppendLine("Authored callback: " + method + " <- " + button.name);
        }
    }

    private static void Click(CanvasMainMenu menu, string method)
    {
        Button button = ButtonFor(menu, method);
        Check(button.isActiveAndEnabled && button.interactable, method + " is not an active interactive button.");
        Check(EventSystem.current != null, "No EventSystem for actual button pointer events.");
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Report.AppendLine("Pointer click: " + method);
    }

    private static void CheckTab(CanvasMainMenu menu, int selected)
    {
        Check(menu.SelectedTab == selected && !menu.IsTabAnimating, "Tab selection did not settle at " + selected);
        RectTransform home = Field<RectTransform>(menu, "homeContent");
        Check(home.gameObject.activeSelf == (selected == 1), "Home visibility disagrees with selected tab.");
        Check(UIManager.Ins.IsOpened<CanvasShop>() == (selected == 0), "Shop visibility disagrees with selected tab.");
        Check(UIManager.Ins.IsOpened<CanvasLeadBoard>() == (selected == 2), "Leaderboard visibility disagrees with selected tab.");
        Check(!UIManager.Ins.IsOpened<CanvasDecoration>(), "Unrequested Decoration page remained open.");
        RectTransform content = selected == 1 ? home : selected == 0 ?
            UIManager.Ins.GetUI<CanvasShop>().pageContent : UIManager.Ins.GetUI<CanvasLeadBoard>().pageContent;
        Check(content.anchoredPosition.sqrMagnitude < 0.02f, "Selected page did not finish at its center.");
        Check(Mathf.Abs(Field<RectTransform>(menu, "tabSelection").anchoredPosition.x - (selected - 1) * 360f) < 0.1f,
            "Tab marker has incorrect final position.");
        for (int i = 0; i < 3; i++)
        {
            RectTransform icon = Icon(menu, i);
            Check(Mathf.Abs(icon.anchoredPosition.y - (i == selected ? 26 : 0)) < 0.1f, "Navigation icon lift is incorrect.");
            Check(Mathf.Abs(icon.anchoredPosition.x) < 0.1f, "Navigation icon did not finish its horizontal slide.");
            Check(Mathf.Abs(icon.localScale.x - (i == selected ? 1.16f : 0.88f)) < 0.01f, "Navigation icon scale is incorrect.");
        }
        Check(content.lossyScale.x > 0 && content.rect.width > 100 && content.rect.height > 100,
            "Selected page has a collapsed Canvas rect or zero scale.");
        if (selected != 1) Check(content.GetComponentInParent<Canvas>().overrideSorting,
            "Tab Canvas must sort above the restaurant background.");
        CanvasGroup group = content.GetComponent<CanvasGroup>();
        Check(group == null || group.interactable && group.blocksRaycasts, "Selected page stays noninteractive after transition.");
    }

    private static void Record(string label, CanvasMainMenu menu)
    {
        string value = label + ": frame=" + Time.frameCount + ", selected=" + menu.SelectedTab +
            ", shopX=" + UIManager.Ins.GetUI<CanvasShop>().pageContent.anchoredPosition.x.ToString("F2") +
            ", markerX=" + Field<RectTransform>(menu, "tabSelection").anchoredPosition.x.ToString("F2") +
            ", shopIconX=" + Icon(menu, 0).anchoredPosition.x.ToString("F2") + ", shopIconY=" + Icon(menu, 0).anchoredPosition.y.ToString("F2") +
            ", homeActive=" + Field<RectTransform>(menu, "homeContent").gameObject.activeSelf;
        Report.AppendLine(value);
        Debug.Log(value);
    }

    private sealed class CanvasState
    {
        public Canvas canvas;
        public Camera camera;
        public RenderMode mode;
        public float planeDistance, scale;
        public CanvasScaler scaler;
        public bool scalerEnabled;
    }

    public static void Capture(string label, int width, int height)
    {
        var cameraObject = new GameObject("Isolated MainMenu capture camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.aspect = width / (float)height;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.06f, 0.035f);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        target.Create();
        camera.targetTexture = target;
        var states = new List<CanvasState>();
        RenderTexture oldActive = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            // Nested page canvases inherit the root render camera and preserve their authored sorting order.
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                var state = new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera,
                    planeDistance = canvas.planeDistance, scale = canvas.scaleFactor,
                    scaler = scaler, scalerEnabled = scaler != null && scaler.enabled };
                states.Add(state);
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.scaleFactor = CaptureScale(scaler, width, height, state.scale);
            }
            Check(states.Count > 0, "No root UI canvas found for capture.");
            Canvas.ForceUpdateCanvases();
            foreach (var fitter in Object.FindObjectsByType<SushiUIFitContent>(FindObjectsSortMode.None)) fitter.Refresh();
            Canvas.ForceUpdateCanvases();
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            else camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            pixels.Apply(false, false);
            string path = "Documentation/MainMenuRefresh" + label + "_" + width + "x" + height + ".png";
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Report.AppendLine("Capture: " + path);
            Debug.Log("SUSHI_MENU_CAPTURE: " + path);
        }
        finally
        {
            foreach (CanvasState state in states)
            {
                state.canvas.renderMode = state.mode;
                state.canvas.worldCamera = state.camera;
                state.canvas.planeDistance = state.planeDistance;
                state.canvas.scaleFactor = state.scale;
                if (state.scaler != null) state.scaler.enabled = state.scalerEnabled;
            }
            RenderTexture.active = oldActive;
            camera.targetTexture = null;
            if (pixels != null) Object.DestroyImmediate(pixels);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static float CaptureScale(CanvasScaler scaler, int width, int height, float fallback)
    {
        if (scaler == null) return fallback;
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return scaler.scaleFactor;
        float x = width / scaler.referenceResolution.x, y = height / scaler.referenceResolution.y;
        switch (scaler.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.Expand: return Mathf.Min(x, y);
            case CanvasScaler.ScreenMatchMode.Shrink: return Mathf.Max(x, y);
            default: return Mathf.Pow(2, Mathf.Lerp(Mathf.Log(x, 2), Mathf.Log(y, 2), scaler.matchWidthOrHeight));
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Fail(Exception error)
    {
        SessionState.SetInt(Key, 0);
        Debug.LogException(error);
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/MainMenuCaptureValidation.txt", Report + "\nFAIL\n" + error);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
#endif
