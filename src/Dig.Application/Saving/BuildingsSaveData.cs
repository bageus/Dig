using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class BuildingsSaveData
{
    [DataMember(Order = 1)]
    public List<BuildingSaveData> Buildings { get; set; } =
        new List<BuildingSaveData>();
}

[DataContract]
public sealed class BuildingSaveData
{
    [DataMember(Order = 1)]
    public string BuildingId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string DefinitionId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int OriginX { get; set; }

    [DataMember(Order = 4)]
    public int OriginY { get; set; }

    [DataMember(Order = 5)]
    public int Orientation { get; set; }

    [DataMember(Order = 6)]
    public int WorkPositionX { get; set; }

    [DataMember(Order = 7)]
    public int WorkPositionY { get; set; }

    [DataMember(Order = 8)]
    public int Status { get; set; }

    [DataMember(Order = 9)]
    public int CompletedWork { get; set; }

    [DataMember(Order = 10)]
    public int Durability { get; set; }

    [DataMember(Order = 11)]
    public long Version { get; set; }

    [DataMember(Order = 12, EmitDefaultValue = false)]
    public string? DiagnosticReason { get; set; }

    [DataMember(Order = 13, EmitDefaultValue = false)]
    public BuildingBoxPlanSaveData? BoxPlan { get; set; }

    [DataMember(Order = 14, EmitDefaultValue = false)]
    public BuildingPackingPlanSaveData? PackingPlan { get; set; }
}

[DataContract]
public sealed class BuildingBoxPlanSaveData
{
    [DataMember(Order = 1)]
    public string SourceStackId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string JobId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int CommitState { get; set; }
}

[DataContract]
public sealed class BuildingPackingPlanSaveData
{
    [DataMember(Order = 1)]
    public string JobId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string OutputStackId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int CompletedWork { get; set; }

    [DataMember(Order = 4)]
    public int CommitState { get; set; }
}
}
