using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class TerrainDepositsSaveData
{
    [DataMember(Order = 1)]
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
    public int X { get; set; }

    [DataMember(Order = 4)]
    public int Y { get; set; }

    [DataMember(Order = 5)]
    public int Z { get; set; }

    [DataMember(Order = 6)]
    public bool IsRevealed { get; set; }

    [DataMember(Order = 7)]
    public int RemainingYield { get; set; }

    [DataMember(Order = 8)]
    public long Version { get; set; }
}

}
