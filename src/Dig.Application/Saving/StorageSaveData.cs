using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class StorageSaveData
{
    [DataMember(Order = 1)]
    public long Version { get; set; }

    [DataMember(Order = 2)]
    public List<StorageZoneSaveData> Zones { get; set; } =
        new List<StorageZoneSaveData>();

    [DataMember(Order = 3)]
    public List<StorageReservationSaveData> Reservations { get; set; } =
        new List<StorageReservationSaveData>();
}

[DataContract]
public sealed class StorageZoneSaveData
{
    [DataMember(Order = 1)] public string Id { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Name { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Priority { get; set; }
    [DataMember(Order = 4)] public int Capacity { get; set; }
    [DataMember(Order = 5)] public int X { get; set; }
    [DataMember(Order = 6)] public int Y { get; set; }
    [DataMember(Order = 7)] public int Z { get; set; }
    [DataMember(Order = 8)] public bool AcceptsAll { get; set; }
    [DataMember(Order = 9)] public List<string> AllowedItems { get; set; } =
        new List<string>();
    [DataMember(Order = 10)] public List<string> AllowedCategories { get; set; } =
        new List<string>();
}

[DataContract]
public sealed class StorageReservationSaveData
{
    [DataMember(Order = 1)] public string JobId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ZoneId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public int Quantity { get; set; }
}

}
