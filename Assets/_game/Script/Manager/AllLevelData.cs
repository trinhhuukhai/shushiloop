using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "_AllLevelData", menuName = "MyGame/AllLevel", order = 1)]
public class AllLevelData : ScriptableObject
{
    [Header("All level")]
    public List<SushiLevelData> listAllLevel;
}
