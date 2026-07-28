using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class SpatialDigJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.spatial_dig.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is SpatialDigJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        SpatialDigJobDefinition job = definition as SpatialDigJobDefinition
            ?? throw new ArgumentException(
                "Expected a spatial digging job.",
                nameof(definition));
        return JobDefinitionSaveCodecValues.Create(job, new[]
        {
            JobDefinitionSaveCodecValues.Property("target.x", job.Target.TargetCell.X),
            JobDefinitionSaveCodecValues.Property("target.y", job.Target.TargetCell.Y),
            JobDefinitionSaveCodecValues.Property("target.z", job.Target.TargetCell.Z),
            JobDefinitionSaveCodecValues.Property("work.x", job.Target.WorkCell.X),
            JobDefinitionSaveCodecValues.Property("work.y", job.Target.WorkCell.Y),
            JobDefinitionSaveCodecValues.Property("work.z", job.Target.WorkCell.Z),
        });
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values =
            JobDefinitionSaveCodecValues.Read(data);
        return new SpatialDigJobDefinition(
            EntityId.Parse(data.JobId),
            new SpatialDigJobTarget(
                JobDefinitionSaveCodecValues.Cell(values, "target"),
                JobDefinitionSaveCodecValues.Cell(values, "work")),
            data.Priority,
            data.CreatedTick,
            JobDefinitionSaveCodecValues.RetryPolicy(data),
            JobDefinitionSaveCodecValues.Dependencies(data));
    }
}

}