using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public enum SkillGrantSourceKind
{
    JobCompleted = 0,
    ProductionCommitted = 1,
    CombatHit = 2,
    ShieldDefense = 3,
    ServiceCompleted = 4,
    TrainingCompleted = 5,
    Migration = 6,
}

public readonly struct SkillGrant
{
    public SkillGrant(AgentSkillId skillId, int requestedUnits)
    {
        if (!AgentSkillCatalog.Contains(skillId))
        {
            throw new ArgumentException(
                $"Unknown authoritative skill '{skillId}'.",
                nameof(skillId));
        }

        if (requestedUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedUnits));
        }

        SkillId = skillId;
        RequestedUnits = requestedUnits;
    }

    public AgentSkillId SkillId { get; }
    public int RequestedUnits { get; }
}

public sealed class SkillGrantBundle
{
    public SkillGrantBundle(
        EntityId agentId,
        SkillGrantSourceKind sourceKind,
        string sourceId,
        long tick,
        IEnumerable<SkillGrant> grants)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Skill source id is required.", nameof(sourceId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (grants is null)
        {
            throw new ArgumentNullException(nameof(grants));
        }

        SkillGrant[] normalized = grants
            .GroupBy(value => value.SkillId)
            .Select(group => new SkillGrant(
                group.Key,
                checked(group.Sum(value => value.RequestedUnits))))
            .OrderBy(value => value.SkillId)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A skill bundle needs at least one grant.", nameof(grants));
        }

        AgentId = agentId;
        SourceKind = sourceKind;
        SourceId = sourceId.Trim();
        Tick = tick;
        Grants = new ReadOnlyCollection<SkillGrant>(normalized);
    }

    public EntityId AgentId { get; }
    public SkillGrantSourceKind SourceKind { get; }
    public string SourceId { get; }
    public long Tick { get; }
    public IReadOnlyList<SkillGrant> Grants { get; }
    public string IdempotencyKey => $"{(int)SourceKind}:{SourceId}";
}

public readonly struct SkillGrantApplication
{
    public SkillGrantApplication(
        AgentSkillId skillId,
        int requestedUnits,
        int eligibleUnits,
        int appliedUnits,
        int freeCapacityUnits,
        bool receivedRoundingUnit)
    {
        if (requestedUnits <= 0
            || eligibleUnits < 0
            || eligibleUnits > requestedUnits
            || appliedUnits < 0
            || appliedUnits > eligibleUnits
            || freeCapacityUnits < 0
            || freeCapacityUnits > appliedUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedUnits));
        }

        SkillId = skillId;
        RequestedUnits = requestedUnits;
        EligibleUnits = eligibleUnits;
        AppliedUnits = appliedUnits;
        FreeCapacityUnits = freeCapacityUnits;
        ReceivedRoundingUnit = receivedRoundingUnit;
    }

    public AgentSkillId SkillId { get; }
    public int RequestedUnits { get; }
    public int EligibleUnits { get; }
    public int AppliedUnits { get; }
    public int FreeCapacityUnits { get; }
    public int RejectedUnits => RequestedUnits - AppliedUnits;
    public bool ReceivedRoundingUnit { get; }
}

public readonly struct SkillDonorLoss
{
    public SkillDonorLoss(
        AgentSkillId skillId,
        int valueBeforeUnits,
        int lossUnits,
        long fractionalRemainder,
        bool receivedRoundingUnit)
    {
        if (valueBeforeUnits <= 0
            || lossUnits < 0
            || lossUnits > valueBeforeUnits
            || fractionalRemainder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valueBeforeUnits));
        }

        SkillId = skillId;
        ValueBeforeUnits = valueBeforeUnits;
        LossUnits = lossUnits;
        FractionalRemainder = fractionalRemainder;
        ReceivedRoundingUnit = receivedRoundingUnit;
    }

    public AgentSkillId SkillId { get; }
    public int ValueBeforeUnits { get; }
    public int LossUnits { get; }
    public long FractionalRemainder { get; }
    public bool ReceivedRoundingUnit { get; }
}

public sealed class SkillRedistributionReport
{
    public SkillRedistributionReport(
        EntityId agentId,
        SkillGrantSourceKind sourceKind,
        string sourceId,
        long tick,
        int capacityUnits,
        int sumBeforeUnits,
        int sumAfterUnits,
        int freeCapacityGainUnits,
        int overflowUnits,
        bool wasAlreadyApplied,
        IEnumerable<SkillGrantApplication> grants,
        IEnumerable<SkillDonorLoss> donorLosses,
        IEnumerable<AgentSkillValue> resultingValues)
    {
        if (agentId.IsEmpty
            || string.IsNullOrWhiteSpace(sourceId)
            || tick < 0
            || capacityUnits < AgentSkillCatalog.BaseCapacityUnits
            || capacityUnits > AgentSkillCatalog.UniversityCapacityUnits
            || sumBeforeUnits < 0
            || sumAfterUnits < 0
            || sumAfterUnits > capacityUnits
            || freeCapacityGainUnits < 0
            || overflowUnits < 0
            || sumAfterUnits != checked(sumBeforeUnits + freeCapacityGainUnits))
        {
            throw new ArgumentException("Skill redistribution report is invalid.");
        }

        AgentId = agentId;
        SourceKind = sourceKind;
        SourceId = sourceId ?? string.Empty;
        Tick = tick;
        CapacityUnits = capacityUnits;
        SumBeforeUnits = sumBeforeUnits;
        SumAfterUnits = sumAfterUnits;
        FreeCapacityGainUnits = freeCapacityGainUnits;
        OverflowUnits = overflowUnits;
        WasAlreadyApplied = wasAlreadyApplied;
        Grants = Copy(grants);
        DonorLosses = Copy(donorLosses);
        ResultingValues = Copy(resultingValues);
        if (Grants.Count == 0
            || Grants.Sum(value => value.AppliedUnits)
                != checked(freeCapacityGainUnits + overflowUnits)
            || DonorLosses.Sum(value => value.LossUnits) != overflowUnits
            || ResultingValues.Count != 12
            || ResultingValues.Select(value => value.Id).Distinct().Count() != 12
            || ResultingValues.Any(value => !AgentSkillCatalog.Contains(value.Id))
            || ResultingValues.Sum(value => value.Level) != sumAfterUnits)
        {
            throw new ArgumentException("Skill redistribution report totals are invalid.");
        }
    }

    public EntityId AgentId { get; }
    public SkillGrantSourceKind SourceKind { get; }
    public string SourceId { get; }
    public long Tick { get; }
    public int CapacityUnits { get; }
    public int SumBeforeUnits { get; }
    public int SumAfterUnits { get; }
    public int FreeCapacityGainUnits { get; }
    public int OverflowUnits { get; }
    public bool WasAlreadyApplied { get; }
    public IReadOnlyList<SkillGrantApplication> Grants { get; }
    public IReadOnlyList<SkillDonorLoss> DonorLosses { get; }
    public IReadOnlyList<AgentSkillValue> ResultingValues { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
    {
        return new ReadOnlyCollection<T>((values ?? throw new ArgumentNullException(
            nameof(values))).ToArray());
    }
}

}
