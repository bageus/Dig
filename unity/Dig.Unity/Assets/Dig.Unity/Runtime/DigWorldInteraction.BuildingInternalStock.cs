using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private bool TryHandleBuildingInternalStockPointerInput(RaycastHit[] hits)
    {
        if (_agentRenderer!.SelectedCount == 0
            || !TryResolveBuildingInternalStockHit(
                hits,
                out DigBuildingInternalStockVisual stock))
        {
            return false;
        }

        CancelResidentMarquee();
        DisableExcavationDrawing();
        DisableCaveRoomPlanning();
        if (!_terrainSession!.TryResolveBuildingInternalStockPickup(
                stock.StackId,
                out CellId workPosition))
        {
            _hud!.SetStatus("Internal stock is reserved or empty.");
            return true;
        }

        ContextPointerTarget target = new ContextPointerTarget(
            ContextWorldTargetKind.GenericItem,
            EntityId.Parse(stock.StackId),
            workPosition,
            reachable: true,
            supportsAltInteraction: true);
        ApplyDecision(_inputRouter.Route(
            Pointer(PointerButtonKind.Left),
            BuildState(PointerButtonKind.Left),
            target));
        return true;
    }

    private bool TryResolveBuildingInternalStockHit(
        RaycastHit[] hits,
        out DigBuildingInternalStockVisual stock)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(hits[index], out _))
            {
                stock = null!;
                return false;
            }

            if (_buildingInternalStockRenderer != null
                && _buildingInternalStockRenderer.TryGetStock(hits[index], out stock))
            {
                return true;
            }
        }

        stock = null!;
        return false;
    }
}

}
