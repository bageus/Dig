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
    private readonly IReadOnlyCollection<Type> _registeredDefinitionTypes;

    public JobDefinitionSaveRegistry(IEnumerable<IJobDefinitionSaveCodec> codecs)
        : this(codecs, Array.Empty<Type>())
    {
    }

    public JobDefinitionSaveRegistry(
        IEnumerable<JobDefinitionSaveRegistration> registrations)
        : this(
            (registrations ?? throw new ArgumentNullException(nameof(registrations)))
                .Select(value => value.Codec),
            registrations.Select(value => value.DefinitionType))
    {
    }

    private JobDefinitionSaveRegistry(
        IEnumerable<IJobDefinitionSaveCodec> codecs,
        IEnumerable<Type> definitionTypes)
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

        Type[] registeredTypes = definitionTypes.ToArray();
        if (registeredTypes.Distinct().Count() != registeredTypes.Length)
        {
            throw new ArgumentException(
                "Job definition save registrations must be unique.",
                nameof(definitionTypes));
        }

        if (registeredTypes.Length != 0 && registeredTypes.Length != values.Length)
        {
            throw new ArgumentException(
                "Every registered codec requires one concrete definition type.",
                nameof(definitionTypes));
        }

        _codecs = values;
        _byType = values.ToDictionary(codec => codec.TypeId, StringComparer.Ordinal);
        _registeredDefinitionTypes = registeredTypes;
    }

    public IReadOnlyList<string> RegisteredTypeIds => _codecs
        .Select(value => value.TypeId)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();

    public void ValidateCoverage(IEnumerable<Type> definitionTypes)
    {
        if (definitionTypes is null)
        {
            throw new ArgumentNullException(nameof(definitionTypes));
        }

        foreach (Type definitionType in definitionTypes.Distinct())
        {
            if (!typeof(JobDefinition).IsAssignableFrom(definitionType)
                || definitionType.IsAbstract)
            {
                throw new ArgumentException(
                    "Coverage entries must be concrete job definition types.",
                    nameof(definitionTypes));
            }

            if (!_registeredDefinitionTypes.Contains(definitionType))
            {
                throw new InvalidOperationException(
                    $"No save codec is registered for job definition '{definitionType.FullName}'.");
            }
        }
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
            throw new KeyNotFoundException(
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