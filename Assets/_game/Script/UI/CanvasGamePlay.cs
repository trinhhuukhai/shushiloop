using DG.Tweening;
using Spine.Unity;
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
    [SerializeField] private Image levelBadge;
    [SerializeField] private Sprite normalLevelBadge;
    [SerializeField] private Sprite hardLevelBadge;
    [SerializeField] private Sprite superHardLevelBadge;
    private FoodInputController input;

    public RectTransform BoosterGroup;
    private Vector2 boosterGroupStartPosition;
    private int observedLoadVersion = int.MinValue;
    private bool hasSeenBoardIngredients;
    private bool boosterGroupSlidDown;

    public CanvasGroup AnimLevelHardGroup;
    public SkeletonGraphic animHard;
    public SkeletonGraphic animSHard;

    public GameObject imgStuck;
    public TMP_Text txtStuck;

    // Có booster đang được dùng (cờ do FoodInputController bật/tắt trong BoosterManager).
    private static bool IsAnyBoosterInUse => BoosterManager.Ins != null && BoosterManager.Ins.IsUsingAny;

    private void Awake()
    {
        if (rootPanel != null) rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
        if (BoosterGroup != null)
            boosterGroupStartPosition = BoosterGroup.anchoredPosition;
        else
            Debug.LogError("CanvasGamePlay requires BoosterGroup to be assigned.", this);
    }

    public override void Open()
    {
        base.Open();
        imgStuck.SetActive(false);
        ShowAnim();
        input = FindFirstObjectByType<FoodInputController>();
        Refresh();
    }

    public void setImgStuck(bool check, SushiReviveReason reviveReason)
    {
        imgStuck.SetActive(check);
        if (reviveReason == SushiReviveReason.FullConveyor)
        {
            txtStuck.text = "No Space!";
        }
        else if (reviveReason == SushiReviveReason.RushCustomer)
        {
            txtStuck.text = "Time Out!";
        }
    }

    
    public void ShowAnim()
    {
        AnimLevelHardGroup.gameObject.SetActive(false);
        animHard.gameObject.SetActive(false);
        animSHard.gameObject.SetActive(false);
        AnimLevelHardGroup.alpha = 1.0f;
        if (LevelManager.Ins.CurrentLevel.TypeLevel == TypeLevel.Hard)
        {
            AnimLevelHardGroup.gameObject.SetActive(true);
            animHard.gameObject.SetActive(true);
            animHard.DOPlay();
        }
        else if (LevelManager.Ins.CurrentLevel.TypeLevel == TypeLevel.SuperHard)
        {
            AnimLevelHardGroup.gameObject.SetActive(true);
            animSHard.gameObject.SetActive(true);
            animSHard.DOPlay();
        }

        DOVirtual.DelayedCall(2.2f, () =>
        {
            if (AnimLevelHardGroup != null) AnimLevelHardGroup.gameObject.SetActive(false);
        });
    }

    private void Update() => Refresh();

    public void Refresh()
    {
        LevelManager level = LevelManager.Ins;
        if (level == null || level.CurrentLevel == null) return;
        RefreshBoosterGroup(level);
        txtLevvel.text = "LV\n" + (level.CurrentLevelIndex + 1);
        if (levelBadge != null)
            levelBadge.sprite = level.CurrentLevel.TypeLevel == TypeLevel.SuperHard ? superHardLevelBadge :
                level.CurrentLevel.TypeLevel == TypeLevel.Hard ? hardLevelBadge : normalLevelBadge;
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
        if (occupancy != null) occupancy.text = level.Conveyor.OccupiedSlotCount + "/" + level.Conveyor.Capacity;
        speedText.text = level.Conveyor.IsDoubleSpeed ? "2X" : "1X";
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
            boosterCounts[i].text = count <= 0 ? "+" : count.ToString();
            //boosterButtons[i].interactable = level.CanInteract && count != 0;
            selectedBoosters[i].SetActive(i == 0 && mode == FoodPickMode.Chopsticks || i == 2 && mode == FoodPickMode.Magnet);
        }
    }

    private void RefreshBoosterGroup(LevelManager level)
    {
        if (BoosterGroup == null) return;

        bool boardIsEmpty = level.Board.IsEmpty();
        if (observedLoadVersion != level.LoadVersion)
        {
            observedLoadVersion = level.LoadVersion;
            DOTween.Kill(BoosterGroup);
            BoosterGroup.anchoredPosition = boosterGroupStartPosition;
            boosterGroupSlidDown = false;
            hasSeenBoardIngredients = !boardIsEmpty;
            return;
        }

        if (!boardIsEmpty)
        {
            hasSeenBoardIngredients = true;
            return;
        }

        if (!hasSeenBoardIngredients || boosterGroupSlidDown) return;

        boosterGroupSlidDown = true;
        float slideDistance = Mathf.Max(24f,
            BoosterGroup.anchoredPosition.y + BoosterGroup.rect.height * (1f - BoosterGroup.pivot.y) + 24f);
        Vector2 targetPosition = boosterGroupStartPosition + Vector2.down * slideDistance;
        BoosterGroup.DOAnchorPos(targetPosition, 0.35f).SetEase(Ease.InOutCubic);
    }

    private void OnDestroy()
    {
        if (BoosterGroup != null) DOTween.Kill(BoosterGroup);
    }

    public void ButtonBoosterPick() { input?.SelectChopsticks(); Refresh(); }
    public void ButtonBoosterHoanDoi() { input?.UseSwapRow(); Refresh(); }
    public void ButtonBoosterHut() { input?.SelectMagnet(); Refresh(); }

    // Cờ "đang dùng" do FoodInputController đặt khi vào chế độ booster thật sự,
    // nên nếu không chọn được (hết lượt, đang khoá input...) cờ không bị kẹt true.
    public void BoosterClick()
    {
        if (IsAnyBoosterInUse) return;

        if (GameDataFragment.cur.dataPlayer.boosterClick > 0)
            ButtonBoosterPick();
        else
            UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Click);
    }

    public void BoosterSuffle()
    {
        if (IsAnyBoosterInUse) return;

        if (GameDataFragment.cur.dataPlayer.boosterSuffle > 0)
            ButtonBoosterHoanDoi();
        else
            UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Shuffle);
    }

    public void BoosterHut()
    {
        if (IsAnyBoosterInUse) return;

        if (GameDataFragment.cur.dataPlayer.boosterHut > 0)
            ButtonBoosterHut();
        else
            UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Magnet);
    }

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