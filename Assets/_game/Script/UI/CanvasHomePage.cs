using UnityEngine;

// Menu tabs share the MainMenu header and navigation. Their layout is authored in prefabs.
public abstract class CanvasHomePage : UICanvas
{
    public RectTransform pageContent;
    protected override bool BlocksGameplay => false;

    public override void Open()
    {
        base.Open();
        Refresh();
    }

    protected virtual void Refresh() { }

    public void GoHome()
    {
        if (UIManager.Ins != null)
            UIManager.Ins.GetUI<CanvasMainMenu>().SelectTab(1);
    }

    public override void BackKey() => GoHome();
}