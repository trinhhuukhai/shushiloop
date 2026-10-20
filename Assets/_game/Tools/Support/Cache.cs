using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Cache
{
    public static Dictionary<Collider2D, FoodQueue> FoodQueues = new Dictionary<Collider2D, FoodQueue>();

    public static FoodQueue GetBucket(Collider2D col)
    {
        if (!FoodQueues.ContainsKey(col))
        {
            FoodQueues.Add(col, col.GetComponent<FoodQueue>());
        }
        return FoodQueues[col];
    }

    private static Dictionary<float, WaitForSeconds> m_WFS = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds GetWFS(float key)
    {
        if (!m_WFS.ContainsKey(key))
        {
            m_WFS[key] = new WaitForSeconds(key);
        }

        return m_WFS[key];
    }
}