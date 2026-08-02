using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class VukerEcologySaveData
{
    [DataMember(Order = 1)] public ulong WorldSeed { get; set; }
    [DataMember(Order = 2)] public long CurrentTick { get; set; }
    [DataMember(Order = 3)] public long NextPairSequence { get; set; }
    [DataMember(Order = 4)] public long Version { get; set; }
    [DataMember(Order = 5)] public List<VukerIndividualSaveData> Individuals { get; set; }
        = new List<VukerIndividualSaveData>();
    [DataMember(Order = 6)] public List<VukerPairSaveData> Pairs { get; set; }
        = new List<VukerPairSaveData>();
}

[DataContract]
public sealed class VukerIndividualSaveData
{
    [DataMember(Order = 1)] public string EntityId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Lifecycle { get; set; }
    [DataMember(Order = 3)] public int Disposition { get; set; }
    [DataMember(Order = 4)] public int RegionX { get; set; }
    [DataMember(Order = 5)] public int RegionY { get; set; }
    [DataMember(Order = 6)] public int RegionZ { get; set; }
    [DataMember(Order = 7)] public int PositionX { get; set; }
    [DataMember(Order = 8)] public int PositionY { get; set; }
    [DataMember(Order = 9)] public int PositionZ { get; set; }
    [DataMember(Order = 10)] public bool IsAlive { get; set; }
    [DataMember(Order = 11)] public long BirthTick { get; set; }
    [DataMember(Order = 12)] public long MaturityTick { get; set; }
    [DataMember(Order = 13)] public string? KidnapReservedBy { get; set; }
    [DataMember(Order = 14)] public string? TamedByResidentId { get; set; }
    [DataMember(Order = 15)] public string? ActivePairId { get; set; }
    [DataMember(Order = 16)] public long Version { get; set; }
}

[DataContract]
public sealed class VukerPairSaveData
{
    [DataMember(Order = 1)] public string PairId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string FirstParentId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string SecondParentId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public int RegionX { get; set; }
    [DataMember(Order = 5)] public int RegionY { get; set; }
    [DataMember(Order = 6)] public int RegionZ { get; set; }
    [DataMember(Order = 7)] public int SuccessfulCycles { get; set; }
    [DataMember(Order = 8)] public long NextBirthTick { get; set; }
    [DataMember(Order = 9)] public bool IsActive { get; set; }
    [DataMember(Order = 10)] public string? TerminalReason { get; set; }
    [DataMember(Order = 11)] public string? BlockedReason { get; set; }
    [DataMember(Order = 12)] public long Version { get; set; }
}


public sealed class SaveVersionThirteenVukerEcologyMigration : ISaveMigration
{
    public string Id => "save.v13_to_v14.vuker_ecology";
    public int FromVersion => 13;
    public int ToVersion => 14;

    public void Apply(SaveGameDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException(
                "Migration received the wrong source version.");
        }

        document.Vukers ??= new VukerEcologySaveData
        {
            WorldSeed = document.Metadata?.WorldSeed ?? 0,
            CurrentTick = document.Metadata?.SimulationTick ?? 0,
        };
        document.FormatVersion = ToVersion;
    }

}
