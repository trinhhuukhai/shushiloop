using AssetKits.ParticleImage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasVictory : UICanvas
{
    public TMP_Text goldRwTxt;
    public TMP_Text textGoldAds;
    public GameObject btnAds;
    public GameObject btnNormal;
    public Button btnAdsRW;
    public Button btnGoldRW;
    private bool advancing;

    public TMP_Text txtGold;


    public override void Open()
    {
        base.Open();
        advancing = false;
        btnGoldRW.interactable = true;
        LevelManager level = LevelManager.Ins;
        bool returnHome = GameManager.ins != null && GameManager.ins.CanReturnHome;
        setTexxtGold();
        setTexxtGoldAds();
        setTxtGold();

    }

    public void setTexxtGold() => goldRwTxt.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward : 0).ToString();
    public void setTexxtGoldAds() => textGoldAds.text = (LevelManager.Ins != null ? LevelManager.Ins.VictoryReward * 2 : 0).ToString();
    public void setTxtGold() => txtGold.text = (GameDataFragment.cur.dataPlayer.money).ToString();

    public void BtnNext()
    {
        LevelManager level = LevelManager.Ins;
        if (advancing || level == null || !level.HasWon) return;
        advancing = true;
        btnGoldRW.interactable = false;
        if (GameManager.ins != null)
        {
            // Keep this popup underneath UICTransis until the next board is ready.
            if (!GameManager.ins.ContinueAfterVictory())
            {
                advancing = false;
                btnGoldRW.interactable = true;
            }
            return;
        }
        // Standalone gameplay validation scenes do not own the application navigation manager.
        level.ClaimVictoryReward();
        CloseDirectly();
        if (level.HasNextLevel) level.NextLevel();
        else level.ReplayLevel();
    }

    public void BtnNextAds() => BtnNext();
    public void CheckMoneyTemp() => BtnNext();
    public void checkShowBtnAds() { if (btnAds != null) btnAds.SetActive(false); }
    public void Close() => CloseDirectly();
}