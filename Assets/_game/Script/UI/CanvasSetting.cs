using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSetting : UICanvas
{
    //public Animation anim;
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
    private Tween popTween;
    //public List<CanvasButtonLanguage> listButtonLanguage;
    public override void Open()
    {
        //anim.Play("ShowUI");
        base.Open();
        //SoundManager.PlayEfx(SoundID.sfx_ui_popup_show);

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
        UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(false);
        homeBt.interactable = true;
        resumeBt.interactable = true;
        back_Bt.interactable = true;
        if(GameDataFragment.cur.dataPlayer.cur_level < GameManager.ins.levelBackToHome - 1)
        {
            ingame_btn.SetActive(false);
            ingame_btn_continue.SetActive(LevelManager.Ins.isPlay);
        }
        else
        {
            ingame_btn.SetActive(LevelManager.Ins.isPlay);
            ingame_btn_continue.SetActive(false);
        }
  
        home_btn.SetActive(!LevelManager.Ins.isPlay);
        btnExit.SetActive(LevelManager.Ins.isPlay);
        panelSettingLanguage.SetActive(false);
        //SlotManager.Ins.StopMove();
        //if (LevelManager.Ins.isPlay == true)
        //{
        //    if (SlotManager.Ins.CurrentBuckets.Length > 0)
        //    {
        //        for (int i = 0; i < SlotManager.Ins.CurrentBuckets.Length; i++)
        //        {
        //            if (SlotManager.Ins.CurrentBuckets[i] != null)
        //                SlotManager.Ins.CurrentBuckets[i].khonghutcat = true;
        //        }
        //    }
        //}
        Time.timeScale = 0;

    }
    public void OnBackHome()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);
        //if (ShouldShowExitWarning())
        //{
        //    //UIManager.Ins.OpenUI<CanvasExitLives>();

        //}
        //else
        //{
        //    OnGiveUp();
        //    if (UIManager.Ins.IsOpened<CanvasFloating>())
        //    {
        //        UIManager.Ins.GetUI<CanvasFloating>().showHeart();
        //        UIManager.Ins.GetUI<CanvasFloating>().ShowLives();
        //        UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
        //    }
        //}
    }

    public void OnReplayLevel()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);
        if (GameDataFragment.cur.dataPlayer.cur_level < GameManager.ins.levelBackToHome - 1)
        {
            OnReplay();
        }
        else
        {
            //if (LivesDataFragment.cur.HasLife())
            //{
            //    if (ShouldShowExitWarning())
            //    {
            //        UIManager.Ins.OpenUI<CanvasExitLivesReplay>();

            //    }
            //    else
            //    {
            //        OnReplay();
            //        if (UIManager.Ins.IsOpened<CanvasFloating>())
            //        {
            //            UIManager.Ins.GetUI<CanvasFloating>().showHeart();
            //            UIManager.Ins.GetUI<CanvasFloating>().ShowLives();
            //            UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
            //        }
            //    }
            //}
            //else
            //{
            //    UIManager.Ins.OpenUI<CanvasGetMoreLiveReplay>();
            //}



        }
    }

    //private bool ShouldShowExitWarning()
    //{
    //    bool isUnlimitedLive =
    //        LivesDataFragment.cur != null &&
    //        LivesDataFragment.cur.IsUnlimitedLives();


    //    bool isLavaInProgress =
    //        LavaQuestDataFragment.cur != null &&
    //        LavaQuestDataFragment.cur.gameData != null &&
    //        LavaQuestDataFragment.cur.IsUserInprogress;



    //    bool isOceanInProgress =
    //        OceanOdysseyDataFragment.cur != null &&
    //        OceanOdysseyDataFragment.cur.gameData != null &&
    //        OceanOdysseyDataFragment.cur.IsEventInprogress;



    //    return !isUnlimitedLive ||
    //           isLavaInProgress ||
    //           isOceanInProgress;
    //}

    public void OnGiveUp()
    {
        //LivesDataFragment.cur.ConsumeLife();
        //LavaQuestDataFragment.cur.OnUserDefeat();
        //OceanOdysseyDataFragment.cur.OnUserDefeat();
        //SandyRaceDataFragment.cur.OnUserDefeat();
        //GameDataFragment.cur.dataPlayer.loseStreak++;
        //ProfileDataFragment.cur?.RecordLevelLose();
        //DataManager.ins.SaveData();

        ////SoundManager.PlayEfx(SoundID.sfx_click);
        //if (GameDataFragment.cur.dataPlayer.cur_level > GameManager.ins.GetLevelShowInter())
        //{
        //    if(GameDataFragment.cur.dataPlayer.loseStreak >= (int)AdsManager.Ins.numberFailLevel)
        //    {
        //        AdsManager.Ins._timeWatchAdsInter = 181;
        //        AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "back_home", true, () => {
        //            GameDataFragment.cur.dataPlayer.loseStreak = 0;
        //            GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //            DataManager.ins.SaveData();
        //            OnNexxttHome();
        //        });
        //    }
        //    else
        //    {

        //        AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "back_home", true, () => {
        //            GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //            DataManager.ins.SaveData();
        //            OnNexxttHome();
        //        });
        //    }
        //}
        //else
        //{
        //    GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //    DataManager.ins.SaveData();
        //    OnNexxttHome();
        //}
       

        void OnNexxttHome()
        {
             Time.timeScale = 1;
            var dataResource = GameDataFragment.cur.dataPlayer;
            //FirebaseManager.Ins.g_game_end(LevelManager.Ins.curLevelData, false, LevelManager.Ins.IsFirstTryThisLevel(), (int)LevelManager.Ins.totalPlayTime, "back_home", dataResource.money, dataResource.boosterClick, dataResource.boosterSuffle, dataResource.boosterHut);
            UIManager.Ins.OpenUI<CanvasTransit>();
            UIManager.Ins.GetUI<CanvasTransit>()
                .MakeDelayTransit(() =>
                {
                    //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
                    //int remainingCoin = Mathf.Clamp(GameDataFragment.cur.dataPlayer.GetItem(ItemT.Money).amount, 0, int.MaxValue);
                    homeBt.interactable = false;
                    resumeBt.interactable = false;
                    //LevelManager.Ins.OnMainMenu();


                    UIManager.Ins.OpenUI<CanvasMainMenu>();
                    UIManager.Ins.GetUI<CanvasFloating>().ShowGold_Heart();
                    UIManager.Ins.GetUI<CanvasGamePlay>().Close();
                    Close(0);
                }, 1f);
        }
    }
    
    public void OnReplay()
    {
        //    //SoundManager.PlayEfx(SoundID.sfx_click);
        //    LivesDataFragment.cur.ConsumeLife();
        //    GameDataFragment.cur.dataPlayer.loseStreak++;
        //    ProfileDataFragment.cur?.RecordLevelLose();
        //    LavaQuestDataFragment.cur.OnUserDefeat();
        //    SandyRaceDataFragment.cur.OnUserDefeat();
        //    OceanOdysseyDataFragment.cur.OnUserDefeat();
        //    DataManager.ins.SaveData();

        //    if (GameDataFragment.cur.dataPlayer.cur_level > GameManager.ins.GetLevelShowInter())
        //    {
        //        if(GameDataFragment.cur.dataPlayer.loseStreak >= (int)AdsManager.Ins.numberFailLevel)
        //        {
        //            AdsManager.Ins._timeWatchAdsInter = 181;
        //            AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "replay_game", true, () => {
        //                GameDataFragment.cur.dataPlayer.loseStreak = 0;
        //                GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //                DataManager.ins.SaveData();
        //                OnNexxttReplay();
        //            });
        //        }
        //        else
        //        {
        //            AdsManager.Ins.ShowInterstitial((GameDataFragment.cur.dataPlayer.cur_level), "replay_game", true, () => {
        //                GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //                DataManager.ins.SaveData();
        //                OnNexxttReplay();
        //            });
        //        }

        //    }
        //    else
        //    {
        //        GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
        //        DataManager.ins.SaveData();
        //        OnNexxttReplay();
        //    }


        void OnNexxttReplay()
        {
          
            //LevelManager.Ins.OnRetry();
            //var dataResource = GameDataFragment.cur.dataPlayer;
            //FirebaseManager.Ins.g_game_end(LevelManager.Ins.curLevelData, false, LevelManager.Ins.IsFirstTryThisLevel(), (int)LevelManager.Ins.totalPlayTime, "replay", dataResource.money, dataResource.boosterClick, dataResource.boosterSuffle, dataResource.boosterHut);
            //UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);
            //Time.timeScale = 1;
            Close(0);
        }


    }

    //public void OnNexxttReplayWhenAddLive()
    //{
    //    //GameDataFragment.cur.dataPlayer.loseStreak = 0;
    //    GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
    //    DataManager.ins.SaveData();
    //    LevelManager.Ins.OnRetry();
    //    var dataResource = GameDataFragment.cur.dataPlayer;
    //    FirebaseManager.Ins.g_game_end(LevelManager.Ins.curLevelData, false, LevelManager.Ins.IsFirstTryThisLevel(), (int)LevelManager.Ins.totalPlayTime, "replay", dataResource.money, dataResource.boosterClick, dataResource.boosterSuffle, dataResource.boosterHut);
    //    UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);
    //    Time.timeScale = 1;
    //    Close(0);
    //}

    public void OnResume()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        //SoundManager.PlayEfx(SoundID.sfx_click);
        homeBt.interactable = false;
        resumeBt.interactable = false;
        //anim.Play("HideUI");
        //LevelManager.Ins.OnResume();
        //SlotManager.Ins.ResumeMove();
        //if (SlotManager.Ins.CurrentBuckets.Length > 0)
        //{
        //    for (int i = 0; i < SlotManager.Ins.CurrentBuckets.Length; i++)
        //    {
        //        if (SlotManager.Ins.CurrentBuckets[i] != null)
        //        {
        //            SlotManager.Ins.CurrentBuckets[i].khonghutcat = false;
        //            SlotManager.Ins.CurrentBuckets[i].StartCollect();

        //        }
        //    }
        //}
        UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);
        Time.timeScale = 1;
        Close(0);
    }


   
    public void OnSettingLanguage()
    {
        panelSettingLanguage.SetActive(true);
        backbuttonLanguage.gameObject.SetActive(true);
        //backbuttonLanguage.interactable = true;
        //ChoosenLanguage();
    }
    //public void ChoosenLanguage()
    //{
    //    string lang = LocalizationManager.CurrentLanguageCode;
    //    foreach (CanvasButtonLanguage btn in listButtonLanguage)
    //    {
    //        if (btn.LanguageCode.ToString() == lang)
    //        {
    //            btn.SetActiveImage(true);
    //        }
    //        else
    //        {
    //            btn.SetActiveImage(false);
    //        }
    //    }
    //}
    public void CloseSettingLanguage()
    {
        panelSettingLanguage.SetActive(false);
    }
    public void Close()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_slide_out);
        //LevelManager.Ins.OnResume();
        back_Bt.interactable = false;
        //anim.Play("HideUI");
        UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
        Time.timeScale = 1;
        Close(0);
    }

    public void BackInHome()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_slide_out);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        back_Bt.interactable = false;
        UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
        Time.timeScale = 1;
        Close(0);
    }
}
