#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class SushiRestaurantRegression
{
    private const string Key = "SushiRestaurantRegression";
    private static readonly List<string> Results = new List<string>();
    private static double readyAt;
    static SushiRestaurantRegression()
    {
        EditorApplication.update += Tick;
        if (SessionState.GetInt(Key, 0) > 0)
            Results.AddRange(SessionState.GetString(Key + ".Results", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
    }

    public static void RunBatch()
    {
        try
        {
            if (!Application.isBatchMode || PlayerSettings.productName != "Sushi UI Verification" ||
                !Path.GetFullPath(Application.dataPath).Contains("SushiVerification"))
                throw new InvalidOperationException("Use the isolated verification project.");
            SushiDecorationSetup.Setup();
            Results.Clear(); SessionState.SetString(Key + ".Results", "");
            CheckModel();
            var data = AssetDatabase.LoadAssetAtPath<GameDataFragment>("Assets/_game/DataSO/GameDataFragment.asset");
            data.dataPlayer = new GameDataFragment.DataPlayer { cur_level = 29, showTutorialLevel1 = true,
                completedLevels = Enumerable.Range(0, 29).ToList(), restaurantDecorationCount = 1 };
            data.Save();
            SessionState.SetFloat(Key + ".Start", (float)EditorApplication.timeSinceStartup);
            SetPhase(1);
            EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void CheckModel()
    {
        var list = SushiRestaurantCatalog.Current;
        var catalog = list.decorations;
        string second = list.restaurants[1].id;
        Check(list.restaurants.Length == 2 && list.restaurants[1].unlockAfterLevel == 30,
            "Two configurable restaurants, second unlocks after level 30");
        var player = JsonConvert.DeserializeObject<GameDataFragment.DataPlayer>(
            "{\"cur_level\":29,\"restaurantDecorationCount\":1,\"money\":500}");
        player.completedLevels = Enumerable.Range(0, 29).ToList();
        Check(SushiRestaurantCatalog.IsUnlocked(player, list.restaurants[0]) &&
            !SushiRestaurantCatalog.IsUnlocked(player, list.restaurants[1]), "Level 29 keeps the second restaurant locked");
        Check(SushiDecorationProgress.Built(player, catalog) == 1 && SushiDecorationProgress.Built(player, catalog, second) == 0,
            "Old save retains the first restaurant and starts the second empty");
        Check(!SushiDecorationProgress.TryBuild(player, list, second, "deco_sign", out _) &&
            SushiDecorationProgress.AvailableStars(player, catalog) == 27, "Locked restaurant cannot consume stars");
        player.completedLevels.Add(29);
        Check(SushiRestaurantCatalog.IsUnlocked(player, list.restaurants[1]), "Winning level 30 unlocks immediately");
        Check(SushiDecorationProgress.TryBuild(player, list, second, "deco_sign", out _) &&
            SushiDecorationProgress.AvailableStars(player, catalog) == 26 && player.restaurantDecorationCount == 1,
            "Second restaurant has independent construction and shares the star wallet");
        player = JsonConvert.DeserializeObject<GameDataFragment.DataPlayer>(JsonConvert.SerializeObject(player));
        Check(SushiDecorationProgress.Built(player, catalog, second) == 1 && player.restaurants[0].starsSpent == 2,
            "Save/load retains per-restaurant spending and construction");
        player.cur_level = 30; player.completedLevels.Clear();
        Check(SushiRestaurantCatalog.IsUnlocked(player, list.restaurants[1]), "Older campaign saves retain level-based unlocks");
        player.completedLevels = Enumerable.Range(0, 100).ToList();
        foreach (var restaurant in list.restaurants)
            for (int i = 1; i < catalog.items.Length; i++)
                Check(SushiDecorationProgress.TryBuild(player, list, restaurant.id, catalog.items[i].id, out _),
                    "Build " + restaurant.id + "/" + catalog.items[i].id);
        Check(player.money == 2500 && player.restaurantCompletionRewardClaimed && player.restaurants[0].rewardClaimed &&
            SushiDecorationProgress.AvailableStars(player, catalog) == 0, "Each restaurant rewards 1000 once; both consume 100 stars");
        player = JsonConvert.DeserializeObject<GameDataFragment.DataPlayer>(JsonConvert.SerializeObject(player));
        Check(!SushiDecorationProgress.TryBuild(player, list, second, "deco_torii", out _) &&
            !SushiDecorationProgress.TryBuild(player, list, "unknown", "deco_sign", out _) && player.money == 2500,
            "Completion and unknown IDs cannot pay again after reload");
        Check(player.cur_level == 30 && player.completedLevels.Count == 100 && player.boosterClick == 2,
            "Building either restaurant preserves gameplay state");
    }

    private static void Tick()
    {
        int phase = SessionState.GetInt(Key, 0);
        if (phase == 0 || !Application.isBatchMode || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Start", 0) > 180)
                throw new TimeoutException("Restaurant flow timed out.");
            if (!Application.isPlaying || UIManager.Ins == null || DataManager.ins == null || !DataManager.ins.isLoaded ||
                GameManager.ins == null || GameManager.ins.IsLoadingLevel || !UIManager.Ins.IsOpened<CanvasMainMenu>()) return;
            var menu = UIManager.Ins.GetUI<CanvasMainMenu>();
            var carousel = menu.restaurantCarousel;
            var drawer = UIManager.Ins.GetUI<CanvasDecoration>();
            if (menu.IsTabAnimating || carousel.IsAnimating || drawer.IsAnimating || EditorApplication.timeSinceStartup < readyAt) return;
            var data = GameDataFragment.cur;
            var player = data.dataPlayer;
            var list = carousel.catalog;
            string second = list.restaurants[1].id;
            if (phase == 1)
            {
                SessionState.SetInt(Key + ".Scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle);
                SessionState.SetInt(Key + ".Load", LevelManager.Ins != null ? LevelManager.Ins.LoadVersion : -1);
                Check(carousel.SelectedIndex == 0 && menu.decorationProgress.text == "1/12" &&
                    !carousel.previousButton.interactable && carousel.nextButton.interactable, "Home restores the first restaurant and arrow boundaries");
                Capture("RestaurantFirst", true);
                carousel.nextButton.onClick.Invoke(); SetPhase(2); return;
            }
            if (phase == 2)
            {
                Check(carousel.SelectedIndex == 1 && carousel.lockedPanel.activeInHierarchy && !carousel.homeActions.activeSelf &&
                    carousel.closedSigns[1].activeInHierarchy && carousel.lockDescription.text.Contains("30"),
                    "Next arrow shows the locked building, closed sign and level-30 condition");
                CheckArrowHit(carousel.previousButton);
                menu.OpenDecoration(); menu.Play();
                Check(!UIManager.Ins.IsOpened<CanvasDecoration>() && player.cur_level == 29 &&
                    SushiDecorationProgress.AvailableStars(player, list.decorations) == 27, "Locked building cannot open upgrades or start gameplay");
                Capture("RestaurantLocked", true);
                data.Load(); carousel.RestoreSelection();
                Check(carousel.SelectedIndex == 1, "Save/load restores the selected restaurant, including a locked preview");
                Swipe(carousel, new Vector2(-200, 0), new Vector2(200, 0)); SetPhase(3); return;
            }
            if (phase == 3)
            {
                Check(carousel.SelectedIndex == 0 && menu.decorationProgress.text == "1/12" && carousel.homeActions.activeSelf,
                    "Right swipe restores the first restaurant with its saved construction");
                carousel.Previous(); Swipe(carousel, Vector2.zero, new Vector2(-35, 0));
                Swipe(carousel, Vector2.zero, new Vector2(-40, 250));
                Check(carousel.SelectedIndex == 0, "Edge arrows, short drags and vertical drags cannot change restaurants");
                Swipe(carousel, new Vector2(200, 0), new Vector2(-200, 0)); SetPhase(4); return;
            }
            if (phase == 4)
            {
                Check(carousel.SelectedIndex == 1, "Left swipe opens the next restaurant");
                carousel.Next(); carousel.Next();
                Check(carousel.SelectedIndex == 1 && !carousel.nextButton.interactable, "Rapid arrows cannot pass the last restaurant");
                player.completedLevels.Add(29); player.cur_level = 30;
                menu.RefreshLevelMap();
                Check(menu.IsRestaurantUnlocked && !carousel.lockedPanel.activeSelf && carousel.homeActions.activeSelf &&
                    !carousel.closedSigns[1].activeSelf && menu.decorationProgress.text == "0/12",
                    "Passing level 30 unlocks the current preview immediately without copying construction");
                Capture("RestaurantUnlocked", true);
                menu.OpenDecoration(); SetPhase(5); return;
            }
            if (phase == 5)
            {
                Check(drawer.restaurant == carousel.views[1] && menu.restaurantLayout.IsExpanded && drawer.progressText.text.Contains("0/12"),
                    "Upgrades share the selected second building on Home");
                drawer.buildButton.onClick.Invoke(); drawer.BuildNext();
                Check(SushiDecorationProgress.Built(player, list.decorations, second) == 1 && player.restaurantDecorationCount == 1 &&
                    SushiDecorationProgress.AvailableStars(player, list.decorations) == 26,
                    "One second-restaurant purchase spends once and preserves the first restaurant");
                carousel.Previous(); Swipe(carousel, new Vector2(-200, 0), new Vector2(200, 0));
                Check(carousel.SelectedIndex == 1, "Drawer interaction cannot swipe to a different build target");
                drawer.ClosePanel(); SetPhase(6); return;
            }
            if (phase == 6)
            {
                data.Load(); menu.RefreshLevelMap();
                Check(menu.decorationProgress.text == "1/12" && player.restaurantDecorationCount == 1 &&
                    SushiDecorationProgress.Built(data.dataPlayer, list.decorations, second) == 1,
                    "Reload keeps independent progress and the shared wallet");
                Capture("RestaurantSecondBuilt", true);
                carousel.previousButton.onClick.Invoke(); SetPhase(7); return;
            }
            if (phase == 7)
            {
                Check(carousel.SelectedIndex == 0 && menu.decorationProgress.text == "1/12", "Arrow returns to the original building unchanged");
                menu.OpenShop(); SetPhase(8); return;
            }
            if (phase == 8)
            {
                menu.OpenHome(); SetPhase(9); return;
            }
            if (phase == 9)
            {
                Check(carousel.SelectedIndex == 0 && !menu.restaurantLayout.IsExpanded && carousel.nextButton.interactable,
                    "Returning from another tab preserves selection and restores navigation");
                Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle == SessionState.GetInt(Key + ".Scene", 0) &&
                    (LevelManager.Ins != null ? LevelManager.Ins.LoadVersion : -1) == SessionState.GetInt(Key + ".Load", 0) &&
                    player.cur_level == 30 && player.completedLevels.Count == 30 && player.money == 500,
                    "Browsing, unlocking and building preserve the scene, gameplay and coin wallet");
                File.WriteAllLines("Documentation/RestaurantRegressionResults.txt", Results);
                Debug.Log("SUSHI_RESTAURANTS_OK: " + Results.Count + " assertions passed");
                SessionState.SetInt(Key, 0); EditorApplication.Exit(0);
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Swipe(SushiRestaurantCarousel carousel, Vector2 from, Vector2 to)
    {
        var canvas = carousel.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            pressPosition = RectTransformUtility.WorldToScreenPoint(camera, carousel.transform.TransformPoint(from)),
            position = RectTransformUtility.WorldToScreenPoint(camera, carousel.transform.TransformPoint(from)),
            pointerPressRaycast = new RaycastResult { module = carousel.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>() } };
        carousel.OnBeginDrag(data);
        data.position = RectTransformUtility.WorldToScreenPoint(camera, carousel.transform.TransformPoint(to));
        carousel.OnDrag(data); carousel.OnEndDrag(data);
    }

    private static void CheckArrowHit(UnityEngine.UI.Button button)
    {
        Canvas.ForceUpdateCanvases();
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var pointer = new PointerEventData(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(camera, button.transform.position) };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button,
            "Visible navigation arrow receives pointer input without shortcut overlap");
    }

    private static void Capture(string label, bool allSizes)
    {
        SushiMainMenuCapture.Capture(label, 720, 1280);
        if (!allSizes) return;
        SushiMainMenuCapture.Capture(label, 1080, 2400);
        SushiMainMenuCapture.Capture(label, 1536, 2048);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Results.Add("PASS: " + message);
        // Model results must survive the editor's Play Mode domain reload.
        SessionState.SetString(Key + ".Results", string.Join("\n", Results));
    }
    private static void SetPhase(int phase)
    {
        SessionState.SetInt(Key, phase); readyAt = EditorApplication.timeSinceStartup + 0.7;
        if (Results.Count == 0)
            Results.AddRange(SessionState.GetString(Key + ".Results", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
    }
    private static void Fail(Exception error)
    {
        SessionState.SetInt(Key, 0); Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/RestaurantRegressionResults.txt", "FAIL\n" + error);
        Debug.LogException(error); EditorApplication.Exit(1);
    }
}
#endif
