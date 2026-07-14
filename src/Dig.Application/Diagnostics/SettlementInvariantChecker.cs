using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;

namespace Dig.Application.Diagnostics
{

public sealed partial class SettlementInvariantChecker :
    IInventoryInspectionVisitor,
    IJobInspectionVisitor,
    IStorageInspectionVisitor,
    IBuildingFacilityInspectionVisitor
{
    private readonly IAgentRepository _agents;
    private readonly IInventoryRepository _inventory;
    private readonly IStorageRepository _storage;
    private readonly IJobRepository _jobs;
    private readonly IBuildingFacilitiesRepository _facilities;
    private readonly List<SimulationInvariantViolation> _violations =
        new List<SimulationInvariantViolation>();
    private readonly Dictionary<EntityId, EntityId> _jobByAgent =
        new Dictionary<EntityId, EntityId>();
    private readonly HashSet<EntityId> _facilityAgents = new HashSet<EntityId>();
    private InventoryState? _currentInventory;
    private StorageState? _currentStorage;
    private JobSystem? _currentJobs;
    private BuildingFacilitiesState? _currentFacilities;

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

        _violations.Clear();
        _jobByAgent.Clear();
        _facilityAgents.Clear();
        _currentInventory = _inventory.Get();
        _currentStorage = _storage.Get();
        _currentJobs = _jobs.Get();
        _currentFacilities = _facilities.Get();

        _currentInventory.VisitInspection(this);
        _currentJobs.VisitInspection(this);
        _currentStorage.VisitInspection(this);
        CheckAgentTargets(tick);
        _currentFacilities.VisitInspection(this);

        SimulationInvariantReport report = new SimulationInvariantReport(tick, _violations);
        _currentInventory = null;
        _currentStorage = null;
        _currentJobs = null;
        _currentFacilities = null;
        return report;
    }

    public void VisitStack(InventoryStackInspection stack)
    {
        if (stack.Quantity <= 0)
        {
            Add(
                "inventory.non_positive_quantity",
                $"Stack quantity is {stack.Quantity}.",
                stack.StackId);
        }

        if (stack.ReservedQuantity > stack.Quantity)
        {
            Add(
                "inventory.over_reserved",
                $"Reserved {stack.ReservedQuantity} of {stack.Quantity}.",
                stack.StackId);
        }
    }

    public void VisitReservation(EntityId stackId, EntityId ownerId, int quantity)
    {
        if (quantity <= 0)
        {
            Add(
                "inventory.non_positive_reservation",
                "Stack contains a non-positive reservation.",
                stackId);
        }
    }

    public void VisitJob(JobInspection job)
    {
        if (job.Definition is not HaulJobDefinition hauling)
        {
            return;
        }

        InventoryState inventory = Require(_currentInventory);
        StorageState storage = Require(_currentStorage);
        int reservedQuantity = inventory.GetReservedQuantity(
            hauling.SourceStackId,
            job.Id);
        StorageReservationSnapshot? destinationReservation =
            hauling.Destination.Kind == ItemLocationKind.Storage
                ? storage.GetReservation(job.Id)
                : null;

        if (job.IsTerminal)
        {
            if (reservedQuantity != 0 || destinationReservation.HasValue)
            {
                Add(
                    "hauling.terminal_external_reservation",
                    "Terminal hauling job still owns external reservations.",
                    job.Id);
            }

            return;
        }

        if (reservedQuantity != hauling.Quantity)
        {
            Add(
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
                "hauling.storage_reservation_mismatch",
                "Incoming storage reservation does not match hauling definition.",
                job.Id);
        }
    }

    public void VisitJobReservation(ReservationSnapshot reservation)
    {
        JobSystem jobs = Require(_currentJobs);
        if (!jobs.TryGetInspection(reservation.JobId, out JobInspection job))
        {
            Add(
                "jobs.orphan_reservation",
                "Reservation references a missing job.",
                reservation.JobId);
            return;
        }

        if (job.IsTerminal)
        {
            Add(
                "jobs.terminal_reservation",
                "Terminal job still owns a reservation.",
                job.Id);
        }

        if (job.AssignedAgentId != reservation.AgentId)
        {
            Add(
                "jobs.agent_mismatch",
                "Reservation agent differs from the assigned agent.",
                job.Id);
        }

        if (_jobByAgent.TryGetValue(reservation.AgentId, out EntityId existingJobId))
        {
            if (existingJobId != reservation.JobId)
            {
                Add(
                    "jobs.agent_multiple_jobs",
                    "One agent owns reservations for multiple jobs.",
                    reservation.AgentId);
            }
        }
        else
        {
            _jobByAgent.Add(reservation.AgentId, reservation.JobId);
        }
    }

    public void VisitStorageReservation(StorageReservationSnapshot reservation)
    {
        JobSystem jobs = Require(_currentJobs);
        if (!jobs.TryGetInspection(reservation.JobId, out JobInspection job)
            || job.IsTerminal
            || job.Definition is not HaulJobDefinition)
        {
            Add(
                "storage.orphan_incoming",
                "Incoming capacity reservation has no active hauling job.",
                reservation.JobId);
        }
    }

    private void Add(string code, string detail, EntityId entityId)
    {
        _violations.Add(new SimulationInvariantViolation(code, detail, entityId));
    }

    private static T Require<T>(T? value)
        where T : class
    {
        return value ?? throw new InvalidOperationException(
            "Invariant inspection state is not initialized.");
    }
}
}
