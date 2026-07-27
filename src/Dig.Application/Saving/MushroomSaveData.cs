using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class MushroomSaveData
{
    [DataMember(Order = 1)]
    public List<MushroomSiteSaveData> Sites { get; set; } =
        new List<MushroomSiteSaveData>();
}

[DataContract]
public sealed class MushroomSiteSaveData
{
    [DataMember(Order = 1)]
    public string SiteId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string DefinitionId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int X { get; set; }

    [DataMember(Order = 4)]
    public int Y { get; set; }

    [DataMember(Order = 5)]
    public int Z { get; set; }

    [DataMember(Order = 6)]
    public int Stage { get; set; }

    [DataMember(Order = 7)]
    public long StageStartedTick { get; set; }

    [DataMember(Order = 8, EmitDefaultValue = false)]
    public long? NextStageTick { get; set; }

    [DataMember(Order = 9)]
    public long GrowthGeneration { get; set; }

    [DataMember(Order = 10, EmitDefaultValue = false)]
    public string? ActiveChopJobId { get; set; }

    [DataMember(Order = 11, EmitDefaultValue = false)]
    public string? ActiveWorkerId { get; set; }

    [DataMember(Order = 12)]
    public int RequiredSwings { get; set; }

    [DataMember(Order = 13)]
    public int CompletedSwings { get; set; }

    [DataMember(Order = 14, EmitDefaultValue = false)]
    public long? GrowthPausedAtTick { get; set; }

    [DataMember(Order = 15)]
    public long Version { get; set; }
}

}
