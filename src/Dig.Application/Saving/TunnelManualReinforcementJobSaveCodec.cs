using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{
public sealed class TunnelManualReinforcementJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.tunnel_manual_reinforcement.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is TunnelManualReinforcementJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        TunnelManualReinforcementJobDefinition work =
            (TunnelManualReinforcementJobDefinition)definition;
        return new JobDefinitionSaveData
        {
            JobId = work.Id.ToString(),
            Priority = work.Priority,
            CreatedTick = work.CreatedTick,
            MaximumRetries = work.RetryPolicy.MaximumRetries,
            RetryDelayTicks = work.RetryPolicy.RetryDelayTicks,
            Dependencies = work.Dependencies.Select(value => value.ToString()).ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("resident_id", work.ResidentId),
                Property("source_stack_id", work.SourceStackId),
                Property("segment_id", work.SegmentId),
                Property("kind", (int)work.Kind),
                Property("target_x", work.TargetCell.X),
                Property("target_y", work.TargetCell.Y),
                Property("target_z", work.TargetCell.Z),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        Dictionary<string, string> properties = data.Properties.ToDictionary(
            value => value.Key,
            value => value.Value,
            StringComparer.Ordinal);
        TunnelManualReinforcementKind kind =
            (TunnelManualReinforcementKind)Integer(properties, "kind");
        if (!Enum.IsDefined(typeof(TunnelManualReinforcementKind), kind))
        {
            throw new InvalidOperationException(
                "Saved manual tunnel reinforcement kind is unknown.");
        }

        return new TunnelManualReinforcementJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "resident_id")),
            EntityId.Parse(Required(properties, "source_stack_id")),
            EntityId.Parse(Required(properties, "segment_id")),
            kind,
            new CellId(
                Integer(properties, "target_x"),
                Integer(properties, "target_y"),
                Integer(properties, "target_z")),
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
                $"Saved manual tunnel reinforcement is missing property '{key}'.");
        }

        return value;
    }

    private static int Integer(
        IReadOnlyDictionary<string, string> properties,
        string key)
    {
        string value = Required(properties, key);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int parsed))
        {
            throw new InvalidOperationException(
                $"Saved manual tunnel reinforcement property '{key}' is not an integer.");
        }

        return parsed;
    }
}
}
