using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public static class AgentRuntimeRestoreErrors
{
    public static readonly DomainError InvalidFoodMealSnapshot = new DomainError(
        "agent.runtime.food_meal.invalid_snapshot",
        "The saved resident meal state is invalid.");
}

public sealed class AgentRuntimeSnapshot
{
    public AgentRuntimeSnapshot(
        AgentNeedsSnapshot needs,
        long lastNeedsTick,
        FoodMealSnapshot? activeMeal,
        long? mealStartedTick,
        LeisureRuntimeSnapshot? leisure = null)
    {
        if (lastNeedsTick < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(lastNeedsTick));
        }

        if (activeMeal == null && mealStartedTick.HasValue)
        {
            throw new ArgumentException(
                "Meal start tick requires an active meal.",
                nameof(mealStartedTick));
        }

        if (activeMeal != null && (!mealStartedTick.HasValue || mealStartedTick.Value < 0))
        {
            throw new ArgumentException(
                "An active meal requires its original start tick.",
                nameof(mealStartedTick));
        }

        Needs = needs;
        LastNeedsTick = lastNeedsTick;
        ActiveMeal = activeMeal;
        MealStartedTick = mealStartedTick;
        Leisure = leisure ?? new LeisureRuntimeSnapshot(
            Array.Empty<LeisureVarietyId>(), null, null, -1, false, 100);
    }

    public AgentNeedsSnapshot Needs { get; }
    public long LastNeedsTick { get; }
    public FoodMealSnapshot? ActiveMeal { get; }
    public long? MealStartedTick { get; }
    public LeisureRuntimeSnapshot Leisure { get; }
}

public sealed partial class AgentState
{
    public AgentRuntimeSnapshot CreateRuntimeSnapshot()
    {
        FoodMealSnapshot? meal = CreateFoodMealSnapshot();
        long? mealStartedTick = meal == null ? null : _activeAction?.StartedTick;
        if (meal != null
            && (_activeAction == null || _activeAction.IntentKind != AgentIntentKind.Eat))
        {
            throw new InvalidOperationException(
                "An active food meal must own the active Eat action.");
        }

        return new AgentRuntimeSnapshot(
            _needs.CreateSnapshot(),
            _lastNeedsTick,
            meal,
            mealStartedTick,
            CreateLeisureRuntimeSnapshot());
    }

    public Result RestoreRuntime(AgentRuntimeSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        Result validation = ValidateRuntimeSnapshot(snapshot);
        if (validation.IsFailure)
        {
            return validation;
        }

        _needs.Restore(snapshot.Needs);
        _lastNeedsTick = snapshot.LastNeedsTick;
        _activeAction = null;
        _activeFoodMeal = null;
        LastActionBlockReason = null;
        RestoreLeisureRuntime(snapshot.Leisure);

        FoodMealSnapshot? meal = snapshot.ActiveMeal;
        if (meal != null)
        {
            _activeFoodMeal = new ActiveFoodMeal(
                meal.SourceStackId,
                meal.ItemId,
                meal.TotalNutrition,
                meal.BiteCount,
                meal.NextBiteTick);
            _activeAction = new ActiveAgentAction(
                AgentIntentKind.Eat,
                playerOrderId: null,
                snapshot.MealStartedTick!.Value,
                meal.BiteCount);
            for (int index = 0; index < meal.CompletedBites; index++)
            {
                _activeFoodMeal.RestoreCompletedBite();
                _activeAction.Advance();
            }

            LastActionSwitchTick = snapshot.MealStartedTick.Value;
        }

        return Result.Success();
    }

    private static Result ValidateRuntimeSnapshot(AgentRuntimeSnapshot snapshot)
    {
        FoodMealSnapshot? meal = snapshot.ActiveMeal;
        if (meal == null)
        {
            return Result.Success();
        }

        if (snapshot.Needs.Health.Points == NeedValue.Minimum
            || meal.SourceStackId.IsEmpty
            || meal.ItemId.IsEmpty
            || meal.TotalNutrition <= 0
            || meal.BiteCount <= 0
            || meal.CompletedBites < 0
            || meal.CompletedBites >= meal.BiteCount
            || meal.NextBiteTick <= snapshot.MealStartedTick)
        {
            return Result.Failure(
                AgentRuntimeRestoreErrors.InvalidFoodMealSnapshot);
        }

        return Result.Success();
    }
}

}
