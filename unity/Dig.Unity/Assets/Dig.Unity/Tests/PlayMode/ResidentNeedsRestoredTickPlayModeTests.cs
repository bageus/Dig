using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ResidentNeedsRestoredTickPlayModeTests
{
    [Test]
    public void Restored_simulation_clock_continues_needs_on_the_next_tick()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        SimulationState simulationState =
            ResidentNeedsRuntimePlayModeHarness.GetField<SimulationState>(
                runtime.Residents,
                "_simulationState");
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        const long restoredTick = 10;

        while (simulationState.Clock.Tick < restoredTick)
        {
            simulationState.Clock.AdvanceOneTick();
        }

        foreach (AgentState resident in runtime.Residents.Repository.GetAll())
        {
            Result restored = resident.AdvanceNeeds(policy, restoredTick);
            Assert.That(
                restored.IsSuccess,
                Is.True,
                restored.Error?.ToString());
            runtime.Residents.Repository.Save(resident);
        }

        Assert.DoesNotThrow(() =>
        {
            Result advanced = runtime.Residents.Advance();
            Assert.That(
                advanced.IsSuccess,
                Is.True,
                advanced.Error?.ToString());
        });

        Assert.That(runtime.Residents.Tick, Is.EqualTo(restoredTick + 1));
        Assert.That(simulationState.Clock.Tick, Is.EqualTo(restoredTick + 1));
    }
}

}
