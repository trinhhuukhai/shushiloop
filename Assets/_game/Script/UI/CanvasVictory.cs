using AssetKits.ParticleImage;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasVictory : UICanvas
{
    public TMP_Text goldRwTxt;
    public TMP_Text textGoldAds;
    public GameObject btnAds;
    public GameObject btnNormal;
    public Button btnAdsRW;
    public Button btnGoldRW;
    private bool advancing;

    public TMP_Text txtGold;

    [Header("Gold reward")]
    public ParticleImage coinAttact;
    public ParticleImage efectcoin;
    [SerializeField, Min(0f)] private float goldCountDuration = 0.5f;
    [SerializeField, Min(0f)] private float rewardEndDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float coinFlightTimeout = 6f;
    private Tween goldCountTween;
    private Tween coinFlightWatchdog;
    private Tween rewardAdvanceTween;
    private LevelManager rewardLevel;
    private int rewardLoadVersion;
    private int displayedGold;
    private int rewardGoldTarget;
    private bool goldReceiving;
    private bool goldCountFinished;
    private bool coinFlightFinished;

    public GameObject iconHoiCham;
    public GameObject iconBang;
    public GameObject iconLinked;
    public GameObject iconTray;
    public TMP_Text txtDesNewItem;
    public Image fillUnlockItem;
    public GameObject unlockRoot;
    public TMP_Text txtUnlockProgress;
    [SerializeField, Min(0f)] private float unlockFillDuration = 0.8f;
    private Tween unlockTween;

    public override void Open()
    {
        base.Open();
        StopRewardEffects();
        advancing = false;
        SetRewardButtons(true);
        LevelManager level = LevelManager.Ins;
        setTexxtGold();
        setTexxtGoldAds();
        setTxtGold();
        RefreshUnlockProgress(level != null ? level.VictoryReachedLevelBefore : 1,
            level != null ? level.VictoryReachedLevelAfter : 1);
        setGoldRwTmp();
    }

    public void setGoldRwTmp()
    {
        GameDataFragment.cur.dataPlayer.moneyTemp = LevelManager.Ins.VictoryReward;
        GameDataFragment.cur.Save();
    }

    public void ResetGoldtemp()
    {
        GameDataFragment.cur.dataPlayer.moneyTemp = 0;
        GameDataFragment.cur.Save();
    }

    public void RefreshUnlockProgress(int fromLevel, int toLevel, bool animate = true)
    {
        StopUnlockAnimation();
        SetUnlockIcon(FoodState.Normal);
        if (GameManager.ins == null || !GameManager.ins.TryGetNextItemUnlock(fromLevel,
                out FoodState item, out int previousLevel, out int unlockLevel))
        {
            if (unlockRoot != null) unlockRoot.SetActive(false);
            return;
        }

        if (unlockRoot != null) unlockRoot.SetActive(true);
        SetUnlockIcon(item);
        int span = unlockLevel - previousLevel;
        float start = Mathf.Clamp01((fromLevel - previousLevel) / (float)span);
        float end = Mathf.Clamp01((Mathf.Max(fromLevel, toLevel) - previousLevel) / (float)span);
        string itemName = UnlockItemName(item);
        txtDesNewItem.text = $"Unlock: {itemName} (Lv {unlockLevel})";
        fillUnlockItem.type = Image.Type.Filled;
        fillUnlockItem.fillMethod = Image.FillMethod.Horizontal;
        fillUnlockItem.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillUnlockItem.fillAmount = start;
        UpdateCount();

        if (!animate || unlockFillDuration <= 0f || end <= start)
        {
            CompleteFill();
            return;
        }

        unlockTween = fillUnlockItem.DOFillAmount(end, unlockFillDuration)
            .SetEase(Ease.OutCubic).SetUpdate(true)
            .OnUpdate(UpdateCount).OnComplete(CompleteFill).OnKill(() => unlockTween = null);

        void UpdateCount()
        {
            if (txtUnlockProgress != null)
                txtUnlockProgress.text = $"{Mathf.FloorToInt(fillUnlockItem.fillAmount * span + 0.0001f)}/{span}";
        }

        void CompleteFill()
        {
            fillUnlockItem.fillAmount = end;
            UpdateCount();
            if (toLevel >= unlockLevel) txtDesNewItem.text = $"Unlocked: {itemName} (Lv {unlockLevel})";
        }
    }

    private void SetUnlockIcon(FoodState item)
    {
        if (iconHoiCham != null) iconHoiCham.SetActive(item == FoodState.HoiCham);
        if (iconBang != null) iconBang.SetActive(item == FoodState.Ice);
        if (iconLinked != null) iconLinked.SetActive(item == FoodState.Lock);
        if (iconTray != null) iconTray.SetActive(item == FoodState.RigidTray);
    }

    private static string UnlockItemName(FoodState item)
    {
        switch (item)
        {
            case FoodState.HoiCham: return "Hidden tile";
            case FoodState.Ice: return "Frozen tile";
            case FoodState.Lock: return "Linked tiles";
            case FoodState.RigidTray: return "Tray";
            default: return string.Empty;
        }
    }

    private void StopUnlockAnimation()
    {
        unlockTween?.Kill();
        unlockTween = null;
    }

    protected override void OnDisable()
    {
        StopUnlockAnimation();
        StopRewardEffects();
        base.OnDisable();
    }

    public void setTexxtGold() => goldRwTxt.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward : 0).ToString();
    public void setTexxtGoldAds() => textGoldAds.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward * 2 : 0).ToString();
    private static int CurrentGold => GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null
        ? GameDataFragment.cur.dataPlayer.money : 0;
    public void setTxtGold()
    {
        displayedGold = CurrentGold;
        if (txtGold != null) txtGold.text = displayedGold.ToString();
    }

    public void BtnNext() => BeginReward(1);

    // The existing Ads button uses this entry point; an ads SDK should call it after reward success.
    public void BtnNextAds() => BeginReward(2);

    private void BeginReward(int multiplier)
    {
        LevelManager level = LevelManager.Ins;
        if (advancing || !isActiveAndEnabled || level == null || !level.HasWon) return;
        advancing = true;
        SetRewardButtons(false);
        rewardLevel = level;
        rewardLoadVersion = level.LoadVersion;
        setTxtGold();
        // Save exactly once before the visual sequence, including if the app closes mid-flight.
        if (!level.ClaimVictoryReward(multiplier))
        {
            ContinueAfterReward();
            return;
        }
        rewardGoldTarget = CurrentGold;
        ResetGoldtemp();
        goldReceiving = goldCountFinished = coinFlightFinished = false;
        if (coinAttact != null && coinAttact.isActiveAndEnabled)
        {
            coinAttact.Stop(true);
            coinAttact.timeScale = AssetKits.ParticleImage.Enumerations.TimeScale.Unscaled;
            coinAttact.onFirstParticleFinish.AddListener(StartReceivingGold);
            coinAttact.onStop.AddListener(FinishCoinFlight);
            coinAttact.Play();
            // A disabled/empty effect must not leave the claim buttons locked forever.
            coinFlightWatchdog = DOVirtual.DelayedCall(coinFlightTimeout, FinishCoinFlight).SetUpdate(true);
        }
        else FinishCoinFlight();
    }

    private bool IsRewardCurrent => advancing && rewardLevel != null && rewardLevel == LevelManager.Ins &&
        rewardLevel.HasWon && rewardLevel.LoadVersion == rewardLoadVersion;

    private void StartReceivingGold()
    {
        if (!IsRewardCurrent || goldReceiving) return;
        goldReceiving = true;
        if (efectcoin != null && efectcoin.isActiveAndEnabled)
        {
            efectcoin.Stop(true);
            efectcoin.timeScale = AssetKits.ParticleImage.Enumerations.TimeScale.Unscaled;
            efectcoin.Play();
        }
        goldCountTween = DOTween.To(() => displayedGold, value =>
            {
                displayedGold = value;
                if (txtGold != null) txtGold.text = value.ToString();
            }, rewardGoldTarget, goldCountDuration)
            .SetEase(Ease.OutCubic).SetUpdate(true)
            .OnComplete(() =>
            {
                setTxtGold();
                goldCountFinished = true;
                TryFinishReward();
            }).OnKill(() => goldCountTween = null);
    }

    private void FinishCoinFlight()
    {
        if (!IsRewardCurrent || coinFlightFinished) return;
        coinFlightFinished = true;
        coinFlightWatchdog?.Kill();
        coinFlightWatchdog = null;
        StartReceivingGold();
        TryFinishReward();
    }

    private void TryFinishReward()
    {
        if (!IsRewardCurrent || !coinFlightFinished || !goldCountFinished || rewardAdvanceTween != null) return;
        rewardAdvanceTween = DOVirtual.DelayedCall(rewardEndDelay, ContinueAfterReward).SetUpdate(true)
            .OnKill(() => rewardAdvanceTween = null);
    }

    private void ContinueAfterReward()
    {
        if (!IsRewardCurrent) return;
        LevelManager level = rewardLevel;
        if (GameManager.ins != null)
        {
            if (!GameManager.ins.ContinueAfterVictory())
            {
                advancing = false;
                SetRewardButtons(true);
                return;
            }
            // The transition is already open before this canvas is hidden.
            CloseDirectly();
            return;
        }
        // Standalone gameplay validation scenes do not own the application navigation manager.
        CloseDirectly();
        if (level.HasNextLevel) level.NextLevel();
        else level.ReplayLevel();
    }

    private void SetRewardButtons(bool interactable)
    {
        if (btnGoldRW != null) btnGoldRW.interactable = interactable;
        if (btnAdsRW != null) btnAdsRW.interactable = interactable;
    }

    private void StopRewardEffects()
    {
        advancing = false;
        rewardLevel = null;
        goldCountTween?.Kill();
        goldCountTween = null;
        coinFlightWatchdog?.Kill();
        coinFlightWatchdog = null;
        rewardAdvanceTween?.Kill();
        rewardAdvanceTween = null;
        if (coinAttact != null)
        {
            coinAttact.onFirstParticleFinish.RemoveListener(StartReceivingGold);
            coinAttact.onStop.RemoveListener(FinishCoinFlight);
            coinAttact.Stop(true);
        }
        if (efectcoin != null) efectcoin.Stop(true);
    }

    public void CheckMoneyTemp() => BtnNext();
    public void checkShowBtnAds() { if (btnAds != null) btnAds.SetActive(false); }
    public void Close() => CloseDirectly();
}
