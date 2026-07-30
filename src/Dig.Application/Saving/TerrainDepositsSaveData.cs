using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class TerrainDepositsSaveData
{
    [DataMember(Order = 1)]
    public int FormatVersion { get; set; }

    [DataMember(Order = 2)]
    public int GeneratorVersion { get; set; }

    [DataMember(Order = 3)]
    public List<TerrainDepositSaveData> Deposits { get; set; } =
        new List<TerrainDepositSaveData>();
}

[DataContract]
public sealed class TerrainDepositSaveData
{
    [DataMember(Order = 1)]
    public string InstanceId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string DefinitionId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int DefinitionVersion { get; set; }

    [DataMember(Order = 4)]
    public int X { get; set; }

    [DataMember(Order = 5)]
    public int Y { get; set; }

    [DataMember(Order = 6)]
    public int Z { get; set; }

    [DataMember(Order = 7)]
    public bool IsRevealed { get; set; }

    [DataMember(Order = 8)]
    public int RemainingYield { get; set; }

    [DataMember(Order = 9)]
    public long Version { get; set; }
}

}
