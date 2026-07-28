using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class BuildingWorkJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.building_work.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is BuildingWorkJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        BuildingWorkJobDefinition job = definition as BuildingWorkJobDefinition
            ?? throw new ArgumentException(
                "Expected a building work job.",
                nameof(definition));
        return JobDefinitionSaveCodecValues.Create(job, new[]
        {
            JobDefinitionSaveCodecValues.Property("building.id", job.BuildingId),
            JobDefinitionSaveCodecValues.Property("work.kind", (int)job.Kind),
            JobDefinitionSaveCodecValues.Property("work.x", job.WorkPosition.X),
            JobDefinitionSaveCodecValues.Property("work.y", job.WorkPosition.Y),
            JobDefinitionSaveCodecValues.Property("work.z", job.WorkPosition.Z),
        });
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values =
            JobDefinitionSaveCodecValues.Read(data);
        int kindValue = JobDefinitionSaveCodecValues.Integer(values, "work.kind");
        if (!Enum.IsDefined(typeof(BuildingWorkKind), kindValue))
        {
            throw new InvalidOperationException("Saved building work kind is invalid.");
        }

        return new BuildingWorkJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(JobDefinitionSaveCodecValues.Required(values, "building.id")),
            (BuildingWorkKind)kindValue,
            JobDefinitionSaveCodecValues.Cell(values, "work"),
            data.Priority,
            data.CreatedTick,
            JobDefinitionSaveCodecValues.RetryPolicy(data),
            JobDefinitionSaveCodecValues.Dependencies(data));
    }
}

}