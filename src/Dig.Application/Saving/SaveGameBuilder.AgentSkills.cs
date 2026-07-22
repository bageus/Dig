using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameBuilder
{
    private static AgentSkillsSaveData BuildAgentSkills(IEnumerable<AgentState> agents)
    {
        AgentSkillsSaveData data = new AgentSkillsSaveData();
        foreach (AgentState agent in agents.OrderBy(value => value.Id.ToString(),
            StringComparer.Ordinal))
        {
            AgentSkillProgressionSnapshot progression =
                agent.CreateSkillProgressionSnapshot();
            AgentSkillProgressionSaveData saved = new AgentSkillProgressionSaveData
            {
                AgentId = agent.Id.ToString(),
                SchemaVersion = progression.SchemaVersion,
                PrecisionVersion = progression.PrecisionVersion,
                UnitsPerPoint = AgentSkillCatalog.UnitsPerPoint,
                TotalCapacityUnits = progression.TotalCapacityUnits,
                LastReport = BuildReport(progression.LastReport),
                AutomaticPlanningEnabled = agent.AutomaticPlanningEnabled,
            };
            saved.AppliedSourceKeys.AddRange(progression.AppliedSourceKeys);
            saved.MigrationSteps.AddRange(progression.MigrationSteps);
            saved.Values.AddRange(progression.Values.Select(value =>
                new AgentSkillValueSaveData
                {
                    SkillId = value.Id.ToString(),
                    Units = value.Level,
                }));
            data.Agents.Add(saved);
        }

        return data;
    }

    private static SkillRedistributionReportSaveData? BuildReport(
        SkillRedistributionReport? report)
    {
        if (report is null)
        {
            return null;
        }

        SkillRedistributionReportSaveData saved = new SkillRedistributionReportSaveData
        {
            SourceKind = (int)report.SourceKind,
            SourceId = report.SourceId,
            Tick = report.Tick,
            CapacityUnits = report.CapacityUnits,
            SumBeforeUnits = report.SumBeforeUnits,
            SumAfterUnits = report.SumAfterUnits,
            FreeCapacityGainUnits = report.FreeCapacityGainUnits,
            OverflowUnits = report.OverflowUnits,
        };
        saved.Grants.AddRange(report.Grants.Select(value =>
            new SkillGrantApplicationSaveData
            {
                SkillId = value.SkillId.ToString(),
                RequestedUnits = value.RequestedUnits,
                EligibleUnits = value.EligibleUnits,
                AppliedUnits = value.AppliedUnits,
                FreeCapacityUnits = value.FreeCapacityUnits,
                ReceivedRoundingUnit = value.ReceivedRoundingUnit,
            }));
        saved.DonorLosses.AddRange(report.DonorLosses.Select(value =>
            new SkillDonorLossSaveData
            {
                SkillId = value.SkillId.ToString(),
                ValueBeforeUnits = value.ValueBeforeUnits,
                LossUnits = value.LossUnits,
                FractionalRemainder = value.FractionalRemainder,
                ReceivedRoundingUnit = value.ReceivedRoundingUnit,
            }));
        saved.ResultingValues.AddRange(report.ResultingValues.Select(value =>
            new AgentSkillValueSaveData
            {
                SkillId = value.Id.ToString(),
                Units = value.Level,
            }));
        return saved;
    }
}

}
