using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, CreateAssetMenu(menuName = "Stuff/Data Fragment/Daily Fragment", fileName = "Daily Fragment")]
public class DailyRewardDataFragment : DataFragment
{
    public static DailyRewardDataFragment cur;

    public Data gameData;


#if UNITY_EDITOR
    private void OnEnable()
    {
        cur = this;
    }
#else
    private void Awake()
    {
        cur = this;
    }
#endif

    public override void Load()
    {
        if (!LoadData(ref gameData, key)) ResetData();
        gameData.baseOpenTime = DateTime.FromBinary(gameData.baseOpenTimeLong);

        if (gameData.itemFlags.Count > 4) gameData.itemFlags = new List<bool> { false, false, false, false };

        Timer.ScheduleCondition(() => UnbiasedTime.IsValidTime, () => CheckNewDailyDay(out _));
    }

    public override void Save()
    {
        gameData.baseOpenTimeLong = gameData.baseOpenTime.ToBinary();
        SaveData(gameData, key);
    }

    public override void ResetData()
    {
        gameData = new Data();
        gameData.baseOpenTime = new DateTime(2222, 1, 1, 0, 0, 0);
        gameData.baseOpenTimeLong = gameData.baseOpenTime.ToBinary();

        gameData.itemFlags = new List<bool> { false, false, false, false };
    }

    public bool CheckNewDailyDay(out TimeSpan timeLeft)
    {
        TimeSpan diff = UnbiasedTime.TrueDateTime.Subtract(gameData.baseOpenTime);
        timeLeft = TimeSpan.FromSeconds(Variables.TIME_SPACE_HALF_DAY).Subtract(diff);

        bool isNewCycle = diff.TotalSeconds >= Variables.TIME_SPACE_HALF_DAY;
        if (isNewCycle)
        {
            OnNewDayRefresh();
            Save();
            return true;
        }

        return false;
    }

    public void OnNewDayRefresh()
    {
        for (int i = 0; i < gameData.itemFlags.Count; i++)
        {
            gameData.itemFlags[i] = false;
        }

        // gameData.lastFreeTime = 0;
    }

    public void PrepareCountDown()
    {
        gameData.baseOpenTime = UnbiasedTime.TrueDateTime;
    }

    public bool CheckIfItsTimeToFree(out TimeSpan timeLeft)
    {
        var timeSpace = Variables.TIME_SPACE_6_HOUR;
        if (gameData.lastFreeTime + timeSpace > (UnbiasedTime.TrueDateTime.Subtract(Variables.rootDate).TotalSeconds))
        {
            timeLeft = TimeSpan.FromSeconds(timeSpace - ((UnbiasedTime.TrueDateTime.Subtract(Variables.rootDate).TotalSeconds) - gameData.lastFreeTime));
            return false;
        }

        timeLeft = TimeSpan.Zero;
        return true;
    }

    public void OverrideDate(long timeLong, double lastFreeTime, List<bool> flags)
    {
        gameData.baseOpenTimeLong = timeLong;
        gameData.baseOpenTime = DateTime.FromBinary(timeLong);

        gameData.lastFreeTime = lastFreeTime;
        if (flags != null && flags.Count == 4) gameData.itemFlags = flags;
    }

    [Serializable]
    public class Data
    {
        public DateTime baseOpenTime;
        public long baseOpenTimeLong;

        public double lastFreeTime;

        public List<bool> itemFlags = new List<bool>(); // exclude first free hour-ly item
        //        public List<bool> itemFlags = new List<bool> { false, false, false, false };
        // make new like above then when deserialize it will add additional 4 var in first index, or can change to array to fix
        // BUT remember to init the list or array
    }
}