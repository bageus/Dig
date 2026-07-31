using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class MiningOutputCommitOutputSaveData
{
    [DataMember(Order = 1)]
    public string ItemId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public int Quantity { get; set; }

    [DataMember(Order = 3)]
    public List<string> StackIds { get; set; } = new List<string>();
}

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

    // v1 compatibility fields.
    [DataMember(Order = 5)]
    public string ItemId { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public int Quantity { get; set; }

    [DataMember(Order = 7)]
    public string? StackId { get; set; }

    [DataMember(Order = 8)]
    public bool HasStack { get; set; }

    // v2 source diagnostics and multi-output payload.
    [DataMember(Order = 9)]
    public string SourceId { get; set; } = string.Empty;

    [DataMember(Order = 10)]
    public int SourceVersion { get; set; }

    [DataMember(Order = 11)]
    public List<MiningOutputCommitOutputSaveData> Outputs { get; set; } =
        new List<MiningOutputCommitOutputSaveData>();
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
            MiningOutputCommitSaveData saved = new MiningOutputCommitSaveData
            {
                X = entry.Cell.X,
                Y = entry.Cell.Y,
                Z = entry.Cell.Z,
                SourceKind = (int)entry.SourceKind,
                SourceId = entry.SourceId,
                SourceVersion = entry.SourceVersion,
            };
            foreach (MiningOutputCommitLineSaveEntry output in entry.Outputs)
            {
                saved.Outputs.Add(new MiningOutputCommitOutputSaveData
                {
                    ItemId = output.ItemId,
                    Quantity = output.Quantity,
                    StackIds = output.StackIds.ToList(),
                });
            }

            if (entry.Outputs.Count == 1)
            {
                MiningOutputCommitLineSaveEntry output = entry.Outputs[0];
                saved.ItemId = output.ItemId;
                saved.Quantity = output.Quantity;
                saved.StackId = output.StackIds.Count == 1
                    ? output.StackIds[0]
                    : null;
                saved.HasStack = true;
            }

            data.Commits.Add(saved);
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

            if (data.FormatVersion == 1)
            {
                commits.Add(new MiningOutputCommitSaveEntry(
                    new CellId(saved.X, saved.Y, saved.Z),
                    (MiningOutputSourceKind)saved.SourceKind,
                    saved.ItemId,
                    saved.Quantity,
                    saved.StackId,
                    saved.HasStack));
                continue;
            }

            if (saved.Outputs == null
                || string.IsNullOrWhiteSpace(saved.SourceId)
                || saved.SourceVersion <= 0)
            {
                throw new InvalidOperationException(
                    "Mining output v2 save entry is incomplete.");
            }

            commits.Add(new MiningOutputCommitSaveEntry(
                new CellId(saved.X, saved.Y, saved.Z),
                (MiningOutputSourceKind)saved.SourceKind,
                saved.SourceId,
                saved.SourceVersion,
                saved.Outputs.Select(output => new MiningOutputCommitLineSaveEntry(
                    output.ItemId,
                    output.Quantity,
                    output.StackIds ?? new List<string>()))));
        }

        return new MiningOutputCommitSaveSnapshot(data.FormatVersion, commits);
    }
}

}
