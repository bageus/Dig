using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dig.Unity.Editor
{
    internal static class DigProjectSetup
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/Main.unity";

        [MenuItem("Tools/Dig/Create Bootstrap Scene")]
        private static void CreateBootstrapScene()
        {
            if (File.Exists(ScenePath) && !ConfirmOverwrite())
            {
                return;
            }

            Directory.CreateDirectory(SceneDirectory);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            GameObject runtime = new GameObject("Dig Runtime");
            runtime.AddComponent<DigUnityBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Selection.activeGameObject = runtime;

            Debug.Log($"Created Dig bootstrap scene at {ScenePath}.", runtime);
        }

        private static bool ConfirmOverwrite()
        {
            return EditorUtility.DisplayDialog(
                "Replace bootstrap scene?",
                $"{ScenePath} already exists. Replace it with a new bootstrap scene?",
                "Replace",
                "Cancel");
        }
    }
}
