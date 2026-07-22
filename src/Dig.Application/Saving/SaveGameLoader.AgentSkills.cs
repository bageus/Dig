using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static IReadOnlyDictionary<EntityId, bool> BuildAgentAutomaticPlanning(
        AgentSkillsSaveData data)
    {
        if (data is null || data.Agents is null)
        {
            throw new InvalidOperationException("Agent save data is missing.");
        }

        Dictionary<EntityId, bool> result = new Dictionary<EntityId, bool>();
        foreach (AgentSkillProgressionSaveData saved in data.Agents
            .OrderBy(value => value.AgentId, StringComparer.Ordinal))
        {
            EntityId agentId = EntityId.Parse(saved.AgentId);
            if (!result.TryAdd(
                agentId,
                saved.AutomaticPlanningEnabled ?? true))
            {
                throw new InvalidOperationException(
                    $"Duplicate saved agent id '{agentId}'.");
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<EntityId, AgentSkillProgressionSnapshot>
        BuildAgentSkills(AgentSkillsSaveData data)
    {
        if (data is null || data.Agents is null)
        {
            throw new InvalidOperationException("Agent skills save data is missing.");
        }

        Dictionary<EntityId, AgentSkillProgressionSnapshot> result =
            new Dictionary<EntityId, AgentSkillProgressionSnapshot>();
        foreach (AgentSkillProgressionSaveData saved in data.Agents
            .OrderBy(value => value.AgentId, StringComparer.Ordinal))
        {
            if (saved is null
                || saved.Values is null
                || saved.AppliedSourceKeys is null
                || saved.MigrationSteps is null
                || saved.Values.Any(value => value is null)
                || saved.SchemaVersion != AgentSkillCatalog.SchemaVersion
                || saved.PrecisionVersion != AgentSkillCatalog.PrecisionVersion
                || saved.UnitsPerPoint != AgentSkillCatalog.UnitsPerPoint)
            {
                throw new InvalidOperationException("Agent skill progression is invalid.");
            }

            EntityId agentId = EntityId.Parse(saved.AgentId);
            AgentSkillValue[] values = saved.Values.Select(value =>
                new AgentSkillValue(
                    new AgentSkillId(value.SkillId),
                    value.Units)).ToArray();
            SkillRedistributionReport? report = BuildReport(
                agentId,
                saved.LastReport);
            AgentSkillProgressionSnapshot progression =
                new AgentSkillProgressionSnapshot(
                    saved.SchemaVersion,
                    saved.PrecisionVersion,
                    saved.TotalCapacityUnits,
                    values,
                    saved.AppliedSourceKeys,
                    report,
                    saved.MigrationSteps);
            if (!result.TryAdd(agentId, progression))
            {
                throw new InvalidOperationException(
                    $"Duplicate saved agent skill id '{agentId}'.");
            }
        }

        return result;
    }

    private static SkillRedistributionReport? BuildReport(
        EntityId agentId,
        SkillRedistributionReportSaveData? saved)
    {
        if (saved is null)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(SkillGrantSourceKind), saved.SourceKind)
            || saved.Grants is null
            || saved.DonorLosses is null
            || saved.ResultingValues is null)
        {
            throw new InvalidOperationException("Saved skill report is invalid.");
        }

        if (saved.Grants.Any(value => value is null)
            || saved.DonorLosses.Any(value => value is null)
            || saved.ResultingValues.Any(value => value is null))
        {
            throw new InvalidOperationException("Saved skill report entry is missing.");
        }

        SkillGrantApplication[] grants = saved.Grants.Select(value =>
            new SkillGrantApplication(
                new AgentSkillId(value.SkillId),
                value.RequestedUnits,
                value.EligibleUnits,
                value.AppliedUnits,
                value.FreeCapacityUnits,
                value.ReceivedRoundingUnit)).ToArray();
        SkillDonorLoss[] losses = saved.DonorLosses.Select(value =>
            new SkillDonorLoss(
                new AgentSkillId(value.SkillId),
                value.ValueBeforeUnits,
                value.LossUnits,
                value.FractionalRemainder,
                value.ReceivedRoundingUnit)).ToArray();
        AgentSkillValue[] resultingValues = saved.ResultingValues.Select(value =>
            new AgentSkillValue(
                new AgentSkillId(value.SkillId),
                value.Units)).ToArray();
        return new SkillRedistributionReport(
            agentId,
            (SkillGrantSourceKind)saved.SourceKind,
            saved.SourceId,
            saved.Tick,
            saved.CapacityUnits,
            saved.SumBeforeUnits,
            saved.SumAfterUnits,
            saved.FreeCapacityGainUnits,
            saved.OverflowUnits,
            wasAlreadyApplied: false,
            grants,
            losses,
            resultingValues);
    }
}

}
