using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSetting : UICanvas
{
    public Button homeBt;
    public Button retry;
    public Button close;

    public Button backbuttonLanguage;
    public GameObject panelSettingLanguage;
    public RectTransform mainPanelRect;
    private Tween popTween;


    public override void Open()
    {
        base.Open();

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        Time.timeScale = 0f;
        bool inGame = LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null;
        bool allowHome = inGame && GameManager.ins != null && GameManager.ins.CanReturnHome;
        if (homeBt != null) homeBt.gameObject.SetActive(inGame);
        if (retry != null) retry.gameObject.SetActive(inGame);
        if (close != null) close.gameObject.SetActive(!inGame);
        if (homeBt != null)
        {
            homeBt.interactable = allowHome;
            homeBt.gameObject.SetActive(allowHome);
        }

    }
    public void OnResume()
    {

        Time.timeScale = 1f;
        CloseDirectly();
    }
    public void OnReplayLevel() => OnReplay();
    public void OnReplay()
    {


        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (isUnlimited)
        {
            Time.timeScale = 1f;
            if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
        }
        else
        {
            UIManager.Ins.OpenUI<CanvasWarningLiveSetting>().Setupp(false);
        }

    }
    public void OnGiveUp() { CloseDirectly(); LevelManager.Ins?.LoseLevel(); }
    public void OnBackHome()
    {


        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (isUnlimited)
        {
            if (GameManager.ins == null || !GameManager.ins.CanReturnHome || GameManager.ins.IsLoadingLevel) return;
            Time.timeScale = 1f;
            GameManager.ins.ReturnToMainMenu();
        }
        else
        {
            UIManager.Ins.OpenUI<CanvasWarningLiveSetting>().Setupp(true);

        }

    }
    public void OnSettingLanguage() { if (panelSettingLanguage != null) panelSettingLanguage.SetActive(true); }
    public void CloseSettingLanguage() { if (panelSettingLanguage != null) panelSettingLanguage.SetActive(false); }
    public void BackInHome()
    {
        Time.timeScale = 1f;CloseDirectly();
    }
    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
