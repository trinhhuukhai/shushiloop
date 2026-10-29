using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DG.Tweening;
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
    private static int firstLoadVersion;
    private static bool transitionCaptured;
    private static IEnumerator checks;
    static SushiProjectUIRegression() => EditorApplication.update += Update;

    public static void RunBatch()
    {
        if (!Application.isBatchMode || PlayerSettings.productName != "Sushi UI Verification")
            throw new InvalidOperationException("Run only in the isolated Sushi UI Verification project.");
        SushiProjectUISetup.Setup();
        SushiMainMenuSetup.Setup();
        SushiTutorialSetup.Setup();
        Check(Application.persistentDataPath.Contains("Sushi UI Verification"), "Never seed the user's save directory.");
        var seededProfile = AssetDatabase.LoadAssetAtPath<GameDataFragment>("Assets/_game/DataSO/GameDataFragment.asset");
        seededProfile.dataPlayer = new GameDataFragment.DataPlayer { cur_level = 4, showTutorialLevel1 = true };
        seededProfile.Save();
        EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        UICheat cheat = Object.FindFirstObjectByType<UICheat>();
        Canvas cheatCanvas = cheat != null ? cheat.GetComponentInParent<Canvas>().rootCanvas : null;
        SessionState.SetBool(Key + ".CheatCanvas", cheatCanvas != null && cheatCanvas.enabled);
        var cheatRaycaster = cheatCanvas != null ? cheatCanvas.GetComponent<GraphicRaycaster>() : null;
        SessionState.SetBool(Key + ".CheatRaycaster", cheatRaycaster != null && cheatRaycaster.enabled);
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

    private static int LoadVersion(LevelManager level) => level.LoadVersion;

    private static void Update()
    {
        int phase = SessionState.GetInt(Key, 0);
        if (phase == 0 || !Application.isBatchMode || EditorApplication.isCompiling) return;
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Started, 0) > 180)
                throw new TimeoutException("Loading/UI integration timed out.");
            if (!Application.isPlaying || UIManager.Ins == null || GameManager.ins == null ||
                DataManager.ins == null || !DataManager.ins.isLoaded) return;
            UIManager ui = UIManager.Ins;
            if (phase == 1)
            {
                if (!ui.IsOpened<CanvasMainMenu>()) return;
                CheckColdMenu();
                Check(Application.persistentDataPath.Contains("Sushi UI Verification"), "Test must never write the user's save directory.");
                var player = GameDataFragment.cur.dataPlayer;
                player.cur_level = 0; player.money = 1000; player.boosterClick = 2; player.boosterSuffle = 2; player.boosterHut = 2;
                GameManager.ins.isPlayTestingLevel = false;
                readyFrame = Time.frameCount + 8;
                transitionCaptured = false;
                checks = null;
                SessionState.SetInt(Key, 2);
                return;
            }
            if (phase == 2)
            {
                if (Time.frameCount < readyFrame) return;
                CheckColdMenu();
                firstLoadVersion = LoadVersion(LevelManager.Ins);
                CanvasMainMenu menu = ui.GetUI<CanvasMainMenu>();
                menu.Play();
                menu.Play();
                Check(GameManager.ins.IsLoadingLevel, "Level button did not begin the explicit gameplay transition.");
                Check(ui.IsOpened<CanvasTransit>() && LevelManager.Ins.CurrentLevel == null,
                    "UICTransis must open before the level is initialized.");
                Check(!LevelManager.Ins.CanInteract && Time.timeScale == 0, "Level transition did not block gameplay.");
                SessionState.SetInt(Key, 3);
                return;
            }
            if (phase == 3)
            {
                Check(SceneManager.GetActiveScene().name == "GamePlay" && LevelManager.Ins != null,
                    "Play must reuse the GamePlay scene prepared by Loading.");
                LevelManager level = LevelManager.Ins;
                if (GameManager.ins.IsLoadingLevel)
                {
                    Check(ui.IsOpened<CanvasTransit>() && !level.CanInteract && Time.timeScale == 0,
                        "Gameplay became interactive before the transition revealed the level.");
                    if (level.CurrentLevel == null)
                        Check(level.Board.FoodQueues.Count == 0 && level.Conveyor.Slots.Count == 0,
                            "Ingredients appeared before the transition prepared the level.");
                    CanvasTransit transit = ui.GetUI<CanvasTransit>();
                    if (!transitionCaptured && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null &&
                        transit.fill != null && transit.fill.fillAmount > 0.2f && transit.fill.fillAmount < 0.7f)
                    {
                        SushiMainMenuCapture.Capture("LevelTransition_720x1280", 720, 1280);
                        transitionCaptured = true;
                    }
                    return;
                }
                Check(level.CurrentLevel != null && level.CanInteract && Time.timeScale == 1,
                    "The completed transition did not release gameplay.");
                Check(level.LevelCount == DataManager.ins.allLevelData.listAllLevel.Count, "Gameplay did not use shared level data.");
                Check(LoadVersion(level) == firstLoadVersion + 1, "Repeated Level button clicks initialized gameplay more than once.");
                Check(!ui.IsOpened<CanvasMainMenu>() && !ui.IsOpened<CanvasTransit>(),
                    "Menu or transition remained open during gameplay.");
                readyFrame = Time.frameCount + 8;
                SessionState.SetInt(Key, 4);
                return;
            }
            // The suite returns Home and awaits transitions; do not require a loaded level here.
            if (Time.frameCount < readyFrame) return;
            if (checks == null) checks = RunChecks(LevelManager.Ins);
            if (checks.MoveNext()) return;
            (checks as IDisposable)?.Dispose();
            checks = null;
            SessionState.SetInt(Key, 0);
            File.WriteAllText("ProjectUIRegressionResults.txt", "PASS: Unlocked level 5 profile Loading to empty GamePlay/MainMenu, explicit Level button, UICTransis before level initialization and reveal, repeated-click guard, shared levels, prefab binding, HUD input, nested pause, boosters, coin revive, lose/retry, win/reward/next, final level, return home, asynchronous rejected/invalid startup recovery, canvas scale restoration, clean re-entry/replay.\n");
            Debug.Log("PROJECT_UI_REGRESSION_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            SessionState.SetInt(Key, 0);
            try { (checks as IDisposable)?.Dispose(); }
            finally { checks = null; }
            Debug.LogException(error);
            File.WriteAllText("ProjectUIRegressionResults.txt", "FAIL\n" + error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckColdMenu()
    {
        UIManager ui = UIManager.Ins;
        Check(SceneManager.GetActiveScene().name == "GamePlay", "Loading did not prepare the GamePlay scene before Home.");
        LevelManager level = LevelManager.Ins;
        Check(level != null && level.Board != null && level.Conveyor != null,
            "Cold home is missing its prepared gameplay systems.");
        Check(!level.IsPlaying && !level.CanInteract && level.CurrentLevel == null && level.totalPlayTime == 0,
            "Cold home started a level or gameplay timer.");
        Check(level.Board.FoodQueues.Count == 0 && level.Conveyor.Slots.Count == 0 &&
            level.Conveyor.Customers.TotalCustomers == 0, "Cold home initialized ingredients, conveyor plates, or customers.");
        Check(GameManager.IsScene(GameScene.Home) && GameManager.IsState(GameState.MainMenu), "Cold home has the wrong navigation state.");
        CanvasScaler homeScaler = ui.GetUI<CanvasMainMenu>().GetComponent<Canvas>().rootCanvas.GetComponent<CanvasScaler>();
        Check(homeScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight && homeScaler.matchWidthOrHeight == 0,
            "Home must use the root canvas width policy on phones and tablets.");
        Check(ui.IsOpened<CanvasMainMenu>() && !ui.IsOpened<CanvasGamePlay>() && !ui.IsOpened<CanvasFloating>(), "Cold home opened gameplay UI.");
        Check(!GameManager.ins.IsLoadingLevel && Time.timeScale == 1, "Cold home retained a loading or pause state.");
        if (UICheat.Ins != null)
        {
            Canvas cheatCanvas = UICheat.Ins.GetComponentInParent<Canvas>().rootCanvas;
            var raycaster = cheatCanvas.GetComponent<GraphicRaycaster>();
            Check(!cheatCanvas.enabled && (raycaster == null || !raycaster.enabled), "Debug overlay obscures or intercepts the menu.");
        }
        Check(LoadingScreen.ins == null || !LoadingScreen.ins.gameObject.activeInHierarchy, "Loading cover did not close before home.");
        Check(EditorBuildSettings.scenes.Length >= 2 && EditorBuildSettings.scenes[0].path.EndsWith("/Loading.unity"),
            "The player must launch from Loading.");
    }

    private static IEnumerator RunChecks(LevelManager level)
    {
        UIManager ui = UIManager.Ins;
        var player = GameDataFragment.cur.dataPlayer;
        Check(ui.IsOpened<CanvasFloating>() && ui.IsOpened<CanvasGamePlay>(), "Project HUD did not open.");
        Check(level.CanInteract && Time.timeScale == 1, "HUD must not pause gameplay.");
        Check(Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length == 1, "Duplicate UI manager after loading.");
        Check(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Duplicate or missing EventSystem.");
        Check(Object.FindFirstObjectByType<SushiGameplayHud>() == null, "Legacy generated HUD is still enabled.");
        CanvasGamePlay hud = ui.GetUI<CanvasGamePlay>();
        Check(hud.GetComponent<Canvas>().rootCanvas.GetComponent<CanvasScaler>().screenMatchMode == CanvasScaler.ScreenMatchMode.Expand,
            "Leaving home must restore the gameplay canvas scale policy.");
        CanvasFloating floating = ui.GetUI<CanvasFloating>();
        if (UICheat.Ins != null)
        {
            Canvas cheatCanvas = UICheat.Ins.GetComponentInParent<Canvas>().rootCanvas;
            var raycaster = cheatCanvas.GetComponent<GraphicRaycaster>();
            Check(cheatCanvas.enabled == SessionState.GetBool(Key + ".CheatCanvas", false) &&
                (raycaster == null || raycaster.enabled == SessionState.GetBool(Key + ".CheatRaycaster", false)),
                "Leaving home must restore the original debug overlay settings.");
        }
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
        Check(GameManager.ins.IsLoadingLevel && ui.IsOpened<CanvasTransit>() && level.CurrentLevelIndex == 0,
            "Early victory Next must show UICTransis before replacing the board.");
        while (GameManager.ins.IsLoadingLevel)
        {
            Check(!level.CanInteract && Time.timeScale == 0, "Victory transition resumed before its cover closed.");
            yield return null;
        }
        Check(level.CurrentLevelIndex == 1 && level.CanInteract && player.money == level.VictoryReward, "Next level duplicated the reward or retained pause.");
        level.LoadLevel(level.LevelCount - 1); Invoke(level, "CompleteLevel");
        Click(ui.GetUI<CanvasVictory>(), "Content/Victory card/Claim and next");
        Check(ui.IsOpened<CanvasMainMenu>() && level.CurrentLevel == null && !GameManager.ins.IsLoadingLevel,
            "Final unlocked level must return Home after claiming its reward.");
        level.LoadLevel(level.LevelCount - 1);
        ui.OpenUI<CanvasSetting>();
        ui.OpenUI<CanvasFail>();
        Check(Time.timeScale == 0, "Home teardown test requires nested paused popups.");
        ui.GetUI<CanvasSetting>().OnBackHome();
        Check(ui.IsOpened<CanvasMainMenu>() && !ui.IsOpened<CanvasGamePlay>() &&
            !ui.IsOpened<CanvasFloating>() && !ui.IsOpened<CanvasSetting>() && !ui.IsOpened<CanvasFail>(), "Home did not close gameplay and its popups.");
        Check(!level.IsPlaying && !level.IsPopupOpen && level.CurrentLevel == null && level.totalPlayTime == 0,
            "Home retained a running level, timer, or popup.");
        Check(level.Board.FoodQueues.Count == 0 && level.Conveyor.Slots.Count == 0 &&
            level.Conveyor.Customers.TotalCustomers == 0, "Home retained ingredients, conveyor plates, or customers.");
        Check(Time.timeScale == 1 && GameManager.IsState(GameState.MainMenu), "Returning home left the app paused.");
        player.cur_level = 0;
        IEnumerator recovery = CheckStartupRecovery(level, ui);
        try
        {
            while (recovery.MoveNext()) yield return recovery.Current;
        }
        finally { (recovery as IDisposable)?.Dispose(); }
        CanvasMainMenu menu = ui.GetUI<CanvasMainMenu>();
        int reentryVersion = LoadVersion(level);
        menu.Play();
        menu.Play();
        Check(GameManager.ins.IsLoadingLevel && ui.IsOpened<CanvasTransit>(),
            "Re-entering gameplay did not show UICTransis.");
        while (GameManager.ins.IsLoadingLevel) yield return null;
        Check(level.CanInteract && level.CurrentLevelIndex == 0 && Time.timeScale == 1 &&
            !ui.IsOpened<CanvasTransit>(), "Level button could not re-enter after returning home.");
        Check(LoadVersion(level) == reentryVersion + 1, "Repeated re-entry clicks initialized the level more than once.");
        FoodQueue firstQueue = level.Board.FoodQueues[0];
        menu.Play();
        Check(level.Board.FoodQueues[0] == firstQueue, "A second Level click restarted the already entered level.");
        IEnumerator replay = CheckSettingsReplay(level);
        try { while (replay.MoveNext()) yield return replay.Current; }
        finally { (replay as IDisposable)?.Dispose(); }
        GameDataFragment.cur.Save();
        int savedLevel = player.cur_level; int savedMoney = player.money;
        GameDataFragment.cur.Load();
        Check(GameDataFragment.cur.dataPlayer.cur_level == savedLevel && GameDataFragment.cur.dataPlayer.money == savedMoney, "Progress or wallet did not persist.");
    }

    public static IEnumerator CheckSettingsReplay(LevelManager level)
    {
        UIManager ui = UIManager.Ins;
        var player = GameDataFragment.cur.dataPlayer;
        int index = level.CurrentLevelIndex;
        SushiLevelData data = level.CurrentLevel;
        int version = LoadVersion(level);
        int progress = player.cur_level, money = player.money;
        int click = player.boosterClick, swap = player.boosterSuffle, magnet = player.boosterHut;
        // Leave a real pickup in flight so replay must clear the old board and pending deliveries.
        //Check(level.Conveyor.TryTakeFromQueue(level.Board.FoodQueues[0]), "Replay test needs a legal initial pickup.");
        //CanvasSetting settings = ui.OpenUI<CanvasSetting>();
        //Check(settings.back_Bt != null && settings.back_Bt.onClick.GetPersistentEventCount() > 0,
        //    "Settings replay button has no saved callback.");
        //settings.back_Bt.onClick.Invoke();
        //Check(GameManager.ins.IsLoadingLevel && ui.IsOpened<CanvasTransit>() &&
        //    !ui.IsOpened<CanvasSetting>() && LoadVersion(level) == version,
        //    "Settings replay must display Transis before reloading the level.");
        //settings.OnReplayLevel();
        //Check(!GameManager.ins.ReplayCurrentLevel(), "Replay accepted a duplicate request while loading.");
        bool sawCoveredReload = false;
        double deadline = EditorApplication.timeSinceStartup + 10;
        while (GameManager.ins.IsLoadingLevel)
        {
            Check(EditorApplication.timeSinceStartup < deadline, "Settings replay transition timed out.");
            Check(ui.IsOpened<CanvasTransit>() && !level.CanInteract && Time.timeScale == 0,
                "Replay resumed gameplay before Transis closed.");
            Check(level.CurrentLevelIndex == index && level.CurrentLevel == data,
                "Replay loaded a different level.");
            if (LoadVersion(level) != version)
            {
                Check(LoadVersion(level) == version + 1, "Repeated replay clicks loaded more than once.");
                Check(!ui.IsOpened<CanvasGamePlay>() && !ui.IsOpened<CanvasFloating>(),
                    "Replay showed its new HUD before the transition finished.");
                if (!sawCoveredReload)
                {
                    var cover = ui.GetUI<CanvasTransit>().GetComponent<CanvasGroup>();
                    Check(cover != null && cover.alpha > 0.99f, "Replay rebuilt the board before Transis covered it.");
                    sawCoveredReload = true;
                }
            }
            yield return null;
        }
        Check(sawCoveredReload && LoadVersion(level) == version + 1 && level.CanInteract &&
            !level.IsPopupOpen && Time.timeScale == 1 && !ui.IsOpened<CanvasTransit>() &&
            !ui.IsOpened<CanvasSetting>() && ui.IsOpened<CanvasGamePlay>() && ui.IsOpened<CanvasFloating>(),
            "Replay did not reveal one fresh, interactive level with its HUD.");
        Check(level.Conveyor.OccupiedSlotCount == 0 && level.Conveyor.DeliveringCount == 0 &&
            level.Conveyor.Customers.CompletedCustomers == 0 && !level.HasWon,
            "Replay retained food, deliveries, or completed orders from the previous attempt.");
        Check(player.cur_level == progress && player.money == money && player.boosterClick == click &&
            player.boosterSuffle == swap && player.boosterHut == magnet,
            "Replay changed saved progress, money, or booster inventory.");
        CanvasGamePlay hud = ui.GetUI<CanvasGamePlay>();
        Vector2 initialBoosterPosition = hud.BoosterGroup.anchoredPosition;
        level.Board.ClearBoard();
        hud.Refresh();
        DOTween.Complete(hud.BoosterGroup);
        Check(hud.BoosterGroup.anchoredPosition.y < initialBoosterPosition.y,
            "An emptied ingredient board did not slide the booster group down.");
        level.ReplayLevel();
        hud.Refresh();
        Check(Vector2.Distance(hud.BoosterGroup.anchoredPosition, initialBoosterPosition) < 0.01f,
            "Loading or replaying a level did not restore the booster group position.");
        Debug.Log("SETTINGS_REPLAY_TRANSITION_OK");
    }

    private static IEnumerator CheckStartupRecovery(LevelManager level, UIManager ui)
    {
        CanvasMainMenu menu = ui.GetUI<CanvasMainMenu>();
        var playButton = (Button)typeof(CanvasMainMenu).GetField("playButton",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
        // A ready button can race with an unavailable service. A rejected request must release its latch.
        DataManager.ins.isLoaded = false;
        try { menu.Play(); }
        finally { DataManager.ins.isLoaded = true; }
        menu.RefreshLevelMap();
        Check(playButton.interactable && !GameManager.ins.IsLoadingLevel, "A rejected Level request locked the menu.");

        var levels = DataManager.ins.allLevelData.listAllLevel;
        SushiLevelData original = levels[0];
        SushiLevelData invalid = Object.Instantiate(original);
        invalid.beltSlots = 0;
        var expectedLog = new ExpectedErrorLogHandler(Debug.unityLogger.logHandler, invalid.ValidateData());
        try
        {
            levels[0] = invalid;
            Debug.unityLogger.logHandler = expectedLog;
            menu.Play();
            Check(GameManager.ins.IsLoadingLevel && ui.IsOpened<CanvasTransit>(),
                "Invalid-level startup must still enter the transition before validation.");
            while (GameManager.ins.IsLoadingLevel) yield return null;
            Check(expectedLog.Count == 1, "Invalid level startup did not report its validation error.");
        }
        finally
        {
            Debug.unityLogger.logHandler = expectedLog.Inner;
            levels[0] = original;
            Object.DestroyImmediate(invalid);
        }
        menu.RefreshLevelMap();
        Check(ui.IsOpened<CanvasMainMenu>() && playButton.interactable && !GameManager.ins.IsLoadingLevel &&
            !ui.IsOpened<CanvasTransit>() && !level.IsPlaying && level.CurrentLevel == null && Time.timeScale == 1,
            "Failed level startup did not restore a usable, unpaused home menu.");
        menu.SelectTab(0);
        Check(menu.SelectedTab == 0, "Failed level startup left tab navigation locked.");
        menu.SelectTab(1);
        Check(menu.SelectedTab == 1, "Home tab could not recover after failed startup.");
    }

    private sealed class ExpectedErrorLogHandler : ILogHandler
    {
        public readonly ILogHandler Inner;
        private readonly string expected;
        public int Count { get; private set; }
        public ExpectedErrorLogHandler(ILogHandler inner, string expected)
        {
            Inner = inner;
            this.expected = expected;
        }
        public void LogFormat(LogType type, Object context, string format, params object[] arguments)
        {
            if (type == LogType.Error && string.Format(format, arguments) == expected) { Count++; return; }
            Inner.LogFormat(type, context, format, arguments);
        }
        public void LogException(Exception error, Object context) => Inner.LogException(error, context);
    }

    private static string[] Required(UICanvas view) => view is CanvasGamePlay
        ? new[] { "rootPanel", "txtLevvel", "boosterHint", "speedText", "cancelBooster", "levelSelector", "BoosterGroup" }
        : view is CanvasFloating ? new[] { "rootPanel", "txtGold", "TopGroup", "btnSettingIngame" }
        : view is CanvasVictory ? new[] { "goldRwTxt", "btnGoldRW", "title", "nextLabel" }
        : new[] { "title", "description", "coinPrice", "retryLabel", "feedback", "reviveActions", "coinButton", "adButton" };
}
