using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class BarrelAttackJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.barrel_attack.v1";
    public string TypeId => StableTypeId;
    public bool CanEncode(JobDefinition definition) => definition is BarrelAttackJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        BarrelAttackJobDefinition job = definition as BarrelAttackJobDefinition
            ?? throw new ArgumentException("Expected a barrel attack job.", nameof(definition));
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
                Property("barrel.id", job.BarrelId.ToString()),
                Property("target.x", job.TargetCell.X),
                Property("target.y", job.TargetCell.Y),
                Property("target.z", job.TargetCell.Z),
                Property("work.x", job.WorkPosition.X),
                Property("work.y", job.WorkPosition.Y),
                Property("work.z", job.WorkPosition.Z),
                Property("barrel.version", job.BarrelVersion),
                Property("contents.generation", job.ContentsGeneration),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        Dictionary<string, string> properties = data.Properties.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.Ordinal);
        return new BarrelAttackJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Get(properties, "barrel.id")),
            Cell(properties, "target"),
            Cell(properties, "work"),
            ParseLong(properties, "barrel.version"),
            ParseLong(properties, "contents.generation"),
            data.Priority,
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks),
            data.Dependencies.Select(EntityId.Parse));
    }

    private static CellId Cell(IReadOnlyDictionary<string, string> values, string prefix) =>
        new CellId(
            ParseInt(values, prefix + ".x"),
            ParseInt(values, prefix + ".y"),
            ParseInt(values, prefix + ".z"));

    private static SavePropertyData Property(string key, object value) =>
        new SavePropertyData
        {
            Key = key,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };

    private static string Get(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Saved barrel job property '{key}' is invalid.");
        }

        return value;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key) =>
        int.Parse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static long ParseLong(IReadOnlyDictionary<string, string> values, string key) =>
        long.Parse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture);
}

}
