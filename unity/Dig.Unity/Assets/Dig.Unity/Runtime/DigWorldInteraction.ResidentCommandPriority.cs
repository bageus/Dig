using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Input;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigWorldInteraction
{
    private bool TryHandlePriorityResidentPointerInput()
    {
        if (!Input.GetMouseButtonDown(0)
            || _hud == null
            || _hud.ContainsScreenPoint(Input.mousePosition))
        {
            return false;
        }

        RaycastHit[] hits = GetPointerHits();
        if (TryResolveAgentHit(hits, out DigAgentVisual agent))
        {
            CancelResidentMarquee();
            if (_buildingPlacementMode.HasValue)
            {
                CancelBuildingPlacement();
            }

            if (IsAdditiveResidentSelectionPressed())
            {
                ToggleResidentSelection(agent);
                return true;
            }

            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            int clickCount = RegisterResidentClick(agent.Model.Id);
            ContextPointerTarget target = new ContextPointerTarget(
                ContextWorldTargetKind.Resident,
                EntityId.Parse(agent.Model.Id),
                new CellId(agent.Model.CellX, agent.Model.CellY),
                isAlive: agent.Model.IsAlive);
            ApplyDecision(
                _inputRouter.Route(
                    Pointer(PointerButtonKind.Left, clickCount),
                    BuildState(PointerButtonKind.Left),
                    target),
                agent: agent);
            return true;
        }

        if (_agentRenderer!.SelectedCount == 0
            || !TryApplyTunnelMove(hits, leftButton: true))
        {
            return false;
        }

        CancelResidentMarquee();
        DisableExcavationDrawing();
        DisableCaveRoomPlanning();
        return true;
    }
}
}
