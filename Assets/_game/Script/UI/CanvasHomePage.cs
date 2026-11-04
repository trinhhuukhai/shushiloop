using UnityEngine;

// Menu tabs share the MainMenu header and navigation. Their layout is authored in prefabs.
public abstract class CanvasHomePage : UICanvas
{
    public RectTransform pageContent;
    protected override bool BlocksGameplay => false;

    // Mở trang con rồi gọi hàm làm mới nội dung theo loại trang.
    public override void Open()
    {
        base.Open();
        Refresh();
    }

    // Điểm mở rộng để các trang con cập nhật nội dung riêng khi được mở.
    protected virtual void Refresh() { }

    // Yêu cầu menu chọn lại tab trang chủ.
    public void GoHome()
    {
        if (UIManager.Ins != null)
            UIManager.Ins.GetUI<CanvasMainMenu>().SelectTab(1);
    }

    // Quay về tab trang chủ khi nhận phím quay lại.
    public override void BackKey() => GoHome();
}