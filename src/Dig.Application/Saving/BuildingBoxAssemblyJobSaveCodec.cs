using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class BuildingBoxAssemblyJobSaveCodec : IJobDefinitionSaveCodec
{
    public string TypeId => "job.building_box_assembly.v1";

    public bool CanEncode(JobDefinition definition)
    {
        return definition is BuildingBoxAssemblyJobDefinition;
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        BuildingBoxAssemblyJobDefinition box =
            (BuildingBoxAssemblyJobDefinition)definition;
        return new JobDefinitionSaveData
        {
            JobId = box.Id.ToString(),
            Priority = box.Priority,
            CreatedTick = box.CreatedTick,
            MaximumRetries = box.RetryPolicy.MaximumRetries,
            RetryDelayTicks = box.RetryPolicy.RetryDelayTicks,
            Dependencies = box.Dependencies
                .Select(value => value.ToString())
                .ToList(),
            Properties = new List<SavePropertyData>
            {
                Property("building_id", box.BuildingId.ToString()),
                Property("source_stack_id", box.SourceStackId.ToString()),
                Property("site_x", box.SiteCell.X),
                Property("site_y", box.SiteCell.Y),
                Property("work_x", box.WorkPosition.X),
                Property("work_y", box.WorkPosition.Y),
            },
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        Dictionary<string, string> properties = data.Properties
            .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
        return new BuildingBoxAssemblyJobDefinition(
            EntityId.Parse(data.JobId),
            EntityId.Parse(Required(properties, "building_id")),
            EntityId.Parse(Required(properties, "source_stack_id")),
            new CellId(
                Integer(properties, "site_x"),
                Integer(properties, "site_y")),
            new CellId(
                Integer(properties, "work_x"),
                Integer(properties, "work_y")),
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
            Value = Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? string.Empty,
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
                $"Saved BuildingBox job is missing property '{key}'.");
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
                $"Saved BuildingBox job property '{key}' is not an integer.");
        }

        return parsed;
    }
}
}