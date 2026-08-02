using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSpatialUnityRuntimeContractTests
{
    [Fact]
    public void Unity_runtime_uses_spatial_execution_and_one_hover_click_classifier()
    {
        string root = FindRepositoryRoot();
        string session = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.Combat.cs"));
        string interaction = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.Combat.cs"));
        string cursor = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.DirectCommandCursor.cs"));
        string selection = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.Selection.cs"));

        Assert.Contains("CombatSpatialExecutionHandler", session, StringComparison.Ordinal);
        Assert.Contains("TryAdvanceCombat", session, StringComparison.Ordinal);
        Assert.Contains("CanIssuePlayerAttackOrder", interaction, StringComparison.Ordinal);
        Assert.Contains("TryResolveHostileCombatHoverTarget", cursor, StringComparison.Ordinal);
        Assert.Contains("DirectCommandCursorKind.Sword", cursor, StringComparison.Ordinal);
        Assert.Contains("InterruptForCombat", interaction, StringComparison.Ordinal);
        Assert.Contains("CancelPlayerAttackOrder", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveCombatAttackCommand", interaction, StringComparison.Ordinal);
    }

    [Fact]
    public void Cave_monster_runtime_uses_inventory_weapons_health_bars_and_real_agents()
    {
        string root = FindRepositoryRoot();
        string session = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.Combat.cs"));
        string enemies = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.Enemies.cs"));
        string equipment = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSession.CombatEquipment.cs"));
        string interaction = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.Combat.cs"));
        string bootstrap = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigUnityBootstrap.cs"));
        string loop = File.ReadAllText(Path.Combine(root,
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSimulationDriverBase.Loop.cs"));
        string autonomy = File.ReadAllText(Path.Combine(root,
            "src/Dig.Application/Agents/AgentAutonomySystem.cs"));

        Assert.Contains("SeedCaveMonsterPair", session, StringComparison.Ordinal);
        Assert.Contains("CaveMonsterOneId", enemies, StringComparison.Ordinal);
        Assert.Contains("CaveMonsterTwoId", enemies, StringComparison.Ordinal);
        Assert.Contains("EnsureAutonomousEnemyIntent", enemies, StringComparison.Ordinal);
        Assert.Contains("LoadResidentCombatHealthBars", enemies, StringComparison.Ordinal);
        Assert.Contains("HeldItemPurpose.WeaponUse", equipment, StringComparison.Ordinal);
        Assert.Contains("ResidentWeaponDefinitions", equipment, StringComparison.Ordinal);
        Assert.Contains("FindResidentWeapon", equipment, StringComparison.Ordinal);
        Assert.Contains("UnarmedProfileId", equipment, StringComparison.Ordinal);
        Assert.Contains("BindCombatInventory", bootstrap, StringComparison.Ordinal);
        Assert.Contains("LoadCreatures", bootstrap, StringComparison.Ordinal);
        Assert.Contains("RenderCombatHealthBars", bootstrap, StringComparison.Ordinal);
        Assert.Contains("LoadCreatures", loop, StringComparison.Ordinal);
        Assert.Contains("RenderCombatHealthBars", loop, StringComparison.Ordinal);
        Assert.Contains("_isEligible", autonomy, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterHostileCombatant", interaction,
            StringComparison.Ordinal);
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
