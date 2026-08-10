using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Input;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private void CreateWorldFoodPickup(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue
                || !decision.TargetEntityId.HasValue
                || !decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.world_consumable.use_missing_target");
                return;
            }

            DigHudOverlay hud = _hud!;
            DigTerrainWorkSession terrainSession = _terrainSession!;
            string residentId = decision.ActorId.Value.ToString();
            string stackId = decision.TargetEntityId.Value.ToString();
            Result effectOwner = terrainSession.ValidateWorldConsumableAction(stackId);
            if (effectOwner.IsFailure)
            {
                hud.SetCommandResult(effectOwner);
                return;
            }

            Result capacity = terrainSession.ValidateResidentCanPickupStack(
                residentId,
                stackId);
            if (capacity.IsFailure)
            {
                hud.SetCommandResult(capacity);
                if (capacity.Error == InventoryErrors.ResidentInventoryCapacityExceeded)
                {
                    _agentRenderer!.PlayInventoryFullReaction(residentId);
                    hud.SetStatus("Resident inventory is full.");
                }

                return;
            }

            Result result = terrainSession.CreateWorldItemPickup(
                stackId,
                residentId,
                decision.TargetCell.Value,
                _simulation!.CurrentTick,
                eatAfterPickup: true);
            hud.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            var jobs = terrainSession.LoadJobs();
            _jobRenderer!.Render(jobs);
            hud.SetJobs(jobs);
            RenderCurrentlyVisibleWorldItems();
            hud.SetStatus("Consumable pickup and use order created.");
        }
    }
}
