using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dig.Unity.PlayModeTests
{
public sealed class RepresentativeSceneConsolePlayModeTests
{
    private const string MainSceneName = "Main";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int StartupFrameLimit = 120;

    [UnityTest]
    public IEnumerator Main_scene_bootstraps_without_console_errors()
    {
        string evidencePath = ResolveEvidencePath();
        if (File.Exists(evidencePath))
        {
            File.Delete(evidencePath);
        }

        LogAssert.Expect(
            LogType.Log,
            new Regex("Dig Unity runtime started with .* residents", RegexOptions.CultureInvariant));
        AsyncOperation load = SceneManager.LoadSceneAsync(
            MainSceneName,
            LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
        {
            yield return null;
        }

        DigAgentSimulationDriver? simulation = null;
        DigWorldInteraction? interaction = null;
        for (int frame = 0; frame < StartupFrameLimit; frame++)
        {
            simulation = UnityEngine.Object.FindFirstObjectByType<DigAgentSimulationDriver>();
            interaction = UnityEngine.Object.FindFirstObjectByType<DigWorldInteraction>();
            if (simulation != null && simulation.enabled
                && interaction != null && interaction.enabled)
            {
                break;
            }

            yield return null;
        }

        Assert.That(simulation, Is.Not.Null, "Simulation driver was not created.");
        Assert.That(simulation!.enabled, Is.True, "Simulation driver did not start.");
        Assert.That(interaction, Is.Not.Null, "World interaction was not created.");
        Assert.That(interaction!.enabled, Is.True, "World interaction did not start.");
        Assert.That(Camera.main, Is.Not.Null, "Main camera was not available.");
        Assert.That(
            UnityEngine.Object.FindFirstObjectByType<DigHudOverlay>(),
            Is.Not.Null,
            "HUD was not created.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<DigAgentVisual>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).Length,
            Is.GreaterThanOrEqualTo(4),
            "Representative residents were not rendered.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<DigWorldRenderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).SingleOrDefault(),
            Is.Not.Null,
            "World renderer was not created.");

        LogAssert.NoUnexpectedReceived();
        WriteEvidence(evidencePath);
    }

    private static string ResolveEvidencePath()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(
            UnityEngine.Application.dataPath,
            "..",
            "..",
            ".."));
        return Path.Combine(
            repositoryRoot,
            "artifacts",
            "unity-tests",
            "runtime",
            "representative-scene.log");
    }

    private static void WriteEvidence(string path)
    {
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Runtime evidence directory is missing.");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(path, new[]
        {
            "status=passed",
            $"scene={MainScenePath}",
            "consoleErrors=0",
            "simulationDriver=enabled",
            "worldInteraction=enabled",
        });
    }
}
}
