#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;


public class ChangeScene : Editor
{

    [MenuItem("Open Scene/Loading #1")]
    public static void OpenLoading()
    {
        OpenScene("Loading");
    }

    [MenuItem("Open Scene/GamePlay #3")]
    public static void OpenGamePlay()
    {
        OpenScene("GamePlay");
    }


    private static void OpenScene(string sceneName)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/_game/Scenes/" + sceneName + ".unity");
        }
    }
}
#endif