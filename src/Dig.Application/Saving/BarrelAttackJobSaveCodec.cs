using System;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class BarrelAttackJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string KindValue = "barrel_attack";

    public string Kind => KindValue;

    public bool CanSerialize(JobDefinition definition) =>
        definition is BarrelAttackJobDefinition;

    public JobDefinitionSaveData Serialize(JobDefinition definition)
    {
        BarrelAttackJobDefinition value = definition as BarrelAttackJobDefinition
            ?? throw new ArgumentException("Expected barrel attack job.", nameof(definition));
        return new JobDefinitionSaveData
        {
            Kind = Kind,
            String1 = value.BarrelId.ToString(),
            Int1 = value.TargetCell.X,
            Int2 = value.TargetCell.Y,
            Int3 = value.TargetCell.Z,
            Int4 = value.WorkPosition.X,
            Int5 = value.WorkPosition.Y,
            Int6 = value.WorkPosition.Z,
            Long1 = value.BarrelVersion,
            Long2 = value.ContentsGeneration,
        };
    }

    public Result<JobDefinition> Deserialize(JobSaveData job, JobDefinitionSaveData definition)
    {
        try
        {
            return Result<JobDefinition>.Success(new BarrelAttackJobDefinition(
                EntityId.Parse(job.JobId),
                EntityId.Parse(definition.String1),
                new CellId(definition.Int1, definition.Int2, definition.Int3),
                new CellId(definition.Int4, definition.Int5, definition.Int6),
                definition.Long1,
                definition.Long2,
                job.Priority,
                job.CreatedTick,
                new JobRetryPolicy(job.RetryMaxAttempts, job.RetryBackoffTicks),
                JobSaveCodecSupport.ParseDependencies(job.DependencyIds)));
        }
        catch (Exception)
        {
            return Result<JobDefinition>.Failure(SaveErrors.InvalidPayload);
        }
    }
}

}