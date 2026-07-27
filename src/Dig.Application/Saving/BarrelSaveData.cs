using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class BarrelSaveData
{
    [DataMember(Order = 1)] public string BarrelId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string DefinitionId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int CellX { get; set; }
    [DataMember(Order = 4)] public int CellY { get; set; }
    [DataMember(Order = 5)] public int CellZ { get; set; }
    [DataMember(Order = 6)] public int Lifecycle { get; set; }
    [DataMember(Order = 7)] public string ContentsItemId { get; set; } = string.Empty;
    [DataMember(Order = 8)] public long ContentsGeneration { get; set; }
    [DataMember(Order = 9)] public bool ContentsMaterialized { get; set; }
    [DataMember(Order = 10)] public bool HasFallSource { get; set; }
    [DataMember(Order = 11)] public int FallSourceX { get; set; }
    [DataMember(Order = 12)] public int FallSourceY { get; set; }
    [DataMember(Order = 13)] public int FallSourceZ { get; set; }
    [DataMember(Order = 14)] public bool HasFallLanding { get; set; }
    [DataMember(Order = 15)] public int FallLandingX { get; set; }
    [DataMember(Order = 16)] public int FallLandingY { get; set; }
    [DataMember(Order = 17)] public int FallLandingZ { get; set; }
    [DataMember(Order = 18)] public long Version { get; set; }
}

[DataContract]
public sealed class BarrelSectionSaveData
{
    [DataMember(Order = 1)] public BarrelSaveData[] Barrels { get; set; } =
        System.Array.Empty<BarrelSaveData>();
}

}