using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class AgentPositionsSaveData
{
    [DataMember(Order = 1)]
    public List<AgentPositionSaveData> Agents { get; set; } =
        new List<AgentPositionSaveData>();
}

[DataContract]
public sealed class AgentPositionSaveData
{
    [DataMember(Order = 1)]
    public string AgentId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public int X { get; set; }

    [DataMember(Order = 3)]
    public int Y { get; set; }

    [DataMember(Order = 4)]
    public int Z { get; set; }
}

}
