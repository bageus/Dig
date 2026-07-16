using Dig.Domain.Core;
using Dig.Presentation.Input;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private void ApplyResidentInventoryUse(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue || !decision.TargetEntityId.HasValue)
            {
                _hud!.SetStatus("input.inventory.use_missing_target");
                return;
            }

            Result result = _terrainSession!.UseResidentInventoryItemWithSlotGuard(
                decision.ActorId.Value.ToString(),
                decision.TargetEntityId.Value.ToString(),
                _simulation!.CurrentTick);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
                _agentRenderer!.RenderEquipment(_terrainSession.LoadResidentEquipment());
                _hud.SetStatus("Inventory item equipped.");
            }
        }

        private void ApplyResidentInventoryDrop(ContextInputDecision decision)
        {
            if (!decision.ActorId.HasValue
                || !decision.TargetEntityId.HasValue
                || !decision.TargetCell.HasValue)
            {
                _hud!.SetStatus("input.inventory.drop_missing_target");
                return;
            }

            Result result = _terrainSession!.DropResidentInventoryStack(
                decision.ActorId.Value.ToString(),
                decision.TargetEntityId.Value.ToString(),
                decision.TargetCell.Value,
                _simulation!.CurrentTick);
            _hud!.SetCommandResult(result);
            if (result.IsSuccess)
            {
                _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
                _agentRenderer!.RenderEquipment(_terrainSession.LoadResidentEquipment());
                _hud.SetStatus("Inventory stack moved to resident cell.");
            }
        }
    }
}