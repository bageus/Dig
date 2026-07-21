using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.Agents
{

internal sealed partial class AgentSkillSet
{
    public SkillRedistributionReport Apply(SkillGrantBundle bundle)
    {
        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }

        EnsureCatalogProgression();
        int sumBefore = _levels.Values.Sum();
        if (_appliedSources.Contains(bundle.IdempotencyKey))
        {
            return CreateDuplicateReport(bundle, sumBefore);
        }

        Dictionary<AgentSkillId, int> eligible = bundle.Grants.ToDictionary(
            value => value.SkillId,
            value => Math.Min(
                value.RequestedUnits,
                AgentSkillCatalog.IndividualMaximumUnits - _levels[value.SkillId]));
        int requestedTotal = eligible.Values.Sum();
        HashSet<AgentSkillId> recipients = new HashSet<AgentSkillId>(eligible
            .Where(value => value.Value > 0)
            .Select(value => value.Key));
        SkillAllocationWeight[] donors = _levels
            .Where(value => value.Value > 0 && !recipients.Contains(value.Key))
            .Select(value => new SkillAllocationWeight(value.Key, value.Value))
            .ToArray();
        int free = Math.Max(0, TotalCapacityUnits - sumBefore);
        int donorTotal = donors.Sum(value => value.Weight);
        int possible = Math.Min(requestedTotal, checked(free + donorTotal));
        IReadOnlyDictionary<AgentSkillId, SkillAllocation> gains =
            ProportionalSkillAllocator.Allocate(
                possible,
                eligible.Select(value => new SkillAllocationWeight(
                    value.Key, value.Value)));
        int freeApplied = Math.Min(possible, free);
        IReadOnlyDictionary<AgentSkillId, SkillAllocation> freeGains =
            ProportionalSkillAllocator.Allocate(
                freeApplied,
                gains.Select(value => new SkillAllocationWeight(
                    value.Key, value.Value.Units)));
        int overflow = possible - freeApplied;
        IReadOnlyDictionary<AgentSkillId, SkillAllocation> losses =
            ProportionalSkillAllocator.Allocate(overflow, donors);

        foreach (SkillAllocation loss in losses.Values)
        {
            _levels[loss.SkillId] = checked(_levels[loss.SkillId] - loss.Units);
        }

        foreach (SkillAllocation gain in gains.Values)
        {
            _levels[gain.SkillId] = checked(_levels[gain.SkillId] + gain.Units);
        }

        _snapshot = null;
        _appliedSources.Add(bundle.IdempotencyKey);
        int sumAfter = _levels.Values.Sum();
        if (sumAfter > TotalCapacityUnits
            || sumAfter != checked(sumBefore + possible - overflow))
        {
            throw new InvalidOperationException(
                "Skill redistribution did not preserve the capacity invariant.");
        }

        LastReport = new SkillRedistributionReport(
            bundle.AgentId,
            bundle.SourceKind,
            bundle.SourceId,
            bundle.Tick,
            TotalCapacityUnits,
            sumBefore,
            sumAfter,
            freeApplied,
            overflow,
            wasAlreadyApplied: false,
            BuildGrantApplications(bundle, eligible, gains, freeGains),
            BuildDonorLosses(donors, losses),
            CreateSnapshot());
        return LastReport;
    }

    private SkillRedistributionReport CreateDuplicateReport(
        SkillGrantBundle bundle,
        int currentSum)
    {
        SkillGrantApplication[] grants = bundle.Grants
            .Select(value => new SkillGrantApplication(
                value.SkillId,
                value.RequestedUnits,
                Math.Min(
                    value.RequestedUnits,
                    AgentSkillCatalog.IndividualMaximumUnits - _levels[value.SkillId]),
                appliedUnits: 0,
                freeCapacityUnits: 0,
                receivedRoundingUnit: false))
            .ToArray();
        return new SkillRedistributionReport(
            bundle.AgentId,
            bundle.SourceKind,
            bundle.SourceId,
            bundle.Tick,
            TotalCapacityUnits,
            currentSum,
            currentSum,
            freeCapacityGainUnits: 0,
            overflowUnits: 0,
            wasAlreadyApplied: true,
            grants,
            Array.Empty<SkillDonorLoss>(),
            CreateSnapshot());
    }

    private static IEnumerable<SkillGrantApplication> BuildGrantApplications(
        SkillGrantBundle bundle,
        IReadOnlyDictionary<AgentSkillId, int> eligible,
        IReadOnlyDictionary<AgentSkillId, SkillAllocation> gains,
        IReadOnlyDictionary<AgentSkillId, SkillAllocation> freeGains)
    {
        foreach (SkillGrant grant in bundle.Grants)
        {
            gains.TryGetValue(grant.SkillId, out SkillAllocation gain);
            freeGains.TryGetValue(grant.SkillId, out SkillAllocation free);
            yield return new SkillGrantApplication(
                grant.SkillId,
                grant.RequestedUnits,
                eligible[grant.SkillId],
                gain.Units,
                free.Units,
                gain.ReceivedRoundingUnit);
        }
    }

    private static IEnumerable<SkillDonorLoss> BuildDonorLosses(
        IEnumerable<SkillAllocationWeight> donors,
        IReadOnlyDictionary<AgentSkillId, SkillAllocation> losses)
    {
        foreach (SkillAllocationWeight donor in donors.OrderBy(value => value.SkillId))
        {
            SkillAllocation loss = losses[donor.SkillId];
            yield return new SkillDonorLoss(
                donor.SkillId,
                donor.Weight,
                loss.Units,
                loss.FractionalRemainder,
                loss.ReceivedRoundingUnit);
        }
    }
}

}
