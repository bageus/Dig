using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class InventorySaveData
{
    [DataMember(Order = 1)]
    public long Version { get; set; }

    [DataMember(Order = 2)]
    public List<ItemStackSaveData> Stacks { get; set; } =
        new List<ItemStackSaveData>();

    [DataMember(Order = 3)]
    public List<HeldItemReferenceSaveData> HeldItems { get; set; } =
        new List<HeldItemReferenceSaveData>();
}

[DataContract]
public sealed class ItemStackSaveData
{
    [DataMember(Order = 1)]
    public string StackId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string ItemId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int Quantity { get; set; }

    [DataMember(Order = 4)]
    public ItemLocationSaveData Location { get; set; } =
        new ItemLocationSaveData();

    [DataMember(Order = 5)]
    public List<ItemReservationSaveData> Reservations { get; set; } =
        new List<ItemReservationSaveData>();
}

[DataContract]
public sealed class ItemLocationSaveData
{
    [DataMember(Order = 1)]
    public int Kind { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public string? OwnerId { get; set; }

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public int? CellX { get; set; }

    [DataMember(Order = 4, EmitDefaultValue = false)]
    public int? CellY { get; set; }

    [DataMember(Order = 5, EmitDefaultValue = false)]
    public int? ResidentCompartment { get; set; }

    [DataMember(Order = 6, EmitDefaultValue = false)]
    public int? ResidentSlotIndex { get; set; }
}

[DataContract]
public sealed class ItemReservationSaveData
{
    [DataMember(Order = 1)]
    public string JobId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public int Quantity { get; set; }
}

[DataContract]
public sealed class HeldItemReferenceSaveData
{
    [DataMember(Order = 1)]
    public string ResidentId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string StackId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int Quantity { get; set; }

    [DataMember(Order = 4)]
    public int Purpose { get; set; }
}

}
