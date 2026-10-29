using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFail : UICanvas
{
    public TMP_Text txtGold;
    public override void Open()
    {
        base.Open();
        Refresh();
    }
    private void Refresh()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString();
    }
    public void BtnHome()
    {
        GameManager.ins.ReturnToMainMenu();
    }
    public void BtnRetry()
    {
        if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
    }

    public void Close() => CloseDirectly();
}
