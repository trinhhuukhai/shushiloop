using DG.Tweening;
using TMPro;
using UnityEngine;

public class CanvasMoreBooster : UICanvas
{
    public RectTransform textNotiTf;
    public Transform UITf;
    public Tween scaleTween;
    private BoosterType boosterType;
    int price = 100;

    [SerializeField] private GameObject clear;
    [SerializeField] private GameObject suffle;
    [SerializeField] private GameObject magnet;

    [SerializeField] private TMP_Text  titleTxt;
    [SerializeField] private TMP_Text priceTxt;
    [SerializeField] private TMP_Text descriptionTxt;
    [SerializeField] private TMP_Text amountBooster;

    public bool isCloseAds;
    //public Text txtTotalGoal;

    public RectTransform contentRectTF;


    public override void Open()
    {
        base.Open();
        LevelManager.Ins.isPlay = false;
        isCloseAds = false;
        UIManager.Ins.GetUI<CanvasFloating>().ShowGold_Heart();
        UIManager.Ins.GetUI<CanvasFloating>().HideAllSetting();
        //ShowGold();

    }


    private void OnEnable()
    {
        scaleTween?.Kill();
        UITf.localScale = 0.6f * Vector3.one;
        scaleTween = UITf.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    public void Innit(BoosterType type)
    {
        DisableAll();
        boosterType = type;
        BoosterData boosterDataSO = DataManager.ins.BoosterData;
        price = boosterDataSO.GetDataByID(type).price;

        switch (type)
        {
            case BoosterType.Click:
                clear.SetActive(true);
                titleTxt.text = "CLEAR".ToUpper();
                priceTxt.text = price.ToString();
                amountBooster.text = "+1";
                if (GameDataFragment.cur.dataPlayer.money < price)
                    priceTxt.color = Color.red;
                else priceTxt.color = Color.white;
                descriptionTxt.text = "Fill 1 random bucket in the waiting board";
                break;
            case BoosterType.Shuffle:
                suffle.SetActive(true);
                titleTxt.text = "Shuffle".ToUpper();
                priceTxt.text = price.ToString();
                amountBooster.text = "+1";
                if (GameDataFragment.cur.dataPlayer.money < price)
                    priceTxt.color = Color.red;
                else priceTxt.color = Color.white;
                descriptionTxt.text = "Shuffle the buckets";
                break;

            case BoosterType.Magnet:
                magnet.SetActive(true);
                titleTxt.text = "Magnet".ToUpper();
                priceTxt.text = price.ToString();
                amountBooster.text = "+1";
                if (GameDataFragment.cur.dataPlayer.money < price)
                    priceTxt.color = Color.red;
                else priceTxt.color = Color.white;
                descriptionTxt.text = "Fill one bucket of sand that's on the conveyor belt.";
                break;
            default:
                Debug.LogError("Booster Item doesn't exist !!!");
                break;
        }
    }

    public void CheckGoldAgain()
    {
        BoosterData boosterDataSO = DataManager.ins.BoosterData;
        price = boosterDataSO.GetDataByID(boosterType).price;

        switch (boosterType)
        {
            case BoosterType.Click:    
                if (GameDataFragment.cur.dataPlayer.money < price)
                    priceTxt.color = Color.red;
                else priceTxt.color = Color.white; 
                break;
            case BoosterType.Shuffle: 
                if (GameDataFragment.cur.dataPlayer.money < price)
                    priceTxt.color = Color.red;
                else priceTxt.color = Color.white;
                break;

            case BoosterType.Magnet:
                if (GameDataFragment.cur.dataPlayer.money < price)
                    priceTxt.color = Color.red;
                else priceTxt.color = Color.white;
                break;
            default:
                Debug.LogError("Booster Item doesn't exist !!!");
                break;
        }
    }

    //public void ShowGold()
    //{
    //    txtTotalGoal.text = GameDataFragment.cur.dataPlayer.money.ToString();
    //}

    private void DisableAll()
    {
        clear.SetActive(false);
        suffle.SetActive(false);
        magnet.SetActive(false);

    }

    public override void CloseDirectly()
    {
        base.CloseDirectly();
    }

    public void UpdateGold(int gold)
    {
        //UIManager.Ins.GetUI<CanvasFloating>().AnimGold(gold, false);
        UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

    }

    public void ButtonWatchAds()
    {
        //AdsManager.Ins.ShowRewardedAd(
        //    LevelManager.Ins.curLevelData,
        //    boosterType.ToString(),
        //    boosterType.ToString(),
        //    boosterType.ToString(),
        //    1,
        //    "popup_more_booster",
        //    OnAdsClosed,
        //    OnFail
        //);

        void OnFail()
        {
            //UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);

        }
    }

    public void OnAdsClosed()
    {

        LevelManager.Ins.isPlay = false;

        switch (boosterType)
        {
            case BoosterType.Click:

                //MaxManager.Ins.ShowReward("ingame_booster_hint", "ingame", () =>
                //{
                //    DataManager.Ins.ChangeHints(1, "ads_reward");
                //GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click, 1, "ads rw more booster");
                UIManager.Ins.GetUI<CanvasGamePlay>().ButtonBoosterPick();
                UIManager.Ins.CloseUI<CanvasMoreBooster>();
                LevelManager.Ins.isPlay = true;
                //UIManager.Ins.GetUI<CanvasGamePlay>().UpdateBoosterClick();
                //});
                break;
            case BoosterType.Shuffle:
                //MaxManager.Ins.ShowReward("ingame_booster_undo", "ingame", () =>
                //{
                //    DataManager.Ins.ChangeBoosterUndo(1, "ads_reward");
                //GameDataFragment.cur.ChangeBoosterCount(BoosterType.Suffle, 1, "ads rw more booster");
                UIManager.Ins.GetUI<CanvasGamePlay>().ButtonBoosterHoanDoi();
                UIManager.Ins.CloseUI<CanvasMoreBooster>();
                LevelManager.Ins.isPlay = true;
                UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);
                //UIManager.Ins.GetUI<CanvasGamePlay>().UpdateBoosterSuffle();

                //});
                break;
            case BoosterType.Magnet:
                //MaxManager.Ins.ShowReward("ingame_booster_undo", "ingame", () =>
                //{
                //    DataManager.Ins.ChangeBoosterUndo(1, "ads_reward");
                //GameDataFragment.cur.ChangeBoosterCount(BoosterType.Hut, 1, "ads rw more booster");
                UIManager.Ins.GetUI<CanvasGamePlay>().ButtonBoosterHut();
                UIManager.Ins.CloseUI<CanvasMoreBooster>();
                LevelManager.Ins.isPlay = true;
                //UIManager.Ins.GetUI<CanvasGamePlay>().UpdateBoosterHut();
                //});
                break;
            default:
                Debug.LogError("Lifeline doesn't exist !!!");
                break;
        }
        UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();

        //UIManager.Ins.CloseUI<CanvasMoreBooster>();
        //DOVirtual.DelayedCall(0.05f, () =>
        //{
        //    LevelManager.Ins.isPlay = true;

        //});

    }

    public void ButtonUseGold()
    {
        bool isEnough = false;
        int gold = 0;
        //SoundManager.PlayEfx(SoundID.sfx_click);
        switch (boosterType)
        {
            case BoosterType.Click:
                if (GameDataFragment.cur.dataPlayer.money >= price)
                {
                    isEnough = true;
                    GameDataFragment.cur.ChangeMoney(-price, "buy_booster_clear");
                    //GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click, 1, "popup more booster");
                    gold = price;
                    UIManager.Ins.GetUI<CanvasGamePlay>().ButtonBoosterPick();
                    UIManager.Ins.CloseUI<CanvasMoreBooster>();
                    LevelManager.Ins.isPlay = true;
                    //DataManager.Ins.ChangeGolds(-price, "buy_booster");
                    //DataManager.Ins.ChangeHints(1, "gems");
                    //ShowGold();
                    //FirebaseManager.Ins.CoinSpend("ingame_booster_hint", price, "booster_hint", DataManager.Ins.playerData.currentLevelIndex);


                }
                break;
            case BoosterType.Shuffle:
                if (GameDataFragment.cur.dataPlayer.money >= price)
                {
                    isEnough = true;
                    GameDataFragment.cur.ChangeMoney(-price, "buy_booster_suffle");
                    //GameDataFragment.cur.ChangeBoosterCount(BoosterType.Suffle, 1, "popup more booster");
                    gold = price;
                    UIManager.Ins.GetUI<CanvasGamePlay>().ButtonBoosterHoanDoi();
                    UIManager.Ins.CloseUI<CanvasMoreBooster>();
                    LevelManager.Ins.isPlay = true;
                    //DataManager.Ins.ChangeGolds(-price, "buy_booster");
                    //DataManager.Ins.ChangeBoosterUndo(1, "gems");
                    //ShowGold();
                    //FirebaseManager.Ins.CoinSpend("ingame_booster_undo", price, "booster_undo", DataManager.Ins.playerData.currentLevelIndex);

                }
                break;

            case BoosterType.Magnet:
                if (GameDataFragment.cur.dataPlayer.money >= price)
                {
                    isEnough = true;
                    GameDataFragment.cur.ChangeMoney(-price, "buy_booster_magnet");
                    //GameDataFragment.cur.ChangeBoosterCount(BoosterType.Hut, 1, "popup more booster");
                    gold = price;
                    UIManager.Ins.GetUI<CanvasGamePlay>().ButtonBoosterHut();
                    UIManager.Ins.CloseUI<CanvasMoreBooster>();
                    LevelManager.Ins.isPlay = true;
                    //DataManager.Ins.ChangeGolds(-price, "buy_booster");
                    //DataManager.Ins.ChangeBoosterUndo(1, "gems");
                    //ShowGold();
                    //FirebaseManager.Ins.CoinSpend("ingame_booster_undo", price, "booster_undo", DataManager.Ins.playerData.currentLevelIndex);

                }
                break;

            default:
                isEnough = true;
                Debug.LogError("Lifeline doesn't exist !!!");
                break;

           
        }



        if (isEnough)
        {
            UIManager.Ins.GetUI<CanvasGamePlay>().UpdateBoosterClick();           
            UIManager.Ins.GetUI<CanvasGamePlay>().UpdateBoosterHut();           
            UIManager.Ins.GetUI<CanvasGamePlay>().UpdateBoosterSuffle();   
            UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();

            UIManager.Ins.GetUI<CanvasGamePlay>().setTxtGold();
            
            if (boosterType == BoosterType.Shuffle)
                UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);
            UpdateGold(-gold);


        }
        else
        {
            DOTween.Kill(textNotiTf, true);
            textNotiTf.gameObject.SetActive(true);

            Vector3 oldPos = textNotiTf.localPosition;
            textNotiTf.DOLocalMoveY(oldPos.y + 7f, 0.5f).SetEase(Ease.OutSine).OnComplete(() =>
            {

                textNotiTf.gameObject.SetActive(false);
                textNotiTf.localPosition = oldPos;
            });

            //UIManager.Ins.OpenUI<CanvasShop>().ScrollToGold();

        }
    }

    public void BackBtn()
    {
        LevelManager.Ins.isPlay = true;
        //SoundManager.PlayEfx(SoundID.sfx_click);

        if (boosterType == BoosterType.Magnet)
        {
            //BoosterManager.Ins.UnLockhutCat();

        }
        UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();
        UIManager.Ins.GetUI<CanvasFloating>().showBtnSettingInGame(true);
        UIManager.Ins.CloseUI<CanvasMoreBooster>();

    }

    public void RefreshResource()
    {
        UIManager.Ins.GetUI<CanvasGamePlay>().RefeshCoountAllBooster();
    }

    //public void OnClickIapButton(IAPButtonPro button)
    //{
    //    SoundManager.PlayEfx(SoundID.sfx_click);
    //    IAPPrime.ins.ClickPurchase(button.id, "BOOSTER_PACK", RefreshResource);
    //}
}
