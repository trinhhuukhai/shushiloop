using TMPro;
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

    public override void Open()
    {
        base.Open();
        Time.timeScale = 0f;
        bool inGame = LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null;
        bool allowHome = inGame && GameManager.ins != null && GameManager.ins.CanReturnHome;
        if (back_Bt != null) back_Bt.gameObject.SetActive(inGame);
        if (homeBt != null)
        {
            homeBt.interactable = allowHome;
            homeBt.gameObject.SetActive(allowHome);
        }
        if (resumeBt != null) resumeBt.GetComponentInChildren<TMP_Text>().text = inGame ? "Tiếp tục" : "Đóng";
        if (mainPanelRect != null)
        {
            var title = mainPanelRect.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null) title.text = inGame ? "Tạm dừng" : "Cài đặt";
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
        // The transition acquires the gameplay pause before Settings releases its own.
        if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
    }
    public void OnGiveUp() { CloseDirectly(); LevelManager.Ins?.LoseLevel(); }
    public void OnBackHome()
    {
        // Also protect the callback against stale UI events and direct invocation.

        if (GameManager.ins == null || !GameManager.ins.CanReturnHome || GameManager.ins.IsLoadingLevel) return;
         Time.timeScale = 1f;
        GameManager.ins.ReturnToMainMenu();
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
