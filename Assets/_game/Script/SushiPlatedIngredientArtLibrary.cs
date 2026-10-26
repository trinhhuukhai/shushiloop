using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SushiKhai/Plated Ingredient Art", fileName = "SushiPlatedIngredientArt")]
public sealed class SushiPlatedIngredientArtLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public PoolType type;
        public Sprite sprite;
        public Vector2 position;
        public Vector2 size = new Vector2(0.76f, 0.6f);
        public float rotation;
        public int sortingOrder = 2;
    }

    [Serializable]
    public sealed class FamilyEntry : Entry
    {
        public SushiDishFamily family;
    }

    public List<Entry> ingredients = new List<Entry>();
    public List<FamilyEntry> familyOverrides = new List<FamilyEntry>();

    public Entry Find(PoolType type, SushiDishFamily family)
    {
        foreach (FamilyEntry entry in familyOverrides)
            if (entry != null && entry.type == type && entry.family == family && entry.sprite != null) return entry;
        foreach (Entry entry in ingredients)
            if (entry != null && entry.type == type && entry.sprite != null) return entry;
        return null;
    }
}
