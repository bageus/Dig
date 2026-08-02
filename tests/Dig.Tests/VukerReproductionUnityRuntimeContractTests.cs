using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class VukerReproductionUnityRuntimeContractTests
{
    [Fact]
    public void RuntimeAdvancesBirthsAndExcludesChildrenAndTamedVukersFromCombat()
    {
        string session = ReadRuntime("DigAgentSession.VukerEcology.cs");
        string enemies = ReadRuntime("DigAgentSession.Enemies.cs");
        string movement = ReadRuntime("DigAgentSession.VukerMovement.cs");

        Assert.Contains("VukerBirthPlanner", session, StringComparison.Ordinal);
        Assert.Contains("CommitBirth", session, StringComparison.Ordinal);
        Assert.Contains("AdvanceVukerEcology", session, StringComparison.Ordinal);
        Assert.Contains("CanVukerInitiateCombat", enemies, StringComparison.Ordinal);
        Assert.Contains("TryAdvanceTamedVukerAutoReturn", enemies,
            StringComparison.Ordinal);
        Assert.Contains("IsMovementStepDue", movement, StringComparison.Ordinal);
        Assert.Contains("ResidentMovementCommandSource.Automatic", movement,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AltLeftClickUsesResidentApproachAndExactlyOnceTameCommit()
    {
        string interaction = ReadRuntime("DigWorldInteraction.Vukers.cs");
        string kidnap = ReadRuntime("DigAgentSession.VukerKidnap.cs");
        string cursor = ReadRuntime("DigWorldInteraction.DirectCommandCursor.cs");

        Assert.Contains("altPressed", interaction, StringComparison.Ordinal);
        Assert.Contains("RequestVukerKidnap", interaction, StringComparison.Ordinal);
        Assert.Contains("PrepareResidentsForDirectCommand", interaction,
            StringComparison.Ordinal);
        Assert.Contains("MoveResidentThroughTunnel", kidnap, StringComparison.Ordinal);
        Assert.Contains("ReserveKidnap", kidnap, StringComparison.Ordinal);
        Assert.Contains("CommitTame", kidnap, StringComparison.Ordinal);
        Assert.Contains("FactionState", kidnap, StringComparison.Ordinal);
        Assert.Contains("TryResolveVukerKidnapHoverTarget", cursor,
            StringComparison.Ordinal);
        Assert.Contains("DirectCommandCursorKind.Pickup", cursor,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CheckedInPlayModeCoversBirthNoCombatKidnapMovementAndMaturity()
    {
        string playMode = Read(
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Tests", "PlayMode",
            "VukerReproductionPlayModeTests.cs");

        Assert.Contains("ReproductionCooldownTicks", playMode, StringComparison.Ordinal);
        Assert.Contains("VukerLifecycleStage.Child", playMode, StringComparison.Ordinal);
        Assert.Contains("GetCombatIntent(child.EntityId), Is.Null", playMode,
            StringComparison.Ordinal);
        Assert.Contains("RequestVukerKidnap", playMode, StringComparison.Ordinal);
        Assert.Contains("VukerDisposition.Tamed", playMode, StringComparison.Ordinal);
        Assert.Contains("MoveTamedVukerThroughTunnel", playMode,
            StringComparison.Ordinal);
        Assert.Contains("VukerLifecycleStage.Adult", playMode, StringComparison.Ordinal);
    }

    private static string ReadRuntime(string file) => Read(
        "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime", file);

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
        }

        return File.ReadAllText(path);
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
