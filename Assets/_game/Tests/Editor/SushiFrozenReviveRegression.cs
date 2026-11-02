#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class SushiGameplayRegression
{
    public static void RunFrozenReviveBatch()
    {
        SessionState.SetBool(PendingKey + ".FrozenRevive", true);
        RunBatch();
    }

    private static void RunFrozenReviveChecks()
    {
        try
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.defaultUpdateType = UpdateType.Manual;
            CreateFixture();
            CreateLevel();
            FrozenRevivePopup();
            Debug.Log("SUSHI_FROZEN_REVIVE_OK: " + passed + " scenarios passed");
            System.IO.File.WriteAllText("SushiFrozenReviveResults.txt", "PASS: " + passed + " scenarios\n");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            System.IO.File.WriteAllText("SushiFrozenReviveResults.txt", "FAIL after " + passed + " scenarios\n" + error);
            EditorApplication.Exit(1);
        }
    }

    private static void FrozenRevivePopup()
    {
        GameDataFragment previousProfile = GameDataFragment.cur;
        SoundManager previousSound = SoundManager.ins;
        var profile = ScriptableObject.CreateInstance<SushiRegressionGoldProfile>();
        profile.dataPlayer = new GameDataFragment.DataPlayer();
        GameDataFragment.cur = profile;
        var soundObject = new GameObject("Frozen revive test sound");
        soundObject.SetActive(false);
        SoundManager.ins = soundObject.AddComponent<SoundManager>();
        Set(SoundManager.ins, "soundMap", new Dictionary<SoundID, SoundEntry>());
        var uiObject = new GameObject("Frozen revive test UI");
        UIManager ui = uiObject.AddComponent<UIManager>();
        ui.CanvasParentTF = ui.transform;
        ui.uiResources = new UICanvas[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/UICRevive.prefab").GetComponent<CanvasRevive>(),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Resources/UI/UICGamePlay.prefab").GetComponent<CanvasGamePlay>()
        };
        try
        {
            CanvasRevive popup = ui.GetUI<CanvasRevive>();
            CanvasGamePlay hud = ui.GetUI<CanvasGamePlay>();
            Button gold = popup.loseStuckIce.transform.Find("btn/btnGold").GetComponent<Button>();
            Button ads = popup.loseStuckIce.transform.Find("btn/btnAds").GetComponent<Button>();
            Run("frozen popup shows the authored panel, banner and matching gold/ads callbacks", () =>
            {
                PrepareFrozenRevive(ui, profile, 1500);
                Check(popup.loseStuckIce.activeSelf && !popup.loseTimeup.activeSelf && !popup.loseOutSpace.activeSelf &&
                    hud.txtStuck.text == "Stuck!" && !hud.imgStuck.activeSelf && level.IsPopupOpen,
                    "frozen popup, intro banner or gameplay pause is incorrect");
                Check(popup.priceTxtStuckIce.text == "1000" && popup.priceTxtStuckIce.color == Color.white &&
                    gold.onClick.GetPersistentEventCount() == 1 && gold.onClick.GetPersistentTarget(0) == popup &&
                    gold.onClick.GetPersistentMethodName(0) == nameof(CanvasRevive.BtnRevive) &&
                    ads.onClick.GetPersistentEventCount() == 1 && ads.onClick.GetPersistentTarget(0) == popup &&
                    ads.onClick.GetPersistentMethodName(0) == nameof(CanvasRevive.ReviveWithAd),
                    "frozen price or button bindings do not match the shared revive flow");
            });
            Run("insufficient gold keeps all ice and the popup without charging", () =>
            {
                PrepareFrozenRevive(ui, profile, 999);
                gold.onClick.Invoke();
                Check(profile.dataPlayer.money == 999 && profile.saves == 0 && popup.gameObject.activeSelf &&
                    popup.priceTxtStuckIce.color == Color.red && level.PendingRevive == SushiReviveReason.FrozenHeads &&
                    Column(0).Foods[0].IceLayers == 5 && Column(0).Foods[4].IceLayers == 3,
                    "insufficient gold changed the board or closed the offer");
            });
            Run("gold clears every ice layer, resumes play and charges only once", () =>
            {
                PrepareFrozenRevive(ui, profile, 2000);
                gold.onClick.Invoke();
                CheckFrozenReviveComplete(popup);
                Check(profile.dataPlayer.money == 1000 && profile.saves == 1, "gold revive did not charge exactly 1000");
                gold.onClick.Invoke();
                Check(profile.dataPlayer.money == 1000 && profile.saves == 1, "repeat click charged a completed revive");
            });
            Run("the shared ads button clears all ice without spending gold", () =>
            {
                PrepareFrozenRevive(ui, profile, 0);
                ads.onClick.Invoke();
                CheckFrozenReviveComplete(popup);
                Check(profile.dataPlayer.money == 0 && profile.saves == 0, "ads revive spent gold");
            });
            Run("reusing the popup for time and space hides the frozen panel", () =>
            {
                foreach (SushiReviveReason reason in new[] { SushiReviveReason.RushCustomer, SushiReviveReason.FullConveyor })
                {
                    typeof(LevelManager).GetMethod("RequestRevive", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(level, new object[] { reason });
                    Invoke(popup, "Refresh");
                    Check(!popup.loseStuckIce.activeSelf && popup.loseTimeup.activeSelf == (reason == SushiReviveReason.RushCustomer) &&
                        popup.loseOutSpace.activeSelf == (reason == SushiReviveReason.FullConveyor), "revive reason panels overlap");
                }
            });
        }
        finally
        {
            ui.CloseAll();
            Object.DestroyImmediate(uiObject);
            Object.DestroyImmediate(soundObject);
            Object.DestroyImmediate(profile);
            SoundManager.ins = previousSound;
            GameDataFragment.cur = previousProfile;
        }
    }

    private static void PrepareFrozenRevive(UIManager ui, SushiRegressionGoldProfile profile, int money)
    {
        ui.CloseAll();
        level.LoadLevel(0);
        Load(8, new[] { Food(ice: 5), Food(ice: 4), Food(amount: 2, tray: 7), Food(PoolType.Egg, tray: 7),
            Food(ice: 3, hidden: true), Food(ice: 2) }, new[] { Food(ice: 1), Food(ice: 4) }, new FoodData[0]);
        profile.dataPlayer.money = money;
        profile.saves = 0;
        CheckLevelState(4);
        Check(level.PendingRevive == SushiReviveReason.None, "frozen offer skipped the grace period");
        CheckLevelState(1);
        Check(level.PendingRevive == SushiReviveReason.FrozenHeads, "frozen board did not offer revive");
        ui.OpenUI<CanvasRevive>();
        List<Tween> tweens = DOTween.PlayingTweens();
        if (tweens != null) foreach (Tween tween in tweens) tween.SetUpdate(UpdateType.Manual);
        Advance(1.4f);
        Advance(0.3f);
    }

    private static void CheckFrozenReviveComplete(CanvasRevive popup)
    {
        Check(level.PendingRevive == SushiReviveReason.None && level.CanInteract && !popup.gameObject.activeSelf &&
            board.HasAvailableTap(8), "revive did not close the popup and restore gameplay");
        Check(Column(0).Count == 6 && Column(1).Count == 2 && Column(2).Count == 0 &&
            Column(0).Foods[2].Amount == 2 && Column(0).Foods[2].TrayId == 7 && Column(0).Foods[3].TrayId == 7 &&
            Column(0).Foods[4].IsHidden, "revive changed ingredient quantities, trays or hidden tiles");
        foreach (FoodQueue column in board.FoodQueues)
            foreach (FoodShushi food in column.Foods)
                Check(food.IceLayers == 0, "revive left ice below the head or visible window");
    }
}
#endif
