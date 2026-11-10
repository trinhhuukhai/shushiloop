using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Chạy SushiHomeArtSetup một lần khi có file trigger Temp/apply_home_art.trigger (dùng khi không bấm menu được),
// sau đó chụp UICMainMenu / UICShop / UICLeaderBoard ra Temp/HomeArt/*.png để soát bố cục.
[InitializeOnLoad]
internal static class SushiHomeArtAutoRun
{
    private const string Trigger = "Temp/apply_home_art.trigger";
    private const string CaptureTrigger = "Temp/capture_home_art.trigger";

    static SushiHomeArtAutoRun()
    {
        if (!File.Exists(Trigger) && !File.Exists(CaptureTrigger)) return;
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        try
        {
            if (File.Exists(Trigger))
            {
                // Nội dung file trigger: "shop" = chỉ dựng lại Shop, còn lại = dựng tất cả.
                string what = File.ReadAllText(Trigger).Trim();
                File.Delete(Trigger);
                if (what == "shop") SushiHomeArtSetup.SetupShopOnly();
                else if (what == "pages") SushiHomeArtSetup.SetupPages();
                else if (what == "safearea") SushiHomeArtSetup.PatchSafeArea();
                else if (what == "swipe") SushiHomeArtSetup.PatchSwipe();
                else if (what == "ingame")
                {
                    SushiBoardColumnConverter.ConvertAll();
                    SushiIngameArtUpdate.Apply();
                    SushiIngameDemoLayout.Apply();
                    Directory.CreateDirectory("Temp/HomeArt");
                    File.WriteAllText("Temp/HomeArt/result.txt", "OK ingame");
                    return;
                }
                else SushiHomeArtSetup.Setup();
            }
            if (File.Exists(CaptureTrigger)) File.Delete(CaptureTrigger);
            CaptureAll();
            File.WriteAllText("Temp/HomeArt/result.txt", "OK");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Directory.CreateDirectory("Temp/HomeArt");
            File.WriteAllText("Temp/HomeArt/result.txt", "FAILED\n" + error);
        }
    }

    [MenuItem("SushiKhai/UI/Capture Home Art Preview")]
    private static void CaptureAll()
    {
        Directory.CreateDirectory("Temp/HomeArt");
        Capture("UICMainMenu", go =>
        {
            var menu = go.GetComponent<CanvasMainMenu>();
            typeof(CanvasMainMenu).GetMethod("PositionTabs", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(menu, new object[] { false, 0 });
        });
        Capture("UICShop", null);
        Capture("UICLeaderBoard", go => go.GetComponent<CanvasLeadBoard>().Open());
        Capture("UICLeaderBoard", go =>
        {
            var board = go.GetComponent<CanvasLeadBoard>();
            board.Open();
            board.ShowRank();
        }, "UICLeaderBoard_Rank");
        Capture("UICProfile", go => ShowProfile(go, false));
        Capture("UICProfile", go => ShowProfile(go, true), "UICProfile_Edit");
    }

    private static void ShowProfile(GameObject go, bool edit)
    {
        go.GetComponent<CanvasProfile>().SetShowEdit(edit);
        DG.Tweening.DOTween.CompleteAll();
    }

    // Dựng prefab trong preview scene, đổi sang Screen Space Camera rồi render 1080x1920 ra PNG.
    private static void Capture(string prefab, Action<GameObject> prepare, string fileName = null)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/" + prefab + ".prefab");
        Scene scene = EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
        GameObject cameraObject = null;
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            cameraObject = new GameObject("Capture camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.targetTexture = texture;
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10;
            var scaler = instance.GetComponent<CanvasScaler>();
            if (scaler != null)
                typeof(CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(scaler, null);
            prepare?.Invoke(instance);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)instance.transform);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes("Temp/HomeArt/" + (fileName ?? prefab) + ".png", image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
        finally
        {
            texture.Release();
            Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
