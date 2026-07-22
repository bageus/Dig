using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class BuildingBoxPackingJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.building_box_packing.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is BuildingBoxPackingJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        BuildingBoxPackingJobDefinition packing =
            (BuildingBoxPackingJobDefinition)definition;
        return new JobDefinitionSaveData
        {
            JobId = packing.Id.ToString(),
            Priority = packing.Priority,
            CreatedTick = packing.CreatedTick,
            MaximumRetries = packing.RetryPolicy.MaximumRetries,
            RetryDelayTicks = packing.RetryPolicy.RetryDelayTicks,
            Dependencies = packing.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("building_id", packing.BuildingId.ToString()),
                Property("output_stack_id", packing.OutputStackId.ToString()),
                Property("work_x", packing.WorkPosition.X),
                Property("work_y", packing.WorkPosition.Y),
                Property("work_z", packing.WorkPosition.Z),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        Dictionary<string, string> properties = data.Properties
            .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
        return new BuildingBoxPackingJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "building_id")),
            EntityId.Parse(Required(properties, "output_stack_id")),
            new CellId(
                Integer(properties, "work_x"),
                Integer(properties, "work_y"),
                Integer(properties, "work_z")),
            data.Priority,
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks),
            data.Dependencies.Select(EntityId.Parse));
    }

    private static SavePropertyData Property(string key, object value)
    {
        return new SavePropertyData
        {
            Key = key,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? string.Empty,
        };
    }

    private static string Required(
        IReadOnlyDictionary<string, string> properties,
        string key)
    {
        if (!properties.TryGetValue(key, out string? value)
            || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Saved BuildingBox packing job is missing property '{key}'.");
        }

        return value;
    }

    private static int Integer(
        IReadOnlyDictionary<string, string> properties,
        string key)
    {
        string value = Required(properties, key);
        if (!int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsed))
        {
            throw new InvalidOperationException(
                $"Saved BuildingBox packing property '{key}' is not an integer.");
        }

        return parsed;
    }
}
}
