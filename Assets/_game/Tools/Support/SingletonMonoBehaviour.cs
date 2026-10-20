using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour {
	protected static T instance;
    public bool isDontDestroy;

	public static T Ins {
		get {
			if (instance == null) {
                //instance = new GameObject ().AddComponent<T> ();
                //instance.gameObject.name = instance.GetType ().Name;
                return null;
			}
			return instance;
		}
	}

    public virtual void Awake()
    {
        instance = this as T;
        if (isDontDestroy)
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public static bool Exists () {
		return (instance != null);
	}
}