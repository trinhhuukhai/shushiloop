using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SushiIngameSceneChecks
{
    private const string Key = "SushiIngameSceneChecks.Pending";
    private static IEnumerator runner;
    private static string runtimeError;
    static SushiIngameSceneChecks()
    {
        EditorApplication.update += Update;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception) &&
                (stack == null || !stack.Contains("UnityEditor.Search"))) runtimeError = message + "\n" + stack;
        };
    }
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    public static void RunBatch()
    {
        Check(Application.isBatchMode && PlayerSettings.productName == "Sushi UI Verification", "Use the isolated verification project.");
        var data = AssetDatabase.LoadAssetAtPath<GameDataFragment>("Assets/_game/DataSO/GameDataFragment.asset");
        data.dataPlayer = new GameDataFragment.DataPlayer { cur_level = 0, showTutorialLevel1 = true, money = 1000 };
        data.Save(); Directory.CreateDirectory("Documentation");
        EditorSceneManager.OpenScene("Assets/_game/Scenes/Loading.unity");
        SessionState.SetFloat(Key + ".Started", (float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    private static void Update()
    {
        if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
        try
        {
            Check(runtimeError == null, runtimeError);
            Check(EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Started", 0) < 180, "Ingame scene test timed out.");
            if (!Application.isPlaying || LevelManager.Ins == null || GameManager.ins == null ||
                (runner == null && (GameManager.ins.IsLoadingLevel || !LevelManager.Ins.IsPlaying))) return;
            if (runner == null) runner = Run();
            if (runner.MoveNext()) return;
            SessionState.SetBool(Key, false);
            File.WriteAllText("Documentation/IngameSceneValidation.txt", "PASS: saved ingame sprites, scene HUD identity, recipe details, all visible ingredient input raycasts, Settings replay, Home/gameplay roundtrip, no duplicated scenery/HUD.\nRendered 720x1280, 1080x2400 and 1536x2048.\n");
            Debug.Log("INGAME_SCENE_CHECKS_OK"); EditorApplication.Exit(0);
        }
        catch (Exception error) { SessionState.SetBool(Key, false); Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static IEnumerator Run()
    {
        var level = LevelManager.Ins; level.LoadLevel(19);
        for (int i = 0; i < 10; i++) yield return null;
        var art = GameObject.Find("Ingame art"); Check(art != null, "Scene is missing authored art.");
        int artCount = art.GetComponentsInChildren<SpriteRenderer>(true).Length;
        Check(artCount >= 5 && art.GetComponentsInChildren<SpriteRenderer>().All(r =>
            AssetDatabase.GetAssetPath(r.sprite).StartsWith("Assets/_game/Art/ingame/")), "Scenery still uses generated/old sprites.");
        Check(Object.FindFirstObjectByType<SushiRestaurantView>() == null && GameObject.Find("Conveyor world sprites") == null,
            "Scenery generator is still active.");
        CanvasGamePlay hud = UIManager.Ins.GetUI<CanvasGamePlay>(); CanvasFloating floating = UIManager.Ins.GetUI<CanvasFloating>();
        Check(hud.transform.parent.name == "Ingame UI" && floating.transform.parent == hud.transform.parent,
            "UIManager did not reuse the saved scene HUDs.");
        Check(Object.FindObjectsByType<CanvasGamePlay>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1 &&
            Object.FindObjectsByType<CanvasFloating>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "Duplicate HUD instances.");
        var hits = new System.Collections.Generic.List<RaycastResult>();
        foreach (var column in level.Board.FoodQueues)
            foreach (var food in column.Foods.Take(4))
            {
                hits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(food.transform.position) }, hits);
                Check(hits.Count == 0, "UI blocks a visible ingredient: " + food.Type);
            }
        SushiVisualCapture.CaptureSet("IngameAuthored", level);
        var customer = level.Conveyor.Customers.ActiveCustomers[0]; var recipe = customer.GetComponent<CustomerRecipeView>();
        var card = recipe.CardAt(0);
        Check(recipe.ToggleRecipeDetailsAtWorldPosition(card.transform.position) && card.IsRecipeExpanded, "Recipe details no longer open.");
        yield return null;
        SushiVisualCapture.CaptureSet("IngameRecipe", level);
        Check(recipe.ToggleRecipeDetailsAtWorldPosition(card.transform.position) && !card.IsRecipeExpanded, "Recipe details no longer close.");
        var replay = SushiProjectUIRegression.CheckSettingsReplay(level);
        while (replay.MoveNext()) yield return replay.Current;
        Check(UIManager.Ins.GetUI<CanvasGamePlay>() == hud && UIManager.Ins.GetUI<CanvasFloating>() == floating &&
            art.GetComponentsInChildren<SpriteRenderer>(true).Length == artCount, "Replay recreated HUD or scenery.");
        GameDataFragment.cur.dataPlayer.cur_level = 19;
        GameManager.ins.ReturnToMainMenu();
        Check(UIManager.Ins.IsOpened<CanvasMainMenu>() && !hud.gameObject.activeInHierarchy, "Home did not hide the scene HUD.");
        Check(GameManager.ins.StartCurrentLevel(), "Home could not restart gameplay with scene HUDs.");
        while (GameManager.ins.IsLoadingLevel) yield return null;
        Check(level.CanInteract && UIManager.Ins.GetUI<CanvasGamePlay>() == hud && UIManager.Ins.GetUI<CanvasFloating>() == floating &&
            hud.gameObject.activeInHierarchy && floating.gameObject.activeInHierarchy, "Home/gameplay navigation lost or duplicated the scene HUDs.");
    }
}
