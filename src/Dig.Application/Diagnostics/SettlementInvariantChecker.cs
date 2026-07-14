using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;

namespace Dig.Application.Diagnostics;

public sealed class SettlementInvariantChecker
{
    private readonly IAgentRepository _agents;
    private readonly IInventoryRepository _inventory;
    private readonly IStorageRepository _storage;
    private readonly IJobRepository _jobs;
    private readonly IBuildingFacilitiesRepository _facilities;

    public SettlementInvariantChecker(
        IAgentRepository agents,
        IInventoryRepository inventory,
        IStorageRepository storage,
        IJobRepository jobs,
        IBuildingFacilitiesRepository facilities)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
    }

    public SimulationInvariantReport Check(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        List<SimulationInvariantViolation> violations =
            new List<SimulationInvariantViolation>();
        InventoryState inventory = _inventory.Get();
        InventorySnapshot inventorySnapshot = inventory.CreateSnapshot();
        JobSystem jobs = _jobs.Get();
        JobSnapshot[] jobSnapshots = jobs.GetAll().ToArray();
        Dictionary<EntityId, JobSnapshot> jobsById = jobSnapshots.ToDictionary(value => value.Id);
        StorageState storage = _storage.Get();
        BuildingFacilitiesState facilities = _facilities.Get();
        AgentSnapshot[] agents = _agents.GetAll()
            .Select(value => value.CreateSnapshot(tick))
            .ToArray();
        Dictionary<EntityId, AgentSnapshot> agentsById = agents.ToDictionary(value => value.Id);

        CheckInventory(inventorySnapshot, violations);
        CheckJobLedger(jobs, jobsById, violations);
        CheckHauling(inventory, storage, jobSnapshots, violations);
        CheckStorageReservations(storage, jobsById, violations);
        CheckAgentTargets(inventory, facilities, agents, violations);
        CheckFacilityReservations(facilities, agentsById, violations);
        return new SimulationInvariantReport(tick, violations);
    }

    private static void CheckInventory(
        InventorySnapshot inventory,
        ICollection<SimulationInvariantViolation> violations)
    {
        foreach (ItemStackSnapshot stack in inventory.Stacks)
        {
            if (stack.Quantity <= 0)
            {
                Add(
                    violations,
                    "inventory.non_positive_quantity",
                    $"Stack quantity is {stack.Quantity}.",
                    stack.StackId);
            }

            if (stack.Reservations.Any(value => value.Quantity <= 0))
            {
                Add(
                    violations,
                    "inventory.non_positive_reservation",
                    "Stack contains a non-positive reservation.",
                    stack.StackId);
            }

            if (stack.ReservedQuantity > stack.Quantity)
            {
                Add(
                    violations,
                    "inventory.over_reserved",
                    $"Reserved {stack.ReservedQuantity} of {stack.Quantity}.",
                    stack.StackId);
            }

            if (stack.Reservations
                .GroupBy(value => value.JobId)
                .Any(group => group.Count() > 1))
            {
                Add(
                    violations,
                    "inventory.duplicate_reservation_owner",
                    "The same reservation owner appears more than once.",
                    stack.StackId);
            }
        }
    }

    private static void CheckJobLedger(
        JobSystem jobs,
        IReadOnlyDictionary<EntityId, JobSnapshot> jobsById,
        ICollection<SimulationInvariantViolation> violations)
    {
        IReadOnlyList<ReservationSnapshot> reservations = jobs.GetReservations();
        foreach (ReservationSnapshot reservation in reservations)
        {
            if (!jobsById.TryGetValue(reservation.JobId, out JobSnapshot? job))
            {
                Add(
                    violations,
                    "jobs.orphan_reservation",
                    "Reservation references a missing job.",
                    reservation.JobId);
                continue;
            }

            if (job.IsTerminal)
            {
                Add(
                    violations,
                    "jobs.terminal_reservation",
                    "Terminal job still owns a reservation.",
                    job.Id);
            }

            if (job.AssignedAgentId != reservation.AgentId)
            {
                Add(
                    violations,
                    "jobs.agent_mismatch",
                    "Reservation agent differs from the assigned agent.",
                    job.Id);
            }
        }

        foreach (IGrouping<EntityId, ReservationSnapshot> byAgent in reservations
            .GroupBy(value => value.AgentId))
        {
            if (byAgent.Select(value => value.JobId).Distinct().Count() > 1)
            {
                Add(
                    violations,
                    "jobs.agent_multiple_jobs",
                    "One agent owns reservations for multiple jobs.",
                    byAgent.Key);
            }
        }
    }

    private static void CheckHauling(
        InventoryState inventory,
        StorageState storage,
        IEnumerable<JobSnapshot> jobs,
        ICollection<SimulationInvariantViolation> violations)
    {
        foreach (JobSnapshot job in jobs)
        {
            if (job.Definition is not HaulJobDefinition hauling)
            {
                continue;
            }

            ItemStackSnapshot? stack = inventory.GetStack(hauling.SourceStackId);
            int reservedQuantity = stack?.Reservations
                .Where(value => value.JobId == job.Id)
                .Sum(value => value.Quantity) ?? 0;
            StorageReservationSnapshot? destinationReservation =
                hauling.Destination.Kind == ItemLocationKind.Storage
                    ? storage.GetReservation(job.Id)
                    : null;

            if (job.IsTerminal)
            {
                if (reservedQuantity != 0 || destinationReservation.HasValue)
                {
                    Add(
                        violations,
                        "hauling.terminal_external_reservation",
                        "Terminal hauling job still owns external reservations.",
                        job.Id);
                }

                continue;
            }

            if (reservedQuantity != hauling.Quantity)
            {
                Add(
                    violations,
                    "hauling.item_reservation_mismatch",
                    $"Expected {hauling.Quantity}, found {reservedQuantity}.",
                    job.Id);
            }

            if (hauling.Destination.Kind == ItemLocationKind.Storage
                && (!destinationReservation.HasValue
                    || destinationReservation.Value.ZoneId != hauling.Destination.OwnerId
                    || destinationReservation.Value.ItemId != hauling.ItemId
                    || destinationReservation.Value.Quantity != hauling.Quantity))
            {
                Add(
                    violations,
                    "hauling.storage_reservation_mismatch",
                    "Incoming storage reservation does not match hauling definition.",
                    job.Id);
            }
        }
    }

    private static void CheckStorageReservations(
        StorageState storage,
        IReadOnlyDictionary<EntityId, JobSnapshot> jobs,
        ICollection<SimulationInvariantViolation> violations)
    {
        foreach (StorageReservationSnapshot reservation in storage.GetReservations())
        {
            if (!jobs.TryGetValue(reservation.JobId, out JobSnapshot? job)
                || job.IsTerminal
                || job.Definition is not HaulJobDefinition)
            {
                Add(
                    violations,
                    "storage.orphan_incoming",
                    "Incoming capacity reservation has no active hauling job.",
                    reservation.JobId);
            }
        }
    }

    private static void CheckAgentTargets(
        InventoryState inventory,
        BuildingFacilitiesState facilities,
        IEnumerable<AgentSnapshot> agents,
        ICollection<SimulationInvariantViolation> violations)
    {
        foreach (AgentSnapshot agent in agents)
        {
            AgentActivityTarget? target = agent.ActiveAction?.Target;
            if (!target.HasValue)
            {
                continue;
            }

            if (target.Value.Kind == AgentActivityTargetKind.Food)
            {
                ItemStackSnapshot? stack = inventory.GetStack(target.Value.EntityId);
                bool valid = stack is not null && stack.Reservations.Any(value =>
                    value.JobId == agent.Id && value.Quantity >= 1);
                if (!valid)
                {
                    Add(
                        violations,
                        "agents.food_target_unreserved",
                        "Active food action has no matching item reservation.",
                        agent.Id);
                }

                continue;
            }

            BuildingFacilitySnapshot? facility = facilities.Get(target.Value.EntityId);
            BuildingFacilityKind expected = target.Value.Kind == AgentActivityTargetKind.Bed
                ? BuildingFacilityKind.Bed
                : BuildingFacilityKind.Leisure;
            if (facility is null
                || facility.Definition.Kind != expected
                || facility.ReservedAgentId != agent.Id)
            {
                Add(
                    violations,
                    "agents.facility_target_unreserved",
                    "Active facility action has no matching facility reservation.",
                    agent.Id);
            }
        }
    }

    private static void CheckFacilityReservations(
        BuildingFacilitiesState facilities,
        IReadOnlyDictionary<EntityId, AgentSnapshot> agents,
        ICollection<SimulationInvariantViolation> violations)
    {
        foreach (IGrouping<EntityId, BuildingFacilityReservation> duplicate in facilities
            .GetReservations()
            .GroupBy(value => value.AgentId)
            .Where(group => group.Count() > 1))
        {
            Add(
                violations,
                "facilities.agent_multiple_reservations",
                "One resident owns multiple facility reservations.",
                duplicate.Key);
        }

        foreach (BuildingFacilityReservation reservation in facilities.GetReservations())
        {
            if (!agents.TryGetValue(reservation.AgentId, out AgentSnapshot? agent)
                || agent.ActiveAction?.Target?.EntityId != reservation.FacilityId)
            {
                Add(
                    violations,
                    "facilities.orphan_reservation",
                    "Facility reservation has no matching active resident action.",
                    reservation.FacilityId);
            }
        }
    }

    private static void Add(
        ICollection<SimulationInvariantViolation> violations,
        string code,
        string detail,
        EntityId entityId)
    {
        violations.Add(new SimulationInvariantViolation(code, detail, entityId));
    }
}
