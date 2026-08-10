using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private Func<EntityId, EntityId?>? _resolveFreeTimeMeetingPartner;

    internal void BindFreeTimeMeetingSource(
        Func<EntityId, EntityId?> resolvePartner)
    {
        _resolveFreeTimeMeetingPartner = resolvePartner
            ?? throw new ArgumentNullException(nameof(resolvePartner));
    }

    private bool TryPlanResidentFreeTimeMovement(
        AgentViewModel resident,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (_resolveFreeTimeMeetingPartner == null || _productionAgents == null)
        {
            return false;
        }

        EntityId residentId = EntityId.Parse(resident.Id);
        EntityId? partnerId = _resolveFreeTimeMeetingPartner(residentId);
        if (!partnerId.HasValue
            || string.Compare(
                residentId.ToString(),
                partnerId.Value.ToString(),
                StringComparison.Ordinal) < 0)
        {
            // The stable greater id is the follower. The other resident holds the
            // meeting point so both do not continuously chase each other.
            return false;
        }

        AgentState? partner = _productionAgents.Get(partnerId.Value);
        AgentState? state = _productionAgents.Get(residentId);
        if (partner == null || state == null)
        {
            return false;
        }

        CellId start = state.Position;
        CellId anchor = partner.Position;
        if (start.Y == anchor.Y
            && Math.Abs(start.X - anchor.X) + Math.Abs(start.Z - anchor.Z) <= 1)
        {
            return true;
        }

        CellId[] destinations =
        {
            new CellId(anchor.X - 1, anchor.Y, anchor.Z),
            new CellId(anchor.X + 1, anchor.Y, anchor.Z),
            new CellId(anchor.X, anchor.Y, anchor.Z - 1),
            new CellId(anchor.X, anchor.Y, anchor.Z + 1),
        };
        NavigationPathfinder pathfinder = new NavigationPathfinder();
        NavigationPath? selected = null;
        foreach (CellId destination in destinations
            .Where(navigation.IsWalkable)
            .Where(HasFullStandingSupport)
            .OrderBy(value => value))
        {
            PathResult result = pathfinder.FindPath(
                navigation,
                new PathRequest(start, destination, navigation.NavigationVersion));
            if (!result.Succeeded || result.Path == null)
            {
                continue;
            }

            if (selected == null || result.Path.TotalCost < selected.TotalCost)
            {
                selected = result.Path;
            }
        }

        if (selected == null)
        {
            return true;
        }

        movement[resident.Id] = selected.Cells.Count > 1
            ? selected.Cells[1]
            : selected.Cells[0];
        return true;
    }
}

}
