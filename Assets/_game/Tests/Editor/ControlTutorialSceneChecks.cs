using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

// Uses an isolated project and the real saved GamePlay scene; never seed the developer's save.
[InitializeOnLoad]
public static class ControlTutorialSceneChecks
{
    private const string Key = "ControlTutorialSceneChecks.Pending";
    private static IEnumerator runner;
    private static string runtimeError;
    private static int lastFrame = -1;

    static ControlTutorialSceneChecks()
    {
        EditorApplication.update += Update;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception) &&
                (stack == null || !stack.Contains("UnityEditor.Search"))) runtimeError = message + "\n" + stack;
        };
    }

    public static void RunBatch()
    {
        Check(Application.isBatchMode && PlayerSettings.productName == "Sushi Tutorial Verification" &&
            Application.persistentDataPath.Contains("Sushi Tutorial Verification"), "Use the isolated tutorial verification project.");
        CheckCampaignTargets();
        // Home startup avoids the early-level WaitForEndOfFrame in batch mode.
        var profile = AssetDatabase.LoadAssetAtPath<GameDataFragment>("Assets/_game/DataSO/GameDataFragment.asset");
        profile.dataPlayer = new GameDataFragment.DataPlayer { cur_level = 4 };
        profile.Save();
        Directory.CreateDirectory("Documentation");
        EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        SessionState.SetFloat(Key + ".Started", (float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    private static void Update()
    {
        if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
        try
        {
            Check(runtimeError == null, runtimeError);
            Check(EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Started", 0) < 150, "Scene tutorial timed out.");
            if (!Application.isPlaying || LevelManager.Ins == null || GameManager.ins == null ||
                GameManager.ins.IsLoadingLevel || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (runner == null) runner = Run();
            if (runner.MoveNext()) return;
            SessionState.SetBool(Key, false);
            int campaignLevelsChecked = SessionState.GetInt(Key + ".CampaignLevels", 0);
            File.WriteAllText("ControlTutorialResults.txt", $"PASS: {campaignLevelsChecked} campaign assets have one shallow ClickTut marker; level 1, Click, Magnet and ingredient lessons; level 10 Rush popup and order lesson, timer pause/resume, correct customer, recipe reveal, direct popup, cancellation/retry, persistence and stale callbacks; missing/deep markers, wrong taps, pointer layout and campaign targets.\n");
            Debug.Log("CONTROL_TUTORIAL_SCENE_CHECKS_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            SessionState.SetBool(Key, false);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static IEnumerator Run()
    {
        UIManager.Ins.CloseAll();
        LevelManager level = LevelManager.Ins;
        var player = GameDataFragment.cur.dataPlayer;
        player.cur_level = 0;
        player.showTutorialLevel1 = false;
        player.completedLevels.Clear();
        level.LoadLevel(0);
        for (int i = 0; i < 12; i++) yield return null;
        ControlTutorial tutorial = ControlTutorial.Ins;
        Check(tutorial != null && tutorial.Step == ControlTutorial.TutorialStep.Order, "Scene controller did not start on level 1.");
        Check(level.IsTutorialHeld && !level.CanInteract && SushiLevelOneTutorial.IsInputLocked, "Tutorial does not hold the game.");
        Check(tutorial.handOrder.activeSelf && tutorial.objDesOrder.activeSelf && !tutorial.handFood.activeSelf &&
            !tutorial.objDesFood.activeSelf, "Order step has incorrect visibility.");
        Check(tutorial.TargetFood.TutorialType == TypeTutFood.ClickTut &&
            tutorial.TargetFood.Owner.GetTopFood() == tutorial.TargetFood, "Did not select the marked head tile.");
        Sprite panel = tutorial.objDesOrder.GetComponent<SpriteRenderer>().sprite;
        Check(panel != null && AssetDatabase.GetAssetPath(panel).EndsWith("TutorialPanel.png"), "Generated panel is missing from the scene.");
        Check(!UIManager.Ins.IsOpened<CanvasTutorial>(), "Legacy tutorial overlay covers the scene controller.");
        CheckPointer(tutorial.handOrder, tutorial.TargetOrder.DishImage.transform.position);
        Vector3 position = tutorial.handOrder.transform.position;
        for (int i = 0; i < 10; i++) yield return null;
        Check(tutorial.handOrder.transform.position != position, "Order pointer is not animated.");
        Capture("ControlTutorialOrder", level);

        FoodQueue targetQueue = tutorial.TargetFood.Owner;
        FoodQueue otherQueue = level.Board.FoodQueues[1];
        int initialCount = targetQueue.Count;
        foreach (FoodQueue queue in level.Board.FoodQueues)
            Check(!level.Conveyor.TryTakeFromQueue(queue), "World tap bypassed the order step.");
        FoodInputController input = UnityEngine.Object.FindFirstObjectByType<FoodInputController>();
        input.SelectChopsticks();
        Check(input.PickMode == FoodPickMode.Tap, "Booster bypassed tutorial input lock.");
        Press(tutorial, otherQueue.GetTopFood().transform.position);
        Check(tutorial.Step == ControlTutorial.TutorialStep.Order, "Wrong tap advanced order step.");
        CheckNoBlockingUI(tutorial.TargetOrder.transform.position);
        Press(tutorial, tutorial.TargetOrder.transform.position);
        Check(tutorial.Step == ControlTutorial.TutorialStep.Food && tutorial.TargetOrder.IsRecipeExpanded,
            "Order tap did not reveal ingredients and advance.");
        Check(!tutorial.handOrder.activeSelf && !tutorial.objDesOrder.activeSelf && tutorial.handFood.activeSelf &&
            tutorial.objDesFood.activeSelf, "Ingredient step has incorrect visibility.");
        foreach (TMP_Text label in tutorial.TargetOrder.GetComponentsInChildren<TMP_Text>())
            Check(!label.text.StartsWith("×"), "Expanded order reintroduced quantity labels.");
        for (int i = 0; i < 20; i++) yield return null;
        CheckPointer(tutorial.handFood, tutorial.TargetFood.transform.position);
        Capture("ControlTutorialIngredient", level);
        Press(tutorial, otherQueue.GetTopFood().transform.position);
        Check(targetQueue.Count == initialCount && !player.showTutorialLevel1, "Wrong ingredient completed tutorial.");
        CheckNoBlockingUI(tutorial.TargetFood.transform.position);
        Press(tutorial, tutorial.TargetFood.transform.position);
        Check(targetQueue.Count == initialCount - 1 && player.showTutorialLevel1, "Correct tap did not pick the marked ingredient.");
        Check(!ControlTutorial.IsInputLocked && !SushiLevelOneTutorial.IsInputLocked && level.CanInteract,
            "Completion did not release gameplay.");
        CheckHidden(tutorial);
        GameDataFragment.cur.Load();
        Check(GameDataFragment.cur.dataPlayer.showTutorialLevel1, "Tutorial completion was not saved.");
        level.LoadLevel(0);
        yield return null;
        Check(!ControlTutorial.IsInputLocked, "Completed tutorial replayed.");
        GameDataFragment.cur.dataPlayer.showTutorialLevel1 = false;
        level.LoadLevel(0);
        yield return null;
        tutorial.CancelTutorial();
        Check(level.CanInteract && !GameDataFragment.cur.dataPlayer.showTutorialLevel1, "Cancel saved completion or retained a hold.");
        CheckHidden(tutorial);
        CheckLegacyMarkerTarget(level);
        level.LoadLevel(0);
        yield return null;
        level.LoadLevel(1);
        yield return null;
        Check(!ControlTutorial.IsInputLocked && level.CanInteract, "Level change retained tutorial state.");
        CheckHidden(tutorial);
        IEnumerator introductions = CheckIntroductions(level);
        while (introductions.MoveNext()) yield return introductions.Current;
        IEnumerator rush = CheckRushIntroduction(level);
        while (rush.MoveNext()) yield return rush.Current;
        IEnumerator campaign = CheckCampaignRuntimeTargets(level);
        while (campaign.MoveNext()) yield return campaign.Current;
    }

    private static void CheckLegacyMarkerTarget(LevelManager level)
    {
        var legacy = level.GetComponent<SushiLevelOneTutorial>();
        bool created = legacy == null;
        if (created) legacy = level.gameObject.AddComponent<SushiLevelOneTutorial>();
        var enabled = typeof(SushiLevelOneTutorial).GetField("enableTutorial",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool original = (bool)enabled.GetValue(legacy);
        try
        {
            enabled.SetValue(legacy, true);
            legacy.Prepare(level);
            Check(legacy.Stage == SushiLevelOneTutorial.TutorialStage.Waiting &&
                legacy.TargetFood.TutorialType == TypeTutFood.ClickTut && legacy.TargetQueue.GetTopFood() == legacy.TargetFood,
                "Legacy overlay did not follow the authored marker.");
        }
        finally
        {
            legacy.Cancel();
            enabled.SetValue(legacy, original);
            if (created) UnityEngine.Object.Destroy(legacy);
        }
    }

    private static IEnumerator CheckCampaignRuntimeTargets(LevelManager level)
    {
        UIManager.Ins.CloseAll();
        var tutorial = ControlTutorial.Ins;
        GameDataFragment.cur.dataPlayer.showTutorialLevel1 = true;
        for (int index = 0; index < level.LevelCount; index++)
        {
            level.LoadLevel(index, false);
            bool click = index + 1 == GameDataFragment.BoosterClickUnlockLevel;
            bool started = click ? tutorial.BeginClickBooster(level, null) : tutorial.BeginItem(level, FoodState.HoiCham);
            Check(started && tutorial.TargetFood != null && tutorial.TargetFood.TutorialType == TypeTutFood.ClickTut,
                "Level " + (index + 1) + " did not resolve its authored tutorial target.");
            yield return null;
            if (!click)
            {
                tutorial.RefreshLayout();
                Check(tutorial.Step == ControlTutorial.TutorialStep.Food && tutorial.TargetFood.Owner.GetTopFood() == tutorial.TargetFood,
                    "Level " + (index + 1) + " lost its head target.");
                CheckPointer(tutorial.handFood, tutorial.TargetFood.transform.position);
            }
            CheckNoBlockingUI(tutorial.TargetFood.transform.position);
            tutorial.CancelTutorial();
        }
    }

    private static IEnumerator CheckIntroductions(LevelManager level)
    {
        ControlTutorial tutorial = ControlTutorial.Ins;
        GameDataFragment data = GameDataFragment.cur;
        FoodInputController input = UnityEngine.Object.FindFirstObjectByType<FoodInputController>();
        data.dataPlayer.cur_level = 0; // Inspector preview of level 4 must also unlock the Click lesson.
        data.dataPlayer.isShowUnlockClick = false;
        data.dataPlayer.showTutorialBooster1 = false;
        data.dataPlayer.fisrtUseBoosterClear = false;
        data.dataPlayer.boosterClick = 0;
        UIManager.Ins.CloseAll();
        level.LoadLevel(3, false);
        CheckMarkerSelection(level);
        level.ShowGameplayHud();
        CanvasGamePlay hud = UIManager.Ins.GetUI<CanvasGamePlay>();
        for (int i = 0; i < 2; i++) yield return null;
        Check(UIManager.Ins.IsOpened<CanvasUnlockBooster>() && !level.CanInteract, "Click unlock popup did not pause immediately.");
        float popupDeadline = Time.realtimeSinceStartup + 0.4f;
        while (Time.realtimeSinceStartup < popupDeadline) yield return null;
        Check(Vector3.Distance(UIManager.Ins.GetUI<CanvasUnlockBooster>().mainPanelRect.localScale, Vector3.one) < 0.01f,
            "Unlock popup still waits before appearing.");
        UIManager.Ins.GetUI<CanvasUnlockBooster>().btnOk();
        yield return null;
        Check(tutorial.Step == ControlTutorial.TutorialStep.Booster, "Click lesson did not point to its button.");
        hud.BoosterClick();
        for (int i = 0; i < 12; i++) yield return null;
        Check(tutorial.Step == ControlTutorial.TutorialStep.Food && tutorial.Lesson == ControlTutorial.TutorialLesson.ClickBooster &&
            input.PickMode == FoodPickMode.Chopsticks, "Click button did not preserve booster mode and show ingredient pointer.");
        Check(tutorial.TargetFood.TutorialType == TypeTutFood.ClickTut && tutorial.TargetFood.Owner.IndexOf(tutorial.TargetFood) == 1,
            "Click lesson did not select the authored marker in the second row.");
        Check(!data.IsBoosterTutorialDone(BoosterType.Click), "Click lesson finished before actual pickup.");
        CheckPointer(tutorial.handFood, tutorial.TargetFood.transform.position);
        CheckNoBlockingUI(tutorial.TargetFood.transform.position);
        Capture("ControlTutorialClickBooster", level);
        Check(!input.TryUseChopsticks(tutorial.TargetFood), "Unconfirmed click booster bypassed tutorial.");
        tutorial.CancelTutorial();
        Check(level.CanInteract && input.PickMode == FoodPickMode.Tap && !data.IsBoosterTutorialDone(BoosterType.Click),
            "Cancelling click lesson retained input state or marked completion.");
        hud.CheckShowUnlockBooster();
        hud.BoosterClick();
        yield return null;
        FoodQueue queue = tutorial.TargetFood.Owner;
        int count = queue.Count;
        Press(tutorial, new Vector3(4, 0, 0));
        Check(tutorial.Step == ControlTutorial.TutorialStep.Food, "Wrong booster tap advanced tutorial.");
        Press(tutorial, tutorial.TargetFood.transform.position);
        Check(queue.Count == count - 1 && data.IsBoosterTutorialDone(BoosterType.Click) && data.dataPlayer.fisrtUseBoosterClear &&
            data.dataPlayer.boosterClick == 0 && level.CanInteract && input.PickMode == FoodPickMode.Tap,
            "Guided click pickup failed or charged the free first use.");
        CheckHidden(tutorial);

        IEnumerator magnet = CheckMagnet(level, hud, input);
        while (magnet.MoveNext()) yield return magnet.Current;

        GameManager.ins.LevelUnlockItemHoiCham = 5;
        GameManager.ins.LevelUnlockItemBang = 11;
        GameManager.ins.LevelUnlockItemLinked = 21;
        GameManager.ins.LevelUnlockItemTray = 31;
        FoodState[] items = { FoodState.HoiCham, FoodState.Ice, FoodState.Lock, FoodState.RigidTray };
        data.dataPlayer.completedItemTutorials = null; // Older saves need no migration to use the new completion flags.
        foreach (FoodState item in items)
        {
            int index = GameManager.ins.GetItemUnlockLevel(item) - 1;
            data.dataPlayer.cur_level = index;
            data.dataPlayer.isShowTrapHoiCham = false;
            data.dataPlayer.isShowTrapIce = false;
            data.dataPlayer.isShowTrapLinked = false;
            data.dataPlayer.isShowTrapTray = false;
            UIManager.Ins.CloseAll();
            level.LoadLevel(index, false);
            level.ShowGameplayHud();
            for (int i = 0; i < 2; i++) yield return null;
            Check(UIManager.Ins.IsOpened<CanvasNewItem>(), item + " did not show its introduction popup.");
            UIManager.Ins.GetUI<CanvasNewItem>().Close();
            for (int i = 0; i < 12; i++) yield return null;
            Check(tutorial.Step == ControlTutorial.TutorialStep.Food && tutorial.Lesson == ControlTutorial.TutorialLesson.Item &&
                tutorial.TargetFood.TutorialType == TypeTutFood.ClickTut && tutorial.TargetFood.Owner.GetTopFood() == tutorial.TargetFood &&
                level.Board.CanTap(tutorial.TargetFood.Owner, level.Conveyor.FreeSlotCount), item + " did not select the marked head ingredient.");
            CheckPointer(tutorial.handFood, tutorial.TargetFood.transform.position);
            CheckNoBlockingUI(tutorial.TargetFood.transform.position);
            if (item == FoodState.HoiCham)
            {
                tutorial.CancelTutorial();
                Check(!data.IsItemTutorialDone(item), "Cancellation marked an item lesson done.");
                hud.OpenUINewItem();
                yield return null;
                Check(tutorial.Step == ControlTutorial.TutorialStep.Food, "Item lesson cannot resume after its popup was already shown.");
                Capture("ControlTutorialHiddenItem", level);
            }
            Check(!data.IsItemTutorialDone(item), "Item lesson completed before pickup.");
            Press(tutorial, new Vector3(4, 0, 0));
            Check(ControlTutorial.IsInputLocked, "Wrong item tap advanced tutorial.");
            if (item == FoodState.Ice) Capture("ControlTutorialIceItem", level);
            Press(tutorial, tutorial.TargetFood.transform.position);
            Check(data.IsItemTutorialDone(item) && !ControlTutorial.IsInputLocked && level.CanInteract,
                item + " did not finish on the valid pickup.");
            CheckHidden(tutorial);
        }
        data.Load();
        foreach (FoodState item in items) Check(data.IsItemTutorialDone(item), item + " completion was not persisted.");
        Check(data.IsBoosterTutorialDone(BoosterType.Click), "Click completion was not persisted.");
        Check(data.IsBoosterTutorialDone(BoosterType.Magnet), "Magnet completion was not persisted.");
        // Closing an old popup after a level change must never start its callback in the new level.
        UIManager.Ins.CloseAll();
        data.dataPlayer.cur_level = 4;
        data.dataPlayer.isShowTrapHoiCham = false;
        level.LoadLevel(4);
        level.LoadLevel(1);
        UIManager.Ins.GetUI<CanvasNewItem>().Close();
        yield return null;
        Check(!ControlTutorial.IsInputLocked && level.CanInteract, "Stale item popup started a lesson in another level.");
    }

    private static IEnumerator CheckMagnet(LevelManager level, CanvasGamePlay hud, FoodInputController input)
    {
        var data = GameDataFragment.cur;
        var tutorial = ControlTutorial.Ins;
        string originalDescription = tutorial.objDesOrder.GetComponentInChildren<TMP_Text>(true).text;
        data.dataPlayer.cur_level = GameDataFragment.BoosterMagnetUnlockLevel - 1;
        data.dataPlayer.isShowUnlockMagnet = false;
        data.dataPlayer.showTutorialBooster3 = false;
        data.dataPlayer.fisrtUseBoosterMagnet = false;
        data.dataPlayer.boosterHut = 0;
        UIManager.Ins.CloseAll();
        level.LoadLevel(data.dataPlayer.cur_level, false);
        level.ShowGameplayHud();
        for (int i = 0; i < 2; i++) yield return null;
        Check(UIManager.Ins.IsOpened<CanvasUnlockBooster>(), "Magnet unlock popup is missing.");
        UIManager.Ins.GetUI<CanvasUnlockBooster>().btnOk();
        yield return null;
        Check(tutorial.Step == ControlTutorial.TutorialStep.Booster &&
            tutorial.Lesson == ControlTutorial.TutorialLesson.MagnetBooster && hud.handBsMagnet.activeSelf &&
            level.IsTutorialHeld, "Magnet lesson did not hold gameplay and point to its button.");
        hud.BoosterClick();
        hud.BoosterSuffle();
        Check(tutorial.Step == ControlTutorial.TutorialStep.Booster && input.PickMode == FoodPickMode.Tap,
            "A different booster advanced the Magnet lesson.");
        tutorial.CancelTutorial();
        Check(!hud.handBsMagnet.activeSelf && !hud.spotlight.dimOverlay.activeSelf && level.CanInteract,
            "Cancelling Magnet at its button retained the spotlight or gameplay hold.");
        data.dataPlayer.showTutorialBooster3 = true; // Legacy saves ended the lesson at the booster button.
        hud.CheckShowUnlockBooster();
        hud.BoosterHut();
        for (int i = 0; i < 12; i++) yield return null;
        Check(tutorial.Step == ControlTutorial.TutorialStep.Order && input.PickMode == FoodPickMode.Magnet &&
            tutorial.handOrder.activeSelf && tutorial.objDesOrder.activeSelf && !hud.handBsMagnet.activeSelf &&
            !hud.spotlight.dimOverlay.activeSelf, "Magnet button did not show the dish hand and description.");
        Check(tutorial.objDesOrder.GetComponentInChildren<TMP_Text>(true).text.Contains("hút đủ nguyên liệu"),
            "Magnet dish description is missing.");
        Check(!data.IsBoosterTutorialDone(BoosterType.Magnet) && !data.dataPlayer.fisrtUseBoosterMagnet,
            "Selecting Magnet completed the lesson or consumed its first use.");
        CheckPointer(tutorial.handOrder, tutorial.TargetOrder.DishImage.transform.position);
        CheckNoBlockingUI(tutorial.TargetOrder.DishImage.transform.position);
        Check(!input.TryUseMagnet(tutorial.TargetCustomer, tutorial.TargetDish) &&
            !level.Conveyor.TryTakeMagnet(level.Board, tutorial.TargetCustomer, tutorial.TargetDish),
            "Unconfirmed Magnet use bypassed the tutorial.");
        Press(tutorial, level.Board.FoodQueues[0].GetTopFood().transform.position);
        Check(tutorial.Step == ControlTutorial.TutorialStep.Order && !data.IsBoosterTutorialDone(BoosterType.Magnet),
            "Tapping an ingredient completed the Magnet lesson.");
        Capture("ControlTutorialMagnet", level);
        tutorial.CancelTutorial();
        Check(level.CanInteract && input.PickMode == FoodPickMode.Tap && !data.IsBoosterTutorialDone(BoosterType.Magnet) &&
            tutorial.objDesOrder.GetComponentInChildren<TMP_Text>(true).text == originalDescription,
            "Cancelling Magnet retained booster mode, changed the default text or saved completion.");
        CheckHidden(tutorial);
        hud.CheckShowUnlockBooster();
        hud.BoosterHut();
        yield return null;
        SushiDishOrder selected = tutorial.TargetDish;
        Press(tutorial, tutorial.TargetOrder.DishImage.transform.position);
        Check(data.IsBoosterTutorialDone(BoosterType.Magnet) && data.dataPlayer.fisrtUseBoosterMagnet &&
            data.dataPlayer.boosterHut == 0 && level.CanInteract && input.PickMode == FoodPickMode.Tap,
            "Guided Magnet failed or charged its free first use.");
        CheckHidden(tutorial);
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!selected.IsComplete && Time.realtimeSinceStartup < deadline) yield return null;
        Check(selected.IsComplete, "Guided Magnet did not complete the selected dish.");
        level.LoadLevel(GameDataFragment.BoosterMagnetUnlockLevel - 1);
        yield return null;
        Check(!ControlTutorial.IsInputLocked, "Completed Magnet lesson replayed.");
        data.dataPlayer.showTutorialBooster3 = false;
        data.dataPlayer.boosterHut = 1;
        hud.CheckShowUnlockBooster();
        hud.BoosterHut();
        Check(ControlTutorial.KeepsBoosterSelected, "Magnet lesson did not restart for the reload check.");
        level.LoadLevel(1);
        Check(!ControlTutorial.IsInputLocked && input.PickMode == FoodPickMode.Tap && level.CanInteract &&
            !data.IsBoosterTutorialDone(BoosterType.Magnet), "Level change retained or completed the Magnet lesson.");
        CheckHidden(tutorial);
        data.Load(); // Restore the successful, persisted completion.
    }

    private static IEnumerator CheckRushIntroduction(LevelManager level)
    {
        var data = GameDataFragment.cur;
        var tutorial = ControlTutorial.Ins;
        CanvasGamePlay hud = UIManager.Ins.GetUI<CanvasGamePlay>();
        Check(GameManager.ins.GetItemUnlockLevel(FoodState.Time) == 10, "The rush example should be configured at level 10.");
        data.dataPlayer.isShowTrapTime = false;
        data.dataPlayer.completedItemTutorials.Remove(FoodState.Time);
        data.dataPlayer.cur_level = 9;
        UIManager.Ins.CloseAll();
        level.LoadLevel(9, false);
        foreach (FoodQueue queue in level.Board.FoodQueues)
            foreach (FoodShushi food in queue.Foods)
                typeof(FoodShushi).GetProperty("TutorialType").SetValue(food, TypeTutFood.Normal);
        level.ShowGameplayHud();
        for (int i = 0; i < 3; i++) yield return null;
        Check(UIManager.Ins.IsOpened<CanvasNewItem>(), "Level 10 did not open the Rush introduction.");
        var popup = UIManager.Ins.GetUI<CanvasNewItem>();
        Check(popup.imgItemTime != null && popup.imgItemTime.activeSelf && !popup.imgItemHoiCham.activeSelf &&
            !popup.imgItemHoiBang.activeSelf && !popup.imgItemLink.activeSelf && !popup.imgItemKhay.activeSelf,
            "Rush artwork is missing or overlaps another item.");
        Customer rush = level.Conveyor.Customers.ActiveCustomers[0];
        Check(rush.IsRush && rush.IsSeated && Mathf.Approximately(rush.RushDuration, 85),
            "Level 10 needs its first customer rushing with an 85-second limit.");
        float remaining = rush.RushRemaining;
        for (int i = 0; i < 12; i++) yield return null;
        Check(Mathf.Approximately(remaining, rush.RushRemaining) && !level.CanInteract, "Rush timer ran during its popup.");
        popup.Close();
        for (int i = 0; i < 12; i++) yield return null;
        Check(tutorial.Lesson == ControlTutorial.TutorialLesson.RushCustomer && tutorial.Step == ControlTutorial.TutorialStep.Order &&
            tutorial.TargetCustomer == rush && tutorial.TargetOrder != null && tutorial.TargetFood == null &&
            level.IsTutorialHeld && !level.CanInteract && Mathf.Approximately(remaining, rush.RushRemaining),
            "Rush tutorial did not hold the timer and target the rush customer's order without food markers.");
        Check(tutorial.handOrder.activeSelf && tutorial.objDesOrder.activeSelf && !tutorial.handFood.activeSelf &&
            tutorial.objDesOrder.GetComponentInChildren<TMP_Text>(true).text.Contains("Ưu tiên"), "Rush explanation or hand is missing.");
        CheckPointer(tutorial.handOrder, tutorial.TargetOrder.DishImage.transform.position);
        CheckNoBlockingUI(tutorial.TargetOrder.DishImage.transform.position);
        Press(tutorial, level.Board.FoodQueues[0].GetTopFood().transform.position);
        Press(tutorial, level.Conveyor.Customers.ActiveCustomers[1].GetComponent<CustomerRecipeView>().CardAt(0).DishImage.transform.position);
        Check(ControlTutorial.IsInputLocked && !data.IsItemTutorialDone(FoodState.Time), "A wrong customer or ingredient dismissed the Rush lesson.");
        Capture("ControlTutorialRushCustomer", level);
        tutorial.CancelTutorial();
        Check(!data.IsItemTutorialDone(FoodState.Time) && !level.IsTutorialHeld, "Cancelling Rush completed its lesson or kept gameplay held.");
        hud.OpenUINewItem();
        for (int i = 0; i < 2; i++) yield return null;
        Check(tutorial.Step == ControlTutorial.TutorialStep.Order, "Rush lesson cannot resume after its popup was shown.");
        SushiDishCardView order = tutorial.TargetOrder;
        Press(tutorial, order.DishImage.transform.position);
        Check(order.IsRecipeExpanded && data.IsItemTutorialDone(FoodState.Time) && !ControlTutorial.IsInputLocked && level.CanInteract,
            "Rush order tap did not reveal ingredients, save completion and resume play.");
        CheckHidden(tutorial);
        remaining = rush.RushRemaining;
        for (int i = 0; i < 3; i++) yield return null;
        Check(rush.RushRemaining < remaining, "Rush timer did not resume after the explanation.");
        data.Load();
        Check(data.IsItemPopupShown(FoodState.Time) && data.IsItemTutorialDone(FoodState.Time), "Rush introduction was not persisted.");
        level.LoadLevel(9);
        for (int i = 0; i < 3; i++) yield return null;
        Check(!ControlTutorial.IsInputLocked && !UIManager.Ins.IsOpened<CanvasNewItem>(), "Completed Rush introduction replayed.");

        data.dataPlayer.isShowTrapTime = false;
        data.dataPlayer.completedItemTutorials.Remove(FoodState.Time);
        UIManager.Ins.CloseAll();
        level.LoadLevel(9, false);
        level.ShowGameplayHud();
        for (int i = 0; i < 3; i++) yield return null;
        Check(UIManager.Ins.IsOpened<CanvasNewItem>(), "Rush popup retry is missing.");
        level.LoadLevel(1, false);
        popup.Close();
        yield return null;
        Check(!ControlTutorial.IsInputLocked && !level.IsTutorialHeld && !data.IsItemTutorialDone(FoodState.Time),
            "Closing an old Rush popup started or completed a lesson in another level.");
        Check(!tutorial.BeginItem(level, FoodState.Time), "A normal customer was selected as a Rush target.");

        // The public popup API must work without a HUD-supplied callback too.
        UIManager.Ins.CloseAll();
        level.LoadLevel(9, false);
        UIManager.Ins.OpenUI<CanvasNewItem>().Setupp(FoodState.Time);
        popup.Close();
        for (int i = 0; i < 3; i++) yield return null;
        Check(tutorial.Step == ControlTutorial.TutorialStep.Order && tutorial.TargetCustomer.IsRush,
            "Directly opening the Time popup did not start its lesson.");
        Press(tutorial, tutorial.TargetOrder.DishImage.transform.position);
        Check(data.IsItemTutorialDone(FoodState.Time) && level.CanInteract, "Direct Rush lesson did not finish.");
    }

    private static void CheckCampaignTargets()
    {
        string[] assets = AssetDatabase.FindAssets("t:SushiLevelData", new[] { "Assets/_game/LevelData/Campaign" });
        Check(assets.Length > 0, "Campaign levels are missing.");
        SessionState.SetInt(Key + ".CampaignLevels", assets.Length);
        foreach (string guid in assets)
        {
            var data = AssetDatabase.LoadAssetAtPath<SushiLevelData>(AssetDatabase.GUIDToAssetPath(guid));
            var board = data.CreateBoardData();
            int markers = 0;
            foreach (FoodQueueData queue in board)
                for (int row = 0; row < queue.foods.Count; row++)
                {
                    FoodData food = queue.foods[row];
                    if (food.tutFood != TypeTutFood.ClickTut) continue;
                    markers++;
                    Check(row == (data.number == 4 ? 1 : 0) && food.foodState == FoodState.Normal && !food.hidden &&
                        food.iceLayers == 0 && food.linkId == 0 && food.lockId == 0 && food.trayId == 0 && food.amount <= data.beltSlots,
                        data.name + " has an inaccessible or deep tutorial marker.");
                }
            Check(markers == 1, data.name + " needs exactly one tutorial marker.");
        }
    }

    private static void CheckMarkerSelection(LevelManager level)
    {
        var tutorial = ControlTutorial.Ins;
        var property = typeof(FoodShushi).GetProperty("TutorialType");
        var original = new Dictionary<FoodShushi, TypeTutFood>();
        FoodShushi deep = null;
        foreach (FoodQueue queue in level.Board.FoodQueues)
            for (int row = 0; row < queue.Count; row++)
            {
                FoodShushi food = queue.Foods[row];
                original.Add(food, food.TutorialType);
                property.SetValue(food, TypeTutFood.Normal);
                if (row == 2 && !food.IsHidden && food.CanUseChopsticks) deep = food;
            }
        try
        {
            Check(!tutorial.BeginClickBooster(level, null) && !tutorial.BeginItem(level, FoodState.HoiCham) &&
                !level.IsTutorialHeld && !ControlTutorial.IsInputLocked, "Missing markers selected an unmarked tile or held gameplay.");
            CanvasGamePlay hud = UIManager.Ins.GetUI<CanvasGamePlay>();
            hud.StartBoosterTutorial(BoosterType.Click);
            Check(!hud.handBsClick.activeSelf && !hud.spotlight.dimOverlay.activeSelf &&
                !GameDataFragment.cur.IsBoosterTutorialDone(BoosterType.Click),
                "A missing marker started the old button-only lesson or marked it complete.");
            Check(deep != null, "Missing deep tile for marker regression.");
            property.SetValue(deep, TypeTutFood.ClickTut);
            Check(!tutorial.BeginClickBooster(level, null) && !tutorial.BeginItem(level, FoodState.HoiCham),
                "A deep marker was selected for the tutorial.");
            var head = level.Board.FoodQueues[1].GetTopFood();
            property.SetValue(head, TypeTutFood.ClickTut);
            Check(tutorial.BeginItem(level, FoodState.HoiCham) && tutorial.TargetFood == head,
                "An explicitly marked head did not override other ingredients.");
            tutorial.CancelTutorial();
            Check(tutorial.BeginClickBooster(level, null) && tutorial.TargetFood == head,
                "Click selected a deep tile instead of the shallow marker.");
        }
        finally
        {
            tutorial.CancelTutorial();
            foreach (var entry in original) property.SetValue(entry.Key, entry.Value);
        }
    }

    private static void Capture(string label, LevelManager level)
    {
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) SushiVisualCapture.CaptureSet(label, level);
    }

    private static void Press(ControlTutorial tutorial, Vector3 world)
    {
        tutorial.TryHandlePress(Camera.main.WorldToScreenPoint(world), Camera.main);
    }

    private static void CheckPointer(GameObject hand, Vector3 target)
    {
        Bounds bounds = hand.GetComponent<SpriteRenderer>().sprite.bounds;
        Vector3 tip = bounds.min + Vector3.Scale(bounds.size, new Vector3(0.17f, 0.95f, 0));
        tip.z = 0;
        Check(Vector2.Distance(hand.transform.TransformPoint(tip), target) < 0.2f, "Fingertip misses its world target.");
    }

    private static void CheckNoBlockingUI(Vector3 world)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(world) }, hits);
        Check(hits.Count == 0, "UI blocks the tutorial target.");
    }

    private static void CheckHidden(ControlTutorial tutorial)
    {
        Check(!tutorial.img_lop_phu_duoi.gameObject.activeSelf && !tutorial.handOrder.activeSelf && !tutorial.handFood.activeSelf &&
            !tutorial.objDesOrder.activeSelf && !tutorial.objDesFood.activeSelf, "Tutorial visuals remain visible.");
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
