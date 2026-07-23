using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Input;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private void CreateWorldItemPickup(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue
                || !decision.TargetEntityId.HasValue
                || !decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.world_item.pickup_missing_target");
                return;
            }

            string residentId = decision.ActorId.Value.ToString();
            string stackId = decision.TargetEntityId.Value.ToString();
            Result capacity = _terrainSession!.ValidateResidentCanPickupStack(
                residentId,
                stackId);
            if (capacity.IsFailure)
            {
                _hud!.SetCommandResult(capacity);
                if (capacity.Error == InventoryErrors.ResidentInventoryCapacityExceeded)
                {
                    _agentRenderer!.PlayInventoryFullReaction(residentId);
                    _hud.SetStatus("Resident inventory is full.");
                }

                return;
            }

            Result result = _terrainSession.CreateWorldItemPickup(
                stackId,
                residentId,
                decision.TargetCell.Value,
                _simulation!.CurrentTick);
            _hud!.SetCommandResult(result);
            if (result.IsFailure)
            {
                return;
            }

            var jobs = _terrainSession.LoadJobs();
            _jobRenderer!.Render(jobs);
            _hud.SetJobs(jobs);
            _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
            _hud.SetStatus("World item pickup order created.");
        }
    }
}
