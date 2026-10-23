using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run in an isolated project with productName "Sushi UI Verification" so saved player data stays separate.
[InitializeOnLoad]
public static class SushiProjectUIRegression
{
    private const string Key = "SushiProjectUIRegression.Pending";
    private const string Started = "SushiProjectUIRegression.Started";
    private static int readyFrame;
    static SushiProjectUIRegression() => EditorApplication.update += Update;

    public static void RunBatch()
    {
        if (!Application.isBatchMode || PlayerSettings.productName != "Sushi UI Verification")
            throw new InvalidOperationException("Run only in the isolated Sushi UI Verification project.");
        SushiProjectUISetup.Setup();
        EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        SessionState.SetFloat(Started, (float)EditorApplication.timeSinceStartup);
        SessionState.SetInt(Key, 1);
        EditorApplication.isPlaying = true;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Invoke(object target, string method, params object[] arguments) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    private static void Click(Component view, string path)
    {
        Button button = view.transform.Find(path)?.GetComponent<Button>();
        Check(button != null && button.onClick.GetPersistentEventCount() > 0, "Missing authored button callback: " + path);
        button.onClick.Invoke();
    }
    private static void Capture(string label, LevelManager level)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        Directory.CreateDirectory("Documentation");
        SushiVisualCapture.CaptureSet("ProjectUI" + label, level);
    }

    private static void Update()
    {
        int phase = SessionState.GetInt(Key, 0);
        if (phase == 0 || !Application.isBatchMode || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Started, 0) > 180)
                throw new TimeoutException("Loading/UI integration timed out.");
            if (!Application.isPlaying || SceneManager.GetActiveScene().name != "GamePlay" ||
                LevelManager.Ins == null || LevelManager.Ins.CurrentLevel == null) return;
            LevelManager level = LevelManager.Ins;
            if (phase == 1)
            {
                Check(Application.persistentDataPath.Contains("Sushi UI Verification"), "Test must never write the user's save directory.");
                Check(DataManager.ins.isLoaded && level.LevelCount == DataManager.ins.allLevelData.listAllLevel.Count, "Loading did not wait for the shared level data.");
                var player = GameDataFragment.cur.dataPlayer;
                player.cur_level = 0; player.money = 1000; player.boosterClick = 2; player.boosterSuffle = 2; player.boosterHut = 2;
                GameManager.ins.isPlayTestingLevel = false;
                level.LoadLevel(0);
                readyFrame = Time.frameCount + 8;
                SessionState.SetInt(Key, 2);
                return;
            }
            if (Time.frameCount < readyFrame) return;
            SessionState.SetInt(Key, 0);
            RunChecks(level);
            File.WriteAllText("ProjectUIRegressionResults.txt", "PASS: Loading, shared levels, prefab binding, HUD input, nested pause, boosters, coin revive, lose/retry, win/reward/next, final level.\n");
            Debug.Log("PROJECT_UI_REGRESSION_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            SessionState.SetInt(Key, 0);
            Debug.LogException(error);
            File.WriteAllText("ProjectUIRegressionResults.txt", "FAIL\n" + error);
            EditorApplication.Exit(1);
        }
    }

    private static void RunChecks(LevelManager level)
    {
        UIManager ui = UIManager.Ins;
        var player = GameDataFragment.cur.dataPlayer;
        Check(ui.IsOpened<CanvasFloating>() && ui.IsOpened<CanvasGamePlay>(), "Project HUD did not open.");
        Check(level.CanInteract && Time.timeScale == 1, "HUD must not pause gameplay.");
        Check(Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length == 1, "Duplicate UI manager after loading.");
        Check(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Duplicate or missing EventSystem.");
        Check(Object.FindFirstObjectByType<SushiGameplayHud>() == null, "Legacy generated HUD is still enabled.");
        CanvasGamePlay hud = ui.GetUI<CanvasGamePlay>();
        CanvasFloating floating = ui.GetUI<CanvasFloating>();
        Canvas.ForceUpdateCanvases();
        Check(floating.GoldFr.transform.position.y > Screen.height * 0.8f,
            "The wallet must stay in the header, clear of conveyor and ingredients.");
        Button settingsButton = floating.btnSettingIngame.GetComponent<Button>();
        Check(settingsButton.isActiveAndEnabled && settingsButton.image.enabled, "Settings button is disabled or invisible.");
        foreach (UICanvas view in new UICanvas[] { hud, ui.GetUI<CanvasFloating>(), ui.GetUI<CanvasVictory>(), ui.GetUI<CanvasFail>() })
        {
            var serialized = new SerializedObject(view);
            foreach (string field in Required(view))
                Check(serialized.FindProperty(field)?.objectReferenceValue != null, view.name + " missing " + field);
        }
        // GetUI caches inactive popups without opening or pausing them.
        ui.CloseUI<CanvasVictory>(); ui.CloseUI<CanvasFail>();
        var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(new Vector3(0, -4, 0)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count == 0, "Decorative HUD graphics block ingredient picks.");
        Capture("Gameplay", level);
        Click(hud, "Content/Header/Speed"); Check(level.Conveyor.IsDoubleSpeed, "Speed button callback failed.");
        settingsButton.onClick.Invoke();
        Check(ui.IsOpened<CanvasSetting>(), "Floating settings callback failed.");
        CanvasSetting settings = ui.GetUI<CanvasSetting>();
        ui.OpenUI<CanvasSetting>();
        Check(!level.CanInteract && Time.timeScale == 0, "Settings must pause gameplay.");
        ui.OpenUI<CanvasFail>();
        Click(settings, "Content/Pause card/Resume");
        Check(level.IsPopupOpen && Time.timeScale == 0, "Closing one popup resumed while another remains open.");
        ui.CloseUI<CanvasFail>();
        Check(level.CanInteract && Time.timeScale == 1, "Popup pause count did not balance.");
        FoodInputController input = Object.FindFirstObjectByType<FoodInputController>();
        Click(hud, "Content/Boosters/Cây gắp");
        Check(input.PickMode == FoodPickMode.Chopsticks && player.boosterClick == 2, "Selecting a booster must not charge it.");
        Click(hud, "Content/Boosters/Cancel booster");
        Check(input.PickMode == FoodPickMode.Tap && player.boosterClick == 2, "Cancel charged a booster.");
        hud.BoosterClick();
        FoodShushi food = level.Board.FoodQueues[0].GetTopFood();
        Check(input.TryUseChopsticks(food) && player.boosterClick == 1, "Successful pick must debit exactly one booster.");
        Check(!input.TryUseChopsticks(food) && player.boosterClick == 1, "Failed pick charged a booster.");
        level.LoadLevel(0);
        Click(hud, "Content/Boosters/Swap Row");
        Check(player.boosterSuffle == 1, "Swap button did not use the saved inventory.");
        player.boosterSuffle = 0; hud.Refresh(); hud.BoosterSuffle();
        Check(player.boosterSuffle == 0, "Empty inventory went negative.");
        Invoke(level, "RequestRevive", SushiReviveReason.FrozenHeads);
        Check(ui.IsOpened<CanvasFail>() && !level.CanInteract, "Revive did not use the project Lose prefab.");
        CanvasFail lose = ui.GetUI<CanvasFail>();
        int money = player.money;
        Click(lose, "Content/Lose and revive card/Revive actions/Revive with coins");
        Check(player.money == money - 300 && level.CanInteract && !ui.IsOpened<CanvasFail>(), "Coin revive did not debit/resume correctly.");
        Invoke(level, "RequestRevive", SushiReviveReason.FrozenHeads);
        Check(level.ReviveCost == 600, "Second revive must cost 600.");
        player.money = 0;
        Click(lose, "Content/Lose and revive card/Revive actions/Revive with coins");
        Check(player.money == 0 && level.PendingRevive != SushiReviveReason.None, "Insufficient funds revived the level.");
        Capture("Revive", level);
        Click(lose, "Content/Lose and revive card/Retry or decline");
        Check(!level.IsPlaying && level.PendingRevive == SushiReviveReason.None && ui.IsOpened<CanvasFail>(), "Decline did not show final Lose state.");
        Capture("Lose", level);
        Click(lose, "Content/Lose and revive card/Retry or decline");
        Check(level.CanInteract && level.CurrentLevelIndex == 0 && Time.timeScale == 1, "Retry did not reset/release the popup.");
        Invoke(level, "CompleteLevel");
        Check(ui.IsOpened<CanvasVictory>() && player.cur_level == 1, "Win/progress did not use shared level order.");
        Check(level.ClaimVictoryReward() && !level.ClaimVictoryReward() && player.money == level.VictoryReward, "Win reward can be claimed more than once.");
        Capture("Win", level);
        Click(ui.GetUI<CanvasVictory>(), "Content/Victory card/Claim and next");
        Check(level.CurrentLevelIndex == 1 && level.CanInteract && player.money == level.VictoryReward, "Next level duplicated the reward or retained pause.");
        level.LoadLevel(level.LevelCount - 1); Invoke(level, "CompleteLevel");
        Click(ui.GetUI<CanvasVictory>(), "Content/Victory card/Claim and next");
        Check(level.CurrentLevelIndex == level.LevelCount - 1 && level.CanInteract, "Last level navigated outside AllLevelData.");
        GameDataFragment.cur.Save();
        int savedLevel = player.cur_level; int savedMoney = player.money;
        GameDataFragment.cur.Load();
        Check(GameDataFragment.cur.dataPlayer.cur_level == savedLevel && GameDataFragment.cur.dataPlayer.money == savedMoney, "Progress or wallet did not persist.");
    }

    private static string[] Required(UICanvas view) => view is CanvasGamePlay
        ? new[] { "rootPanel", "txtLevvel", "occupancy", "boosterHint", "speedText", "cancelBooster", "levelSelector" }
        : view is CanvasFloating ? new[] { "rootPanel", "txtGold", "TopGroup", "btnSettingIngame" }
        : view is CanvasVictory ? new[] { "goldRwTxt", "btnGoldRW", "title", "nextLabel" }
        : new[] { "title", "description", "coinPrice", "retryLabel", "feedback", "reviveActions", "coinButton", "adButton" };
}
