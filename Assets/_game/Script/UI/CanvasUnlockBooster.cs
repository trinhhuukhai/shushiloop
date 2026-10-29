using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasUnlockBooster : UICanvas
{

    public TMP_Text des;
    public TMP_Text txtTile;

    public GameObject togs;
    public GameObject swap;
    public GameObject magnet;

    public override void Open()
    {
        base.Open();
        Setupp();
    }

    public void Setupp()
    {

    }

    public void btnGold()
    {

    }

    public void BtnAds()
    {

    }

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
