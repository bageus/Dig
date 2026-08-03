using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Agents
{
    public sealed partial class AgentState
    {
        private const int FoodBiteCooldownTicks = 1;
        private ActiveFoodMeal? _activeFoodMeal;

        public bool HasActiveFoodMeal => _activeFoodMeal != null;

        public FoodMealSnapshot? CreateFoodMealSnapshot()
        {
            return _activeFoodMeal?.CreateSnapshot();
        }

        public Result BeginFoodMeal(
            EntityId sourceStackId,
            ItemId itemId,
            int totalNutrition,
            int biteCount,
            long tick)
        {
            ValidateTick(tick);
            if (!IsAlive)
            {
                return Result.Failure(AgentErrors.AgentDead);
            }

            if (sourceStackId.IsEmpty || itemId.IsEmpty)
            {
                throw new ArgumentException("Food source and item ids are required.");
            }

            if (totalNutrition <= 0 || biteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalNutrition));
            }

            if (_activeFoodMeal != null)
            {
                return Result.Failure(FoodMealErrors.AlreadyActive);
            }

            if (_activeAction != null)
            {
                Raise(new AgentActionInterrupted(
                    tick,
                    Id,
                    _activeAction.IntentKind,
                    AgentIntentKind.Eat));
            }

            _activeAction = new ActiveAgentAction(
                AgentIntentKind.Eat,
                null,
                tick,
                biteCount);
            _activeFoodMeal = new ActiveFoodMeal(
                sourceStackId,
                itemId,
                totalNutrition,
                biteCount,
                nextBiteTick: checked(tick + 1L));
            LastActionSwitchTick = tick;
            LastActionBlockReason = null;
            Version = checked(Version + 1);
            Raise(new AgentActionStarted(
                tick,
                Id,
                AgentIntentKind.Eat,
                playerOrderId: null));
            Raise(new AgentFoodMealStarted(
                tick,
                Id,
                sourceStackId,
                itemId,
                totalNutrition,
                biteCount));
            return Result.Success();
        }

        public Result<bool> AdvanceFoodMealBite(long tick)
        {
            ValidateTick(tick);
            if (!IsAlive)
            {
                return Result<bool>.Failure(AgentErrors.AgentDead);
            }

            if (_activeFoodMeal == null)
            {
                return Result<bool>.Failure(FoodMealErrors.NotActive);
            }

            if (_activeAction == null
                || _activeAction.IntentKind != AgentIntentKind.Eat)
            {
                throw new InvalidOperationException(
                    "An active food meal must own the existing Eat action.");
            }

            if (tick < _activeFoodMeal.NextBiteTick)
            {
                return Result<bool>.Success(false);
            }

            int nutrition = _activeFoodMeal.ResolveNextBiteNutrition();
            ApplyNeedDelta(new NeedDelta(nutrition, 0, 0, 0), tick);
            _activeFoodMeal.CompleteBite(tick, FoodBiteCooldownTicks);
            _activeAction.Advance();
            Version = checked(Version + 1);
            Raise(new AgentFoodBiteCompleted(
                tick,
                Id,
                _activeFoodMeal.ItemId,
                _activeFoodMeal.CompletedBites,
                _activeFoodMeal.BiteCount,
                nutrition));

            if (!_activeFoodMeal.IsComplete)
            {
                return Result<bool>.Success(false);
            }

            ItemId completedItem = _activeFoodMeal.ItemId;
            _activeFoodMeal = null;
            _activeAction = null;
            LastActionSwitchTick = tick;
            Raise(new AgentActionCompleted(tick, Id, AgentIntentKind.Eat));
            Raise(new AgentFoodMealCompleted(tick, Id, completedItem));
            return Result<bool>.Success(true);
        }

        public Result InterruptFoodMeal(string reason, long tick)
        {
            ValidateTick(tick);
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Interruption reason is required.", nameof(reason));
            }

            if (_activeFoodMeal == null)
            {
                return Result.Success();
            }

            FoodMealSnapshot snapshot = _activeFoodMeal.CreateSnapshot();
            _activeFoodMeal = null;
            _activeAction = null;
            LastActionSwitchTick = tick;
            LastActionBlockReason = reason.Trim();
            Version = checked(Version + 1);
            Raise(new AgentActionInterrupted(
                tick,
                Id,
                AgentIntentKind.Eat,
                AgentIntentKind.Idle));
            Raise(new AgentFoodMealInterrupted(
                tick,
                Id,
                snapshot.ItemId,
                snapshot.CompletedBites,
                snapshot.BiteCount,
                reason.Trim()));
            return Result.Success();
        }

        private sealed class ActiveFoodMeal
        {
            internal ActiveFoodMeal(
                EntityId sourceStackId,
                ItemId itemId,
                int totalNutrition,
                int biteCount,
                long nextBiteTick)
            {
                if (nextBiteTick < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(nextBiteTick));
                }

                SourceStackId = sourceStackId;
                ItemId = itemId;
                TotalNutrition = totalNutrition;
                BiteCount = biteCount;
                NextBiteTick = nextBiteTick;
            }

            internal EntityId SourceStackId { get; }
            internal ItemId ItemId { get; }
            internal int TotalNutrition { get; }
            internal int BiteCount { get; }
            internal int CompletedBites { get; private set; }
            internal long NextBiteTick { get; private set; }
            internal bool IsComplete => CompletedBites >= BiteCount;

            internal int ResolveNextBiteNutrition()
            {
                int quotient = TotalNutrition / BiteCount;
                int remainder = TotalNutrition % BiteCount;
                return quotient + (CompletedBites < remainder ? 1 : 0);
            }

            internal void CompleteBite(long tick, int cooldownTicks)
            {
                if (IsComplete)
                {
                    throw new InvalidOperationException("The meal is already complete.");
                }

                if (tick < NextBiteTick || cooldownTicks < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(tick));
                }

                CompletedBites = checked(CompletedBites + 1);
                if (!IsComplete)
                {
                    NextBiteTick = checked(tick + cooldownTicks + 1L);
                }
            }

            internal void RestoreCompletedBite()
            {
                if (IsComplete)
                {
                    throw new InvalidOperationException("The meal is already complete.");
                }

                CompletedBites = checked(CompletedBites + 1);
            }

            internal FoodMealSnapshot CreateSnapshot()
            {
                return new FoodMealSnapshot(
                    SourceStackId,
                    ItemId,
                    TotalNutrition,
                    BiteCount,
                    CompletedBites,
                    NextBiteTick);
            }
        }
    }
}
