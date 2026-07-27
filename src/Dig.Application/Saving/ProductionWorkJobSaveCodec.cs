using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class ProductionWorkJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.production_work.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is ProductionWorkJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        ProductionWorkJobDefinition job = definition as ProductionWorkJobDefinition
            ?? throw new ArgumentException("Expected a production job.", nameof(definition));
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
                Property("order.id", job.OrderId),
                Property("building.id", job.BuildingId),
                Property("recipe.id", job.RecipeId),
                Property("work.x", job.WorkPosition.X),
                Property("work.y", job.WorkPosition.Y),
                Property("work.z", job.WorkPosition.Z),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values = Values(data);
        return new ProductionWorkJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Get(values, "order.id")),
            EntityId.Parse(Get(values, "building.id")),
            new RecipeId(Get(values, "recipe.id")),
            new CellId(
                ParseInt(values, "work.x"),
                ParseInt(values, "work.y"),
                ParseInt(values, "work.z")),
            data.Priority,
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks));
    }

    private static IReadOnlyDictionary<string, string> Values(JobDefinitionSaveData data)
    {
        return data.Properties.ToDictionary(value => value.Key, value => value.Value);
    }

    private static SavePropertyData Property(string key, object value)
    {
        return new SavePropertyData
        {
            Key = key,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing production job property '{key}'.");
        }

        return value;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!int.TryParse(Get(values, key), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException($"Invalid production job property '{key}'.");
        }

        return value;
    }
}

}
