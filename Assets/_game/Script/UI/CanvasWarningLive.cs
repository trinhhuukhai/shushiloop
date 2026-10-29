using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasWarningLive : UICanvas
{

    public override void Open()
    {
        base.Open();
        Setupp();
    }
    public void Setupp()
    {

    }

    public void Quit()
    {

    }

    public void Stay()
    {

    }

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
