using TMPro;
using UnityEngine;

// Menu tabs share the MainMenu navigation. Their layout is authored in prefabs.
// Trang Shop/Ranking có header riêng (ví xu + tiêu đề + cài đặt) phủ lên header của trang chủ.
public abstract class CanvasHomePage : UICanvas
{
    public RectTransform pageContent;
    [Tooltip("Số xu hiển thị trên header riêng của trang (có thể để trống).")]
    public TMP_Text coinText;
    protected override bool BlocksGameplay => false;

    // Mở trang con rồi gọi hàm làm mới nội dung theo loại trang.
    public override void Open()
    {
        base.Open();
        RefreshCoins();
        Refresh();
    }

    // Điểm mở rộng để các trang con cập nhật nội dung riêng khi được mở.
    protected virtual void Refresh() { }

    // Giữ số xu trên header khớp với ví người chơi.
    protected virtual void Update() => RefreshCoins();

    // Cập nhật nhãn số xu của header trang.
    protected void RefreshCoins()
    {
        if (coinText == null) return;
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        coinText.text = (player != null ? player.money : 0).ToString("N0");
    }

    // Yêu cầu menu chọn lại tab trang chủ.
    public void GoHome()
    {
        if (UIManager.Ins != null)
            UIManager.Ins.GetUI<CanvasMainMenu>().SelectTab(1);
    }

    // Nút "+" cạnh ví xu: chuyển sang tab Shop.
    public void OpenShop()
    {
        if (UIManager.Ins != null)
            UIManager.Ins.GetUI<CanvasMainMenu>().SelectTab(0);
    }

    // Mở popup cài đặt từ header trang.
    public void OpenSettings() => UIManager.Ins?.OpenUI<CanvasSetting>();

    // Quay về tab trang chủ khi nhận phím quay lại.
    public override void BackKey() => GoHome();
}
