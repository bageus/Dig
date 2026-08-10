#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class DigProjectSceneBootstrap
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string SessionKey = "DigProjectSceneBootstrap.Initialized";

    static DigProjectSceneBootstrap()
    {
        EditorApplication.delayCall += OpenMainSceneWhenEditorStartsEmpty;
    }

    [MenuItem("Tools/Dig/Open Main Scene")]
    private static void OpenMainSceneFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        OpenMainScene();
    }

    private static void OpenMainSceneWhenEditorStartsEmpty()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path) || activeScene.isDirty)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
        {
            return;
        }

        OpenMainScene();
    }

    private static void OpenMainScene()
    {
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
    }
}
#endif
