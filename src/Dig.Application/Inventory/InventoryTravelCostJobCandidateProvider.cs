using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public sealed class InventoryTravelCostJobCandidateProvider : IJobCandidateProvider
{
    private readonly IJobCandidateProvider _inner;
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryTravelCostJobCandidateProvider(
        IJobCandidateProvider inner,
        IInventoryRepository inventoryRepository)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
    }

    public IReadOnlyCollection<JobCandidate> GetCandidates(JobSnapshot job, long tick)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobCandidate[] adjusted = _inner.GetCandidates(job, tick)
            .Select(candidate => Adjust(candidate, inventory))
            .ToArray();
        return new ReadOnlyCollection<JobCandidate>(adjusted);
    }

    private static JobCandidate Adjust(
        JobCandidate candidate,
        InventoryState inventory)
    {
        if (candidate.DistanceCost == 0)
        {
            return candidate;
        }

        int effectiveCost = inventory.ResolveResidentTravelTiming(
            candidate.AgentId,
            candidate.DistanceCost).EffectiveTicks;
        return candidate.WithDistanceCost(effectiveCost);
    }
}

}