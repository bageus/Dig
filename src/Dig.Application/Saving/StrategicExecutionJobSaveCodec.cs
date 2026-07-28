using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Jobs;
using Dig.Domain.Strategy;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class StrategicExecutionJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.strategic_execution.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is StrategicExecutionJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        StrategicExecutionJobDefinition job =
            definition as StrategicExecutionJobDefinition
            ?? throw new ArgumentException(
                "Expected a strategic execution job.",
                nameof(definition));
        List<SavePropertyData> properties = new List<SavePropertyData>
        {
            JobDefinitionSaveCodecValues.Property("plan.id", job.PlanId),
            JobDefinitionSaveCodecValues.Property("faction.id", job.FactionId),
            JobDefinitionSaveCodecValues.Property("goal", (int)job.Goal),
            JobDefinitionSaveCodecValues.Property("target.cell", job.TargetCell.HasValue),
            JobDefinitionSaveCodecValues.Property(
                "target.faction",
                job.TargetFactionId.HasValue),
        };
        if (job.TargetCell.HasValue)
        {
            CellId cell = job.TargetCell.Value;
            properties.Add(JobDefinitionSaveCodecValues.Property("target.x", cell.X));
            properties.Add(JobDefinitionSaveCodecValues.Property("target.y", cell.Y));
            properties.Add(JobDefinitionSaveCodecValues.Property("target.z", cell.Z));
        }

        if (job.TargetFactionId.HasValue)
        {
            properties.Add(JobDefinitionSaveCodecValues.Property(
                "target.faction.id",
                job.TargetFactionId.Value));
        }

        return JobDefinitionSaveCodecValues.Create(job, properties);
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values =
            JobDefinitionSaveCodecValues.Read(data);
        int goalValue = JobDefinitionSaveCodecValues.Integer(values, "goal");
        if (!Enum.IsDefined(typeof(StrategicGoalKind), goalValue))
        {
            throw new InvalidOperationException("Saved strategic goal is invalid.");
        }

        CellId? targetCell = JobDefinitionSaveCodecValues.Boolean(values, "target.cell")
            ? JobDefinitionSaveCodecValues.Cell(values, "target")
            : (CellId?)null;
        FactionId? targetFaction = JobDefinitionSaveCodecValues.Boolean(
            values,
            "target.faction")
                ? new FactionId(JobDefinitionSaveCodecValues.Required(
                    values,
                    "target.faction.id"))
                : (FactionId?)null;
        return new StrategicExecutionJobDefinition(
            EntityId.Parse(data.JobId),
            new StrategicExecutionPlanId(JobDefinitionSaveCodecValues.Required(
                values,
                "plan.id")),
            new FactionId(JobDefinitionSaveCodecValues.Required(values, "faction.id")),
            (StrategicGoalKind)goalValue,
            targetCell,
            targetFaction,
            data.Priority,
            data.CreatedTick,
            JobDefinitionSaveCodecValues.RetryPolicy(data),
            JobDefinitionSaveCodecValues.Dependencies(data));
    }
}

}