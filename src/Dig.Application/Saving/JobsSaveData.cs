using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class JobsSaveData
{
    [DataMember(Order = 1)]
    public List<JobSaveData> Jobs { get; set; } =
        new List<JobSaveData>();

    [DataMember(Order = 2)]
    public List<JobReservationSaveData> Reservations { get; set; } =
        new List<JobReservationSaveData>();
}

[DataContract]
public sealed class JobSaveData
{
    [DataMember(Order = 1)]
    public JobDefinitionSaveData Definition { get; set; } =
        new JobDefinitionSaveData();

    [DataMember(Order = 2)]
    public int Status { get; set; }

    [DataMember(Order = 3)]
    public int Stage { get; set; }

    [DataMember(Order = 4, EmitDefaultValue = false)]
    public string? AssignedAgentId { get; set; }

    [DataMember(Order = 5)]
    public int RetryCount { get; set; }

    [DataMember(Order = 6)]
    public long NextRetryTick { get; set; }

    [DataMember(Order = 7)]
    public long Version { get; set; }

    [DataMember(Order = 8, EmitDefaultValue = false)]
    public string? ReasonCode { get; set; }

    [DataMember(Order = 9, EmitDefaultValue = false)]
    public string? ReasonMessage { get; set; }
}

[DataContract]
public sealed class JobDefinitionSaveData
{
    [DataMember(Order = 1)]
    public string TypeId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string JobId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int Priority { get; set; }

    [DataMember(Order = 4)]
    public long CreatedTick { get; set; }

    [DataMember(Order = 5)]
    public int MaximumRetries { get; set; }

    [DataMember(Order = 6)]
    public long RetryDelayTicks { get; set; }

    [DataMember(Order = 7)]
    public List<string> Dependencies { get; set; } = new List<string>();

    [DataMember(Order = 8)]
    public List<SavePropertyData> Properties { get; set; } =
        new List<SavePropertyData>();
}

[DataContract]
public sealed class SavePropertyData
{
    [DataMember(Order = 1)]
    public string Key { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Value { get; set; } = string.Empty;
}

[DataContract]
public sealed class JobReservationSaveData
{
    [DataMember(Order = 1)]
    public string JobId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string AgentId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int Kind { get; set; }

    [DataMember(Order = 4)]
    public string Value { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public long AcquiredTick { get; set; }
}
}