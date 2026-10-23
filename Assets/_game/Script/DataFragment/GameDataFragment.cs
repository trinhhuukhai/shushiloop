using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataFragment", menuName = "SO/GameDataFragment")]
public class GameDataFragment : DataFragment
{
    public static GameDataFragment cur;

    public DataPlayer dataPlayer;

#if UNITY_EDITOR
    private void OnEnable()
    {
        cur = this;
    }
#else
    private void Awake()
    {
        // Debug.Log("LEVE DATA FRAGMENT");
        cur = this;
    }
#endif

    [ContextMenu("Load Data")]
    public override void Load()
    {
        cur = this;
        if (!LoadData(ref dataPlayer, key)) ResetData();
        GameDataFragment.cur.dataPlayer.resetGameStreak = 0;
    }

    [ContextMenu("Reset Data")]
    public override void ResetData()
    {
        dataPlayer = new DataPlayer();
    }

    [ContextMenu("Save Data")]
    public override void Save()
    {
        SaveData(dataPlayer, key);
        PlayerPrefs.Save();
    }

    public void ChangeMoney(int amount, string place = null)
    {
        if (amount > 0)
        {
            //if (UIManager.Ins.IsOpened<CanvasVictory>()) {
            //    FirebaseManager.Ins.earn_virtual_currency((dataPlayer.cur_level - 1), "gold", amount, place);
            //}
            //else
            //{
            //    //FirebaseManager.Ins.earn_virtual_currency(dataPlayer.cur_level, "gold", amount, place);
            //}
        }
        else
        {
            //FirebaseManager.Ins.spend_virtual_currency(dataPlayer.cur_level, "gold", amount, place);

        }
        dataPlayer.money += amount;
        if (dataPlayer.money < 0) dataPlayer.money = 0;
        Save();
    }
    public void ChangeBoosterCount(BoosterType type, int amount, string place = null)
    {
        switch (type)
        {
            case BoosterType.Click:
                dataPlayer.boosterClick += amount;
                if(amount > 0)
                {
                    //FirebaseManager.Ins.g_gameplay_earn_booster("booster_clear", place, amount);
                }
                else
                {
                    //FirebaseManager.Ins.g_gameplay_booster_used("booster_clear", dataPlayer.cur_level);
                }
                if (dataPlayer.boosterClick < 0) dataPlayer.boosterClick = 0;
                break;
            case BoosterType.Shuffle:
                dataPlayer.boosterSuffle += amount;
                if (amount > 0)
                {
                    //FirebaseManager.Ins.g_gameplay_earn_booster("booster_suffle", place, amount);
                }
                else
                {
                    //FirebaseManager.Ins.g_gameplay_booster_used("booster_suffle", dataPlayer.cur_level);
                }
                if (dataPlayer.boosterSuffle < 0) dataPlayer.boosterSuffle = 0;
                break;
            case BoosterType.Magnet:
                dataPlayer.boosterHut += amount;
                if (amount > 0)
                {
                    //FirebaseManager.Ins.g_gameplay_earn_booster("booster_magnet", place, amount);
                }
                else
                {
                    //FirebaseManager.Ins.g_gameplay_booster_used("booster_magnet", dataPlayer.cur_level);
                }
                if (dataPlayer.boosterHut < 0) dataPlayer.boosterHut = 0;

                break;
            default:
                break;
        }
        Save();
    }

#if UNITY_EDITOR
    //[ContextMenu("Set Dirty")]
    //public void SetDirty()
    //{
    //    UnityEditor.EditorUtility.SetDirty(this);
    //}
#endif

    [Serializable]
    public class DataPlayer
    {
        public int money = 0;
        public int moneyTemp = 0;
        public int cur_level = 0; //Level hiện tại
        //Booster
        public int boosterClick;
        public int boosterSuffle;
        public int boosterHut;
        public List<int> played_levelID = new List<int>();
        public bool isFirstGame = true;
        public int last_time_receive_free_spin = 0;

        public int day1_level_max = 0; //Level đã chơi ở ngày đầu tiên

        public List<string> elementUnlocked = new List<string>();

        //inter
        public bool isShowInter = false;
        public int _retryCountThisLevel = 0;
        public List<string> boughtIAPPackID = new List<string>();
        //Offer
        public int lastTimeShow_RemoveAdsBundle = 0;
        public int lastTimeShow_RemoveAdsPack = 0;
        public int lastTimeShow_StarterPack = 0;

        public int adsInterCount = 0;
        public int adsRewardCount = 0;


        //Firebase
        public float total_value_purchase = 0;

        public int loseStreak = 0;
        public int resetGameStreak = 0;
        public bool isOfferingRemoveAdsBundle = false;
        public bool isOfferingStarterPack = false;
        public bool isFirstSpin = true;
        public bool showTutorialBooster1 = false;
        public bool showTutorialBooster2 = false;
        public bool showTutorialBooster3 = false;

        public bool isClickBucketTutorialClear = false;
        public bool isClickBucketTutorialMagnet = false;
        public List<int> completedLevels = new();

        //tutorial
        public bool showTutorialLevel1 = false;
        public bool showTutorialLevel2 = false;

        public bool fisrtUseBoosterClear = false;
        public bool fisrtUseBoosterSuffle = false;
        public bool fisrtUseBoosterMagnet = false;

        public bool isFirstRevive = false;
        public bool isShowTrapHoiCham = false;
        public bool isShowTrapLock = false;
        public bool isShowTrapIce = false;
        public bool isShowTrapTunel = false;
        public bool isShowTrapLinked = false;

        //phan click xo chau tut item
        public bool isClickChauTutHoiCham = false;
        public bool isClickChauTutHam = false;
        public bool isClickChauTutLock = false;
        public bool isClickChauTutIce = false;
        public bool isClickChauDayNoi = false;

        public bool isShowTutBattlePass = false;


        public DataPlayer()
        {
            money = 500;
            moneyTemp = 0;
            boosterClick = 2;
            boosterSuffle = 2;
            boosterHut = 2;
            cur_level = 0;
            played_levelID = new List<int>();
            total_value_purchase = 0;
            loseStreak = 0;
            resetGameStreak = 0;
            isOfferingRemoveAdsBundle = false;
            isOfferingStarterPack = false;
            isFirstSpin = true;
            isShowInter = false;
            fisrtUseBoosterClear = false;
            fisrtUseBoosterSuffle = false;
            fisrtUseBoosterMagnet = false;
            showTutorialBooster1 = false;
            showTutorialBooster2 = false;
            showTutorialBooster3 = false;
            showTutorialLevel1 = false;
            showTutorialLevel2 = false;
            isClickBucketTutorialClear = false;
            isClickBucketTutorialMagnet = false;
            isFirstRevive = false;
            isShowTrapLock = false;
            isShowTrapHoiCham = false;
            isShowTrapIce = false;
            isShowTrapTunel = false;
            isShowTrapLinked = false;
            //
            isClickChauTutHoiCham = false;
            isClickChauTutHam = false;
            isClickChauTutLock = false;
            isClickChauTutIce = false;
            isClickChauDayNoi = false;

            isShowTutBattlePass = false;
    }

    }
}
