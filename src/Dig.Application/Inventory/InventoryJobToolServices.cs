using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public sealed class InventoryAwareJobCandidateProvider : IJobCandidateProvider
{
    private readonly IJobCandidateProvider _inner;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly EquipmentRates _equipmentRates;

    public InventoryAwareJobCandidateProvider(
        IJobCandidateProvider inner,
        IInventoryRepository inventoryRepository,
        EquipmentRates equipmentRates)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _equipmentRates = equipmentRates
            ?? throw new ArgumentNullException(nameof(equipmentRates));
    }

    public IReadOnlyCollection<JobCandidate> GetCandidates(JobSnapshot job, long tick)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        IReadOnlyCollection<JobCandidate> candidates = _inner.GetCandidates(job, tick);
        JobToolKind? preferredKind = job.Definition.PreferredToolKind;
        if (!preferredKind.HasValue)
        {
            return candidates;
        }

        InventorySnapshot snapshot = _inventoryRepository.Get().CreateSnapshot();
        JobCandidate[] enriched = candidates
            .Select(candidate => Enrich(candidate, preferredKind.Value, snapshot))
            .ToArray();
        return new ReadOnlyCollection<JobCandidate>(enriched);
    }

    private JobCandidate Enrich(
        JobCandidate candidate,
        JobToolKind preferredKind,
        InventorySnapshot snapshot)
    {
        ItemStackSnapshot[] equipped = snapshot.Stacks
            .Where(stack => stack.Location == ItemLocation.EquippedBy(candidate.AgentId))
            .ToArray();
        if (equipped.Length > 1)
        {
            throw new InvalidOperationException(
                "A resident cannot have more than one equipped item.");
        }

        ItemStackSnapshot? current = equipped.SingleOrDefault();
        if (IsSafeMatchingTool(current, preferredKind))
        {
            return candidate.WithToolReadiness(
                JobToolReadiness.Equipped,
                current!.StackId);
        }

        bool currentCanBeStored = current is null
            || (current.Quantity == 1 && current.ReservedQuantity == 0);
        if (!currentCanBeStored)
        {
            return candidate.WithToolReadiness(JobToolReadiness.Unavailable);
        }

        ItemStackSnapshot? carried = snapshot.Stacks
            .Where(stack => stack.Location == ItemLocation.InAgent(candidate.AgentId))
            .Where(stack => IsSafeMatchingTool(stack, preferredKind))
            .OrderBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        return carried is null
            ? candidate.WithToolReadiness(JobToolReadiness.Unavailable)
            : candidate.WithToolReadiness(
                JobToolReadiness.SwitchAvailable,
                carried.StackId);
    }

    private bool IsSafeMatchingTool(
        ItemStackSnapshot? stack,
        JobToolKind preferredKind)
    {
        return stack is not null
            && stack.Quantity == 1
            && stack.ReservedQuantity == 0
            && Matches(preferredKind, _equipmentRates.ResolveWorkKind(stack.ItemId));
    }
    private static bool Matches(
        JobToolKind preferredKind,
        EquipmentWorkKind? equipmentWorkKind)
    {
        return preferredKind switch
        {
            JobToolKind.Mining => equipmentWorkKind == EquipmentWorkKind.Mining,
            JobToolKind.Construction =>
                equipmentWorkKind == EquipmentWorkKind.Construction,
            _ => false,
        };
    }
}

public sealed class InventoryJobToolPreparationService : IJobToolPreparationService
{
    private readonly IInventoryRepository _repository;
    private readonly IEventSink _eventSink;

    public InventoryJobToolPreparationService(
        IInventoryRepository repository,
        IEventSink eventSink)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Prepare(EntityId agentId, EntityId toolStackId, long tick)
    {
        InventoryState inventory = _repository.Get();
        Result switched = inventory.SwitchTool(toolStackId, agentId, tick);
        if (switched.IsFailure)
        {
            return switched;
        }

        _repository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
