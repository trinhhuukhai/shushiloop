using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFail : UICanvas
{
    public GameObject btnHome;
    public CanvasGroup grandCg;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text coinPrice;
    [SerializeField] private TMP_Text retryLabel;
    [SerializeField] private TMP_Text feedback;
    [SerializeField] private GameObject reviveActions;
    [SerializeField] private Button coinButton;
    [SerializeField] private Button adButton;

    public override void Open()
    {
        base.Open();
        feedback.text = "";
        Refresh();
    }
    private void Update() => Refresh();
    private void Refresh()
    {
        LevelManager level = LevelManager.Ins;
        if (level == null) return;
        if (btnHome != null) btnHome.SetActive(GameManager.ins != null && GameManager.ins.CanReturnHome);
        bool revive = level.PendingRevive != SushiReviveReason.None;
        title.text = revive ? "Tiếp tục phục vụ?" : "Chưa hoàn thành";
        description.text = !revive ? "Thử lại để hoàn thành các order nhé!" :
            level.PendingRevive == SushiReviveReason.RushCustomer ? "Khách đã hết giờ. Tiếp tục để thêm 30 giây." :
            level.PendingRevive == SushiReviveReason.FullConveyor ?
            "Gom nguyên liệu từ 5 đĩa vào 1 túi, mở thêm 4 chỗ trống. Nguyên liệu tự bay từ túi đến đĩa món khi khách cần." :
            "Các cột đã đóng băng. Tiếp tục để phá băng hàng đầu.";
        reviveActions.SetActive(revive);
        coinPrice.text = level.ReviveCost + " xu";
        coinButton.interactable = level.CanPurchaseRevive;
        adButton.gameObject.SetActive(revive && level.CanWatchReviveAd);
        retryLabel.text = revive ? "Không tiếp tục" : "Chơi lại";
    }
    public void BtnRevive()
    {
        if (LevelManager.Ins != null && LevelManager.Ins.TryReviveWithCoins()) CloseDirectly();
        else feedback.text = "Chưa đủ xu để tiếp tục.";
    }
    public void ReviveWithAd() => LevelManager.Ins?.TryReviveWithAd();
    public void BtnNext()
    {
        LevelManager level = LevelManager.Ins;
        if (level == null) return;
        if (level.PendingRevive != SushiReviveReason.None) { level.DeclineRevive(); Refresh(); }
        else { CloseDirectly(); level.ReplayLevel(); }
    }
    public void CheckBtnRetry() => BtnNext();
    public void OnNextRetryWhenAddLive() { CloseDirectly(); LevelManager.Ins?.ReplayLevel(); }
    public void BtnHome() { if (GameManager.ins == null || !GameManager.ins.CanReturnHome) return; GameManager.ins.ReturnToMainMenu(); }
    public void OnHandTouchPanel() { if (grandCg != null) grandCg.alpha = 0.2f; }
    public void OnHAndReleasePanel() { if (grandCg != null) grandCg.alpha = 1; }
    public void Close() => CloseDirectly();
}
