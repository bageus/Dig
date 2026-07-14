using Dig.Domain.Core;

namespace Dig.Domain.Buildings;

public sealed partial class BuildingFacilitiesState
{
    public bool HasAvailable(
        BuildingFacilityKind kind,
        EntityId includeAgentId)
    {
        if (includeAgentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(includeAgentId));
        }

        foreach (BuildingFacilityDefinition definition in _facilities.Values)
        {
            if (definition.Kind != kind)
            {
                continue;
            }

            if (!_reservations.TryGetValue(
                    definition.Id,
                    out BuildingFacilityReservation reservation)
                || reservation.AgentId == includeAgentId)
            {
                return true;
            }
        }

        return false;
    }

    public EntityId? FindFirstAvailableId(
        BuildingFacilityKind kind,
        EntityId includeAgentId)
    {
        if (includeAgentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(includeAgentId));
        }

        BuildingFacilityDefinition? selected = null;
        foreach (BuildingFacilityDefinition definition in _facilities.Values)
        {
            if (definition.Kind != kind)
            {
                continue;
            }

            if (_reservations.TryGetValue(
                    definition.Id,
                    out BuildingFacilityReservation reservation)
                && reservation.AgentId != includeAgentId)
            {
                continue;
            }

            if (selected is null || CompareDefinitions(definition, selected) < 0)
            {
                selected = definition;
            }
        }

        return selected?.Id;
    }

    public bool IsReservedBy(
        EntityId facilityId,
        EntityId agentId,
        BuildingFacilityKind expectedKind)
    {
        if (!_facilities.TryGetValue(
                facilityId,
                out BuildingFacilityDefinition? definition)
            || definition.Kind != expectedKind)
        {
            return false;
        }

        return _reservations.TryGetValue(
            facilityId,
            out BuildingFacilityReservation reservation)
            && reservation.AgentId == agentId;
    }

    private static int CompareDefinitions(
        BuildingFacilityDefinition left,
        BuildingFacilityDefinition right)
    {
        int positionComparison = left.Position.CompareTo(right.Position);
        if (positionComparison != 0)
        {
            return positionComparison;
        }

        int buildingComparison = string.Compare(
            left.BuildingId.ToString(),
            right.BuildingId.ToString(),
            StringComparison.Ordinal);
        return buildingComparison != 0
            ? buildingComparison
            : string.Compare(
                left.Id.ToString(),
                right.Id.ToString(),
                StringComparison.Ordinal);
    }
}
