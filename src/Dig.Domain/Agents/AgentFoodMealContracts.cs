using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Agents
{

public static class FoodMealErrors
{
    public static readonly DomainError AlreadyActive = new DomainError(
        "agent.food_meal.already_active",
        "The resident is already eating a meal.");

    public static readonly DomainError NotActive = new DomainError(
        "agent.food_meal.not_active",
        "The resident has no active meal.");
}

public sealed class AgentFoodMealStarted : IDomainEvent
{
    public AgentFoodMealStarted(
        long tick,
        EntityId agentId,
        EntityId sourceStackId,
        ItemId itemId,
        int totalNutrition,
        int biteCount)
    {
        Tick = tick;
        AgentId = agentId;
        SourceStackId = sourceStackId;
        ItemId = itemId;
        TotalNutrition = totalNutrition;
        BiteCount = biteCount;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public EntityId SourceStackId { get; }
    public ItemId ItemId { get; }
    public int TotalNutrition { get; }
    public int BiteCount { get; }
}

public sealed class AgentFoodBiteCompleted : IDomainEvent
{
    public AgentFoodBiteCompleted(
        long tick,
        EntityId agentId,
        ItemId itemId,
        int completedBites,
        int biteCount,
        int nutritionApplied)
    {
        Tick = tick;
        AgentId = agentId;
        ItemId = itemId;
        CompletedBites = completedBites;
        BiteCount = biteCount;
        NutritionApplied = nutritionApplied;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public ItemId ItemId { get; }
    public int CompletedBites { get; }
    public int BiteCount { get; }
    public int NutritionApplied { get; }
}

public sealed class AgentFoodMealCompleted : IDomainEvent
{
    public AgentFoodMealCompleted(long tick, EntityId agentId, ItemId itemId)
    {
        Tick = tick;
        AgentId = agentId;
        ItemId = itemId;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public ItemId ItemId { get; }
}

public sealed class AgentFoodMealInterrupted : IDomainEvent
{
    public AgentFoodMealInterrupted(
        long tick,
        EntityId agentId,
        ItemId itemId,
        int completedBites,
        int biteCount,
        string reason)
    {
        Tick = tick;
        AgentId = agentId;
        ItemId = itemId;
        CompletedBites = completedBites;
        BiteCount = biteCount;
        Reason = reason;
    }

    public long Tick { get; }
    public EntityId AgentId { get; }
    public ItemId ItemId { get; }
    public int CompletedBites { get; }
    public int BiteCount { get; }
    public string Reason { get; }
}

}
