using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasLive : UICanvas
{
    public TMP_Text txtLive;
    public GameObject timeObject;
    public TMP_Text time;
    public TMP_Text txtTile;

    public GameObject HeartRed;
    public GameObject HeartBlack;

    public override void Open()
    {
        base.Open();
    }
    public void CheckShowTime()
    {

    }

    public void RefillFullLive()
    {

    }

    public void RefillAds()
    {

    }

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
