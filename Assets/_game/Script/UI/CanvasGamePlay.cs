using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Layout, artwork and button callbacks are authored in UICGamePlay.prefab.
public class CanvasGamePlay : UICanvas
{
    protected override bool BlocksGameplay => false;
    public RectTransform rootPanel;
    public RectTransform TopGroup;
    public TMP_Text txtLevvel;
    public TMP_Text txtGold;
    [SerializeField] private TMP_Text occupancy;
    [SerializeField] private TMP_Text boosterHint;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text[] boosterCounts;
    [SerializeField] private Button[] boosterButtons;
    [SerializeField] private GameObject[] selectedBoosters;
    [SerializeField] private Button cancelBooster;
    [SerializeField] private GameObject levelSelector;
    [SerializeField] private Button previousLevel;
    [SerializeField] private Button nextLevel;
    [SerializeField] private TMP_Text customerProgress;
    [SerializeField] private Image customerProgressFill;
    [SerializeField] private TMP_Text difficulty;
    private FoodInputController input;

    private void Awake()
    {
        if (rootPanel != null) rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
        //if (occupancy != null) occupancy.gameObject.SetActive(false);

    }

    public override void Open()
    {
        base.Open();
        input = FindFirstObjectByType<FoodInputController>();
        Refresh();
    }

    private void Update() => Refresh();

    public void Refresh()
    {
        LevelManager level = LevelManager.Ins;
        if (level == null || level.CurrentLevel == null) return;
        txtLevvel.text = "Level " + (level.CurrentLevelIndex + 1);
        if (customerProgress != null)
        {
            var customers = level.Conveyor.Customers;
            customerProgress.text = customers.CompletedCustomers + "/" + customers.TotalCustomers;
            customerProgressFill.fillAmount = customers.TotalCustomers > 0 ? (float)customers.CompletedCustomers / customers.TotalCustomers : 0;
        }
        if (difficulty != null)
        {
            difficulty.gameObject.SetActive(level.CurrentLevel.TypeLevel != TypeLevel.Normal);
            difficulty.text = level.CurrentLevel.TypeLevel == TypeLevel.SuperHard ? "SUPER HARD" : "HARD";
        }
        occupancy.text = level.Conveyor.OccupiedSlotCount + "/" + level.Conveyor.Capacity;
        speedText.text = level.Conveyor.IsDoubleSpeed ? "2×" : "1×";
        setTxtGold();
        FoodPickMode mode = input != null ? input.PickMode : FoodPickMode.Tap;
        boosterHint.text = mode == FoodPickMode.Chopsticks ? "Chọn nguyên liệu đang thấy · không gắp ô băng" :
            mode == FoodPickMode.Magnet ? "Chọn một món trong order để hút đủ nguyên liệu" : "";
        cancelBooster.gameObject.SetActive(mode != FoodPickMode.Tap);
        levelSelector.SetActive(level.ShowLevelSelector);
        previousLevel.interactable = level.HasPreviousLevel;
        nextLevel.interactable = level.HasNextLevel;
        for (int i = 0; i < boosterButtons.Length; i++)
        {
            int count = FoodInputController.BoosterCount((BoosterType)i);
            boosterCounts[i].text = count < 0 ? "∞" : count.ToString();
            boosterButtons[i].interactable = level.CanInteract && count != 0;
            selectedBoosters[i].SetActive(i == 0 && mode == FoodPickMode.Chopsticks || i == 2 && mode == FoodPickMode.Magnet);
        }
    }

    public void ButtonBoosterPick() { input?.SelectChopsticks(); Refresh(); }
    public void ButtonBoosterHoanDoi() { input?.UseSwapRow(); Refresh(); }
    public void ButtonBoosterHut() { input?.SelectMagnet(); Refresh(); }
    public void BoosterClick() => ButtonBoosterPick();
    public void BoosterSuffle() => ButtonBoosterHoanDoi();
    public void BoosterHut() => ButtonBoosterHut();
    public void CancelBooster() { input?.CancelBooster(); Refresh(); }
    public void OnClickChangeSpeed() { if (LevelManager.Ins == null || !LevelManager.Ins.CanInteract || SushiLevelOneTutorial.IsInputLocked) return; LevelManager.Ins.Conveyor.ToggleSpeed(); Refresh(); }
    public void ChangeSpeed(bool value)
    {
        if (LevelManager.Ins != null && LevelManager.Ins.Conveyor.IsDoubleSpeed != value) OnClickChangeSpeed();
    }
    public void PreviousLevel() => LevelManager.Ins?.PreviousLevel();
    public void NextLevel() => LevelManager.Ins?.NextLevel();
    public void ReplayLevel() => LevelManager.Ins?.ReplayLevel();
    public void btnSettingIngame() { if (!SushiLevelOneTutorial.IsInputLocked) UIManager.Ins.OpenUI<CanvasSetting>(); }
    public void showTxtLevel() => Refresh();
    public void setTxtGold()
    {
        if (txtGold != null) txtGold.text = GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null ?
            GameDataFragment.cur.dataPlayer.money.ToString("N0") : "0";
    }
    public void RefeshCoountAllBooster() => Refresh();
    public void UpdateBoosterClick() => Refresh();
    public void UpdateBoosterSuffle() => Refresh();
    public void UpdateBoosterHut() => Refresh();
    public void ResfeshX2() => Refresh();
    public void Close() => CloseDirectly();
}
