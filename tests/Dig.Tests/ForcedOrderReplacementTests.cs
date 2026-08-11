using System;
using System.IO;
using Dig.Domain.Agents;
using Xunit;

namespace Dig.Tests
{
    public sealed class ForcedOrderReplacementTests
    {
        [Fact]
        public void Prepare_for_forced_order_clears_action_and_player_order_idempotently()
        {
            AgentState agent = AgentTestFactory.CreateAgent(
                nutrition: 8_000,
                alertness: 8_000,
                mood: 8_000);
            AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
            PlayerOrder previousOrder = new PlayerOrder(
                "order-tunnel-a",
                "Dig tunnel A",
                priority: 10_000,
                issuedTick: 0,
                expiresTick: 20);

            Assert.True(agent.SetPlayerOrder(previousOrder, tick: 0).IsSuccess);
            Assert.True(agent.ApplyDecision(
                AgentTestFactory.CreateForcedDecision(
                    AgentIntentKind.PlayerOrder,
                    tick: 0,
                    playerOrderId: previousOrder.Id),
                policy,
                tick: 0).IsSuccess);

            AgentSnapshot beforeReplacement = agent.CreateSnapshot(0);
            Assert.NotNull(beforeReplacement.ActiveAction);
            Assert.NotNull(beforeReplacement.PlayerOrder);

            Assert.True(agent.PrepareForForcedOrder(
                "direct_command_replaced",
                tick: 1).IsSuccess);

            AgentSnapshot replaced = agent.CreateSnapshot(1);
            Assert.Null(replaced.ActiveAction);
            Assert.Null(replaced.PlayerOrder);
            Assert.True(agent.PrepareForForcedOrder(
                "direct_command_replaced",
                tick: 1).IsSuccess);
        }

        [Fact]
        public void Runtime_direct_command_boundary_clears_domain_state_and_releases_jobs()
        {
            string root = FindRepositoryRoot();
            string domain = File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Domain",
                "Agents",
                "AgentState.ForcedOrders.cs"));
            string runtime = Path.Combine(
                root,
                "Assets",
                "Dig.Unity",
                "Runtime");
            string direct = File.ReadAllText(Path.Combine(
                runtime,
                "DigTerrainWorkSession.DirectCommands.cs"));
            string movement = File.ReadAllText(Path.Combine(
                runtime,
                "DigWorldInteraction.TunnelMovement.cs"));
            string consumables = File.ReadAllText(Path.Combine(
                runtime,
                "DigResidentInventory.Consumables.cs"));
            string planning = File.ReadAllText(Path.Combine(
                root,
                "src",
                "Dig.Presentation.Abstractions",
                "Agents",
                "AgentViewModel.cs"));

            Assert.Contains("InterruptActiveAction(reason.Trim(), tick)", domain);
            Assert.Contains("ClearPlayerOrder(tick)", domain);
            Assert.Contains("PrepareAgentStateForDirectCommand", direct);
            Assert.Contains("resident.PrepareForForcedOrder(", direct);
            Assert.Contains("CollectAssignedActiveJobs", direct);
            Assert.Contains("CancelPickupForDirectCommand", direct);
            Assert.Contains("CancelMushroomForDirectCommand", direct);
            Assert.Contains("CancelBarrelForDirectCommand", direct);
            Assert.Contains("InterruptProductionForDirectCommand", direct);
            Assert.Contains("CancelBuildingSupplyForDirectCommand", direct);
            Assert.Contains("CancelProductionPackageUseForDirectCommand", direct);
            Assert.Contains("ReleaseDigWorkForDirectCommand", direct);
            Assert.Contains("PrepareResidentsForDirectCommand", movement);
            Assert.Contains("PrepareExplicitExcavationResidents", movement);
            Assert.Contains("if (directCommand)", consumables);
            Assert.Contains("PrepareResidentsForDirectCommand(", consumables);
            Assert.Contains("!string.Equals(ActiveIntent, \"Eat\"", planning);
            Assert.Contains("!string.Equals(ActiveIntent, \"Sleep\"", planning);
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
