using System;
using System.Collections;
using UnityEngine;

public class DelayRunner : MonoBehaviour
{
    private static DelayRunner _instance;

    // load before every scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("__DelayRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DelayRunner>();
        }
    }

    // Hàm static để gọi delay
    public static void Delay(float seconds, Action callback)
    {
        if (_instance == null)
        {
            Debug.LogError("DelayRunner chưa được khởi tạo!");
            return;
        }
        _instance.StartCoroutine(_instance.RunDelayed(seconds, callback));
    }

    private IEnumerator RunDelayed(float secs, Action cb)
    {
        yield return new WaitForSeconds(secs);
        cb?.Invoke();
    }
}
