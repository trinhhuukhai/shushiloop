using UnityEngine;
using UnityEngine.UI;

public class CanvasSetting : UICanvas
{
    public Button homeBt;
    public Button resumeBt;
    public Button back_Bt;
    public GameObject ingame_btn;
    public GameObject ingame_btn_continue;
    public GameObject home_btn;
    public GameObject btnExit;
    public Button backbuttonLanguage;
    public GameObject panelSettingLanguage;
    public RectTransform mainPanelRect;

    public void OnResume() => CloseDirectly();
    public void OnReplayLevel() => OnReplay();
    public void OnReplay()
    {
        CloseDirectly();
        LevelManager.Ins?.ReplayLevel();
    }
    public void OnGiveUp()
    {
        CloseDirectly();
        LevelManager.Ins?.LoseLevel();
    }
    public void OnBackHome() => OnGiveUp();
    public void OnSettingLanguage() { if (panelSettingLanguage != null) panelSettingLanguage.SetActive(true); }
    public void CloseSettingLanguage() { if (panelSettingLanguage != null) panelSettingLanguage.SetActive(false); }
    public void BackInHome() => CloseDirectly();
    public void Close() => CloseDirectly();
    public override void BackKey() => CloseDirectly();
}
