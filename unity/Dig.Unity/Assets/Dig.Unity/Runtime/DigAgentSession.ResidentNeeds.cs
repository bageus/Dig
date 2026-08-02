using System;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    public TimeSpan TickDuration => _simulationState.Clock.TickDuration;

    internal void BindResidentNeedsRuntime(DigTerrainWorkSession terrain)
    {
        _residentNeedsRuntime.Bind(
            terrain,
            IsResidentCombatActiveOrThreatened,
            HasResidentDirectCommandPriority);
    }
}

}
