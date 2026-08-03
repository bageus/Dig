using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static IReadOnlyDictionary<EntityId, AgentRuntimeSnapshot> BuildAgentRuntime(
        AgentRuntimeSaveData data,
        long simulationTick)
    {
        if (data?.Agents == null)
        {
            throw new InvalidOperationException("Agent runtime save data is missing.");
        }

        Dictionary<EntityId, AgentRuntimeSnapshot> values =
            new Dictionary<EntityId, AgentRuntimeSnapshot>();
        foreach (AgentRuntimeStateSaveData saved in data.Agents
            .OrderBy(value => value.AgentId, StringComparer.Ordinal))
        {
            if (saved == null
                || string.IsNullOrWhiteSpace(saved.AgentId)
                || saved.LastNeedsTick < -1
                || saved.LastNeedsTick > simulationTick)
            {
                throw new InvalidOperationException("Saved agent runtime state is invalid.");
            }

            EntityId agentId = EntityId.Parse(saved.AgentId);
            if (values.ContainsKey(agentId))
            {
                throw new InvalidOperationException(
                    "Saved agent runtime ids must be unique.");
            }

            AgentNeedsSnapshot needs = new AgentNeedsSnapshot(
                new NeedValue(saved.Nutrition),
                new NeedValue(saved.Alertness),
                new NeedValue(saved.Mood),
                new NeedValue(saved.Health));
            FoodMealSnapshot? meal = DecodeMeal(saved.ActiveMeal, simulationTick);
            AgentRuntimeSnapshot runtime = new AgentRuntimeSnapshot(
                needs,
                saved.LastNeedsTick,
                meal,
                meal == null ? null : saved.ActiveMeal!.StartedTick);
            ValidateRestoredMeal(runtime);
            values.Add(agentId, runtime);
        }

        return new ReadOnlyDictionary<EntityId, AgentRuntimeSnapshot>(values);
    }

    private static FoodMealSnapshot? DecodeMeal(
        ActiveFoodMealSaveData? saved,
        long simulationTick)
    {
        if (saved == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(saved.SourceStackId)
            || string.IsNullOrWhiteSpace(saved.ItemId)
            || saved.TotalNutrition <= 0
            || saved.BiteCount <= 0
            || saved.CompletedBites < 0
            || saved.CompletedBites >= saved.BiteCount
            || saved.StartedTick < 0)
        {
            throw new InvalidOperationException("Saved active food meal is invalid.");
        }

        long nextBiteTick = saved.NextBiteTick > saved.StartedTick
            ? saved.NextBiteTick
            : checked(Math.Max(
                simulationTick + 1L,
                saved.StartedTick + 1L + (saved.CompletedBites * 2L)));
        return new FoodMealSnapshot(
            EntityId.Parse(saved.SourceStackId),
            new ItemId(saved.ItemId),
            saved.TotalNutrition,
            saved.BiteCount,
            saved.CompletedBites,
            nextBiteTick);
    }

    private static void ValidateRestoredMeal(AgentRuntimeSnapshot runtime)
    {
        if (runtime.ActiveMeal != null
            && runtime.Needs.Health.Points == NeedValue.Minimum)
        {
            throw new InvalidOperationException(
                "A dead resident cannot restore an active meal.");
        }
    }
}

}
