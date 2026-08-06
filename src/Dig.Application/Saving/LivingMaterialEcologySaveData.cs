using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class LivingMaterialEcologySaveData
{
    [DataMember(Order = 1)] public ulong WorldSeed { get; set; }
    [DataMember(Order = 2)] public long EcologyStep { get; set; }
    [DataMember(Order = 3)] public long Version { get; set; }
    [DataMember(Order = 4)] public List<LivingMaterialIndividualSaveData> Creatures { get; set; }
        = new List<LivingMaterialIndividualSaveData>();
    [DataMember(Order = 5)] public int TimingCadenceVersion { get; set; }
}

[DataContract]
public sealed class LivingMaterialIndividualSaveData
{
    [DataMember(Order = 1)] public string CreatureId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ItemEntityId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Species { get; set; }
    [DataMember(Order = 4)] public int Containment { get; set; }
    [DataMember(Order = 5)] public bool HasCell { get; set; }
    [DataMember(Order = 6)] public int CellX { get; set; }
    [DataMember(Order = 7)] public int CellY { get; set; }
    [DataMember(Order = 8)] public int CellZ { get; set; }
    [DataMember(Order = 9)] public int AnchorX { get; set; }
    [DataMember(Order = 10)] public int AnchorY { get; set; }
    [DataMember(Order = 11)] public int AnchorZ { get; set; }
    [DataMember(Order = 12)] public int PlaneRootX { get; set; }
    [DataMember(Order = 13)] public int PlaneRootY { get; set; }
    [DataMember(Order = 14)] public int PlaneRootZ { get; set; }
    [DataMember(Order = 15)] public int Direction { get; set; }
    [DataMember(Order = 16)] public int Activity { get; set; }
    [DataMember(Order = 17)] public int ActivityStepsRemaining { get; set; }
    [DataMember(Order = 18)] public int MovementCredit { get; set; }
    [DataMember(Order = 19)] public int SuccessfulMovementSteps { get; set; }
    [DataMember(Order = 20)] public int NextSearchAtStep { get; set; }
    [DataMember(Order = 21)] public int NextSleepAtStep { get; set; }
    [DataMember(Order = 22)] public int ReproductionCyclesCompleted { get; set; }
    [DataMember(Order = 23)] public long NextReproductionStep { get; set; }
    [DataMember(Order = 24)] public long DeterministicSequence { get; set; }
    [DataMember(Order = 25)] public string? BlockedReason { get; set; }
    [DataMember(Order = 26)] public long Version { get; set; }
}

}
