using DG.Tweening;
using Spine.Unity;
using TMPro;
using Unity.VisualScripting;
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
    //[SerializeField] private GameObject[] selectedBoosters;
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

    //
    public GameObject bsClickUnlock;
    public GameObject bsClickLock;

    public GameObject bsSwapUnlock;
    public GameObject bsSwapLock;

    public GameObject bsMagnetUnlock;
    public GameObject bsMagnetLock;

    public GameObject countBsClick;
    public GameObject imgFreeBsClick;

    public GameObject countBsSwap;
    public GameObject imgFreeBsSwap;

    public GameObject countBsMagnet;
    public GameObject imgFreeMagnet;

    public BoosterSpotlight spotlight;
    public GameObject handBsClick;
    public GameObject handBsSwap;
    public GameObject handBsMagnet;
    private static readonly BoosterType[] AllBoosterTypes = { BoosterType.Click, BoosterType.Shuffle, BoosterType.Magnet };
    private static readonly FoodState[] TutorialItems = { FoodState.HoiCham, FoodState.Ice, FoodState.Lock, FoodState.RigidTray };
    private BoosterType? tutorialBooster;




    // Có booster đang được dùng (cờ do FoodInputController bật/tắt trong BoosterManager).
    private static bool IsAnyBoosterInUse => BoosterManager.Existing != null && BoosterManager.Existing.IsUsingAny;

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
        GameManager.ChangeState(GameState.GamePlay);
        CheckShowTabBooster();
        RefreshBoosterStates();
        imgStuck.SetActive(false);
        OpenUINewItem();
        ShowAnim();
        input = FindFirstObjectByType<FoodInputController>();
        Refresh();
        CheckShowUnlockBooster();
    }

    // Đúng level unlock: chưa hiện popup -> popup rồi tut; đã hiện popup mà chưa xong tut -> chạy lại tut
    public void CheckShowUnlockBooster()
    {
        var data = GameDataFragment.cur;
        LevelManager owner = LevelManager.Ins;
        if (data == null || data.dataPlayer == null || tutorialBooster.HasValue || owner == null || !owner.IsPlaying ||
            owner.IsPopupOpen || SushiLevelOneTutorial.IsInputLocked || (GameManager.ins != null && GameManager.ins.IsLoadingLevel)) return;

        int level = owner.CurrentLevelIndex + 1;
        foreach (BoosterType type in AllBoosterTypes)
        {
            if (level != GameDataFragment.GetBoosterUnlockLevel(type)) continue;

            if (!data.IsUnlockPopupShown(type))
            {
                int version = owner.LoadVersion;
                UIManager.Ins.OpenUI<CanvasUnlockBooster>().Setupp(type, () =>
                {
                    if (owner != null && owner.IsPlaying && owner.LoadVersion == version) StartBoosterTutorial(type);
                });
                data.MarkUnlockPopupShown(type);
                return;
            }

            if (!data.IsBoosterTutorialDone(type))
            {
                StartBoosterTutorial(type);
                return;
            }
        }
    }

    public void StartBoosterTutorial(BoosterType type)
    {
        if (SushiLevelOneTutorial.IsInputLocked) return;
        GameObject targetObj = GetBoosterUnlockObj(type);
        if (spotlight == null || targetObj == null) return;

        RefreshBoosterStates(); // đảm bảo obj unlock đã bật trước khi spotlight
        // Select the ingredient before the spotlight starts intercepting world raycasts.
        if (type == BoosterType.Click && ControlTutorial.Ins != null)
            ControlTutorial.Ins.BeginClickBooster(LevelManager.Ins, () => EndBoosterTutorial(type, false));
        if (type == BoosterType.Magnet && ControlTutorial.Ins != null &&
            !ControlTutorial.Ins.BeginMagnetBooster(LevelManager.Ins, () => EndBoosterTutorial(type, false))) return;
        tutorialBooster = type;
        spotlight.ShowSpotlight(targetObj.GetComponent<RectTransform>());

        GameObject hand = GetBoosterHand(type);
        if (hand != null)
        {
            // Bàn tay phải nằm trên overlay và trên booster
            Canvas c = hand.GetComponent<Canvas>();
            if (c == null) c = hand.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = spotlight.targetOrder + 1;
            hand.SetActive(true);
        }
    }

    private void EndBoosterTutorial(BoosterType type, bool completed = true)
    {
        if (tutorialBooster != type) return;
        tutorialBooster = null;

        if (spotlight != null) spotlight.HideSpotlight();
        SetActiveSafe(GetBoosterHand(type), false);
        if (completed) GameDataFragment.cur.MarkBoosterTutorialDone(type);
    }

    private GameObject GetBoosterUnlockObj(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Click: return bsClickUnlock;
            case BoosterType.Shuffle: return bsSwapUnlock;
            case BoosterType.Magnet: return bsMagnetUnlock;
            default: return null;
        }
    }

    private GameObject GetBoosterHand(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Click: return handBsClick;
            case BoosterType.Shuffle: return handBsSwap;
            case BoosterType.Magnet: return handBsMagnet;
            default: return null;
        }
    }

    public void CheckShowTabBooster()
    {
        if (BoosterGroup == null) return;
        BoosterGroup.gameObject.SetActive(GameDataFragment.cur.IsAnyBoosterUnlocked());
    }


    // Ẩn/hiện các obj lock, unlock, count, free theo trạng thái từng booster
    private void RefreshBoosterStates()
    {
        var data = GameDataFragment.cur;
        if (data == null || data.dataPlayer == null) return;

        ApplyBoosterState(BoosterType.Click, bsClickUnlock, bsClickLock, countBsClick, imgFreeBsClick);
        ApplyBoosterState(BoosterType.Shuffle, bsSwapUnlock, bsSwapLock, countBsSwap, imgFreeBsSwap);
        ApplyBoosterState(BoosterType.Magnet, bsMagnetUnlock, bsMagnetLock, countBsMagnet, imgFreeMagnet);
    }

    private static void ApplyBoosterState(BoosterType type, GameObject unlockObj, GameObject lockObj,
        GameObject countObj, GameObject freeObj)
    {
        var data = GameDataFragment.cur;
        bool unlocked = data.IsBoosterUnlocked(type);
        bool firstUsed = data.IsBoosterFirstUsed(type);

        SetActiveSafe(unlockObj, unlocked);
        SetActiveSafe(lockObj, !unlocked);

        // Chưa unlock: ẩn cả count và free
        // Đã unlock, chưa dùng lần đầu: hiện "free", ẩn count
        // Đã unlock, đã dùng lần đầu: hiện count, ẩn "free"
        SetActiveSafe(freeObj, unlocked && !firstUsed);
        SetActiveSafe(countObj, unlocked && firstUsed);
    }

    private static void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active) obj.SetActive(active);
    }



    public void FlyIconBooster(RectTransform iconFly, RectTransform target, int id)
    {
        if (iconFly == null || target == null)
            return;
        BlockUI.Ins.Block();
        iconFly.DOKill();

        iconFly.gameObject.SetActive(true);

        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Join(
            iconFly.DOMove(target.position, 0.8f)
                .SetEase(Ease.InOutQuad)
        );

        seq.Join(
            iconFly.DOScale(Vector3.one, 0.8f)
                .SetEase(Ease.OutQuad)
        );

        seq.OnComplete(() =>
        {
            iconFly.gameObject.SetActive(false);
            switch (id)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
            BlockUI.Ins.UnBlock();
        });
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
        else if (reviveReason == SushiReviveReason.FrozenHeads)
        {
            txtStuck.text = "Stuck!";
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

    public void OpenUINewItem()
    {
        var data = GameDataFragment.cur;
        LevelManager owner = LevelManager.Ins;
        if (data == null || data.dataPlayer == null || owner == null || !owner.IsPlaying || GameManager.ins == null ||
            owner.IsPopupOpen || SushiLevelOneTutorial.IsInputLocked || GameManager.ins.IsLoadingLevel) return;
        foreach (FoodState item in TutorialItems)
        {
            if (owner.CurrentLevelIndex + 1 != GameManager.ins.GetItemUnlockLevel(item)) continue;
            if (!data.IsItemPopupShown(item))
            {
                int version = owner.LoadVersion;
                UIManager.Ins.OpenUI<CanvasNewItem>().Setupp(item, () =>
                {
                    if (owner != null && owner.IsPlaying && owner.LoadVersion == version)
                        ControlTutorial.Ins?.BeginItem(owner, item);
                });
                data.MarkItemPopupShown(item);
                return;
            }
            if (!data.IsItemTutorialDone(item) && ControlTutorial.Ins != null && ControlTutorial.Ins.BeginItem(owner, item)) return;
        }
    }

    private void Update()
    {
        Refresh();
        LevelManager level = LevelManager.Ins;
        if (level == null || !level.CanInteract) return;
        OpenUINewItem();
        CheckShowUnlockBooster();
    }

    public void Refresh()
    {
        LevelManager level = LevelManager.Ins;
        if (level == null || level.CurrentLevel == null) return;
        RefreshBoosterGroup(level);
        RefreshBoosterStates();
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
        if (occupancy != null)
        {
            occupancy.text = level.Conveyor.OccupiedSlotCount + "/" + level.Conveyor.Capacity;
            occupancy.color = level.Conveyor.OccupancyColor;
        }
        speedText.text = level.Conveyor.IsDoubleSpeed ? "2X" : "1X";
        setTxtGold();
        FoodPickMode mode = input != null ? input.PickMode : FoodPickMode.Tap;
        bool guidedBooster = ControlTutorial.KeepsBoosterSelected;
        boosterHint.text = guidedBooster ? "" : mode == FoodPickMode.Chopsticks ? "Chọn nguyên liệu đang thấy · không gắp ô băng" :
            mode == FoodPickMode.Magnet ? "Chọn một món trong order để hút đủ nguyên liệu" : "";
        cancelBooster.gameObject.SetActive(mode != FoodPickMode.Tap && !guidedBooster);
        levelSelector.SetActive(level.ShowLevelSelector);
        previousLevel.interactable = level.HasPreviousLevel;
        nextLevel.interactable = level.HasNextLevel;
        for (int i = 0; i < boosterButtons.Length; i++)
        {
            int count = FoodInputController.BoosterCount((BoosterType)i);
            boosterCounts[i].text = count <= 0 ? "+" : count.ToString();
            //boosterButtons[i].interactable = level.CanInteract && count != 0;
            //selectedBoosters[i].SetActive(i == 0 && mode == FoodPickMode.Chopsticks || i == 2 && mode == FoodPickMode.Magnet);
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
        //float slideDistance = Mathf.Max(24f,
        //    BoosterGroup.anchoredPosition.y + BoosterGroup.rect.height * (1f - BoosterGroup.pivot.y) + 24f);
        //Vector2 targetPosition = boosterGroupStartPosition + Vector2.down * slideDistance;
        Vector2 targetPosition = new Vector2(0, -300f);
        BoosterGroup.DOAnchorPos(targetPosition, 0.35f).SetEase(Ease.InOutSine);
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
        if (ControlTutorial.IsInputLocked && ControlTutorial.Ins.Step == ControlTutorial.TutorialStep.Booster)
        {
            ControlTutorial.Ins.ConfirmClickBooster();
            Refresh();
            return;
        }
        if (SushiLevelOneTutorial.IsInputLocked) return;
        if (IsAnyBoosterInUse) return;
        if (!GameDataFragment.cur.IsBoosterUnlocked(BoosterType.Click)) return;
        EndBoosterTutorial(BoosterType.Click);

        if (GameDataFragment.cur.CanUseBooster(BoosterType.Click))
            ButtonBoosterPick();
        else
            UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Click);
    }

    public void BoosterSuffle()
    {
        if (SushiLevelOneTutorial.IsInputLocked) return;
        if (IsAnyBoosterInUse) return;
        if (!GameDataFragment.cur.IsBoosterUnlocked(BoosterType.Shuffle)) return;
        EndBoosterTutorial(BoosterType.Shuffle);

        if (GameDataFragment.cur.CanUseBooster(BoosterType.Shuffle))
            ButtonBoosterHoanDoi();
        else
            UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Shuffle);
    }

    public void BoosterHut()
    {
        if (ControlTutorial.IsInputLocked && ControlTutorial.Ins.Step == ControlTutorial.TutorialStep.Booster)
        {
            ControlTutorial.Ins.ConfirmMagnetBooster();
            Refresh();
            return;
        }
        if (SushiLevelOneTutorial.IsInputLocked) return;
        if (IsAnyBoosterInUse) return;
        if (!GameDataFragment.cur.IsBoosterUnlocked(BoosterType.Magnet)) return;
        EndBoosterTutorial(BoosterType.Magnet);

        if (GameDataFragment.cur.CanUseBooster(BoosterType.Magnet))
            ButtonBoosterHut();
        else
            UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Magnet);
    }

    public void CancelBooster() { if (SushiLevelOneTutorial.IsInputLocked) return; input?.CancelBooster(); Refresh(); }
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
