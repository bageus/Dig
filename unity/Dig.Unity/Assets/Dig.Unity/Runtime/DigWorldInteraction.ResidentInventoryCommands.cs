using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using UnityEngine;

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

        Result result = _terrainSession!.UseResidentInventoryActionWithSlotGuard(
            decision.ActorId.Value.ToString(),
            decision.TargetEntityId.Value.ToString(),
            _simulation!.CurrentTick);
        _hud!.SetCommandResult(result);
        if (result.IsSuccess)
        {
            ClearSelectedInventoryStack();
            _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
            _agentRenderer!.RenderEquipment(_terrainSession.LoadResidentEquipment());
            _hud.SetStatus("Inventory item action started.");
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

        ExecuteResidentInventoryDrop(
            decision.ActorId.Value,
            decision.TargetEntityId.Value,
            decision.TargetCell.Value);
    }

    private void ExecuteResidentInventoryDrop(
        EntityId actorId,
        EntityId stackId,
        CellId targetCell)
    {
        Result result = _terrainSession!.DropResidentInventoryStack(
            actorId.ToString(),
            stackId.ToString(),
            targetCell,
            _simulation!.CurrentTick);
        _hud!.SetCommandResult(result);
        if (result.IsFailure)
        {
            return;
        }

        ClearSelectedInventoryStack();
        Result synchronized = _terrainSession.SynchronizeLivingMaterials(
            _simulation.CurrentTick);
        if (synchronized.IsFailure)
        {
            _hud.SetCommandResult(synchronized);
            return;
        }

        _itemRenderer!.Render(_terrainSession.LoadAllWorldItems());
        _creatureRenderer!.Render(
            _agentSession!.LoadCreatures(
                _terrainSession.LoadLivingMaterialCreatures()),
            Camera.main,
            movementDuration: 0.1f);
        _agentRenderer!.RenderEquipment(_terrainSession.LoadResidentEquipment());
        _hud.SetStatus("Inventory stack dropped at the resident position.");
    }
}

}