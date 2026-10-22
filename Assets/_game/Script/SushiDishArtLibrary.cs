using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SushiKhai/Dish Art", fileName = "SushiDishArt")]
public class SushiDishArtLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SushiDishId dish;
        public Sprite sprite;
    }
    public List<Entry> dishes = new List<Entry>();

    public Sprite GetSprite(SushiDishId id)
    {
        foreach (var entry in dishes) if (entry != null && entry.dish == id) return entry.sprite;
        return null;
    }
}
