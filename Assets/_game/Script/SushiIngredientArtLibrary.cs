using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SushiKhai/Ingredient Art", fileName = "SushiIngredientArt")]
public sealed class SushiIngredientArtLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public PoolType type;
        public Sprite sprite;
    }

    public List<Entry> ingredients = new List<Entry>();

    // Tìm sprite được gán cho loại nguyên liệu, trả về null nếu chưa cấu hình.
    public Sprite GetSprite(PoolType type)
    {
        foreach (Entry entry in ingredients)
            if (entry != null && entry.type == type) return entry.sprite;
        return null;
    }
}
