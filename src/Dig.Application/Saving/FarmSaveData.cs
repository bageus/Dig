using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class FarmSaveData
{
    [DataMember(Order = 1)]
    public List<FarmStateSaveData> Farms { get; set; } = new List<FarmStateSaveData>();

    [DataMember(Order = 2)]
    public List<FarmLogisticsReservationSaveData> Reservations { get; set; } =
        new List<FarmLogisticsReservationSaveData>();
}

[DataContract]
public sealed class FarmLogisticsReservationSaveData
{
    [DataMember(Order = 1)] public string JobId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string BuildingId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Kind { get; set; }
    [DataMember(Order = 4)] public int Quantity { get; set; }
    [DataMember(Order = 5)] public int Direction { get; set; }
}

[DataContract]
public sealed class FarmStateSaveData
{
    [DataMember(Order = 1)] public string BuildingId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Mode { get; set; }
    [DataMember(Order = 3)] public bool MushroomSeedEstablished { get; set; }
    [DataMember(Order = 4)] public int MushroomSlotsOccupied { get; set; }
    [DataMember(Order = 5)] public int ResidualMushrooms { get; set; }
    [DataMember(Order = 6)] public int HamsterCount { get; set; }
    [DataMember(Order = 7)] public int GrubCount { get; set; }
    [DataMember(Order = 8)] public int FeedCount { get; set; }
    [DataMember(Order = 9)] public long NextReproductionTick { get; set; } = -1;
    [DataMember(Order = 10)] public long NextFeedConsumptionTick { get; set; } = -1;
    [DataMember(Order = 11)] public int EscapingHamsterCount { get; set; }
    [DataMember(Order = 12)] public int EscapingGrubCount { get; set; }
    [DataMember(Order = 13)] public long NextEscapeTick { get; set; } = -1;
}

}
