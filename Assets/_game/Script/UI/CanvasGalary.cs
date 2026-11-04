using System.Collections.Generic;
using UnityEngine;

public class CanvasGalary : UICanvas
{
    public Transform content;
    public RectTransform rootPanel;

    // Căn panel thư viện theo vùng an toàn của canvas.
    private void Awake()
    {
        rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
    }

    // Mở giao diện thư viện qua lớp canvas cơ sở.
    public override void Open()
    {
        base.Open();

    }

    // Mở lại UI nổi và đóng giao diện thư viện ngay lập tức.
    public void Close()
    {
        UIManager.Ins.OpenUI<CanvasFloating>(); 
        base.Close(0f);
    }
}