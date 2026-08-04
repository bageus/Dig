using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatPreemptionUnityRuntimeContractTests
{
    [Fact]
    public void Combat_threat_is_synchronized_before_needs_and_work_continue()
    {
        string runtime = ReadRuntime("DigResidentNeedsRuntime.cs");
        string combat = ReadRuntime("DigAgentSession.CombatPreemption.cs");
        string interruption = ReadRuntime("DigTerrainWorkSession.CombatInterruption.cs");
        string autonomy = ReadSource(
            "src/Dig.Application/Agents/AgentAutonomySystem.cs");

        Assert.Contains("IAgentActionExecutionGate", runtime);
        Assert.Contains("IsResidentCombatActiveOrThreatened", combat);
        Assert.Contains("EnsureAutonomousEnemyIntent", combat);
        Assert.Contains("InterruptResidentForCombat", runtime);
        Assert.Contains("InterruptFoodMeal(\"combat_preempted\"", interruption);
        Assert.Contains("InterruptActiveAction(\"combat_preempted\"", interruption);
        Assert.Contains("CollectAssignedActiveJobs", interruption);
        Assert.Contains("gate.CanExecuteActions(agent, context.Tick)", autonomy);
        Assert.True(
            autonomy.IndexOf("gate.CanExecuteActions", StringComparison.Ordinal)
            < autonomy.IndexOf("agent.HasActiveFoodMeal", StringComparison.Ordinal));
    }

    [Fact]
    public void Combat_interruption_uses_current_production_and_supply_cleanup_signatures()
    {
        string interruption = ReadRuntime(
            "DigTerrainWorkSession.CombatInterruption.cs");
        string direct = ReadRuntime("DigTerrainWorkSession.DirectCommands.cs");

        Assert.Contains(
            "InterruptProductionForDirectCommand(\n"
            + "                        job,\n"
            + "                        production,\n"
            + "                        resident.Id,\n"
            + "                        tick)",
            interruption);
        Assert.Contains("BuildingSupplyJobDefinition =>", interruption);
        Assert.Contains(
            "CancelBuildingSupplyForDirectCommand(\n"
            + "                        job,\n"
            + "                        resident.Id,\n"
            + "                        tick)",
            interruption);
        Assert.Contains(
            "private Result InterruptProductionForDirectCommand(\n"
            + "            JobSnapshot job,\n"
            + "            ProductionWorkJobDefinition production,\n"
            + "            EntityId residentId,\n"
            + "            long tick)",
            direct);
        Assert.Contains(
            "private Result CancelBuildingSupplyForDirectCommand(\n"
            + "            JobSnapshot job,\n"
            + "            EntityId residentId,\n"
            + "            long tick)",
            direct);
    }

    [Fact]
    public void Direct_command_priority_is_checked_before_self_defense_and_autonomy()
    {
        string runtime = ReadRuntime("DigResidentNeedsRuntime.cs");
        string sessionNeeds = ReadRuntime("DigAgentSession.ResidentNeeds.cs");
        string combat = ReadRuntime("DigAgentSession.CombatPreemption.cs");
        string priority = ReadRuntime("DigAgentSession.DirectCommandPriority.cs");
        string terrainPriority = ReadRuntime(
            "DigTerrainWorkSession.DirectCommandPriority.cs");
        string driver = ReadRuntime("DigAgentSimulationDriverBase.cs");

        Assert.Contains("_hasDirectCommandPriority(agent.Id, tick)", runtime);
        Assert.Contains("HasResidentDirectCommandPriority", sessionNeeds);
        Assert.Contains("SuppressResidentCombatForDirectCommand", combat);
        Assert.Contains("BeginResidentDirectCommand", priority);
        Assert.Contains("DisengageResidentForDirectOrder", priority);
        Assert.Contains("CancelExecution", priority);
        Assert.Contains("resident_direct_command_overrode_combat", priority);
        Assert.Contains("CollectAssignedActiveJobs", terrainPriority);
        Assert.Contains("BindDirectCommandPrioritySource", driver);
        Assert.Contains("HasActiveResidentDirectCommand", driver);
        Assert.Contains("AgentSession.BeginResidentDirectCommand", driver);
        Assert.True(
            runtime.IndexOf(
                "_hasDirectCommandPriority(agent.Id, tick)",
                StringComparison.Ordinal)
            < runtime.IndexOf(
                "_isCombatActiveOrThreatened(agent.Id, tick)",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Enemy_sight_loss_and_task_transition_pause_use_authoritative_owners()
    {
        string targeting = ReadSource(
            "src/Dig.Application/Combat/CombatSpatialExecutionHandler.Targeting.cs");
        string agent = ReadSource("src/Dig.Domain/Agents/AgentState.cs");
        string decisions = ReadSource(
            "src/Dig.Domain/Agents/AgentDecisionCandidates.cs");
        string movement = ReadRuntime("DigAgentSession.TunnelMovement.cs");
        string taskTransitions = ReadRuntime("DigAgentSession.TaskTransitions.cs");
        string terrain = ReadRuntime("DigTerrainWorkDirectMovement.cs");
        string driver = ReadRuntime("DigAgentSimulationDriverBase.Loop.cs");

        Assert.Contains("enemy_target_out_of_sight", targeting);
        Assert.DoesNotContain("persistent_aggro_target_tracked", targeting);
        Assert.Contains("LastTaskCompletionTick", agent);
        Assert.Contains("RecordTaskCompletionCore", agent);
        Assert.Contains("taskPauseActive", decisions);
        Assert.Contains("AgentIntentKind.Idle", decisions);
        Assert.Contains("manual_movement_completed", movement);
        Assert.Contains("RecordCompletedResidentTasks", taskTransitions);
        Assert.Contains("_isTaskTransitionPaused", terrain);
        Assert.Contains("CaptureActiveResidentTaskAssignments", driver);
        Assert.Contains("ResolveCompletedResidentTasks", driver);
        Assert.Contains("AgentSession.RecordCompletedResidentTasks", driver);
    }

    private static string ReadRuntime(string file)
    {
        return ReadSource(Path.Combine(
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime",
            file));
    }

    private static string ReadSource(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
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