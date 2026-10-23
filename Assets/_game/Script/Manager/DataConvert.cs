using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class DataConvert
{
    private readonly string path;
    private const string key = "DataUltimateSupreme";

    public DataConvert(string dataDirPath, string dataFileName)
    {
        path = Path.Combine(dataDirPath, dataFileName);
    }

    public string GetPath()
    {
        return path;
    }

    public SetupData Load()
    {
        //SetupData gameData = null;
        //var data = PlayerPrefs.GetString(key);
        //gameData = JsonUtility.FromJson<SetupData>(data);
        //UpdateDaysPlayed(gameData);
        //return gameData;

        string data = PlayerPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(data))
            return null;

        return JsonUtility.FromJson<SetupData>(data);
    }

    public void Save(SetupData gameData)
    {
        var data = JsonUtility.ToJson(gameData);
        PlayerPrefs.SetString(key, data);
    }

    public void DeleteData()
    {
        PlayerPrefs.SetString(key, null);
    }


    public void UpdateDaysPlayed(SetupData gameData)
    {
        if (gameData == null)
            return;

        int today = GetDayCounter();

        // =========================
        // FIRST INSTALL / DATA CŨ
        // =========================
        if (gameData.timeInstallforFirebase <= 0)
        {
            gameData.timeInstallforFirebase = today;

            if (gameData.dayInstall <= 0)
                gameData.dayInstall = today;

            if (string.IsNullOrEmpty(gameData.timeInstall))
                gameData.timeInstall = DateTime.Now.ToString("O");
        }

        // =========================
        // FIRST PLAY DAY
        // D0 được tính là 1 ngày chơi
        // =========================
        if (gameData.daysPlayed <= 0)
        {
            gameData.daysPlayed = 1;
            gameData.timeLastOpen = today;

            Save(gameData);

            Debug.Log(
                $"[Firebase Tracking] First play | " +
                $"daysPlayed={gameData.daysPlayed} | day={today}"
            );

            return;
        }

        // Trường hợp data cũ chưa có timeLastOpen
        if (gameData.timeLastOpen <= 0)
        {
            gameData.timeLastOpen = today;
            Save(gameData);
            return;
        }

        // =========================
        // NEW DAY
        // =========================
        if (today > gameData.timeLastOpen)
        {
            gameData.daysPlayed++;
            gameData.timeLastOpen = today;

            Save(gameData);

            Debug.Log(
                $"[Firebase Tracking] New play day | " +
                $"daysPlayed={gameData.daysPlayed} | day={today}"
            );
        }

        // Nếu today == timeLastOpen:
        // reopen game trong cùng ngày -> KHÔNG tăng.

        // Nếu today < timeLastOpen:
        // clock bị lùi -> KHÔNG thay đổi timeLastOpen.
    }

    public void UpdateDay1LevelMax(SetupData gameData, int levelPlayed)
    {
        if (gameData == null)
            return;

        int today = GetDayCounter();

        // Chưa có install day thì khởi tạo
        if (gameData.timeInstallforFirebase <= 0)
        {
            gameData.timeInstallforFirebase = today;

            if (gameData.dayInstall <= 0)
                gameData.dayInstall = today;
        }

        // Chỉ ghi nhận level trong ngày D0
        if (today != gameData.timeInstallforFirebase)
            return;

        // Chỉ lấy level cao nhất
        if (levelPlayed > gameData.day1_level_max)
        {
            gameData.day1_level_max = levelPlayed;

            Save(gameData);

            Debug.Log(
                $"[Firebase Tracking] D0 level max = {gameData.day1_level_max}"
            );
        }
    }

    private int GetDayCounter()
    {
        DateTime startPoint = new DateTime(2000, 1, 1);
        return (int)(UnbiasedTime.TrueDateTime.Date - startPoint).TotalDays;
        //return (int)(DateTime.Now.Date - startPoint).TotalDays;
    }

    public bool IsFirstSessionToday(SetupData gameData)
    {
        // Lần đầu chơi hoặc chưa lưu ngày mở game.
        if (gameData == null || gameData.timeLastOpen <= 0)
            return true;

        int today = GetDayCounter();

        // Ngày hiện tại lớn hơn ngày mở game gần nhất.
        return today > gameData.timeLastOpen;
    }


}

[System.Serializable]
public class SetupData
{
    public int dataKey;
    public int dateVersion;

    [Header("--------- Game Setting ---------")]
    public bool isNew = true;
    public bool isMusic = true;
    public bool isSound = true;
    public bool isVibrate = true;
    public bool isNoAds = false;
    public int starRate = -1;

    [Header("--------- Firebase ---------")]
    public string timeInstall;//Thời điểm cài game
    public int dayInstall;//Thời điểm cài game (Tính theo ngày)
    public int timeLastOpen;//Thời điểm cuối cùng mở game. Tính số ngày kể từ 1/1/2000
    public int timeInstallforFirebase; //Dùng trong hàm bắn Firebase UserProperty. Số ngày tính từ ngày 1/1/2000
    public int daysPlayed = 0;//Số ngày đã User có mở game lên
    public int sessionCount = 0;//Tống số session
    public int playTime = 0;//Tổng số lần nhấn play game
    public int playTime_Session = 0;//Số lần nhấn play game trong 1 session
    public int dieCount_levelCur = 0;//Số lần chết tại level hiện tại
    public int day1_level_max = 0;  //Số level đã chơi ở ngày đầu tiên

    [Header("--------Appsflyer------------")]
    public bool isFirstOpen = true;

    //Offer
    public int lastTimeShow_RemoveAdsBundle = 0;
    public int countToShow_RemoveAdsPack = 10;
    public int lastTimeShow_StarterPack = 0;

    public int adsInterCount = 0;
    public int adsRewardCount = 0;

    //Ingame Tutorial
    public SetupData()
    {
        //timeInstall = TimeManager.ins.RealDateTime.ToString();
        //dayInstall = TimeManager.ins.GetDaysCounterFromStartTimePoint();
        adsInterCount = 0;
        adsRewardCount = 0;
    }
}
