using System;
using Dig.Domain.Core;

namespace Dig.Domain.Buildings
{

public readonly struct BuildingFacilityInspection
{
    public BuildingFacilityInspection(
        BuildingFacilityDefinition definition,
        EntityId? reservedAgentId)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ReservedAgentId = reservedAgentId;
    }

    public BuildingFacilityDefinition Definition { get; }

    public EntityId? ReservedAgentId { get; }
}

public interface IBuildingFacilityInspectionVisitor
{
    void VisitFacilityReservation(BuildingFacilityReservation reservation);
}

public sealed partial class BuildingFacilitiesState
{
    public bool TryGetInspection(
        EntityId facilityId,
        out BuildingFacilityInspection inspection)
    {
        if (!_facilities.TryGetValue(facilityId, out BuildingFacilityDefinition? definition))
        {
            inspection = default;
            return false;
        }

        EntityId? agentId = _reservations.TryGetValue(
            facilityId,
            out BuildingFacilityReservation reservation)
            ? reservation.AgentId
            : null;
        inspection = new BuildingFacilityInspection(definition, agentId);
        return true;
    }

    public void VisitInspection(IBuildingFacilityInspectionVisitor visitor)
    {
        if (visitor is null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        foreach (BuildingFacilityReservation reservation in _reservations.Values)
        {
            visitor.VisitFacilityReservation(reservation);
        }
    }
}
}
