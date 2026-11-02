using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasWarningLive : UICanvas
{
    public TMP_Text txtLive;
    public GameObject ObjLive;
    public RectTransform mainPanelRect;
    private Tween popTween;

    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
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
        UIManager.Ins.CloseUI<CanvasRevive>();
        CloseDirectly();
    }

    public void Stay()
    {
        CloseDirectly();
    }

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
