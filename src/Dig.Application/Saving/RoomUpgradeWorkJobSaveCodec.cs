using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class RoomUpgradeWorkJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.room_upgrade_work.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is RoomUpgradeWorkJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        RoomUpgradeWorkJobDefinition work =
            (RoomUpgradeWorkJobDefinition)definition;
        return JobDefinitionSaveCodecValues.Create(
            work,
            new[]
            {
                JobDefinitionSaveCodecValues.Property(
                    "room_infrastructure_id",
                    work.RoomInfrastructureId.ToString()),
                JobDefinitionSaveCodecValues.Property("work.x", work.WorkCell.X),
                JobDefinitionSaveCodecValues.Property("work.y", work.WorkCell.Y),
                JobDefinitionSaveCodecValues.Property("work.z", work.WorkCell.Z),
            });
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        IReadOnlyDictionary<string, string> values =
            JobDefinitionSaveCodecValues.Read(data);
        return new RoomUpgradeWorkJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(JobDefinitionSaveCodecValues.Required(
                values,
                "room_infrastructure_id")),
            JobDefinitionSaveCodecValues.Cell(values, "work"),
            data.Priority,
            data.CreatedTick,
            JobDefinitionSaveCodecValues.RetryPolicy(data),
            JobDefinitionSaveCodecValues.Dependencies(data));
    }
}

}
