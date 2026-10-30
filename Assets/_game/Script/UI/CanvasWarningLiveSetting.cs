using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasWarningLiveSetting : UICanvas
{
    public TMP_Text txtLive;
    public GameObject ObjLive;

    public GameObject objHome;
    public GameObject objRetry;

    public RectTransform mainPanelRect;
    private Tween popTween;

    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

    }
    public void Setupp(bool onHome)
    {
        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (!isUnlimited)
        {
            txtLive.text = LivesDataFragment.cur.gameData.lives.ToString();
        }

        objHome.SetActive(onHome);
        objRetry.SetActive(!onHome);

    }

    public void Home()
    {
        if (GameManager.ins == null || !GameManager.ins.CanReturnHome || GameManager.ins.IsLoadingLevel) return;
        Time.timeScale = 1f;
        GameManager.ins.ReturnToMainMenu();
    }

    public void Stay()
    {
        CloseDirectly();
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
        UIManager.Ins.CloseUI<CanvasSetting>();
    }

    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
