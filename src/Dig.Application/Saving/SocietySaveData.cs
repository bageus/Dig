using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class SocietySaveData
{
    [DataMember(Order = 1)] public long Version { get; set; }
    [DataMember(Order = 2)] public SocietyPolicySaveData? Policy { get; set; }
    [DataMember(Order = 3)] public List<SocietyResidentSaveData> Residents { get; set; } =
        new List<SocietyResidentSaveData>();
    [DataMember(Order = 4)] public List<SocialBondSaveData> Bonds { get; set; } =
        new List<SocialBondSaveData>();
}

[DataContract]
public sealed class SocietyPolicySaveData
{
    [DataMember(Order = 1)] public long AdultAgeTicks { get; set; }
    [DataMember(Order = 2)] public long OldAgeTicks { get; set; }
    [DataMember(Order = 3)] public long MaximumAgeTicks { get; set; }
    [DataMember(Order = 4)] public long GestationTicks { get; set; }
    [DataMember(Order = 5)] public int CloseKinshipDepth { get; set; }
    [DataMember(Order = 6)] public int MinimumPartnershipSympathy { get; set; }
    [DataMember(Order = 7)] public int MinimumPartnershipTrust { get; set; }
    [DataMember(Order = 8)] public int MinimumReproductionMood { get; set; }
    [DataMember(Order = 9)] public int MinimumReproductionHealth { get; set; }
    [DataMember(Order = 10)] public long PostpartumCooldownTicks { get; set; }
}

[DataContract]
public sealed class SocietyResidentSaveData
{
    [DataMember(Order = 1)] public string Id { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Name { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Sex { get; set; }
    [DataMember(Order = 4)] public long BirthTick { get; set; }
    [DataMember(Order = 5)] public int LifeStage { get; set; }
    [DataMember(Order = 6)] public string? MotherId { get; set; }
    [DataMember(Order = 7)] public string? FatherId { get; set; }
    [DataMember(Order = 8)] public string? PartnerId { get; set; }
    [DataMember(Order = 9)] public string? PregnancyFatherId { get; set; }
    [DataMember(Order = 10)] public long PregnancyConceptionTick { get; set; }
    [DataMember(Order = 11)] public long PregnancyDueTick { get; set; }
    [DataMember(Order = 12)] public int X { get; set; }
    [DataMember(Order = 13)] public int Y { get; set; }
    [DataMember(Order = 14)] public int Z { get; set; }
    [DataMember(Order = 15)] public string? DeathCause { get; set; }
    [DataMember(Order = 16)] public long? DeathTick { get; set; }
    [DataMember(Order = 17)] public int Potential { get; set; }
    [DataMember(Order = 18)] public List<string> Traits { get; set; } = new List<string>();
    [DataMember(Order = 19)] public long PostpartumUntilTick { get; set; } = -1;
}

[DataContract]
public sealed class SocialBondSaveData
{
    [DataMember(Order = 1)] public string FirstResidentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string SecondResidentId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Sympathy { get; set; }
    [DataMember(Order = 4)] public int Trust { get; set; }
    [DataMember(Order = 5)] public long LastInteractionTick { get; set; }
}

}
