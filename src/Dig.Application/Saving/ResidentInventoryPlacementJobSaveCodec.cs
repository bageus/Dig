using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class ResidentInventoryPlacementJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.resident_inventory_placement.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is ResidentInventoryPlacementJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        var placement = (ResidentInventoryPlacementJobDefinition)definition;
        return new JobDefinitionSaveData
        {
            JobId = placement.Id.ToString(),
            Priority = placement.Priority,
            CreatedTick = placement.CreatedTick,
            MaximumRetries = placement.RetryPolicy.MaximumRetries,
            RetryDelayTicks = placement.RetryPolicy.RetryDelayTicks,
            Dependencies = placement.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("resident_id", placement.ResidentId.ToString()),
                Property("stack_id", placement.StackId.ToString()),
                Property("quantity", placement.Quantity),
                Property("destination_x", placement.DestinationCell.X),
                Property("destination_y", placement.DestinationCell.Y),
                Property("destination_z", placement.DestinationCell.Z),
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
        return new ResidentInventoryPlacementJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "resident_id")),
            EntityId.Parse(Required(properties, "stack_id")),
            Integer(properties, "quantity"),
            new CellId(
                Integer(properties, "destination_x"),
                Integer(properties, "destination_y"),
                Integer(properties, "destination_z")),
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
            Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
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
                $"Saved resident inventory placement job is missing property '{key}'.");
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
                $"Saved resident inventory placement property '{key}' is not an integer.");
        }

        return parsed;
    }
}
}
