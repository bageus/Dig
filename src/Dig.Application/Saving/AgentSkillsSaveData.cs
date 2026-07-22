using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class AgentSkillsSaveData
{
    [DataMember(Order = 1)]
    public List<AgentSkillProgressionSaveData> Agents { get; set; } =
        new List<AgentSkillProgressionSaveData>();
}

[DataContract]
public sealed class AgentSkillProgressionSaveData
{
    [DataMember(Order = 1)] public string AgentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int SchemaVersion { get; set; }
    [DataMember(Order = 3)] public int PrecisionVersion { get; set; }
    [DataMember(Order = 4)] public int TotalCapacityUnits { get; set; }
    [DataMember(Order = 5)] public List<AgentSkillValueSaveData> Values { get; set; } =
        new List<AgentSkillValueSaveData>();
    [DataMember(Order = 6)] public List<string> AppliedSourceKeys { get; set; } =
        new List<string>();
    [DataMember(Order = 7)] public SkillRedistributionReportSaveData? LastReport { get; set; }
    [DataMember(Order = 8)] public int UnitsPerPoint { get; set; }
    [DataMember(Order = 9)] public List<string> MigrationSteps { get; set; } =
        new List<string>();
    [DataMember(Order = 10, EmitDefaultValue = false)]
    public bool? AutomaticPlanningEnabled { get; set; }
}

[DataContract]
public sealed class AgentSkillValueSaveData
{
    [DataMember(Order = 1)] public string SkillId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Units { get; set; }
}

[DataContract]
public sealed class SkillRedistributionReportSaveData
{
    [DataMember(Order = 1)] public int SourceKind { get; set; }
    [DataMember(Order = 2)] public string SourceId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public long Tick { get; set; }
    [DataMember(Order = 4)] public int CapacityUnits { get; set; }
    [DataMember(Order = 5)] public int SumBeforeUnits { get; set; }
    [DataMember(Order = 6)] public int SumAfterUnits { get; set; }
    [DataMember(Order = 7)] public int FreeCapacityGainUnits { get; set; }
    [DataMember(Order = 8)] public int OverflowUnits { get; set; }
    [DataMember(Order = 9)] public List<SkillGrantApplicationSaveData> Grants { get; set; } =
        new List<SkillGrantApplicationSaveData>();
    [DataMember(Order = 10)] public List<SkillDonorLossSaveData> DonorLosses { get; set; } =
        new List<SkillDonorLossSaveData>();
    [DataMember(Order = 11)] public List<AgentSkillValueSaveData> ResultingValues { get; set; } =
        new List<AgentSkillValueSaveData>();
}

[DataContract]
public sealed class SkillGrantApplicationSaveData
{
    [DataMember(Order = 1)] public string SkillId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int RequestedUnits { get; set; }
    [DataMember(Order = 3)] public int EligibleUnits { get; set; }
    [DataMember(Order = 4)] public int AppliedUnits { get; set; }
    [DataMember(Order = 5)] public int FreeCapacityUnits { get; set; }
    [DataMember(Order = 6)] public bool ReceivedRoundingUnit { get; set; }
}

[DataContract]
public sealed class SkillDonorLossSaveData
{
    [DataMember(Order = 1)] public string SkillId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int ValueBeforeUnits { get; set; }
    [DataMember(Order = 3)] public int LossUnits { get; set; }
    [DataMember(Order = 4)] public long FractionalRemainder { get; set; }
    [DataMember(Order = 5)] public bool ReceivedRoundingUnit { get; set; }
}

}
