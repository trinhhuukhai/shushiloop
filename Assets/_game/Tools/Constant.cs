using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constant
{

}


public enum ItemT
{
    Money,
}

public enum TagLevelType
{
    Normal = 0,
    Hard = 1,
    SuperHard = 2,
}


public enum LanguageCode
{
    en,
    zh,
    ko,
    pt,
    es,
    ar,
    de,
    it,
    ja,
    ru,
    th,
    fr,
    id,
    vi,
}

[System.Serializable]
public class CoupleValue<T, V>
{
    public T value1;
    public V value2;

    public CoupleValue()
    {
    }

    public CoupleValue(T firstValue, V secondValue)
    {
        this.value1 = firstValue;
        this.value2 = secondValue;
    }
}

[System.Serializable]
public class TripleValue<T, U, V>
{
    public T value1;
    public U value2;
    public V value3;

    public TripleValue()
    {
    }

    public TripleValue(T firstValue, U secondValue, V thirdValue)
    {
        this.value1 = firstValue;
        this.value2 = secondValue;
        this.value3 = thirdValue;
    }
}
