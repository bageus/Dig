using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Dig.Application.Saving;

namespace Dig.Infrastructure.Saving
{

public enum SaveStorageFailureKind
{
    InvalidSlotId = 0,
    Missing = 1,
    Corrupted = 2,
    IoFailure = 3,
}

public sealed class SaveStorageException : Exception
{
    public SaveStorageException(
        SaveStorageFailureKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public SaveStorageFailureKind Kind { get; }
}

public sealed class FileSaveSlotStore : ISaveSlotStore
{
    private const string Extension = ".digsave";
    private readonly string _directory;
    private readonly ISaveGameCodec _codec;

    public FileSaveSlotStore(string directory, ISaveGameCodec codec)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Save directory is required.", nameof(directory));
        }

        _directory = Path.GetFullPath(directory);
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
    }

    public void Save(string slotId, SaveGameDocument document)
    {
        ValidateSlotId(slotId);
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (!string.Equals(
            slotId,
            document.Metadata?.SlotId,
            StringComparison.Ordinal))
        {
            throw new SaveStorageException(
                SaveStorageFailureKind.InvalidSlotId,
                "Save metadata slot id does not match the destination slot.");
        }

        Directory.CreateDirectory(_directory);
        string target = GetPath(slotId);
        string temporary = target + ".tmp";
        string backup = target + ".bak";
        DeleteIfExists(temporary);
        byte[] bytes;
        try
        {
            bytes = _codec.Serialize(document);
            using FileStream stream = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
        catch (Exception exception) when (exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SerializationException)
        {
            DeleteIfExists(temporary);
            throw new SaveStorageException(
                SaveStorageFailureKind.IoFailure,
                "Unable to write the temporary save file.",
                exception);
        }

        bool movedOriginal = false;
        try
        {
            DeleteIfExists(backup);
            if (File.Exists(target))
            {
                File.Move(target, backup);
                movedOriginal = true;
            }

            File.Move(temporary, target);
            DeleteIfExists(backup);
        }
        catch (Exception exception) when (exception is IOException
            || exception is UnauthorizedAccessException)
        {
            DeleteIfExists(target);
            if (movedOriginal && File.Exists(backup))
            {
                File.Move(backup, target);
            }

            DeleteIfExists(temporary);
            throw new SaveStorageException(
                SaveStorageFailureKind.IoFailure,
                "Atomic save replacement failed; the previous slot was restored.",
                exception);
        }
    }

    public SaveGameDocument Load(string slotId)
    {
        ValidateSlotId(slotId);
        Directory.CreateDirectory(_directory);
        string target = GetPath(slotId);
        RecoverInterruptedReplacement(target);
        if (!File.Exists(target))
        {
            throw new SaveStorageException(
                SaveStorageFailureKind.Missing,
                $"Save slot '{slotId}' does not exist.");
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(target);
            SaveGameDocument document = _codec.Deserialize(bytes);
            if (!string.Equals(
                slotId,
                document.Metadata?.SlotId,
                StringComparison.Ordinal))
            {
                throw new SerializationException(
                    "Save metadata does not match the selected slot.");
            }

            return document;
        }
        catch (Exception exception) when (exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SerializationException)
        {
            throw new SaveStorageException(
                exception is SerializationException
                    ? SaveStorageFailureKind.Corrupted
                    : SaveStorageFailureKind.IoFailure,
                $"Unable to load save slot '{slotId}'.",
                exception);
        }
    }

    public IReadOnlyList<SaveSlotInfo> List()
    {
        Directory.CreateDirectory(_directory);
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        foreach (string path in Directory.GetFiles(_directory, "*" + Extension)
            .OrderBy(value => value, StringComparer.Ordinal))
        {
            string slotId = Path.GetFileNameWithoutExtension(path);
            try
            {
                SaveGameDocument document = Load(slotId);
                slots.Add(new SaveSlotInfo(
                    slotId,
                    document.Metadata,
                    isCorrupted: false,
                    errorMessage: null));
            }
            catch (SaveStorageException exception)
            {
                slots.Add(new SaveSlotInfo(
                    slotId,
                    metadata: null,
                    isCorrupted: true,
                    exception.Message));
            }
        }

        return new ReadOnlyCollection<SaveSlotInfo>(slots);
    }

    private string GetPath(string slotId)
    {
        return Path.Combine(_directory, slotId + Extension);
    }

    private static void RecoverInterruptedReplacement(string target)
    {
        string backup = target + ".bak";
        string temporary = target + ".tmp";
        if (!File.Exists(target) && File.Exists(backup))
        {
            File.Move(backup, target);
        }

        DeleteIfExists(temporary);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void ValidateSlotId(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)
            || slotId.Length > 64
            || slotId.Any(character => !char.IsLetterOrDigit(character)
                && character != '_'
                && character != '-'))
        {
            throw new SaveStorageException(
                SaveStorageFailureKind.InvalidSlotId,
                "Save slot id may contain only letters, digits, '_' and '-'.");
        }
    }
}
}
