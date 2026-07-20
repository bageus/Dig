using Dig.Domain.Core;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

public abstract partial class DigAgentSimulationDriverBase
{
    internal ResidentRosterViewModel LoadResidentRoster(string? selectedResidentId)
    {
        return AgentSession!.LoadResidentRoster(
            TerrainSession!.LoadJobSnapshots(),
            selectedResidentId);
    }

    internal bool TryGetResidentWorkWindow(
        string residentId,
        out int ticksPerDay,
        out int startTickInclusive,
        out int endTickExclusive)
    {
        return AgentSession!.TryGetWorkWindow(
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
        return AgentSession!.SetWorkRestWindow(
            residentId,
            startTickInclusive,
            endTickExclusive);
    }
}

}