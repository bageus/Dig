using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class UnityRuntimeEvidenceGateTests
{
    [Fact]
    public void Unity_workflow_requires_both_modes_and_machine_readable_evidence()
    {
        string root = FindRepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(
            root,
            ".github",
            "workflows",
            "unity-playmode.yml"));
        string quality = File.ReadAllText(Path.Combine(
            root,
            ".github",
            "workflows",
            "quality.yml"));
        string validator = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "quality",
            "validate_unity_runtime_evidence.py"));

        Assert.Contains("testMode: All", workflow);
        Assert.Contains("Validate executed Unity runtime evidence", workflow);
        Assert.Contains("unity-editmode-playmode-results", workflow);
        Assert.Contains("name: unity-runtime-evidence", workflow);
        Assert.Contains("if-no-files-found: error", workflow);
        Assert.Contains("RepresentativeSceneConsolePlayModeTests", workflow);
        Assert.Contains("UnityProjectEditModeTests", workflow);
        Assert.Contains(
            "validate_unity_runtime_evidence.py --self-test",
            quality);
        Assert.Contains("status\": \"verified", validator);
        Assert.Contains("status\": \"blocked", validator);
        Assert.Contains("no Unity test result XML files were found", validator);
        Assert.Contains("required runtime log is missing", validator);
    }

    [Fact]
    public void Representative_scene_and_editmode_contracts_are_checked_in()
    {
        string root = FindRepositoryRoot();
        string editMode = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Tests",
            "EditMode",
            "UnityProjectEditModeTests.cs"));
        string playMode = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "RepresentativeSceneConsolePlayModeTests.cs"));
        string buildSettings = File.ReadAllText(Path.Combine(
            root,
            "ProjectSettings",
            "EditorBuildSettings.asset"));

        Assert.Contains(
            "Main_scene_is_registered_with_runtime_bootstrap",
            editMode);
        Assert.Contains("EditorBuildSettings.scenes", editMode);
        Assert.Contains("GetComponentsInChildren<DigUnityBootstrap>", editMode);
        Assert.Contains(
            "Main_scene_bootstraps_without_console_errors",
            playMode);
        Assert.Contains("LogAssert.NoUnexpectedReceived", playMode);
        Assert.Contains("status=passed", playMode);
        Assert.Contains("consoleErrors=0", playMode);
        Assert.Contains("DigAgentSimulationDriver? simulation = null", playMode);
        Assert.Contains("DigWorldInteraction? interaction = null", playMode);
        Assert.Contains("simulation!.enabled", playMode);
        Assert.Contains("interaction!.enabled", playMode);
        Assert.Contains("UnityEngine.Application.dataPath", playMode);
        Assert.Contains("path: Assets/Scenes/Main.unity", buildSettings);
        Assert.Contains("enabled: 1", buildSettings);
    }

    [Fact]
    public void Checked_in_playmode_regressions_follow_compilable_api_contracts()
    {
        string root = FindRepositoryRoot();
        string caveRoom = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "CaveRoomReapplyAndMediumPreviewPlayModeTests.cs"));
        string combat = File.ReadAllText(Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "CombatSpatialExecutionPlayModeTests.cs"));

        Assert.Contains("Has.No.Member(completed.Cell)", caveRoom);
        Assert.DoesNotContain("Does.Not.Contain(completed.Cell)", caveRoom);
        Assert.Contains("skills: null", combat);
        Assert.Contains("traits: null", combat);
        Assert.Contains("initialPosition: cell", combat);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
