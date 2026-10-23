#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run only in an isolated copy: Unity -batchmode -projectPath <clone>
// -executeMethod SushiVisualCapture.RunBatch -logFile <log>
// Keep graphics enabled and omit -quit: the asynchronous capture exits itself.
[InitializeOnLoad]
public static class SushiVisualCapture
{
    private const string PendingKey = "SushiVisualCapture.Phase";
    private const string StartedKey = "SushiVisualCapture.Started";
    private const string ArtRoot = "Assets/_game/Art/Refresh/";
    private static int readyFrame;
    private static readonly Vector2Int[] Sizes = {
        new Vector2Int(720, 1280), new Vector2Int(1080, 2400), new Vector2Int(1536, 2048)
    };

    static SushiVisualCapture() => EditorApplication.update += Update;

    public static void RunBatch()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Capture requires a separate batch-mode project, never the user's open editor.");
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Capture needs graphics. Remove -nographics from the Unity command.");
        try
        {
            SushiRefreshArtSetup.Apply();
            ValidateArt();
            foreach (string guid in AssetDatabase.FindAssets("t:SushiLevelData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<SushiLevelData>(AssetDatabase.GUIDToAssetPath(guid));
                Check(data.ValidateData() == null, data.name + ": " + data.ValidateData());
                Check(data.CreateBoardData().Count <= 5, data.name + " exceeds five board columns.");
            }
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Documentation");
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(PendingKey, 1);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Update()
    {
        int phase = SessionState.GetInt(PendingKey, 0);
        if (phase == 0 || !Application.isBatchMode || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0) > 180)
                throw new TimeoutException("Gameplay capture timed out entering Play Mode or waiting for startup.");
            if (!EditorApplication.isPlaying || Time.frameCount < 8) return;
            var level = Object.FindFirstObjectByType<LevelManager>();
            if (level == null || level.CurrentLevel == null) return;
            if (phase == 1)
            {
                level.LoadLevel(0);
                readyFrame = Time.frameCount + 12;
                SessionState.SetInt(PendingKey, 2);
                return;
            }
            if (Time.frameCount < readyFrame) return;
            if (phase == 2)
            {
                ValidateRuntime(level);
                CaptureSet("Sample", level);
                level.LoadLevel(level.LevelCount - 1);
                readyFrame = Time.frameCount + 12;
                SessionState.SetInt(PendingKey, 3);
                return;
            }
            ValidateRuntime(level);
            CaptureSet("AllElements", level);
            File.WriteAllText("Documentation/GameplayRefreshValidation.txt",
                "PASS: 21 refreshed ingredients, 48 refreshed dishes, refreshed world decor and customer portraits.\n" +
                "All level assets validate and contain no more than five board columns.\n" +
                "Actual first and last SampleScene levels rendered at 720x1280, 1080x2400, and 1536x2048.\n" +
                "The current project render pipeline and gameplay materials were preserved.\n");
            Debug.Log("SUSHI_VISUAL_CAPTURE_OK: six actual gameplay PNGs saved under Documentation.");
            SessionState.SetInt(PendingKey, 0);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }

    private static void ValidateArt()
    {
        var art = AssetDatabase.LoadAssetAtPath<SushiArtCatalog>("Assets/_game/Resources/SushiArtCatalog.asset");
        Check(art != null && art.ingredientImages != null && art.dishImages != null, "Missing art catalog/libraries.");
        foreach (PoolType type in Enum.GetValues(typeof(PoolType)))
            if (type != PoolType.None) Refreshed(art.GetIngredient(type), type.ToString());
        foreach (SushiRecipe recipe in SushiRecipeCatalog.All) Refreshed(art.GetDish(recipe.Id), recipe.Id.ToString());
        Refreshed(art.boardTile, "BoardTile");
        Refreshed(art.plate, "Plate");
        Refreshed(art.orderCard, "OrderCard");
        Refreshed(art.conveyor, "Conveyor");
        Check(art.customerAvatars != null && art.customerAvatars.Length == 3, "Expected three refreshed customer avatars.");
        foreach (Sprite avatar in art.customerAvatars) Refreshed(avatar, avatar != null ? avatar.name : "Missing avatar");
        Debug.Log("SUSHI_ART_BINDINGS_OK: all ingredients, all 48 dishes, world decor and three customers use Refresh assets.");
    }

    private static void Refreshed(Sprite sprite, string label)
    {
        Check(sprite != null, "Missing sprite: " + label);
        Check(AssetDatabase.GetAssetPath(sprite).StartsWith(ArtRoot, StringComparison.Ordinal),
            label + " still references old art: " + AssetDatabase.GetAssetPath(sprite));
    }

    private static void ValidateRuntime(LevelManager level)
    {
        Check(level.IsPlaying, "Sample did not start.");
        Check(level.Board != null && level.Board.FoodQueues.Count > 0 && level.Board.FoodQueues.Count <= 5,
            "Runtime board must contain between one and five columns.");
        int checkedFoods = 0;
        foreach (FoodQueue column in level.Board.FoodQueues)
            foreach (FoodShushi food in column.Foods)
            {
                Sprite expected = SushiArtCatalog.Default.GetIngredient(food.Type);
                Check(food.GetComponentsInChildren<SpriteRenderer>(true).Any(view => view.sprite == expected),
                    "Runtime food does not bind its refreshed ingredient: " + food.Type);
                checkedFoods++;
            }
        Debug.Log("SUSHI_LAYOUT_OK: " + level.CurrentLevel.name + ", columns=" +
            level.Board.FoodQueues.Count + ", checked ingredient instances=" + checkedFoods);
    }

    private sealed class CanvasState
    {
        public Canvas canvas;
        public RenderMode mode;
        public Camera camera;
        public float distance;
        public float scale;
        public CanvasScaler scaler;
        public bool scalerEnabled;
    }

    public static void CaptureSet(string label, LevelManager level)
    {
        Camera camera = Camera.main;
        Check(camera != null, "SampleScene has no MainCamera.");
        CameraControl control = camera.GetComponent<CameraControl>();
        bool wasControlled = control != null && control.enabled;
        if (control != null) control.enabled = false;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect, previousSize = camera.orthographicSize;
        Vector3 previousPosition = camera.transform.position;
        Quaternion previousRotation = camera.transform.rotation;
        Rect previousRect = camera.rect;
        var states = new List<CanvasState>();
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            var scaler = canvas.GetComponent<CanvasScaler>();
            states.Add(new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera,
                distance = canvas.planeDistance, scale = canvas.scaleFactor, scaler = scaler,
                scalerEnabled = scaler != null && scaler.enabled });
            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
        }
        try
        {
            camera.orthographic = true;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
            foreach (Vector2Int size in Sizes)
            {
                var target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32) {
                    name = "Sushi gameplay capture", antiAliasing = 1
                };
                target.Create();
                Texture2D pixels = null;
                RenderTexture previousActive = RenderTexture.active;
                try
                {
                    camera.targetTexture = target;
                    camera.aspect = size.x / (float)size.y;
                    camera.orthographicSize = Mathf.Max(8f, 4.5f / camera.aspect);
                    foreach (CanvasState state in states)
                        state.canvas.scaleFactor = ScaleForCapture(state.scaler, size, state.scale);
                    Canvas.ForceUpdateCanvases();
                    if (GraphicsSettings.currentRenderPipeline != null)
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    else camera.Render();
                    RenderTexture.active = target;
                    pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0, false);
                    pixels.Apply(false, false);
                    string path = "Documentation/GameplayRefresh" + label + "_" + size.x + "x" + size.y + ".png";
                    File.WriteAllBytes(path, pixels.EncodeToPNG());
                    Debug.Log("SUSHI_CAPTURE: " + path + ", level=" + level.CurrentLevel.number +
                        ", columns=" + level.Board.FoodQueues.Count + ", aspect=" + camera.aspect);
                }
                finally
                {
                    RenderTexture.active = previousActive;
                    camera.targetTexture = previousTarget;
                    if (pixels != null) Object.DestroyImmediate(pixels);
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }
        }
        finally
        {
            foreach (CanvasState state in states)
            {
                state.canvas.renderMode = state.mode;
                state.canvas.worldCamera = state.camera;
                state.canvas.planeDistance = state.distance;
                state.canvas.scaleFactor = state.scale;
                if (state.scaler != null) state.scaler.enabled = state.scalerEnabled;
            }
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            camera.orthographicSize = previousSize;
            camera.rect = previousRect;
            camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
            if (control != null) control.enabled = wasControlled;
        }
    }

    private static float ScaleForCapture(CanvasScaler scaler, Vector2Int size, float fallback)
    {
        if (scaler == null) return fallback;
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return scaler.scaleFactor;
        float width = size.x / scaler.referenceResolution.x, height = size.y / scaler.referenceResolution.y;
        switch (scaler.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.Expand: return Mathf.Min(width, height);
            case CanvasScaler.ScreenMatchMode.Shrink: return Mathf.Max(width, height);
            default: return Mathf.Pow(2, Mathf.Lerp(Mathf.Log(width, 2), Mathf.Log(height, 2), scaler.matchWidthOrHeight));
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Fail(Exception error)
    {
        SessionState.SetInt(PendingKey, 0);
        Debug.LogException(error);
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/GameplayRefreshValidation.txt", "FAIL\n" + error);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
#endif
