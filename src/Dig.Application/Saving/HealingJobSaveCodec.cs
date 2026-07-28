using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class HealingJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.healing.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) => definition is HealingJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        HealingJobDefinition job = definition as HealingJobDefinition
            ?? throw new ArgumentException("Expected a healing job.", nameof(definition));
        return JobDefinitionSaveCodecValues.Create(job, new[]
        {
            JobDefinitionSaveCodecValues.Property("patient.id", job.PatientId),
            JobDefinitionSaveCodecValues.Property("work.x", job.WorkPosition.X),
            JobDefinitionSaveCodecValues.Property("work.y", job.WorkPosition.Y),
            JobDefinitionSaveCodecValues.Property("work.z", job.WorkPosition.Z),
            JobDefinitionSaveCodecValues.Property("health.restored", job.HealthRestored),
        });
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values =
            JobDefinitionSaveCodecValues.Read(data);
        return new HealingJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(JobDefinitionSaveCodecValues.Required(values, "patient.id")),
            JobDefinitionSaveCodecValues.Cell(values, "work"),
            JobDefinitionSaveCodecValues.Integer(values, "health.restored"),
            data.Priority,
            data.CreatedTick,
            JobDefinitionSaveCodecValues.RetryPolicy(data),
            JobDefinitionSaveCodecValues.Dependencies(data));
    }
}

}