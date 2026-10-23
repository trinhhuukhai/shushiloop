using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Objects/Data Booster")]

public class BoosterData : ScriptableObject
{
    public List<BoosterDataModel> dataList = new List<BoosterDataModel>();
    public BoosterDataModel GetDataByID(BoosterType boosterType)
    {
        return dataList.Find(x => x.boostertype == boosterType);
    }
}

[System.Serializable]
public class BoosterDataModel
{
    public BoosterType boostertype;
    public Sprite icBooster;
    public int price;
    public int levelUnlock;
}


