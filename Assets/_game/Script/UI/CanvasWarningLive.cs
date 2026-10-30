using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasWarningLive : UICanvas
{
    public TMP_Text txtLive;
    public GameObject ObjLive;

    public override void Open()
    {
        base.Open();
        Setupp();
    }
    public void Setupp()
    {
        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (!isUnlimited)
        {
            txtLive.text = LivesDataFragment.cur.gameData.lives.ToString();
        }
    }

    public void Quit()
    {
        UIManager.Ins.OpenUI<CanvasFail>();
        CloseDirectly();
    }

    public void Stay()
    {
        CloseDirectly();
    }

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
