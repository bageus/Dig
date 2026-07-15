using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Dig.Application.Saving;

namespace Dig.Infrastructure.Saving
{

public sealed class DataContractJsonSaveCodec : ISaveGameCodec
{
    private readonly DataContractJsonSerializer _serializer =
        new DataContractJsonSerializer(
            typeof(SaveGameDocument),
            new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true,
            });

    public byte[] Serialize(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        using MemoryStream stream = new MemoryStream();
        _serializer.WriteObject(stream, document);
        return stream.ToArray();
    }

    public SaveGameDocument Deserialize(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length == 0)
        {
            throw new SerializationException("Save file is empty.");
        }

        using MemoryStream stream = new MemoryStream(bytes, writable: false);
        object? value = _serializer.ReadObject(stream);
        return value as SaveGameDocument
            ?? throw new SerializationException("Save file does not contain a game document.");
    }
}
}
