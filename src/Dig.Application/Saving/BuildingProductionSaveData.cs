using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class BuildingProductionSaveData
{
    [DataMember(Order = 1)]
    public List<ProductionOrderSaveData> Orders { get; set; } =
        new List<ProductionOrderSaveData>();

    [DataMember(Order = 2)]
    public List<BuildingSupplySaveData> Supplies { get; set; } =
        new List<BuildingSupplySaveData>();

    [DataMember(Order = 3)]
    public List<ProductionOutputPackageSaveData> Packages { get; set; } =
        new List<ProductionOutputPackageSaveData>();
}


[DataContract]
public sealed class ProductionOutputPackageSaveData
{
    [DataMember(Order = 1)] public string StackId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string OrderId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Kind { get; set; }
    [DataMember(Order = 4)] public long Version { get; set; }
    [DataMember(Order = 5)]
    public List<ProductionPackageManifestItemSaveData> Manifest { get; set; } =
        new List<ProductionPackageManifestItemSaveData>();
}

[DataContract]
public sealed class ProductionPackageManifestItemSaveData
{
    [DataMember(Order = 1)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Quantity { get; set; }
}

[DataContract]
public sealed class ProductionOrderSaveData
{
    [DataMember(Order = 1)] public string OrderId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string RecipeId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string BuildingId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public long Sequence { get; set; }
    [DataMember(Order = 5)] public int Status { get; set; }
    [DataMember(Order = 6)] public int CompletedWork { get; set; }
    [DataMember(Order = 7)] public long Version { get; set; }
    [DataMember(Order = 8, EmitDefaultValue = false)]
    public string? Reason { get; set; }
    [DataMember(Order = 9)]
    public List<ProductionInputAllocationSaveData> InputAllocations { get; set; } =
        new List<ProductionInputAllocationSaveData>();
    [DataMember(Order = 10)]
    public List<ProductionMaterialStepSaveData> MaterialSteps { get; set; } =
        new List<ProductionMaterialStepSaveData>();
}

[DataContract]
public sealed class ProductionInputAllocationSaveData
{
    [DataMember(Order = 1)] public string StackId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Quantity { get; set; }
}

[DataContract]
public sealed class ProductionMaterialStepSaveData
{
    [DataMember(Order = 1)] public int Index { get; set; }
    [DataMember(Order = 2)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public long RequiredTicks { get; set; }
    [DataMember(Order = 4)] public long CompletedTicks { get; set; }
    [DataMember(Order = 5)] public bool IsConsumed { get; set; }
    [DataMember(Order = 6, EmitDefaultValue = false)]
    public int? Phase { get; set; }
}

[DataContract]
public sealed class BuildingSupplySaveData
{
    [DataMember(Order = 1)] public string BuildingId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WorkstationId { get; set; } = string.Empty;
    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? ActiveSupplyJobId { get; set; }
    [DataMember(Order = 4)]
    public List<BuildingStockRuleSaveData> Stocks { get; set; } =
        new List<BuildingStockRuleSaveData>();
    [DataMember(Order = 5, EmitDefaultValue = false)]
    public int? OperationTurn { get; set; }
}

[DataContract]
public sealed class BuildingStockRuleSaveData
{
    [DataMember(Order = 1)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Incoming { get; set; }
    [DataMember(Order = 3)] public bool DeliveryEnabled { get; set; }
}

}
