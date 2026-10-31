using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T ins;

    // Read/reset paths, especially OnDisable/OnDestroy, must not create a replacement.
    public static T Existing
    {
        get
        {
            if (ins == null) ins = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            return ins;
        }
    }

    public static T Ins
    {
        get
        {
            if (Existing == null) ins = new GameObject(typeof(T).Name).AddComponent<T>();
            return ins;
        }
    }

}
