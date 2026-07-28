using System;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public sealed class JobDefinitionSaveRegistration
{
    public JobDefinitionSaveRegistration(
        Type definitionType,
        IJobDefinitionSaveCodec codec)
    {
        if (definitionType is null)
        {
            throw new ArgumentNullException(nameof(definitionType));
        }

        if (!typeof(JobDefinition).IsAssignableFrom(definitionType)
            || definitionType.IsAbstract)
        {
            throw new ArgumentException(
                "A save registration requires a concrete job definition type.",
                nameof(definitionType));
        }

        DefinitionType = definitionType;
        Codec = codec ?? throw new ArgumentNullException(nameof(codec));
    }

    public Type DefinitionType { get; }
    public IJobDefinitionSaveCodec Codec { get; }
}

}