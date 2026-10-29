using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasNewItem : UICanvas
{
    public TMP_Text des;
    public TMP_Text txtTile;

    public GameObject imgItemHoiCham;
    public GameObject imgItemHoiBang;
    public GameObject imgItemLink;
    public GameObject imgItemKhay;

    public override void Open()
    {
        base.Open();
        Setupp();
    }

    public void Setupp()
    {

    }
   

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
