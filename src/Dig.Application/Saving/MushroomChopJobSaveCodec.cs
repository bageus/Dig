using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class MushroomChopJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.mushroom_chop.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is MushroomChopJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        MushroomChopJobDefinition job = definition as MushroomChopJobDefinition
            ?? throw new ArgumentException("Expected a mushroom chopping job.", nameof(definition));
        return new JobDefinitionSaveData
        {
            TypeId = TypeId,
            JobId = job.Id.ToString(),
            Priority = job.Priority,
            CreatedTick = job.CreatedTick,
            MaximumRetries = job.RetryPolicy.MaximumRetries,
            RetryDelayTicks = job.RetryPolicy.RetryDelayTicks,
            Dependencies = job.Dependencies.Select(value => value.ToString()).ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("site.id", job.SiteId.ToString()),
                Property("target.x", job.TargetCell.X),
                Property("target.y", job.TargetCell.Y),
                Property("target.z", job.TargetCell.Z),
                Property("work.x", job.WorkPosition.X),
                Property("work.y", job.WorkPosition.Y),
                Property("work.z", job.WorkPosition.Z),
                Property("growth.generation", job.GrowthGeneration),
                Property("required.swings", job.RequiredSwings),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        Dictionary<string, string> properties = data.Properties.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.Ordinal);
        return new MushroomChopJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Get(properties, "site.id")),
            new CellId(
                ParseInt(properties, "target.x"),
                ParseInt(properties, "target.y"),
                ParseInt(properties, "target.z")),
            new CellId(
                ParseInt(properties, "work.x"),
                ParseInt(properties, "work.y"),
                ParseInt(properties, "work.z")),
            ParseLong(properties, "growth.generation"),
            ParseInt(properties, "required.swings"),
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

    private static string Get(IReadOnlyDictionary<string, string> properties, string key)
    {
        if (!properties.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Saved mushroom job property '{key}' is invalid.");
        }

        return value;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> properties, string key)
    {
        if (!int.TryParse(Get(properties, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException($"Saved mushroom job property '{key}' is invalid.");
        }

        return value;
    }

    private static long ParseLong(IReadOnlyDictionary<string, string> properties, string key)
    {
        if (!long.TryParse(Get(properties, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
        {
            throw new InvalidOperationException($"Saved mushroom job property '{key}' is invalid.");
        }

        return value;
    }
}

}
