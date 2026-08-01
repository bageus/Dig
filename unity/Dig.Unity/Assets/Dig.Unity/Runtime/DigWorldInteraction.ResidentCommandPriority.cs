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
        if (TryResolveMushroomHit(hits, out DigMushroomVisual mushroom))
        {
            CancelResidentMarquee();
            EntityId siteId = mushroom.Model.SiteId;
            CellId cell = mushroom.Model.Cell;
            Dig.Presentation.Agents.AgentViewModel? selected =
                _agentRenderer!.SelectedModel;
            bool reachable = selected != null
                && _terrainSession!.CanDirectChopMushroom(
                    siteId,
                    new CellId(selected.CellX, selected.CellY, selected.CellZ),
                    out _);
            ContextPointerTarget mushroomTarget = new ContextPointerTarget(
                ContextWorldTargetKind.Mushroom,
                siteId,
                cell,
                reachable: reachable);
            ApplyDecision(_inputRouter.Route(
                Pointer(PointerButtonKind.Left),
                BuildState(PointerButtonKind.Left),
                mushroomTarget));
            return true;
        }

        if (TryResolveBarrelHit(hits, out DigBarrelVisual barrel))
        {
            CancelResidentMarquee();
            EntityId barrelId = barrel.Model.BarrelId;
            CellId cell = barrel.Model.Cell;
            Dig.Presentation.Agents.AgentViewModel? selected =
                _agentRenderer!.SelectedModel;
            bool reachable = selected != null
                && _terrainSession!.CanDirectAttackBarrel(
                    barrelId,
                    new CellId(selected.CellX, selected.CellY, selected.CellZ),
                    out _);
            ContextPointerTarget barrelTarget = new ContextPointerTarget(
                ContextWorldTargetKind.Barrel,
                barrelId,
                cell,
                reachable: reachable);
            ApplyDecision(_inputRouter.Route(
                Pointer(PointerButtonKind.Left),
                BuildState(PointerButtonKind.Left),
                barrelTarget));
            return true;
        }

        if (TryHandleProductionPackagePointerInput(hits))
        {
            return true;
        }

        if (TryHandleBuildingInternalStockPointerInput(hits))
        {
            return true;
        }


        if (TryResolveCompletedBuildingHit(hits, out DigBuildingVisual completedBuilding))
        {
            CancelResidentMarquee();
            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ContextPointerTarget buildingTarget = new ContextPointerTarget(
                ContextWorldTargetKind.CompletedBuilding,
                EntityId.Parse(completedBuilding.Model.Id),
                new CellId(
                    completedBuilding.Model.OriginX,
                    completedBuilding.Model.OriginY,
                    completedBuilding.Model.OriginZ));
            ApplyDecision(
                _inputRouter.Route(
                    Pointer(PointerButtonKind.Left),
                    BuildState(PointerButtonKind.Left),
                    buildingTarget),
                building: completedBuilding);
            return true;
        }

        // BuildingBoxes remain actionable while the excavation palette is active.
        // Otherwise the default tunnel tool consumes the click before placement or pickup.
        if (TryResolveBuildingBoxHit(hits, out DigWorldItemVisual buildingBox))
        {
            CancelResidentMarquee();
            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ContextPointerTarget boxTarget = new ContextPointerTarget(
                ContextWorldTargetKind.BuildingBox,
                EntityId.Parse(buildingBox.Model.StackId),
                new CellId(
                    buildingBox.Model.CellX,
                    buildingBox.Model.CellY,
                    buildingBox.Model.CellZ),
                reachable: true,
                supportsAltInteraction: buildingBox.Model.AvailableQuantity == 1);
            bool altPressed = IsAltPressed();
            if (altPressed && _agentRenderer!.SelectedCount == 0)
            {
                SelectBuildingBox(buildingBox.Model);
                _hud.SetStatus("Select a dwarf before ordering BuildingBox pickup.");
                return true;
            }

            ApplyDecision(
                _inputRouter.Route(
                    new ContextPointerEvent(
                        PointerInputSurface.World,
                        PointerButtonKind.Left,
                        altPressed: altPressed),
                    BuildState(PointerButtonKind.Left),
                    boxTarget),
                item: buildingBox);
            return true;
        }


        // Excavation drawing owns ground clicks while a tool is active, but not the
        // BuildingBox or direct mushroom branches above.
        if (_excavationMode != DigExcavationDrawingMode.None)
        {
            return false;
        }

        if (_agentRenderer!.SelectedCount > 0
            && TryResolveHostileCreatureHit(hits, out DigCreatureVisual creature))
        {
            CancelResidentMarquee();
            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ContextPointerTarget hostileTarget = BuildHostileTarget(creature);
            ApplyDecision(_inputRouter.Route(
                Pointer(PointerButtonKind.Left),
                BuildState(PointerButtonKind.Left),
                hostileTarget));
            return true;
        }

        if (_agentRenderer.SelectedCount > 0
            && TryResolveWorldItemHit(hits, out DigWorldItemVisual item))
        {
            CancelResidentMarquee();
            DisableExcavationDrawing();
            DisableCaveRoomPlanning();
            ContextPointerTarget itemTarget = new ContextPointerTarget(
                ResolveWorldItemTargetKind(item.Model),
                EntityId.Parse(item.Model.StackId),
                new CellId(item.Model.CellX, item.Model.CellY, item.Model.CellZ),
                reachable: true,
                supportsAltInteraction: item.Model.CanPickup);
            ApplyDecision(
                _inputRouter.Route(
                    new ContextPointerEvent(
                        PointerInputSurface.World,
                        PointerButtonKind.Left,
                        altPressed: IsAltPressed()),
                    BuildState(PointerButtonKind.Left),
                    itemTarget),
                item: item);
            return true;
        }

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
                new CellId(agent.Model.CellX, agent.Model.CellY, agent.Model.CellZ),
                isAlive: agent.Model.IsAlive);
            ApplyDecision(
                _inputRouter.Route(
                    Pointer(PointerButtonKind.Left, clickCount),
                    BuildState(PointerButtonKind.Left),
                    target),
                agent: agent);
            return true;
        }

        if (_agentRenderer.SelectedCount == 0
            || !TryApplyTunnelMove(hits, leftButton: true))
        {
            return false;
        }

        CancelResidentMarquee();
        DisableExcavationDrawing();
        DisableCaveRoomPlanning();
        return true;
    }


    private bool TryResolveCompletedBuildingHit(
        RaycastHit[] hits,
        out DigBuildingVisual building)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(hits[index], out _))
            {
                building = null!;
                return false;
            }

            if (_buildingRenderer != null
                && _buildingRenderer.TryGetBuilding(hits[index], out building))
            {
                return true;
            }
        }

        building = null!;
        return false;
    }

    private bool TryResolveBuildingBoxHit(
        RaycastHit[] hits,
        out DigWorldItemVisual item)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_mushroomRenderer != null
                && _mushroomRenderer.TryGetMushroom(hits[index], out _))
            {
                item = null!;
                return false;
            }

            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(
                    hits[index],
                    out DigWorldItemVisual candidate))
            {
                if (candidate.Model.IsBuildingBox)
                {
                    item = candidate;
                    return true;
                }

                item = null!;
                return false;
            }
        }

        item = null!;
        return false;
    }

    private bool TryResolveMushroomHit(
        RaycastHit[] hits,
        out DigMushroomVisual mushroom)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            // A physical drop in front of a regrown site remains an ordinary item.
            // Do not scan through it and turn the material into an axe target.
            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(hits[index], out _))
            {
                mushroom = null!;
                return false;
            }

            if (_mushroomRenderer != null
                && _mushroomRenderer.TryGetMushroom(hits[index], out mushroom))
            {
                return true;
            }
        }

        mushroom = null!;
        return false;
    }

    private bool TryResolveWorldItemHit(
        RaycastHit[] hits,
        out DigWorldItemVisual item)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_mushroomRenderer != null
                && _mushroomRenderer.TryGetMushroom(hits[index], out _))
            {
                item = null!;
                return false;
            }

            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(
                    hits[index],
                    out DigWorldItemVisual candidate))
            {
                if (!candidate.Model.IsBuildingBox)
                {
                    item = candidate;
                    return true;
                }

                item = null!;
                return false;
            }
        }

        item = null!;
        return false;
    }
}
}