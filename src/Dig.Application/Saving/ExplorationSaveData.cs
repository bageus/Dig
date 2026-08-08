using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{
[DataContract]
public sealed class ExplorationSaveData
{
    [DataMember(Order = 1)] public int SchemaVersion { get; set; } = 1;
    [DataMember(Order = 2)] public List<ExploredCellSaveData> Explored { get; set; }
        = new List<ExploredCellSaveData>();
    [DataMember(Order = 3)] public List<WorldItemMemorySaveData> ItemMarkers { get; set; }
        = new List<WorldItemMemorySaveData>();
}

[DataContract]
public sealed class ExploredCellSaveData
{
    [DataMember(Order = 1)] public int X { get; set; }
    [DataMember(Order = 2)] public int Y { get; set; }
    [DataMember(Order = 3)] public int Z { get; set; }
}

[DataContract]
public sealed class WorldItemMemorySaveData
{
    [DataMember(Order = 1)] public string StackId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int X { get; set; }
    [DataMember(Order = 4)] public int Y { get; set; }
    [DataMember(Order = 5)] public int Z { get; set; }
    [DataMember(Order = 6)] public long ObservedTick { get; set; }
}
}
