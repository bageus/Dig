using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class TunnelAutomaticWorkJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.tunnel_automatic_work.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is TunnelAutomaticWorkJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        TunnelAutomaticWorkJobDefinition work =
            (TunnelAutomaticWorkJobDefinition)definition;
        List<SavePropertyData> properties = new List<SavePropertyData>
        {
            Property("segment_id", work.SegmentId.ToString()),
            Property("kind", (int)work.Kind),
            Property("target_x", work.TargetCell.X),
            Property("target_y", work.TargetCell.Y),
            Property("target_z", work.TargetCell.Z),
            Property("source_resolved", work.IsSourceResolved ? 1 : 0),
        };
        if (work.IsSourceResolved)
        {
            properties.Add(Property("source_stack_id", work.SourceStackId!.Value.ToString()));
            properties.Add(Property("source_x", work.SourceCell!.Value.X));
            properties.Add(Property("source_y", work.SourceCell!.Value.Y));
            properties.Add(Property("source_z", work.SourceCell!.Value.Z));
        }

        return new JobDefinitionSaveData
        {
            JobId = work.Id.ToString(),
            Priority = work.Priority,
            CreatedTick = work.CreatedTick,
            MaximumRetries = work.RetryPolicy.MaximumRetries,
            RetryDelayTicks = work.RetryPolicy.RetryDelayTicks,
            Dependencies = work.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = properties,
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Priority != TunnelAutomaticWorkJobDefinition.AutomaticPriority)
        {
            throw new InvalidOperationException(
                "Saved automatic tunnel work priority is not the authoritative minimum.");
        }

        Dictionary<string, string> properties = data.Properties
            .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
        TunnelAutomaticWorkKind kind = (TunnelAutomaticWorkKind)Integer(properties, "kind");
        if (!Enum.IsDefined(typeof(TunnelAutomaticWorkKind), kind))
        {
            throw new InvalidOperationException(
                "Saved automatic tunnel work kind is unknown.");
        }

        bool sourceResolved = Integer(properties, "source_resolved") switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidOperationException(
                "Saved automatic tunnel source flag must be zero or one."),
        };
        EntityId? sourceStackId = sourceResolved
            ? EntityId.Parse(Required(properties, "source_stack_id"))
            : (EntityId?)null;
        CellId? sourceCell = sourceResolved
            ? new CellId(
                Integer(properties, "source_x"),
                Integer(properties, "source_y"),
                Integer(properties, "source_z"))
            : (CellId?)null;

        return new TunnelAutomaticWorkJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "segment_id")),
            kind,
            new CellId(
                Integer(properties, "target_x"),
                Integer(properties, "target_y"),
                Integer(properties, "target_z")),
            data.CreatedTick,
            new JobRetryPolicy(data.MaximumRetries, data.RetryDelayTicks),
            sourceStackId,
            sourceCell,
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
                $"Saved automatic tunnel work is missing property '{key}'.");
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
                $"Saved automatic tunnel property '{key}' is not an integer.");
        }

        return parsed;
    }
}
}
