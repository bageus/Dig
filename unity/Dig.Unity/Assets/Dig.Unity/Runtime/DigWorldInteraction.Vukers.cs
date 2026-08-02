using System;
using Dig.Application.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private bool TryHandleVukerPointerInput(
        RaycastHit[] hits,
        bool altPressed)
    {
        if (!TryResolveVukerHit(hits, out DigCreatureVisual creature)
            || !EntityId.TryParse(creature.Model.CreatureId, out EntityId vukerId))
        {
            return false;
        }

        if (altPressed && _agentRenderer!.SelectedCount == 1
            && _agentSession!.IsWildVukerChild(vukerId))
        {
            Dig.Presentation.Agents.AgentViewModel selected =
                _agentRenderer.SelectedModel!;
            Result prepared = _terrainSession!.PrepareResidentsForDirectCommand(
                new[] { selected.Id },
                _simulation!.CurrentTick);
            if (prepared.IsFailure)
            {
                _hud!.SetCommandResult(prepared);
                return true;
            }

            Result requested = _agentSession.RequestVukerKidnap(
                EntityId.Parse(selected.Id),
                vukerId);
            _hud!.SetCommandResult(requested);
            _hud.SetStatus(requested.IsSuccess
                ? "Гном идёт похищать детёныша Вукера."
                : requested.Error!.Code);
            RefreshVukerPresentation();
            return true;
        }

        if (!altPressed && _agentSession!.IsTamedVuker(vukerId))
        {
            CancelResidentMarquee();
            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            _agentRenderer!.ClearSelection();
            _creatureRenderer!.SelectById(creature.Model.CreatureId);
            _hud!.SetAgentSelection(null, 0);
            _hud.SetStatus(
                "Приручённый Вукер выбран. ЛКМ по проходу задаёт прямое перемещение.");
            return true;
        }

        return false;
    }

    private bool TryResolveVukerHit(
        RaycastHit[] hits,
        out DigCreatureVisual creature)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_creatureRenderer!.TryGetCreature(hits[index], out creature)
                && string.Equals(
                    creature.Model.SpeciesId,
                    "enemy.vuker",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (_buildingRenderer!.TryGetBuilding(hits[index], out _)
                || _itemRenderer!.TryGetItem(hits[index], out _))
            {
                break;
            }
        }

        creature = null!;
        return false;
    }

    private bool TryResolveVukerKidnapHoverTarget(RaycastHit[] hits)
    {
        if (!IsAltPressed()
            || _agentRenderer!.SelectedCount != 1
            || !TryResolveVukerHit(hits, out DigCreatureVisual creature)
            || !EntityId.TryParse(creature.Model.CreatureId, out EntityId id))
        {
            return false;
        }

        return _agentSession!.IsWildVukerChild(id);
    }

    private bool TryMoveSelectedTamedVuker(RaycastHit[] hits)
    {
        string? selectedId = _creatureRenderer!.SelectedCreatureId;
        if (string.IsNullOrWhiteSpace(selectedId)
            || !EntityId.TryParse(selectedId, out EntityId vukerId)
            || !_agentSession!.IsTamedVuker(vukerId))
        {
            return false;
        }

        DigSelectedResidentTarget target = ResolveSelectedResidentTarget(hits);
        if (target.Kind != DigSelectedResidentTargetKind.Movement)
        {
            return false;
        }

        PlanAgentTunnelRouteReport report =
            _agentSession.MoveTamedVukerThroughTunnel(
                vukerId,
                target.MovementCell);
        _hud!.SetCommandResult(report.Result);
        if (report.Result.IsSuccess)
        {
            _tunnelRenderer!.ShowRoute(report.Path, target.MovementOffsetX);
            PlayMovementCursorFeedback();
            _hud.SetStatus(
                $"Приручённый Вукер движется к X={target.MovementCell.X}, "
                + $"Y={target.MovementCell.Y}, Z={target.MovementCell.Z}.");
        }

        RefreshVukerPresentation();
        return true;
    }

    private void RefreshVukerPresentation()
    {
        _creatureRenderer!.Render(
            _agentSession!.LoadCreatures(
                _terrainSession!.LoadLivingMaterialCreatures()),
            _camera,
            movementDuration: 0.1f);
    }
}

}
