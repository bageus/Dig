using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Buildings;

public enum BuildingFacilityKind
{
    Bed = 0,
    Leisure = 1,
}

public sealed class BuildingFacilityDefinition
{
    public BuildingFacilityDefinition(
        EntityId id,
        EntityId buildingId,
        BuildingFacilityKind kind,
        CellId position)
    {
        if (id.IsEmpty || buildingId.IsEmpty)
        {
            throw new ArgumentException("Facility and building ids cannot be empty.");
        }

        if (!Enum.IsDefined(typeof(BuildingFacilityKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Id = id;
        BuildingId = buildingId;
        Kind = kind;
        Position = position;
    }

    public EntityId Id { get; }

    public EntityId BuildingId { get; }

    public BuildingFacilityKind Kind { get; }

    public CellId Position { get; }
}

public readonly struct BuildingFacilityReservation
{
    public BuildingFacilityReservation(EntityId facilityId, EntityId agentId)
    {
        if (facilityId.IsEmpty || agentId.IsEmpty)
        {
            throw new ArgumentException("Facility reservation ids cannot be empty.");
        }

        FacilityId = facilityId;
        AgentId = agentId;
    }

    public EntityId FacilityId { get; }

    public EntityId AgentId { get; }
}

public sealed class BuildingFacilitySnapshot
{
    public BuildingFacilitySnapshot(
        BuildingFacilityDefinition definition,
        EntityId? reservedAgentId)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ReservedAgentId = reservedAgentId;
    }

    public BuildingFacilityDefinition Definition { get; }

    public EntityId? ReservedAgentId { get; }

    public bool IsAvailable => !ReservedAgentId.HasValue;
}

public sealed class BuildingFacilityReservationChanged : IDomainEvent
{
    public BuildingFacilityReservationChanged(
        long tick,
        EntityId facilityId,
        EntityId? agentId)
    {
        Tick = tick;
        FacilityId = facilityId;
        AgentId = agentId;
    }

    public long Tick { get; }

    public EntityId FacilityId { get; }

    public EntityId? AgentId { get; }
}

public static class BuildingFacilityErrors
{
    public static readonly DomainError AlreadyExists = new DomainError(
        "buildings.facility.already_exists",
        "A facility with the same id already exists.");

    public static readonly DomainError NotFound = new DomainError(
        "buildings.facility.not_found",
        "The requested building facility does not exist.");

    public static readonly DomainError AlreadyReserved = new DomainError(
        "buildings.facility.already_reserved",
        "The requested building facility is reserved by another resident.");

    public static readonly DomainError AgentAlreadyReserved = new DomainError(
        "buildings.facility.agent_already_reserved",
        "The resident already owns another building facility reservation.");
}

public sealed class BuildingFacilitiesState : AggregateRoot
{
    private readonly Dictionary<EntityId, BuildingFacilityDefinition> _facilities =
        new Dictionary<EntityId, BuildingFacilityDefinition>();
    private readonly Dictionary<EntityId, BuildingFacilityReservation> _reservations =
        new Dictionary<EntityId, BuildingFacilityReservation>();

    public long Version { get; private set; }

    public Result Add(BuildingFacilityDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (!_facilities.TryAdd(definition.Id, definition))
        {
            return Result.Failure(BuildingFacilityErrors.AlreadyExists);
        }

        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result Reserve(EntityId facilityId, EntityId agentId, long tick)
    {
        ValidateTick(tick);
        ValidateIds(facilityId, agentId);
        if (!_facilities.ContainsKey(facilityId))
        {
            return Result.Failure(BuildingFacilityErrors.NotFound);
        }

        if (_reservations.TryGetValue(facilityId, out BuildingFacilityReservation existing))
        {
            return existing.AgentId == agentId
                ? Result.Success()
                : Result.Failure(BuildingFacilityErrors.AlreadyReserved);
        }

        if (_reservations.Values.Any(value => value.AgentId == agentId))
        {
            return Result.Failure(BuildingFacilityErrors.AgentAlreadyReserved);
        }

        _reservations.Add(
            facilityId,
            new BuildingFacilityReservation(facilityId, agentId));
        Version = checked(Version + 1);
        Raise(new BuildingFacilityReservationChanged(tick, facilityId, agentId));
        return Result.Success();
    }

    public int ReleaseForAgent(EntityId agentId, long tick)
    {
        ValidateTick(tick);
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        EntityId[] facilityIds = _reservations.Values
            .Where(value => value.AgentId == agentId)
            .Select(value => value.FacilityId)
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (EntityId facilityId in facilityIds)
        {
            _reservations.Remove(facilityId);
            Raise(new BuildingFacilityReservationChanged(tick, facilityId, null));
        }

        if (facilityIds.Length > 0)
        {
            Version = checked(Version + 1);
        }

        return facilityIds.Length;
    }

    public BuildingFacilitySnapshot? Get(EntityId facilityId)
    {
        if (!_facilities.TryGetValue(facilityId, out BuildingFacilityDefinition? definition))
        {
            return null;
        }

        EntityId? agentId = _reservations.TryGetValue(
            facilityId,
            out BuildingFacilityReservation reservation)
            ? reservation.AgentId
            : null;
        return new BuildingFacilitySnapshot(definition, agentId);
    }

    public BuildingFacilitySnapshot? GetForAgent(EntityId agentId)
    {
        BuildingFacilityReservation? reservation = _reservations.Values
            .Where(value => value.AgentId == agentId)
            .Cast<BuildingFacilityReservation?>()
            .FirstOrDefault();
        return reservation.HasValue ? Get(reservation.Value.FacilityId) : null;
    }

    public IReadOnlyList<BuildingFacilitySnapshot> FindAvailable(
        BuildingFacilityKind kind,
        EntityId? includeAgentId = null)
    {
        BuildingFacilitySnapshot[] values = _facilities.Values
            .Where(value => value.Kind == kind)
            .Select(value => Get(value.Id)!)
            .Where(value => value.IsAvailable
                || (includeAgentId.HasValue
                    && value.ReservedAgentId == includeAgentId.Value))
            .OrderBy(value => value.Definition.Position)
            .ThenBy(value => value.Definition.BuildingId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Definition.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<BuildingFacilitySnapshot>(values);
    }

    public IReadOnlyList<BuildingFacilityReservation> GetReservations()
    {
        return new ReadOnlyCollection<BuildingFacilityReservation>(_reservations.Values
            .OrderBy(value => value.FacilityId.ToString(), StringComparer.Ordinal)
            .ToArray());
    }

    private static void ValidateIds(EntityId facilityId, EntityId agentId)
    {
        if (facilityId.IsEmpty || agentId.IsEmpty)
        {
            throw new ArgumentException("Facility and agent ids cannot be empty.");
        }
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
