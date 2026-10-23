using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataManager : MonoBehaviour
{
    public static DataManager ins;
    //NOTE: Global Game Data
    private const int dataKey = 20000101;
    private const int dateVersion = 20250116; //YYYYMMDD
    [SerializeField] private string fileName = "GameData";
    //public ConstantSO ConstantSO;

    //public LevelDataSO levelDataSO;
    public DataFragment[] dataFragmentList;
    private DataConvert dataConvert;
    public BoosterData BoosterData;
    public SetupData setupData;
    public AllLevelData allLevelData;
    public const string FIRST_OPEN_PROFILE = "FIRST_OPEN_PROFILE";


    // True trong toàn bộ session đầu tiên của ngày.
    public bool IsFirstSessionToday { get; private set; }

    // Chặn việc LoadData lại làm thay đổi kết quả trong cùng session.
    private bool _sessionInitialized;

    // Dùng khi chỉ muốn thực hiện một hành động một lần.
    private bool _firstSessionTodayConsumed;

    private void Awake()
    {
        if (ins != null && ins != this) { Destroy(gameObject); return; }
        ins = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy() { if (ins == this) ins = null; }
    public bool isLoaded = false;
    //public PlayerData dt;
    //public const string PLAYER_DATA = "PLAYER_DATA";

    private IEnumerator Start()
    {
        yield return null;
        dataConvert = new DataConvert(Application.persistentDataPath, fileName);
        LoadData();
    }
    private void OnApplicationPause(bool pause) { SaveData(); }
    private void OnApplicationQuit() { SaveData(); }

    //public void LoadData()
    //{
    //    string d = PlayerPrefs.GetString(PLAYER_DATA, "");
    //    if (d != "")
    //    {
    //        dt = JsonUtility.FromJson<PlayerData>(d);
    //    }
    //    else
    //    {
    //        dt = new PlayerData();
    //    }

    //    //loadskin
    //    //load pet

    //    // sau khi hoàn thành tất cả các bước load data ở trên
    //    isLoaded = true;
    //    // FirebaseManager.Ins.OnSetUserProperty();  
    //}
    public void LoadData()
    {
        setupData = dataConvert.Load();
        for (int i = 0; i < dataFragmentList.Length; i++) dataFragmentList[i].Load();
        if (setupData == null || setupData.dataKey != dataKey) //        if (gameData is not { dataKey: dataKey })
        {
            setupData = new SetupData();
            setupData.dataKey = dataKey;
        }

        //if (TimeManager.ins.RealDateTime.Day != setupData.timeLastOpen)
        //{
        //    setupData.daysPlayed++;
        //    setupData.timeLastOpen = TimeManager.ins.RealDateTime.Day;
        //
        //UpdateVersion();


        // sau khi hoàn thành tất cả các bước load data ở trên

        if (!_sessionInitialized)
        {
            int previousLastOpen = setupData.timeLastOpen;

            IsFirstSessionToday =
                dataConvert.IsFirstSessionToday(setupData);

            dataConvert.UpdateDaysPlayed(setupData);

            _firstSessionTodayConsumed = false;
            _sessionInitialized = true;

            Debug.LogWarning(
                $"[Session] FirstToday={IsFirstSessionToday}, " +
                $"PreviousLastOpen={previousLastOpen}, " +
                $"CurrentLastOpen={setupData.timeLastOpen}, " +
                $"DaysPlayed={setupData.daysPlayed}, " +
                $"Now={UnbiasedTime.TrueDateTime:yyyy-MM-dd HH:mm:ss}");
        }


        isLoaded = true;

        if (setupData.isFirstOpen && setupData != null)
        {
            setupData.isFirstOpen = false;
            //Debug.LogError("-----first open app ------");
            //AppsflyerEventRegister.af_First_Open();
        }

        GameDataFragment.cur.dataPlayer.loseStreak = 0;

        DataManager.ins.SaveData();
        //FirebaseManager.Ins.OnSetUserProperty();
    }

    //public void ChangeGold(int gold)
    //{
    //    dt.gold += gold;
    //    SaveData();
    //}
    //public void SaveData()
    //{
    //    if (!isLoaded) return;
    //    string json = JsonUtility.ToJson(dt);
    //    PlayerPrefs.SetString(PLAYER_DATA, json);
    //}
    public bool TryConsumeFirstSessionToday()
    {
        if (!isLoaded ||
            !IsFirstSessionToday ||
            _firstSessionTodayConsumed)
        {
            return false;
        }

        _firstSessionTodayConsumed = true;
        return true;
    }

    public void SaveData(bool isFragmentSave = true)
    {
        if (!isLoaded) return;
        dataConvert.Save(setupData);

        if (!isFragmentSave) return;
        for (int i = 0; i < dataFragmentList.Length; i++) dataFragmentList[i].Save();
    }

    public void SetLevelHigest(int level)
    {
        dataConvert.UpdateDay1LevelMax(setupData, level);
    }

#if UNITY_EDITOR
    [ContextMenu("Delete Data")]
    public void DeleteData()
    {
        PlayerPrefs.DeleteAll();
    }

    [ContextMenu("Reset Fragment")]
    public void ResetFragment()
    {
        for (int i = 0; i < dataFragmentList.Length; i++) dataFragmentList[i].ResetData();
    }

    [ContextMenu("Save Cur Data")]
    public void SaveCurData()
    {
        dataConvert ??= new DataConvert(Application.persistentDataPath, fileName);
        SaveData();
    }

    [ContextMenu("Load Data")]
    public void LoadDataOnEditor()
    {
        dataConvert ??= new DataConvert(Application.persistentDataPath, fileName);
        LoadData();
    }
#endif
}

