using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Dig.Unity.EditModeTests
{
public sealed class UnityProjectEditModeTests
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [Test]
    public void Main_scene_is_registered_with_runtime_bootstrap()
    {
        Assert.That(
            EditorBuildSettings.scenes.Any(scene =>
                scene.enabled && scene.path == MainScenePath),
            Is.True,
            "The representative runtime scene must be enabled in build settings.");

        Scene scene = EditorSceneManager.OpenScene(
            MainScenePath,
            OpenSceneMode.Additive);
        try
        {
            DigUnityBootstrap[] bootstraps = scene
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<DigUnityBootstrap>(includeInactive: true))
                .ToArray();
            Assert.That(
                bootstraps,
                Has.Length.EqualTo(1),
                "Main.unity must have exactly one DigUnityBootstrap owner.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }
    }
}
}
