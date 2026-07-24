using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class MiningOutputCommitSaveData
{
    [DataMember(Order = 1)]
    public int X { get; set; }

    [DataMember(Order = 2)]
    public int Y { get; set; }

    [DataMember(Order = 3)]
    public int Z { get; set; }

    [DataMember(Order = 4)]
    public int SourceKind { get; set; }

    [DataMember(Order = 5)]
    public string ItemId { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public int Quantity { get; set; }

    [DataMember(Order = 7)]
    public string? StackId { get; set; }

    [DataMember(Order = 8)]
    public bool HasStack { get; set; }
}

[DataContract]
public sealed class MiningOutputCommitsSaveData
{
    [DataMember(Order = 1)]
    public int FormatVersion { get; set; } = MiningOutputCommitSaveSnapshot.CurrentFormatVersion;

    [DataMember(Order = 2)]
    public List<MiningOutputCommitSaveData> Commits { get; set; } =
        new List<MiningOutputCommitSaveData>();
}

public static class MiningOutputSaveDataAdapter
{
    public static MiningOutputCommitsSaveData Encode(
        MiningOutputCommitSaveSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        MiningOutputCommitsSaveData data = new MiningOutputCommitsSaveData
        {
            FormatVersion = snapshot.FormatVersion,
        };
        foreach (MiningOutputCommitSaveEntry entry in snapshot.Commits
            .OrderBy(value => value.Cell))
        {
            data.Commits.Add(new MiningOutputCommitSaveData
            {
                X = entry.Cell.X,
                Y = entry.Cell.Y,
                Z = entry.Cell.Z,
                SourceKind = (int)entry.SourceKind,
                ItemId = entry.ItemId,
                Quantity = entry.Quantity,
                StackId = entry.StackId,
                HasStack = entry.HasStack,
            });
        }

        return data;
    }

    public static MiningOutputCommitSaveSnapshot Decode(
        MiningOutputCommitsSaveData data)
    {
        if (data == null || data.Commits == null)
        {
            throw new InvalidOperationException("Mining output save data is missing.");
        }

        if (data.FormatVersion <= 0)
        {
            throw new InvalidOperationException("Mining output save data version is invalid.");
        }

        List<MiningOutputCommitSaveEntry> commits = new List<MiningOutputCommitSaveEntry>();
        foreach (MiningOutputCommitSaveData saved in data.Commits
            .OrderBy(value => value.Z)
            .ThenBy(value => value.Y)
            .ThenBy(value => value.X))
        {
            if (saved == null
                || !Enum.IsDefined(typeof(MiningOutputSourceKind), saved.SourceKind))
            {
                throw new InvalidOperationException("Mining output save entry is invalid.");
            }

            commits.Add(new MiningOutputCommitSaveEntry(
                new CellId(saved.X, saved.Y, saved.Z),
                (MiningOutputSourceKind)saved.SourceKind,
                saved.ItemId,
                saved.Quantity,
                saved.StackId,
                saved.HasStack));
        }

        return new MiningOutputCommitSaveSnapshot(data.FormatVersion, commits);
    }
}

}
