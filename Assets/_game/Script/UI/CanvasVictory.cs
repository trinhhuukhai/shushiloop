using System.Collections;
using TMPro;
using Unity.VisualScripting;
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

    public override void Open()
    {
        base.Open();
        //SoundManager.PlayEfx(SoundID.sfx_game_win);

        //if (LevelManager.Ins.curLevelData == 0)
        //{
        //    UIManager.Ins.OpenUI<CanvasFloating>();

        //}
        //if (BattlePassDataFragment.cur != null) {

        //    if (GameDataFragment.cur.dataPlayer.cur_level >= 31 && BattlePassDataFragment.cur.IsUnlock())
        //    {
        //        if (LevelManager.Ins.refLevel.typeLevel == TypeLevel.Normal)
        //        {
        //            BattlePassDataFragment.cur.AddKey(1);
        //        }else if(LevelManager.Ins.refLevel.typeLevel == TypeLevel.Hard)
        //        {
        //            BattlePassDataFragment.cur.AddKey(2);

        //        }else if (LevelManager.Ins.refLevel.typeLevel == TypeLevel.SuperHard)
        //        {
        //            BattlePassDataFragment.cur.AddKey(3);

        //        }
        //    }
        //}
        btnGoldRW.interactable = true;
        btnAdsRW.interactable = true;
        UIManager.Ins.GetUI<CanvasFloating>().Resfesh();
        UIManager.Ins.GetUI<CanvasGamePlay>().ResfeshX2();
        UIManager.Ins.GetUI<CanvasFloating>().ShowGold_Heart();
        UIManager.Ins.GetUI<CanvasFloating>().HideHeart();
        UIManager.Ins.GetUI<CanvasFloating>().HideAllSetting();
        DataManager.ins.SetLevelHigest(GameDataFragment.cur.dataPlayer.cur_level);
        checkShowBtnAds();
        setTexxtGold();
        //LavaQuestDataFragment.cur.gameData.ShouldDefeatOnKillApp = false;
        //OceanOdysseyDataFragment.cur.gameData.ShouldDefeatOnKillApp = false;
        //LivesDataFragment.cur.gameData.TruTymOnKillApp = false;
        //LavaQuestDataFragment.cur.OnUserWin();
        //OceanOdysseyDataFragment.cur.OnUserWin(1);
        //SandyRaceDataFragment.cur.OnUserWin();
        if (!GameManager.ins.isPlayTestingLevel)
        {
            int levelpassed = GameDataFragment.cur.dataPlayer.cur_level + 1;
            if (levelpassed % 5 == 0 && levelpassed <= 50)
            {
                //AppsflyerEventRegister.af_level_passed(levelpassed);
                //FirebaseManager.Ins.level_passed(levelpassed);
            }
        }
        DataManager.ins.SaveData();

    }

    public void checkShowBtnAds()
    {
        if(GameDataFragment.cur.dataPlayer.cur_level > 3)
        {
            btnAds.SetActive(true);
        }
        else
        {
            btnAds.SetActive(false);
        }
    }

    public void CheckMoneyTemp()
    {
        int temp = GameDataFragment.cur.dataPlayer.moneyTemp;

        if (temp <= 0)
        {
            BlockUI.Ins.UnBlock();
            return;

        }

        StartCoroutine(CoReceiveGold(temp));
    }

    private IEnumerator CoReceiveGold(int amount)
    {
        BlockUI.Ins.Block();
        //yield return new WaitForSeconds(0.2f);

        CanvasFloating floating = UIManager.Ins.GetUI<CanvasFloating>();

        floating.PlayEffGold();

        // đợi particle bay
        yield return new WaitForSeconds(1.2f);
        GameDataFragment.cur.ChangeMoney(amount, "win");
        GameDataFragment.cur.dataPlayer.moneyTemp = 0;

        if (UIManager.Ins.IsOpened<CanvasGamePlay>()) {
            UIManager.Ins.GetUI<CanvasGamePlay>().setTxtGold();
        }


        DataManager.ins.SaveData();

        floating.AnimGold(amount, true);
        yield return new WaitForSeconds(1.2f);
        if((GameDataFragment.cur.dataPlayer.cur_level + 1) < GameManager.ins.levelBackToHome)
        {
            onNextLevel();
        }
        else
        {
            onNextHome();

        }
        void onNextHome()
        {
            UIManager.Ins.OpenUI<CanvasTransit>();

            UIManager.Ins.GetUI<CanvasTransit>()
                .MakeDelayTransit(() =>
                {

                    UIManager.Ins.CloseUI<CanvasGamePlay>();
                    UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
                    UIManager.Ins.GetUI<CanvasFloating>().showHeart();
                    UIManager.Ins.OpenUI<CanvasMainMenu>();
                    UIManager.Ins.GetUI<CanvasMainMenu>().RefreshLevelMap();
                    Close(0);
                    BlockUI.Ins.UnBlock();
                }, 1f);
        }

        void onNextLevel()
        {
            UIManager.Ins.OpenUI<CanvasTransit>();

            UIManager.Ins.GetUI<CanvasTransit>()
                .MakeDelayTransit(() =>
                {
                    //UIManager.Ins.CloseUI<CanvasGamePlay>();
                    UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
                    UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();
                    LevelManager.Ins.NextLevel();
                    Close(0);
                    BlockUI.Ins.UnBlock();
                }, 1f);
        }



    }

    public void Close()
    {
        base.Close(0f);
    }
    public void setTexxtGold()
    {
        //int gold = LevelManager.Ins.GoldRefLevel;
        int gold = 100;
        goldRwTxt.text = gold.ToString();
        textGoldAds.text = (gold * 2).ToString();
    }
    public void BtnNext()
    {
        btnAdsRW.interactable = false;
        btnGoldRW.interactable = false;
        //if (GameDataFragment.cur.dataPlayer.cur_level > GameManager.ins.GetLevelShowInter())
        //{
        //    AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level - 1), "endgame_win", true, () => { OnNexxtt(); });
        //}
        //else
        //{
        //    OnNexxtt();
        //}

        //LevelManager.Ins.OnHome();

        void OnNexxtt()
        {
            CheckMoneyTemp();


        }
       

    }

    public void BtnNextAds()
    {
        //LevelManager.Ins.OnHome();
        btnGoldRW.interactable = false;
        btnAdsRW.interactable= false;
        //AdsManager.Ins.ShowRewardedAd(
        //            LevelManager.Ins.curLevelData,
        //            "Bonus_win",
        //            "Bonus_win",
        //            "Bonus_win",
        //            1,
        //            "popup_win",
        //            OnClose,
        //            falll
        //        );

        void OnClose()
        {
            int goldTemp = GameDataFragment.cur.dataPlayer.moneyTemp;
            GameDataFragment.cur.dataPlayer.moneyTemp += goldTemp;
            DataManager.ins.SaveData();

            CheckMoneyTemp();

        }

        void falll()
        {
            btnGoldRW.interactable = true;
            btnAdsRW.interactable = true;

        }


    }


}