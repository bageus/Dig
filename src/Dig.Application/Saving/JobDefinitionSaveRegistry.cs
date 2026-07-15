using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class JobDefinitionSaveRegistry
{
    private readonly IReadOnlyList<IJobDefinitionSaveCodec> _codecs;
    private readonly Dictionary<string, IJobDefinitionSaveCodec> _byType;

    public JobDefinitionSaveRegistry(IEnumerable<IJobDefinitionSaveCodec> codecs)
    {
        if (codecs is null)
        {
            throw new ArgumentNullException(nameof(codecs));
        }

        IJobDefinitionSaveCodec[] values = codecs.ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one job save codec is required.", nameof(codecs));
        }

        if (values.Any(codec => codec is null || string.IsNullOrWhiteSpace(codec.TypeId)))
        {
            throw new ArgumentException("Job save codecs must have stable type ids.", nameof(codecs));
        }

        if (values.Select(codec => codec.TypeId)
            .Distinct(StringComparer.Ordinal)
            .Count() != values.Length)
        {
            throw new ArgumentException("Job save codec type ids must be unique.", nameof(codecs));
        }

        _codecs = values;
        _byType = values.ToDictionary(codec => codec.TypeId, StringComparer.Ordinal);
    }

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        IJobDefinitionSaveCodec? codec = _codecs.FirstOrDefault(item => item.CanEncode(definition));
        if (codec is null)
        {
            throw new InvalidOperationException(
                $"No save codec is registered for job definition '{definition.GetType().FullName}'.");
        }

        JobDefinitionSaveData data = codec.Encode(definition);
        data.TypeId = codec.TypeId;
        Normalize(data);
        return data;
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (!_byType.TryGetValue(data.TypeId, out IJobDefinitionSaveCodec? codec))
        {
            throw new InvalidOperationException(
                $"Unknown saved job definition type '{data.TypeId}'.");
        }

        Normalize(data);
        return codec.Decode(data);
    }

    private static void Normalize(JobDefinitionSaveData data)
    {
        data.Dependencies = data.Dependencies
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
        data.Properties = data.Properties
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .ToList();
        if (data.Properties.Select(value => value.Key)
            .Distinct(StringComparer.Ordinal)
            .Count() != data.Properties.Count)
        {
            throw new InvalidOperationException(
                "Saved job definition properties must have unique keys.");
        }
    }
}
}
