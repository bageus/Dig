using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class TunnelInfrastructureSaveData
{
    [DataMember(Order = 1)] public long Version { get; set; }
    [DataMember(Order = 2)] public ulong NextAutomaticJobSequence { get; set; } = 1UL;
    [DataMember(Order = 3)] public List<TunnelSegmentSaveData> Segments { get; set; }
        = new List<TunnelSegmentSaveData>();
    [DataMember(Order = 4)] public List<TunnelCellSaveData> CompletedJunctionStoneTrimCells { get; set; }
        = new List<TunnelCellSaveData>();
    [DataMember(Order = 5)] public List<TunnelJunctionStoneTrimTargetSaveData> PendingJunctionStoneTrimTargets { get; set; }
        = new List<TunnelJunctionStoneTrimTargetSaveData>();
}

[DataContract]
public sealed class TunnelSegmentSaveData
{
    [DataMember(Order = 1)] public string SegmentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int OriginKind { get; set; }
    [DataMember(Order = 3)] public int OriginX { get; set; }
    [DataMember(Order = 4)] public int OriginY { get; set; }
    [DataMember(Order = 5)] public int OriginZ { get; set; }
    [DataMember(Order = 6)] public long Version { get; set; }
    [DataMember(Order = 7)] public List<TunnelCellSaveData> OrderedHorizontalCells { get; set; }
        = new List<TunnelCellSaveData>();
    [DataMember(Order = 8)] public List<TunnelStructuralAnchorSaveData> StructuralAnchors { get; set; }
        = new List<TunnelStructuralAnchorSaveData>();
    [DataMember(Order = 9, EmitDefaultValue = false)]
    public TunnelAutomaticSupportTargetSaveData? NextAutomaticSupportTarget { get; set; }
}

[DataContract]
public sealed class TunnelCellSaveData
{
    [DataMember(Order = 1)] public int X { get; set; }
    [DataMember(Order = 2)] public int Y { get; set; }
    [DataMember(Order = 3)] public int Z { get; set; }
}

[DataContract]
public sealed class TunnelStructuralAnchorSaveData
{
    [DataMember(Order = 1)] public int X { get; set; }
    [DataMember(Order = 2)] public int Y { get; set; }
    [DataMember(Order = 3)] public int Z { get; set; }
    [DataMember(Order = 4)] public int Kind { get; set; }
    [DataMember(Order = 5)] public int DistanceFromOrigin { get; set; }
}

[DataContract]
public sealed class TunnelAutomaticSupportTargetSaveData
{
    [DataMember(Order = 1)] public string SegmentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int AnchorX { get; set; }
    [DataMember(Order = 3)] public int AnchorY { get; set; }
    [DataMember(Order = 4)] public int AnchorZ { get; set; }
    [DataMember(Order = 5)] public int TargetX { get; set; }
    [DataMember(Order = 6)] public int TargetY { get; set; }
    [DataMember(Order = 7)] public int TargetZ { get; set; }
    [DataMember(Order = 8)] public int DistanceFromAnchor { get; set; }
}

[DataContract]
public sealed class TunnelJunctionStoneTrimTargetSaveData
{
    [DataMember(Order = 1)] public string OwnerSegmentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int X { get; set; }
    [DataMember(Order = 3)] public int Y { get; set; }
    [DataMember(Order = 4)] public int Z { get; set; }
}

public sealed class SaveVersionFourteenTunnelInfrastructureMigration
    : ISaveMigration
{
    public string Id => "save.v14_to_v15.tunnel_infrastructure";
    public int FromVersion => 14;
    public int ToVersion => 15;

    public void Apply(SaveGameDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new System.InvalidOperationException(
                "Migration received the wrong source version.");
        }

        document.TunnelInfrastructure ??= new TunnelInfrastructureSaveData
        {
            NextAutomaticJobSequence =
                TunnelInfrastructureSaveAdapter.ResolveLegacyNextSequence(
                    document.Jobs),
        };
        document.FormatVersion = ToVersion;
    }
}

}
