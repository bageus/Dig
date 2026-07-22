using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class WorldItemPickupJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.world_item_pickup.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is WorldItemPickupJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        WorldItemPickupJobDefinition pickup = (WorldItemPickupJobDefinition)definition;
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
            Properties = new List<SavePropertyData>
            {
                Property("stack_id", pickup.StackId.ToString()),
                Property("quantity", pickup.Quantity),
                Property("source_x", pickup.SourceCell.X),
                Property("source_y", pickup.SourceCell.Y),
                Property("source_z", pickup.SourceCell.Z),
            },
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
        return new WorldItemPickupJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "stack_id")),
            Integer(properties, "quantity"),
            new CellId(
                Integer(properties, "source_x"),
                Integer(properties, "source_y"),
                Integer(properties, "source_z")),
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
                $"Saved world item pickup job is missing property '{key}'.");
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
                $"Saved world item pickup property '{key}' is not an integer.");
        }

        return parsed;
    }
}

}
