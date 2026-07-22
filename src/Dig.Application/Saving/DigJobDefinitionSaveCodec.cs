using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class DigJobDefinitionSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.dig.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition)
    {
        return definition is DigJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        DigJobDefinition dig = definition as DigJobDefinition
            ?? throw new ArgumentException("Expected a digging job definition.", nameof(definition));
        return new JobDefinitionSaveData
        {
            TypeId = TypeId,
            JobId = dig.Id.ToString(),
            Priority = dig.Priority,
            CreatedTick = dig.CreatedTick,
            MaximumRetries = dig.RetryPolicy.MaximumRetries,
            RetryDelayTicks = dig.RetryPolicy.RetryDelayTicks,
            Dependencies = dig.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("target.x", dig.Target.CellId.X),
                Property("target.y", dig.Target.CellId.Y),
                Property("target.z", dig.Target.CellId.Z),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        Dictionary<string, string> properties = data.Properties
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        int x = ParseInt(properties, "target.x");
        int y = ParseInt(properties, "target.y");
        int z = ParseInt(properties, "target.z");
        EntityId[] dependencies = data.Dependencies
            .Select(EntityId.Parse)
            .ToArray();
        return new DigJobDefinition(
            EntityId.Parse(data.JobId),
            new DigJobTarget(new CellId(x, y, z)),
            data.Priority,
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks),
            dependencies);
    }

    private static SavePropertyData Property(string key, int value)
    {
        return new SavePropertyData
        {
            Key = key,
            Value = value.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static int ParseInt(
        IReadOnlyDictionary<string, string> properties,
        string key)
    {
        if (!properties.TryGetValue(key, out string? value)
            || !int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed))
        {
            throw new InvalidOperationException(
                $"Saved digging job property '{key}' is missing or invalid.");
        }

        return parsed;
    }
}
}
