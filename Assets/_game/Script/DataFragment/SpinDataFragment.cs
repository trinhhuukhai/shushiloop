using System;
using UnityEngine;

[Serializable, CreateAssetMenu(menuName = "Stuff/Data Fragment/Spin Fragment", fileName = "Spin Fragment")]
public class SpinDataFragment : DataFragment
{
    public static SpinDataFragment cur;

    public Data gameData;


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

    public override void Load()
    {
        if (!LoadData(ref gameData, key)) ResetData();
        gameData.baseOpenTime = DateTime.FromBinary(gameData.baseOpenTimeLong);
    }

    public override void Save()
    {
        gameData.baseOpenTimeLong = gameData.baseOpenTime.ToBinary();
        SaveData(gameData, key);
    }

    public override void ResetData()
    {
        gameData = new Data();
        gameData.baseOpenTime = new DateTime(2022, 1, 1, 0, 0, 0);
        gameData.baseOpenTimeLong = gameData.baseOpenTime.ToBinary();

        gameData.isAdsAble = true;
    }

    public bool CheckNewTimeSegment(out TimeSpan timeLeft)
    {
        if (!UnbiasedTime.IsValidTime)
        {
            timeLeft = TimeSpan.Zero;
            return false;
        }

        TimeSpan diff = UnbiasedTime.TrueDateTime.Subtract(gameData.baseOpenTime);
        timeLeft = TimeSpan.FromSeconds(Variables.TIME_SPACE_HALF_DAY).Subtract(diff);

        bool isNewCycle = diff.TotalSeconds >= Variables.TIME_SPACE_HALF_DAY;
        if (isNewCycle)
        {
            gameData.isAdsAble = true;

            Save();
            return true;
        }

        return false;
    }

    public void MarkTime()
    {
        gameData.baseOpenTime = UnbiasedTime.TrueDateTime;
    }

    //public int GetRequiredPassedLevel()
    //{
    //    return gameData.spinRank switch
    //    {
    //        0 => 5,
    //        _ => 10,
    //    };
    //}

    public int GetRequiredPassedLevel()
    {
        if (gameData.spinRank == 0)
            return 5;

        if (gameData.spinRank == 1)
            return 8;

        if (gameData.spinRank <= 4)
            return 10;

        return 15;
    }

    public void ConsumePassedLevel()
    {
        gameData.levelPassedNum = 0;
        gameData.spinRank++;

        NoffSpin.UpdateNoffStatic();
    }

    public void PassedLevel()
    {
        gameData.levelPassedNum++;
    }

    public bool IsAdsAble()
    {
        //if (GameManager.Ins.IsIapCamp() && !AdsManager.Ins.IsShowAdsRwButtonOnVictoryForIapCamp) return false;
        return gameData.isAdsAble;
    }

    [Serializable]
    public class Data
    {
        public DateTime baseOpenTime;
        public long baseOpenTimeLong;

        public int levelPassedNum;
        public int spinRank;

        public bool isAdsAble;
    }
}