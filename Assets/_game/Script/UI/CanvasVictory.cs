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
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text nextLabel;
    private bool advancing;

    public override void Open()
    {
        base.Open();
        advancing = false;
        btnGoldRW.interactable = true;
        // No rewarded-ad SDK is connected yet. Never grant a simulated ad reward.
        if (btnAds != null) btnAds.SetActive(false);
        LevelManager level = LevelManager.Ins;
        title.text = level != null && !level.HasNextLevel ? "Đã hoàn thành tất cả màn!" : "Hoàn thành!";
        bool returnHome = GameManager.ins != null && GameManager.ins.CanReturnHome;
        nextLabel.text = returnHome ? "Nhận & về Home" :
            level != null && level.HasNextLevel ? "Nhận & màn tiếp" : "Nhận & chơi lại";
        setTexxtGold();
    }

    public void setTexxtGold() => goldRwTxt.text = "+" + (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward : 0) + " xu";

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