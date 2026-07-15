using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class WorldSaveData
{
    [DataMember(Order = 1)]
    public int Width { get; set; }

    [DataMember(Order = 2)]
    public int Height { get; set; }

    [DataMember(Order = 3)]
    public int ChunkSize { get; set; }

    [DataMember(Order = 4)]
    public long Version { get; set; }

    [DataMember(Order = 5)]
    public List<WorldChunkSaveData> Chunks { get; set; } =
        new List<WorldChunkSaveData>();
}

[DataContract]
public sealed class WorldChunkSaveData
{
    [DataMember(Order = 1)]
    public int X { get; set; }

    [DataMember(Order = 2)]
    public int Y { get; set; }

    [DataMember(Order = 3)]
    public long Version { get; set; }

    [DataMember(Order = 4)]
    public List<WorldCellSaveData> Cells { get; set; } =
        new List<WorldCellSaveData>();
}

[DataContract]
public sealed class WorldCellSaveData
{
    [DataMember(Order = 1)]
    public int X { get; set; }

    [DataMember(Order = 2)]
    public int Y { get; set; }

    [DataMember(Order = 3)]
    public string MaterialId { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public int Designation { get; set; }

    [DataMember(Order = 5)]
    public bool IsExplored { get; set; }

    [DataMember(Order = 6)]
    public ushort Damage { get; set; }

    [DataMember(Order = 7)]
    public short Temperature { get; set; }
}
}
