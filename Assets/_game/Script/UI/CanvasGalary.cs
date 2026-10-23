using System.Collections.Generic;
using UnityEngine;

public class CanvasGalary : UICanvas
{
    public Transform content;
    public RectTransform rootPanel;

    private void Awake()
    {
        rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
    }

    public override void Open()
    {
        base.Open();

    }

    public void Close()
    {
        UIManager.Ins.OpenUI<CanvasFloating>(); 
        base.Close(0f);
    }
}