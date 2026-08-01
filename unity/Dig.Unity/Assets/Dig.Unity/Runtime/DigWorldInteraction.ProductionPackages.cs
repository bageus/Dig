using Dig.Domain.Core;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private bool TryHandleProductionPackagePointerInput(RaycastHit[] hits)
    {
        if (!TryResolveProductionPackageHit(hits, out DigWorldItemVisual package))
        {
            return false;
        }

        CancelResidentMarquee();
        DisableExcavationDrawing();
        DisableCaveRoomPlanning();
        if (_terrainSession == null || _agentRenderer == null)
        {
            return true;
        }

        EntityId stackId = EntityId.Parse(package.Model.StackId);
        Dig.Presentation.Agents.AgentViewModel? selected =
            _agentRenderer.SelectedModel;
        if (selected == null)
        {
            _hud?.SetStatus("Select a dwarf before using the production box.");
            return true;
        }

        if (!_terrainSession.IsClosedProductionPackage(stackId))
        {
            _hud?.SetStatus("This production box is not finished yet.");
            return true;
        }

        Result result = _terrainSession.StartDirectProductionPackageUse(
            stackId,
            EntityId.Parse(selected.Id),
            new CellId(selected.CellX, selected.CellY, selected.CellZ),
            _agentSession!.Tick);
        _hud?.SetStatus(result.IsSuccess
            ? "Using production box."
            : result.Error?.Message ?? "Production box is unavailable.");
        return true;
    }

    private bool TryResolveProductionPackageHoverTarget(
        RaycastHit[] hits,
        out DigWorldItemVisual package)
    {
        package = null!;
        if (!TryResolveProductionPackageHit(hits, out DigWorldItemVisual candidate)
            || _terrainSession == null
            || _agentRenderer?.SelectedModel == null)
        {
            return false;
        }

        EntityId stackId = EntityId.Parse(candidate.Model.StackId);
        Dig.Presentation.Agents.AgentViewModel selected =
            _agentRenderer.SelectedModel;
        bool reachable = _terrainSession.CanDirectUseProductionPackage(
            stackId,
            new CellId(selected.CellX, selected.CellY, selected.CellZ),
            out _);
        if (reachable)
        {
            package = candidate;
        }

        return reachable;
    }

    private bool TryResolveProductionPackageHit(
        RaycastHit[] hits,
        out DigWorldItemVisual package)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(
                    hits[index],
                    out DigWorldItemVisual candidate)
                && _terrainSession?.IsProductionPackage(
                    EntityId.Parse(candidate.Model.StackId)) == true)
            {
                package = candidate;
                return true;
            }
        }

        package = null!;
        return false;
    }
}

}
