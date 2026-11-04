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

    // Mở chiến thắng, đặt lại luồng nhận thưởng và cập nhật tiền cùng tiến độ mở khóa cơ chế.
    public override void Open()
    {
        base.Open();
        GameManager.ChangeState(GameState.Victory);
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

    // Lưu khoản thưởng màn hiện tại vào tiền tạm để theo dõi thưởng chưa nhận.
    public void setGoldRwTmp()
    {
        GameDataFragment.cur.dataPlayer.moneyTemp = LevelManager.Ins.VictoryReward;
        GameDataFragment.cur.Save();
    }

    // Xóa và lưu khoản tiền thưởng tạm sau khi đã xử lý nhận thưởng.
    public void ResetGoldtemp()
    {
        GameDataFragment.cur.dataPlayer.moneyTemp = 0;
        GameDataFragment.cur.Save();
    }

    // Hiển thị cơ chế sắp mở khóa và chạy thanh tiến trình từ mốc trước đến sau chiến thắng.
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

        // Cập nhật số mốc đã đi qua theo lượng fill hiện tại của thanh mở khóa.
        void UpdateCount()
        {
            if (txtUnlockProgress != null)
                txtUnlockProgress.text = $"{Mathf.FloorToInt(fillUnlockItem.fillAmount * span + 0.0001f)}/{span}";
        }

        // Chốt thanh tiến trình tại giá trị cuối và đổi nội dung khi đã đạt mốc mở khóa.
        void CompleteFill()
        {
            fillUnlockItem.fillAmount = end;
            UpdateCount();
            if (toLevel >= unlockLevel) txtDesNewItem.text = $"Unlocked: {itemName} (Lv {unlockLevel})";
        }
    }

    // Bật biểu tượng tương ứng với cơ chế đang được theo dõi mở khóa.
    private void SetUnlockIcon(FoodState item)
    {
        if (iconHoiCham != null) iconHoiCham.SetActive(item == FoodState.HoiCham);
        if (iconBang != null) iconBang.SetActive(item == FoodState.Ice);
        if (iconLinked != null) iconLinked.SetActive(item == FoodState.Lock);
        if (iconTray != null) iconTray.SetActive(item == FoodState.RigidTray);
    }

    // Lấy tên hiển thị tiếng Anh của cơ chế mở khóa trên popup chiến thắng.
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

    // Dừng và xóa tween tiến trình mở khóa đang chạy.
    private void StopUnlockAnimation()
    {
        unlockTween?.Kill();
        unlockTween = null;
    }

    // Dừng hiệu ứng mở khóa cùng nhận thưởng khi popup chiến thắng bị tắt.
    protected override void OnDisable()
    {
        StopUnlockAnimation();
        StopRewardEffects();
        base.OnDisable();
    }

    // Hiển thị giá trị thưởng thắng thông thường.
    public void setTexxtGold() => goldRwTxt.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward : 0).ToString();
    // Hiển thị giá trị thưởng thắng với hệ số gấp đôi.
    public void setTexxtGoldAds() => textGoldAds.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward * 2 : 0).ToString();
    private static int CurrentGold => GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null
        ? GameDataFragment.cur.dataPlayer.money : 0;
    // Đồng bộ số tiền đang hiển thị với số tiền thực tế của người chơi.
    public void setTxtGold()
    {
        displayedGold = CurrentGold;
        if (txtGold != null) txtGold.text = displayedGold.ToString();
    }

    // Bắt đầu nhận thưởng thông thường rồi tiếp tục luồng sau chiến thắng.
    public void BtnNext() => BeginReward(1);

    // The existing Ads button uses this entry point; an ads SDK should call it after reward success.
    // Bắt đầu nhận thưởng gấp đôi; hiện điểm gọi này chưa thực hiện bước xem quảng cáo.
    public void BtnNextAds() => BeginReward(2);

    // Chặn nhận lặp, chốt thưởng theo phiên màn rồi khởi động hiệu ứng tiền bay hoặc xử lý dự phòng.
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

    // Bắt đầu hiệu ứng nhận tiền và tăng bộ đếm tới tổng tiền mới khi phiên thưởng còn hợp lệ.
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

    // Chốt hiệu ứng tiền bay một lần, dừng bộ chờ dự phòng và kiểm tra có thể chuyển tiếp.
    private void FinishCoinFlight()
    {
        if (!IsRewardCurrent || coinFlightFinished) return;
        coinFlightFinished = true;
        coinFlightWatchdog?.Kill();
        coinFlightWatchdog = null;
        StartReceivingGold();
        TryFinishReward();
    }

    // Chờ cả tiền bay và bộ đếm xong rồi lên lịch tiếp tục sau một nhịp nghỉ.
    private void TryFinishReward()
    {
        if (!IsRewardCurrent || !coinFlightFinished || !goldCountFinished || rewardAdvanceTween != null) return;
        rewardAdvanceTween = DOVirtual.DelayedCall(rewardEndDelay, ContinueAfterReward).SetUpdate(true)
            .OnKill(() => rewardAdvanceTween = null);
    }

    // Tiếp tục về trang chủ hoặc màn kế tiếp khi phiên thưởng vẫn hợp lệ.
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

    // Bật hoặc khóa hai nút nhận thưởng để tránh thao tác lặp.
    private void SetRewardButtons(bool interactable)
    {
        if (btnGoldRW != null) btnGoldRW.interactable = interactable;
        if (btnAdsRW != null) btnAdsRW.interactable = interactable;
    }

    // Hủy tween, sự kiện particle và trạng thái phiên nhận thưởng đang chạy.
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

    // Chuyển yêu cầu xử lý thưởng tạm sang luồng nhận thưởng thông thường.
    public void CheckMoneyTemp() => BtnNext();
    // Ẩn nhóm nút quảng cáo khi tham chiếu đã được gán.
    public void checkShowBtnAds() { if (btnAds != null) btnAds.SetActive(false); }
    // Đóng trực tiếp popup chiến thắng.
    public void Close() => CloseDirectly();
}
