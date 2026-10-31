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
        advancing = false;
        btnGoldRW.interactable = true;
        LevelManager level = LevelManager.Ins;
        setTexxtGold();
        setTexxtGoldAds();
        setTxtGold();
        RefreshUnlockProgress(level != null ? level.VictoryReachedLevelBefore : 1,
            level != null ? level.VictoryReachedLevelAfter : 1);
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
        base.OnDisable();
    }

    public void setTexxtGold() => goldRwTxt.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward : 0).ToString();
    public void setTexxtGoldAds() => textGoldAds.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward * 2 : 0).ToString();
    public void setTxtGold() => txtGold.text = (GameDataFragment.cur.dataPlayer.money).ToString();

    public void BtnNext()
    {
        LevelManager level = LevelManager.Ins;
        if (advancing || level == null || !level.HasWon) return;
        advancing = true;
        btnGoldRW.interactable = false;
        if (GameManager.ins != null)
        {
            // Keep this popup underneath UICTransis until the next board is ready.
            if (!GameManager.ins.ContinueAfterVictory())
            {
                advancing = false;
                btnGoldRW.interactable = true;
            }
            CloseDirectly();
            return;
        }
        // Standalone gameplay validation scenes do not own the application navigation manager.
        level.ClaimVictoryReward();
        CloseDirectly();
        if (level.HasNextLevel) level.NextLevel();
        else level.ReplayLevel();
    }

    public void BtnNextAds() => BtnNext();
    public void CheckMoneyTemp() => BtnNext();
    public void checkShowBtnAds() { if (btnAds != null) btnAds.SetActive(false); }
    public void Close() => CloseDirectly();
}
