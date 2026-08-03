using System;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Domain.Society;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

public abstract partial class DigAgentSimulationDriverBase
{
    internal bool IsHudReady => AgentSession != null && TerrainSession != null;

    internal long CurrentSocietyTick => AgentSession?.SocietyTick ?? 0;

    internal int EffectiveGameSecondsPerRealSecond =>
        Playback.IsPaused
            ? 0
            : GameTimeCadence.EffectiveGameSecondsPerRealSecond(
                Playback.SpeedMultiplier);

    internal GameTimeSnapshot CurrentGameTime =>
        GameTimeCadence.Project(CurrentTick);

    internal SocietySnapshot LoadSocietySnapshot()
    {
        if (AgentSession == null)
        {
            return new SocietySnapshot(
                0,
                Array.Empty<ResidentSocietySnapshot>(),
                Array.Empty<SocialBondSnapshot>());
        }

        return AgentSession.LoadSocietySnapshot();
    }

    internal ResidentRosterViewModel LoadResidentRoster(string? selectedResidentId)
    {
        if (AgentSession == null || TerrainSession == null)
        {
            return new ResidentRosterViewModel(
                Array.Empty<ResidentRosterRowViewModel>(),
                selectedResidentId: null);
        }

        return AgentSession.LoadResidentRoster(
            TerrainSession.LoadJobSnapshots(),
            selectedResidentId);
    }

    internal bool TryGetResidentWorkWindow(
        string residentId,
        out int ticksPerDay,
        out int startTickInclusive,
        out int endTickExclusive)
    {
        if (AgentSession == null)
        {
            ticksPerDay = GameTimeCadence.TicksPerDay;
            startTickInclusive = 0;
            endTickExclusive = GameTimeCadence.TicksPerDay / 2;
            return false;
        }

        return AgentSession.TryGetWorkWindow(
            residentId,
            out ticksPerDay,
            out startTickInclusive,
            out endTickExclusive);
    }

    internal Result SetResidentWorkWindow(
        string residentId,
        int startTickInclusive,
        int endTickExclusive)
    {
        if (AgentSession == null)
        {
            return Result.Failure(NotInitialized);
        }

        return AgentSession.SetWorkRestWindow(
            residentId,
            startTickInclusive,
            endTickExclusive);
    }

    internal bool TryGetResidentAutomaticPlanning(
        string residentId,
        out bool enabled)
    {
        if (AgentSession == null)
        {
            enabled = true;
            return false;
        }

        return AgentSession.TryGetAutomaticPlanning(residentId, out enabled);
    }

    internal Result SetResidentAutomaticPlanning(
        string residentId,
        bool enabled)
    {
        if (AgentSession == null)
        {
            return Result.Failure(NotInitialized);
        }

        return AgentSession.SetAutomaticPlanning(residentId, enabled);
    }
}

}
