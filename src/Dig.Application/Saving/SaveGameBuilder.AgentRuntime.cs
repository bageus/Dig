using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameBuilder
{
    private static AgentRuntimeSaveData BuildAgentRuntime(
        IEnumerable<AgentState> agents)
    {
        AgentRuntimeSaveData data = new AgentRuntimeSaveData();
        foreach (AgentState agent in agents.OrderBy(
            value => value.Id.ToString(),
            StringComparer.Ordinal))
        {
            AgentRuntimeSnapshot runtime = agent.CreateRuntimeSnapshot();
            AgentRuntimeStateSaveData saved = new AgentRuntimeStateSaveData
            {
                AgentId = agent.Id.ToString(),
                Nutrition = runtime.Needs.Nutrition.Points,
                Alertness = runtime.Needs.Alertness.Points,
                Mood = runtime.Needs.Mood.Points,
                Health = runtime.Needs.Health.Points,
                LastNeedsTick = runtime.LastNeedsTick,
                LeisureHistory = runtime.Leisure.History
                    .Select(value => value.ToString())
                    .ToList(),
                ActiveLeisureId = runtime.Leisure.ActiveVariety?.ToString(),
                LeisurePartnerId = runtime.Leisure.PartnerId?.ToString(),
                NextLeisureEffectTick = runtime.Leisure.NextEffectTick,
                LeisureHistoryCommitted = runtime.Leisure.HistoryCommitted,
                LeisureMoodGainPercent = runtime.Leisure.MoodGainPercent,
            };
            if (runtime.ActiveMeal != null)
            {
                FoodMealSnapshot meal = runtime.ActiveMeal;
                saved.ActiveMeal = new ActiveFoodMealSaveData
                {
                    SourceStackId = meal.SourceStackId.ToString(),
                    ItemId = meal.ItemId.ToString(),
                    TotalNutrition = meal.TotalNutrition,
                    BiteCount = meal.BiteCount,
                    CompletedBites = meal.CompletedBites,
                    StartedTick = runtime.MealStartedTick!.Value,
                    NextBiteTick = meal.NextBiteTick,
                };
            }

            data.Agents.Add(saved);
        }

        return data;
    }
}

}
