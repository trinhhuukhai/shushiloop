#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SushiDecorationRegression
{
    private static int checks;
    private static readonly List<string> Results = new List<string>();
    private const string FlowKey = "SushiDecorationRegression.Flow";
    private static double readyAt;
    static SushiDecorationRegression() => EditorApplication.update += TickFlow;

    public static void RunFlowBatch()
    {
        Guard();
        SushiDecorationSetup.Setup();
        StartFlow();
    }

    private static void Guard()
    {
        if (!Application.isBatchMode || PlayerSettings.productName != "Sushi UI Verification" ||
            !Path.GetFullPath(Application.dataPath).Contains("SushiVerification"))
            throw new InvalidOperationException("Run in an isolated Sushi UI Verification project.");
    }

    private static void StartFlow()
    {
        checks = 0; Results.Clear();
        var data = AssetDatabase.LoadAssetAtPath<GameDataFragment>("Assets/_game/DataSO/GameDataFragment.asset");
        data.dataPlayer = new GameDataFragment.DataPlayer { cur_level = 4, showTutorialLevel1 = true,
            completedLevels = Enumerable.Range(0, 2).ToList() };
        data.Save();
        SessionState.SetBool(FlowKey + ".CapturedSix", false);
        SessionState.SetInt(FlowKey, 1);
        SessionState.SetFloat(FlowKey + ".Started", (float)EditorApplication.timeSinceStartup);
        EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        EditorApplication.isPlaying = true;
    }

    private static void TickFlow()
    {
        int phase = SessionState.GetInt(FlowKey, 0);
        if (phase == 0 || !Application.isBatchMode || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(FlowKey + ".Started", 0) > 180)
                throw new TimeoutException("Upgrade drawer flow timed out.");
            if (!Application.isPlaying || UIManager.Ins == null || DataManager.ins == null || !DataManager.ins.isLoaded ||
                GameManager.ins == null || GameManager.ins.IsLoadingLevel || !UIManager.Ins.IsOpened<CanvasMainMenu>()) return;
            var menu = UIManager.Ins.GetUI<CanvasMainMenu>();
            var drawer = UIManager.Ins.GetUI<CanvasDecoration>();
            if (menu.IsTabAnimating || drawer.IsAnimating || EditorApplication.timeSinceStartup < readyAt) return;
            var data = GameDataFragment.cur;
            if (phase == 1)
            {
                Check(menu.decorationProgress.text == "0/12", "Home starts with saved construction progress");
                SessionState.SetInt(FlowKey + ".Scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle);
                SessionState.SetInt(FlowKey + ".Load", LevelManager.Ins != null ? LevelManager.Ins.LoadVersion : -1);
                Capture("UpgradeDrawerHome0", false);
                menu.OpenDecoration(); SetPhase(2); return;
            }
            if (phase == 2)
            {
                Check(menu.SelectedTab == 1 && menu.decorationRestaurant.gameObject.activeInHierarchy &&
                    menu.restaurantLayout.IsExpanded && drawer.restaurant == menu.decorationRestaurant,
                    "Opening upgrades keeps Home visible and uses its only restaurant");
                Check(drawer.pageContent.rect.height < drawer.pageContent.parent.GetComponent<RectTransform>().rect.height * 0.55f &&
                    drawer.itemScroll.vertical, "Upgrade controls occupy a scrollable bottom drawer");
                Capture("UpgradeDrawerOpen0", true);
                drawer.itemButtons[1].onClick.Invoke();
                Check(data.dataPlayer.restaurantDecorationCount == 0, "Tapping a locked card cannot skip the fixed order");
                drawer.buildButton.onClick.Invoke(); drawer.BuildNext();
                Check(data.dataPlayer.restaurantDecorationCount == 1 && drawer.IsBuilding &&
                    drawer.restaurant.itemRoots[0].gameObject.activeSelf && drawer.restaurant.itemRoots[0].localScale.x < 1,
                    "Upgrade pops directly into the live Home building and rejects repeated taps");
                Check(SushiDecorationProgress.AvailableStars(data.dataPlayer, SushiDecorationProgress.Catalog) == 0 &&
                    menu.SelectedTab == 1 && !menu.IsTabAnimating, "Upgrade spends two stars without navigating");
                drawer.ClosePanel(); SetPhase(3); return;
            }
            if (phase == 3)
            {
                data.Load();
                Check(!UIManager.Ins.IsOpened<CanvasDecoration>() && !menu.restaurantLayout.IsExpanded &&
                    data.dataPlayer.restaurantDecorationCount == 1 && menu.decorationRestaurant.itemRoots[0].gameObject.activeSelf,
                    "Closing during the reveal preserves the item on Home");
                menu.OpenDecoration(); SetPhase(4); return;
            }
            if (phase == 4)
            {
                Check(!drawer.buildButton.interactable && drawer.statusText.text.Contains("Thắng thêm"),
                    "Insufficient stars disable upgrading and explain how to earn more");
                data.dataPlayer.completedLevels = Enumerable.Range(0, 50).ToList();
                drawer.RefreshItems();
                Time.timeScale = 0;
                drawer.buildButton.onClick.Invoke(); SetPhase(5, 1); return;
            }
            if (phase == 5)
            {
                Check(!drawer.IsBuilding && data.dataPlayer.restaurantDecorationCount == 2,
                    "Upgrade reveal completes with paused gameplay time");
                Time.timeScale = 1; SetPhase(6); return;
            }
            if (phase == 6)
            {
                if (drawer.IsBuilding) return;
                int built = data.dataPlayer.restaurantDecorationCount;
                if (built == 6 && !SessionState.GetBool(FlowKey + ".CapturedSix", false))
                {
                    Check(drawer.itemScroll.content.anchoredPosition.y > 0, "Cards scroll to the next upgrade as construction progresses");
                    Capture("UpgradeDrawerOpen6", true);
                    SessionState.SetBool(FlowKey + ".CapturedSix", true);
                }
                if (built < 12) { drawer.buildButton.onClick.Invoke(); SetPhase(6, 1); return; }
                Check(data.dataPlayer.money == 1500 && data.dataPlayer.restaurantCompletionRewardClaimed &&
                    drawer.completionPanel.activeSelf && drawer.itemButtons.All(b => !b.interactable),
                    "Final upgrade grants 1000 coins once and shows completion inline");
                Capture("UpgradeDrawerComplete", false);
                var close = drawer.GetComponentsInChildren<Button>().First(b =>
                    b.onClick.GetPersistentEventCount() > 0 && b.onClick.GetPersistentMethodName(0) == "ClosePanel");
                close.onClick.Invoke(); SetPhase(7); return;
            }
            if (phase == 7)
            {
                data.Load();
                Check(menu.decorationProgress.text == "12/12" && menu.restaurantCompleteBadge.activeInHierarchy &&
                    menu.decorationRestaurant.itemRoots.All(r => r.gameObject.activeSelf),
                    "X closes the drawer and keeps all twelve saved upgrades on Home");
                Capture("UpgradeDrawerHome12", true);
                menu.OpenDecoration(); SetPhase(8); return;
            }
            if (phase == 8)
            {
                Check(drawer.completionPanel.activeSelf && data.dataPlayer.money == 1500 &&
                    !drawer.itemButtons.Any(b => b.interactable), "Reopening completion cannot pay or purchase again");
                menu.OpenHome();
                Check(!UIManager.Ins.IsOpened<CanvasDecoration>() && menu.SelectedTab == 1,
                    "Home navigation dismisses upgrades without changing pages");
                menu.OpenDecoration(); menu.OpenDecoration(); menu.OpenShop(); SetPhase(9); return;
            }
            if (phase == 9)
            {
                Check(!UIManager.Ins.IsOpened<CanvasDecoration>() && menu.SelectedTab == 0,
                    "Switching tabs during drawer animation closes upgrades cleanly");
                menu.OpenDecoration(); SetPhase(10); return;
            }
            if (phase == 10)
            {
                Check(menu.SelectedTab == 1 && drawer.restaurant.gameObject.activeInHierarchy &&
                    UIManager.Ins.IsOpened<CanvasDecoration>() && !menu.IsTabAnimating,
                    "Opening upgrades from another tab returns to the same Home immediately");
                drawer.BackKey(); SetPhase(11); return;
            }
            Check(!UIManager.Ins.IsOpened<CanvasDecoration>() && !menu.restaurantLayout.IsExpanded,
                "Back closes the drawer and restores Home controls");
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle == SessionState.GetInt(FlowKey + ".Scene", 0) &&
                (LevelManager.Ins != null ? LevelManager.Ins.LoadVersion : -1) == SessionState.GetInt(FlowKey + ".Load", 0) &&
                data.dataPlayer.completedLevels.Count == 50 && data.dataPlayer.money == 1500,
                "All upgrades preserve the scene, gameplay load version, campaign wins and wallet");
            File.WriteAllLines("Documentation/DecorationFlowRegressionResults.txt", Results);
            Debug.Log("SUSHI_DECORATION_FLOW_OK: " + checks + " assertions passed");
            SessionState.SetInt(FlowKey, 0); EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            SessionState.SetInt(FlowKey, 0); Time.timeScale = 1;
            File.WriteAllText("Documentation/DecorationFlowRegressionResults.txt", "FAIL\n" + error);
            Debug.LogException(error); EditorApplication.Exit(1);
        }
    }

    private static void Capture(string label, bool allSizes)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        SushiMainMenuCapture.Capture(label, 720, 1280);
        if (!allSizes) return;
        SushiMainMenuCapture.Capture(label, 1080, 2400);
        SushiMainMenuCapture.Capture(label, 1536, 2048);
    }

    private static void SetPhase(int phase, double delay = 0.6)
    { SessionState.SetInt(FlowKey, phase); readyAt = EditorApplication.timeSinceStartup + delay; }

    public static void RunBatch()
    {
        try
        {
            Guard();
            SushiDecorationSetup.Setup();
            CheckModel();
            CheckPrefabsAndCapture();
            Directory.CreateDirectory("Documentation");
            File.WriteAllLines("Documentation/DecorationRegressionResults.txt", Results);
            Debug.Log("SUSHI_DECORATION_OK: " + checks + " assertions passed");
            StartFlow();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/DecorationRegressionResults.txt", "FAIL\n" + error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckModel()
    {
        var catalog = SushiDecorationProgress.Catalog;
        int[] costs = { 2, 2, 3, 3, 4, 4, 4, 5, 5, 5, 6, 7 };
        Check(catalog != null && catalog.items.Length == 12 && catalog.completionCoins == 1000, "12 fixed items, completion reward 1000");
        Check(catalog.items.Select(i => i.starCost).SequenceEqual(costs) && catalog.StarsSpent(12) == 50, "Exact spreadsheet costs total 50 stars");
        var player = JsonConvert.DeserializeObject<GameDataFragment.DataPlayer>("{\"money\":500,\"completedLevels\":[0,1]}");
        Check(player.restaurantDecorationCount == 0 && !player.restaurantCompletionRewardClaimed &&
            SushiDecorationProgress.AvailableStars(player, catalog) == 2, "Old saves start with an empty restaurant and retain earned stars");
        Check(!SushiDecorationProgress.TryBuild(player, catalog, "deco_floor", out _) && player.money == 500,
            "Cannot skip fixed order or mutate wallet on rejection");
        Check(SushiDecorationProgress.TryBuild(player, catalog, "deco_sign", out int reward) && reward == 0 &&
            SushiDecorationProgress.AvailableStars(player, catalog) == 0, "Building consumes exactly the item's stars");
        Check(!SushiDecorationProgress.TryBuild(player, catalog, "deco_sign", out _) &&
            !SushiDecorationProgress.TryBuild(player, catalog, "deco_floor", out _) && player.restaurantDecorationCount == 1,
            "Repeated tap and insufficient stars are rejected without changing progress");
        player.completedLevels = new List<int> { 0, 0, 1, -1 };
        Check(SushiDecorationProgress.AvailableStars(player, catalog) == 0, "Duplicate wins/replays and negative IDs cannot create extra stars");
        player.completedLevels = Enumerable.Range(0, 50).ToList();
        for (int i = 1; i < 12; i++)
        {
            Check(SushiDecorationProgress.TryBuild(player, catalog, catalog.items[i].id, out reward), "Build " + catalog.items[i].id);
            Check(SushiDecorationProgress.AvailableStars(player, catalog) == 50 - catalog.StarsSpent(i + 1), "Correct remaining stars at step " + (i + 1));
            player = JsonConvert.DeserializeObject<GameDataFragment.DataPlayer>(JsonConvert.SerializeObject(player));
            Check(player.restaurantDecorationCount == i + 1, "Save/load retains step " + (i + 1));
        }
        Check(player.money == 1500 && player.restaurantCompletionRewardClaimed && reward == 1000,
            "Final build grants exactly 1000 coins and persists the claimed flag");
        Check(!SushiDecorationProgress.TryBuild(player, catalog, "deco_torii", out reward) && reward == 0 && player.money == 1500,
            "Completion reward cannot be claimed twice after reload");
        Check(player.completedLevels.SequenceEqual(Enumerable.Range(0, 50)) && player.boosterClick == 2 && player.cur_level == 0,
            "Decoration preserves campaign wins, level and boosters");
        player.completedLevels.Add(50);
        Check(SushiDecorationProgress.AvailableStars(player, catalog) == 1, "Future wins remain available after completion");
    }

    private static void CheckPrefabsAndCapture()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var home = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/UICMainMenu.prefab"));
        var decor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/UICDecor.prefab"));
        var menu = home.GetComponent<CanvasMainMenu>();
        var drawer = decor.GetComponent<CanvasDecoration>();
        Check(menu.decorationRestaurant != null && menu.decorationProgress != null && menu.restaurantLayout != null,
            "Home binds its restaurant, progress and responsive layout");
        Check(drawer.GetComponentsInChildren<SushiDecorationRestaurantView>(true).Length == 0 &&
            drawer.itemCards.Length == 12 && drawer.itemButtons.Length == 12,
            "Drawer contains upgrade cards and no duplicate restaurant");
        Check(drawer.itemButtons.All(b => b.onClick.GetPersistentMethodName(0) == "BuildItem"),
            "All twelve item cards bind the indexed upgrade action");
        Check(drawer.itemScroll.content.GetComponent<GridLayoutGroup>().constraintCount == 3 &&
            drawer.sheetGroup != null && drawer.completionPanel.GetComponentInParent<ScrollRect>() == null,
            "Three-column scroll grid and inline reward are authored in the drawer");
        var view = menu.decorationRestaurant;
        Check(view.itemRoots.Length == 12 && view.itemRoots.All(r => r != null), "Home contains every item layer");
        for (int count = 0; count <= 12; count++)
        {
            view.Show(count);
            Check(view.itemRoots.Select((r, i) => r.gameObject.activeSelf == (i < count)).All(v => v),
                "Only purchased Home layers visible at " + count + "/12");
        }
        Object.DestroyImmediate(home); Object.DestroyImmediate(decor);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++; Results.Add("PASS: " + message);
    }
}
#endif
