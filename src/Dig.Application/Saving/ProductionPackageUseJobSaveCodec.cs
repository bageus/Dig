using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class ProductionPackageUseJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.production_package_use.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is ProductionPackageUseJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        ProductionPackageUseJobDefinition job =
            definition as ProductionPackageUseJobDefinition
            ?? throw new ArgumentException(
                "Expected a production package use job.",
                nameof(definition));
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
                Property("package.id", job.PackageStackId),
                Property("package.version", job.PackageVersion),
                Property("target.x", job.TargetCell.X),
                Property("target.y", job.TargetCell.Y),
                Property("target.z", job.TargetCell.Z),
                Property("work.x", job.WorkPosition.X),
                Property("work.y", job.WorkPosition.Y),
                Property("work.z", job.WorkPosition.Z),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values = data.Properties.ToDictionary(
            value => value.Key,
            value => value.Value,
            StringComparer.Ordinal);
        return new ProductionPackageUseJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Get(values, "package.id")),
            Cell(values, "target"),
            Cell(values, "work"),
            ParseLong(values, "package.version"),
            data.Priority,
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks),
            data.Dependencies.Select(EntityId.Parse));
    }

    private static CellId Cell(
        IReadOnlyDictionary<string, string> values,
        string prefix)
    {
        return new CellId(
            ParseInt(values, prefix + ".x"),
            ParseInt(values, prefix + ".y"),
            ParseInt(values, prefix + ".z"));
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

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        if (!values.TryGetValue(key, out string? value)
            || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing production package job property '{key}'.");
        }

        return value;
    }

    private static int ParseInt(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        return int.Parse(
            Get(values, key),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture);
    }

    private static long ParseLong(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        return long.Parse(
            Get(values, key),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture);
    }
}

}
