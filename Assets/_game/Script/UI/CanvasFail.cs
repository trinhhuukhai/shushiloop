using DG.Tweening;
using UnityEngine;

public class CanvasFail : UICanvas
{
    public GameObject btnHome;
 
    public override void Open()
    {
        base.Open();
        //SoundManager.PlayEfx(SoundID.sfx_game_lose);

        UIManager.Ins.CloseUI<CanvasFloating>();
        if (GameDataFragment.cur.dataPlayer.cur_level >= GameManager.ins.levelBackToHome - 1)
        {
            //btnHome.SetActive(true);
            //LivesDataFragment.cur.ConsumeLife();
            //LavaQuestDataFragment.cur.gameData.ShouldDefeatOnKillApp = false;
            //OceanOdysseyDataFragment.cur.gameData.ShouldDefeatOnKillApp = false;
            //LavaQuestDataFragment.cur.OnUserDefeat();
            //OceanOdysseyDataFragment.cur.OnUserDefeat();
            //SandyRaceDataFragment.cur.OnUserDefeat();

        }
        else
        {
            btnHome.SetActive(false);
        }

        GameDataFragment.cur.dataPlayer.loseStreak++;
        //ProfileDataFragment.cur?.RecordLevelLose();
        var dataResource = GameDataFragment.cur.dataPlayer;
        //FirebaseManager.Ins.g_game_end(LevelManager.Ins.curLevelData, false,LevelManager.Ins.IsFirstTryThisLevel(), (int)LevelManager.Ins.totalPlayTime, "normal_lose", dataResource.money, dataResource.boosterClick, dataResource.boosterSuffle, dataResource.boosterHut);
        LevelManager.Ins.totalPlayTime = 0;
        DataManager.ins.SaveData();


    }

    public void Close()
    {
        base.Close(0f);
    }

    public void CheckBtnRetry()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);

        //if (LivesDataFragment.cur.HasLife())
        //{
        //    BtnNext();
        //}
        //else
        //{
        //    UIManager.Ins.OpenUI<CanvasGetMoreLiveReplay>();
        //}
    }

    public void BtnNext()
    {



        //if (GameDataFragment.cur.dataPlayer.cur_level > GameManager.ins.GetLevelShowInter())
        //{
        //    if(GameDataFragment.cur.dataPlayer.loseStreak >= (int)AdsManager.Ins.numberFailLevel)
        //    {
        //        AdsManager.Ins._timeWatchAdsInter = 181;

        //        AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "endgame_lose", true, () =>
        //        {
        //            GameDataFragment.cur.dataPlayer.loseStreak = 0;
        //            GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //            DataManager.ins.SaveData();
        //            onNextRetry();
        //        });
        //    }
        //    else
        //    {
        //        AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "endgame_lose", true, () =>
        //        {
        //            GameDataFragment.cur.dataPlayer.loseStreak = 0;
        //            GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //            DataManager.ins.SaveData();
        //            onNextRetry();
        //        });
        //    }

        //}
        //else
        //{
        //    GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //    DataManager.ins.SaveData();
        //    onNextRetry();
        //}


        void onNextRetry()
        {
            LevelManager.Ins.ReplayLevel();
            Close();
            if (GameDataFragment.cur.dataPlayer.cur_level >= 0)
            {
                UIManager.Ins.OpenUI<CanvasFloating>();
            }
        }
    }


    public void OnNextRetryWhenAddLive()
    {
        //GameDataFragment.cur.dataPlayer.loseStreak = 0;
        GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        DataManager.ins.SaveData();
        LevelManager.Ins.ReplayLevel();
        Close();
        if (GameDataFragment.cur.dataPlayer.cur_level >= 0)
        {
            UIManager.Ins.OpenUI<CanvasFloating>();
        }
    }
    public void BtnHome()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);
        //if (GameDataFragment.cur.dataPlayer.cur_level > GameManager.ins.GetLevelShowInter())
        //{
        //    if (GameDataFragment.cur.dataPlayer.loseStreak >= (int)AdsManager.Ins.numberFailLevel)
        //    {
        //        AdsManager.Ins._timeWatchAdsInter = 181;

        //        AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "endgame_lose", true, () =>
        //        {
        //            GameDataFragment.cur.dataPlayer.loseStreak = 0;
        //            GameDataFragment.cur.dataPlayer.resetGameStreak = 0;

        //            DataManager.ins.SaveData();
        //            onNextHome();
        //        });
        //    }
        //    else
        //    {
        //        AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "endgame_lose", true, () =>
        //        {
        //            GameDataFragment.cur.dataPlayer.loseStreak = 0;
        //            GameDataFragment.cur.dataPlayer.resetGameStreak = 0;

        //            DataManager.ins.SaveData();
        //            onNextHome();
        //        });
        //    }

        //}
        //else
        //{
        //    GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //    DataManager.ins.SaveData();
        //    onNextHome();
        //}


        void onNextHome()
        {
            UIManager.Ins.OpenUI<CanvasTransit>();

            UIManager.Ins.GetUI<CanvasTransit>()
                .MakeDelayTransit(() =>
                {
                    UIManager.Ins.OpenUI<CanvasMainMenu>();
                    UIManager.Ins.GetUI<CanvasMainMenu>().RefreshLevelMap();
                    UIManager.Ins.CloseUI<CanvasGamePlay>();
                    UIManager.Ins.OpenUI<CanvasFloating>();
                    Close(0);
                }, 1f);
        }
        



    }

    public CanvasGroup grandCg;
    private Tween grandCgTween;

    public void OnHandTouchPanel()
    {
        grandCgTween.Kill();
        grandCgTween = grandCg.DOFade(0, .32f);
    }

    public void OnHAndReleasePanel()
    {
        grandCgTween.Kill();
        grandCgTween = grandCg.DOFade(1, .32f);
    }

    public void BtnRevive()
    {
        //LevelManager.Ins.OnRevive();
        Close();
    }




}