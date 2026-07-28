using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class BarrelSaveData
{
    [DataMember(Order = 1)]
    public List<BarrelEntitySaveData> Barrels { get; set; } =
        new List<BarrelEntitySaveData>();
}

[DataContract]
public sealed class BarrelEntitySaveData
{
    [DataMember(Order = 1)] public string BarrelId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string DefinitionId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int X { get; set; }
    [DataMember(Order = 4)] public int Y { get; set; }
    [DataMember(Order = 5)] public int Z { get; set; }
    [DataMember(Order = 6)] public int Lifecycle { get; set; }
    [DataMember(Order = 7)] public string ContentsItemId { get; set; } = string.Empty;
    [DataMember(Order = 8)] public long ContentsGeneration { get; set; }
    [DataMember(Order = 9)] public bool ContentsMaterialized { get; set; }
    [DataMember(Order = 10, EmitDefaultValue = false)] public int? FallSourceX { get; set; }
    [DataMember(Order = 11, EmitDefaultValue = false)] public int? FallSourceY { get; set; }
    [DataMember(Order = 12, EmitDefaultValue = false)] public int? FallSourceZ { get; set; }
    [DataMember(Order = 13, EmitDefaultValue = false)] public int? FallLandingX { get; set; }
    [DataMember(Order = 14, EmitDefaultValue = false)] public int? FallLandingY { get; set; }
    [DataMember(Order = 15, EmitDefaultValue = false)] public int? FallLandingZ { get; set; }
    [DataMember(Order = 16)] public long Version { get; set; }
}

}
