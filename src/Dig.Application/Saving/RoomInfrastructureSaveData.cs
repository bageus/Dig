using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class RoomInfrastructureSaveData
{
    [DataMember(Order = 1)] public long Version { get; set; }
    [DataMember(Order = 2)] public ulong NextRuntimeSequence { get; set; } = 1UL;
    [DataMember(Order = 3)] public List<RoomInfrastructureProjectSaveData> Rooms { get; set; }
        = new List<RoomInfrastructureProjectSaveData>();
    [DataMember(Order = 4)] public List<RoomInfrastructureProvenanceSaveData> Provenance { get; set; }
        = new List<RoomInfrastructureProvenanceSaveData>();
}

[DataContract]
public sealed class RoomInfrastructureProjectSaveData
{
    [DataMember(Order = 1)] public string RoomInfrastructureId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string TemplateInstanceId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int TemplateKind { get; set; }
    [DataMember(Order = 4)] public int UpgradeOrderCount { get; set; }
    [DataMember(Order = 5)] public int Status { get; set; }
    [DataMember(Order = 6)] public bool CancellationLocked { get; set; }
    [DataMember(Order = 7)] public int RequestedPurpose { get; set; }
    [DataMember(Order = 8)] public int ActivePurpose { get; set; }
    [DataMember(Order = 9, EmitDefaultValue = false)]
    public RoomCellSaveData? TemporaryStockCell { get; set; }
    [DataMember(Order = 10)] public List<RoomMaterialLedgerSaveData> Materials { get; set; }
        = new List<RoomMaterialLedgerSaveData>();
    [DataMember(Order = 11)] public List<RoomMaterialUnitSaveData> CompletedMaterialUnits { get; set; }
        = new List<RoomMaterialUnitSaveData>();
    [DataMember(Order = 12)] public List<string> ActiveJobIds { get; set; }
        = new List<string>();
    [DataMember(Order = 13)] public long Version { get; set; }
}

[DataContract]
public sealed class RoomMaterialLedgerSaveData
{
    [DataMember(Order = 1)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Required { get; set; }
    [DataMember(Order = 3)] public int Delivered { get; set; }
    [DataMember(Order = 4)] public int Consumed { get; set; }
    [DataMember(Order = 5)] public int ReleasedOnCancel { get; set; }
}

[DataContract]
public sealed class RoomMaterialUnitSaveData
{
    [DataMember(Order = 1)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Ordinal { get; set; }
}

[DataContract]
public sealed class RoomInfrastructureProvenanceSaveData
{
    [DataMember(Order = 1)] public string RoomInfrastructureId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string TemplateInstanceId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int TemplateKind { get; set; }
    [DataMember(Order = 4)] public List<RoomCellSaveData> OrderedRoomCells { get; set; }
        = new List<RoomCellSaveData>();
}

[DataContract]
public sealed class RoomCellSaveData
{
    [DataMember(Order = 1)] public int X { get; set; }
    [DataMember(Order = 2)] public int Y { get; set; }
    [DataMember(Order = 3)] public int Z { get; set; }
}

public sealed class SaveVersionFifteenRoomInfrastructureMigration
    : ISaveMigration
{
    public string Id => "save.v15_to_v16.room_infrastructure";
    public int FromVersion => 15;
    public int ToVersion => 16;

    public void Apply(SaveGameDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException(
                "Migration received the wrong source version.");
        }

        document.RoomInfrastructure ??= new RoomInfrastructureSaveData
        {
            NextRuntimeSequence = RoomInfrastructureSaveAdapter
                .ResolveLegacyNextRuntimeSequence(
                    document.Jobs,
                    document.Inventory),
        };
        document.FormatVersion = ToVersion;
    }
}

}
