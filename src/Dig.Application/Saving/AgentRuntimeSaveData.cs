using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dig.Application.Saving
{

[DataContract]
public sealed class AgentRuntimeSaveData
{
    [DataMember(Order = 1)]
    public List<AgentRuntimeStateSaveData> Agents { get; set; } =
        new List<AgentRuntimeStateSaveData>();
}

[DataContract]
public sealed class AgentRuntimeStateSaveData
{
    [DataMember(Order = 1)] public string AgentId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Nutrition { get; set; }
    [DataMember(Order = 3)] public int Alertness { get; set; }
    [DataMember(Order = 4)] public int Mood { get; set; }
    [DataMember(Order = 5)] public int Health { get; set; }
    [DataMember(Order = 6)] public long LastNeedsTick { get; set; } = -1;
    [DataMember(Order = 7, EmitDefaultValue = false)]
    public ActiveFoodMealSaveData? ActiveMeal { get; set; }
}

[DataContract]
public sealed class ActiveFoodMealSaveData
{
    [DataMember(Order = 1)] public string SourceStackId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ItemId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int TotalNutrition { get; set; }
    [DataMember(Order = 4)] public int BiteCount { get; set; }
    [DataMember(Order = 5)] public int CompletedBites { get; set; }
    [DataMember(Order = 6)] public long StartedTick { get; set; }
    [DataMember(Order = 7, EmitDefaultValue = false)] public long NextBiteTick { get; set; }
}

}
