using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

internal static class JobDefinitionSaveCodecValues
{
    public static JobDefinitionSaveData Create(
        JobDefinition definition,
        IEnumerable<SavePropertyData> properties)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return new JobDefinitionSaveData
        {
            JobId = definition.Id.ToString(),
            Priority = definition.Priority,
            CreatedTick = definition.CreatedTick,
            MaximumRetries = definition.RetryPolicy.MaximumRetries,
            RetryDelayTicks = definition.RetryPolicy.RetryDelayTicks,
            Dependencies = definition.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = properties.ToList(),
        };
    }

    public static SavePropertyData Property(string key, object? value)
    {
        return new SavePropertyData
        {
            Key = key,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? string.Empty,
        };
    }

    public static IReadOnlyDictionary<string, string> Read(JobDefinitionSaveData data)
    {
        return data.Properties.ToDictionary(
            value => value.Key,
            value => value.Value,
            StringComparer.Ordinal);
    }

    public static string Required(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        if (!values.TryGetValue(key, out string? value)
            || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Saved job property '{key}' is missing.");
        }

        return value;
    }

    public static int Integer(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        if (!int.TryParse(
            Required(values, key),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsed))
        {
            throw new InvalidOperationException(
                $"Saved job property '{key}' is not an integer.");
        }

        return parsed;
    }

    public static bool Boolean(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        if (!bool.TryParse(Required(values, key), out bool parsed))
        {
            throw new InvalidOperationException(
                $"Saved job property '{key}' is not a boolean.");
        }

        return parsed;
    }

    public static CellId Cell(
        IReadOnlyDictionary<string, string> values,
        string prefix)
    {
        return new CellId(
            Integer(values, prefix + ".x"),
            Integer(values, prefix + ".y"),
            Integer(values, prefix + ".z"));
    }

    public static EntityId[] Dependencies(JobDefinitionSaveData data)
    {
        return data.Dependencies.Select(EntityId.Parse).ToArray();
    }

    public static JobRetryPolicy RetryPolicy(JobDefinitionSaveData data)
    {
        return new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks);
    }
}

}