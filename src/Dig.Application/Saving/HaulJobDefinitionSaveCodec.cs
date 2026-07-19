using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class HaulJobDefinitionSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.haul.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition)
    {
        return definition is HaulJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        HaulJobDefinition haul = definition as HaulJobDefinition
            ?? throw new ArgumentException("Expected a hauling job definition.", nameof(definition));
        if (haul.Destination.Kind != ItemLocationKind.Storage
            || !haul.Destination.HasOwner)
        {
            throw new InvalidOperationException(
                "The v1 hauling save codec supports storage destinations only.");
        }

        return new JobDefinitionSaveData
        {
            TypeId = TypeId,
            JobId = haul.Id.ToString(),
            Priority = haul.Priority,
            CreatedTick = haul.CreatedTick,
            MaximumRetries = haul.RetryPolicy.MaximumRetries,
            RetryDelayTicks = haul.RetryPolicy.RetryDelayTicks,
            Dependencies = haul.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("source.stack_id", haul.SourceStackId.ToString()),
                Property("item.id", haul.ItemId.ToString()),
                Property("quantity", haul.Quantity),
                Property("destination.storage_id", haul.Destination.OwnerId.ToString()),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        Dictionary<string, string> properties = data.Properties
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        EntityId[] dependencies = data.Dependencies
            .Select(EntityId.Parse)
            .ToArray();
        return new HaulJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "source.stack_id")),
            new ItemId(Required(properties, "item.id")),
            ParseInt(properties, "quantity"),
            EntityId.Parse(Required(properties, "destination.storage_id")),
            data.Priority,
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks),
            dependencies);
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
                $"Saved hauling job property '{key}' is missing or invalid.");
        }

        return value;
    }

    private static int ParseInt(
        IReadOnlyDictionary<string, string> properties,
        string key)
    {
        string value = Required(properties, key);
        if (!int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed)
            || parsed <= 0)
        {
            throw new InvalidOperationException(
                $"Saved hauling job property '{key}' is missing or invalid.");
        }

        return parsed;
    }
}

}