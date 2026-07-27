using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class BuildingBoxPickupJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.building_box_pickup.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is BuildingBoxPickupJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        BuildingBoxPickupJobDefinition pickup =
            (BuildingBoxPickupJobDefinition)definition;
        List<SavePropertyData> properties = new List<SavePropertyData>
        {
            Property("stack_id", pickup.StackId.ToString()),
            Property("source_x", pickup.SourceCell.X),
            Property("source_y", pickup.SourceCell.Y),
            Property("source_z", pickup.SourceCell.Z),
            Property("starts_held", pickup.StartsHeld),
        };
        if (pickup.DestinationCell.HasValue)
        {
            properties.Add(Property("destination_x", pickup.DestinationCell.Value.X));
            properties.Add(Property("destination_y", pickup.DestinationCell.Value.Y));
            properties.Add(Property("destination_z", pickup.DestinationCell.Value.Z));
        }

        return new JobDefinitionSaveData
        {
            JobId = pickup.Id.ToString(),
            Priority = pickup.Priority,
            CreatedTick = pickup.CreatedTick,
            MaximumRetries = pickup.RetryPolicy.MaximumRetries,
            RetryDelayTicks = pickup.RetryPolicy.RetryDelayTicks,
            Dependencies = pickup.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = properties,
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        Dictionary<string, string> properties = data.Properties
            .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
        EntityId jobId = EntityId.Parse(data.JobId);
        EntityId stackId = EntityId.Parse(Required(properties, "stack_id"));
        CellId source = new CellId(
            Integer(properties, "source_x"),
            Integer(properties, "source_y"),
            Integer(properties, "source_z"));
        JobRetryPolicy retry = new JobRetryPolicy(
            data.MaximumRetries,
            data.RetryDelayTicks);
        IEnumerable<EntityId> dependencies = data.Dependencies.Select(EntityId.Parse);
        if (!properties.ContainsKey("destination_x"))
        {
            return new BuildingBoxPickupJobDefinition(
                jobId,
                stackId,
                source,
                data.Priority,
                data.CreatedTick,
                retry,
                dependencies);
        }

        CellId destination = new CellId(
            Integer(properties, "destination_x"),
            Integer(properties, "destination_y"),
            Integer(properties, "destination_z"));
        bool startsHeld = Boolean(properties, "starts_held", defaultValue: false);
        return new BuildingBoxPickupJobDefinition(
            jobId,
            stackId,
            source,
            destination,
            startsHeld,
            data.Priority,
            data.CreatedTick,
            retry,
            dependencies);
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
                $"Saved BuildingBox pickup job is missing property '{key}'.");
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
                $"Saved BuildingBox pickup property '{key}' is not an integer.");
        }

        return parsed;
    }

    private static bool Boolean(
        IReadOnlyDictionary<string, string> properties,
        string key,
        bool defaultValue)
    {
        if (!properties.TryGetValue(key, out string? value))
        {
            return defaultValue;
        }

        if (!bool.TryParse(value, out bool parsed))
        {
            throw new InvalidOperationException(
                $"Saved BuildingBox pickup property '{key}' is not a boolean.");
        }

        return parsed;
    }
}

}
