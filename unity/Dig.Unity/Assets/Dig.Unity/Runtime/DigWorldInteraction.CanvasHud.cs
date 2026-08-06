using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Input;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private string? _selectedInventoryStackId;
    private bool _selectedInventoryItemUsable;
    private bool _selectedInventoryItemIsBuildingBox;
    private bool _selectedInventoryItemCanUse;
    private bool _selectedInventoryItemCanDrop;
    private string? _lastRosterResidentClickId;
    private float _lastRosterResidentClickTime = float.NegativeInfinity;

    internal bool HasActiveBuildingPlacement => _buildingPlacementMode.HasValue;

    internal bool BuildingPlacementValid => _buildingPlacementPreview?.IsValid ?? false;

    internal string? BuildingPlacementReasonCode =>
        _buildingPlacementPreview?.ReasonCode;

    internal string? SelectedInventoryStackId => _selectedInventoryStackId;

    internal void SelectResidentFromHud(string residentId)
    {
        AgentViewModelFacts facts = GetResidentFacts(residentId);
        ContextPointerTarget target = new ContextPointerTarget(
            ContextWorldTargetKind.Resident,
            EntityId.Parse(residentId),
            new CellId(facts.CellX, facts.CellY, facts.CellZ),
            isAlive: facts.IsAlive);
        ContextInputDecision decision = _inputRouter.Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentRoster,
                PointerButtonKind.Left,
                clickCount: RegisterRosterResidentClick(residentId)),
            BuildState(PointerButtonKind.Left),
            target);
        ApplyDecision(decision);
        ClearSelectedInventoryStack();
    }

    internal void SelectBuildingFromHud(string buildingId)
    {
        var building = _terrainSession!.LoadBuildings()
            .FirstOrDefault(value => string.Equals(
                value.Id,
                buildingId,
                StringComparison.Ordinal));
        if (building == null)
        {
            _hud!.SetStatus("input.building.stale");
            return;
        }

        ContextPointerTarget target = new ContextPointerTarget(
            ContextWorldTargetKind.CompletedBuilding,
            EntityId.Parse(building.Id),
            new CellId(building.OriginX, building.OriginY, building.OriginZ));
        ContextInputDecision decision = _inputRouter.Route(
            new ContextPointerEvent(
                PointerInputSurface.World,
                PointerButtonKind.Left),
            BuildState(PointerButtonKind.Left),
            target);
        ApplyDecision(decision);
        ClearSelectedInventoryStack();
    }

    internal void SelectJobFromHud(string jobId)
    {
        JobOverlayViewModel? model = _terrainSession!.LoadJobs().FirstOrDefault(value =>
            string.Equals(value.Id, jobId, StringComparison.Ordinal));
        if (model == null)
        {
            _hud!.SetStatus("input.job.stale");
            return;
        }

        DigJobVisual? selected = _jobRenderer!.SelectById(jobId);
        _agentRenderer!.ClearSelection();
        _buildingRenderer!.Select(null);
        _selectedCell = null;
        _renderer!.Select(null);
        ClearSelectedInventoryStack();
        _hud!.SetJobSelection(selected?.Model ?? model);
        _hud.SetStatus($"Selected job: {model.Description}.");
    }

    internal void CancelBuildingPlacementFromHud()
    {
        CancelBuildingPlacement();
        _hud!.SetStatus("Building placement cancelled.");
    }

    internal void SelectResidentInventoryLayoutSlot(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        InteractResidentInventoryLayoutSlot(
            slot,
            altPressed: false,
            dropPressed: false);
    }

    internal void ActivateResidentInventoryLayoutSlot(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        SelectResidentInventoryLayoutSlot(slot);
    }

    internal void UseResidentInventoryLayoutSlot(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        InteractResidentInventoryLayoutSlot(
            slot,
            altPressed: true,
            dropPressed: false);
    }

    internal void DropResidentInventoryLayoutSlot(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        InteractResidentInventoryLayoutSlot(
            slot,
            altPressed: false,
            dropPressed: true);
    }

    internal void InteractResidentInventoryLayoutSlot(
        ResidentInventoryLayoutSlotViewModel slot,
        bool altPressed,
        bool dropPressed)
    {
        EnsureLayoutSlot(slot);
        var resident = _agentRenderer?.SelectedModel;
        if (resident == null || !resident.IsAlive || _hud == null)
        {
            _hud?.SetStatus("input.inventory.resident_not_selected");
            return;
        }

        if (!altPressed
            && !dropPressed
            && TryBeginTunnelReinforcementPlacement(slot))
        {
            return;
        }

        EntityId residentId = EntityId.Parse(resident.Id);
        EntityId stackId = EntityId.Parse(slot.StackId!);
        ContextInputState state = new ContextInputState(
            selectedResidentId: residentId,
            selectedResidentAlive: resident.IsAlive,
            selectedInventoryStackId: stackId,
            selectedInventoryItemUsable:
                slot.InteractionProfile.SupportsInventoryAction(
                    ItemInventoryInteractionAction.DirectUse),
            selectedInventoryItemIsBuildingBox: slot.IsBuildingBox,
            canUseSelectedInventoryItem: slot.CanUse,
            canDropSelectedInventoryItem: slot.CanDrop,
            canPlaceSelectedInventoryItem: slot.CanPlace);
        ContextPointerTarget target = new ContextPointerTarget(
            ContextWorldTargetKind.GenericItem,
            stackId,
            new CellId(resident.CellX, resident.CellY, resident.CellZ));
        ContextInputDecision decision = _inputRouter.Route(
            new ContextPointerEvent(
                PointerInputSurface.ResidentInventory,
                PointerButtonKind.Left,
                altPressed: altPressed,
                dropPressed: dropPressed),
            state,
            target);
        ApplyDecision(decision, inventorySlot: slot);
        ClearSelectedInventoryStack();
    }

    private int RegisterRosterResidentClick(string residentId)
    {
        float now = Time.unscaledTime;
        bool doubleClick = string.Equals(
                _lastRosterResidentClickId,
                residentId,
                StringComparison.Ordinal)
            && now - _lastRosterResidentClickTime <= DoubleClickSeconds;
        _lastRosterResidentClickId = residentId;
        _lastRosterResidentClickTime = now;
        return doubleClick ? 2 : 1;
    }

    private AgentViewModelFacts GetResidentFacts(string residentId)
    {
        var model = _agentRenderer!.GetHudModels().FirstOrDefault(value =>
            string.Equals(value.Id, residentId, StringComparison.Ordinal));
        if (model == null)
        {
            return new AgentViewModelFacts(
                isAlive: false,
                cellX: 0,
                cellY: 0,
                cellZ: 0);
        }

        return new AgentViewModelFacts(model.IsAlive, model.CellX, model.CellY, model.CellZ);
    }

    private static ResidentInventorySlotViewModel ToLegacySlot(
        ResidentInventoryLayoutSlotViewModel slot)
    {
        EnsureLayoutSlot(slot);
        ResidentInventoryItemKind itemKind = slot.VisualKind switch
        {
            ResidentInventorySlotVisualKind.BuildingBox =>
                ResidentInventoryItemKind.BuildingBox,
            ResidentInventorySlotVisualKind.Tool => ResidentInventoryItemKind.Tool,
            _ => ResidentInventoryItemKind.Generic,
        };
        return new ResidentInventorySlotViewModel(
            slot.StackId!,
            slot.ItemId!,
            slot.Quantity,
            slot.ReservedQuantity,
            itemKind,
            isEquipped: slot.IsHeld,
            heldQuantity: slot.HeldQuantity,
            interactionProfile: slot.InteractionProfile);
    }

    private static void EnsureLayoutSlot(ResidentInventoryLayoutSlotViewModel slot)
    {
        if (slot == null)
        {
            throw new ArgumentNullException(nameof(slot));
        }

        if (slot.IsEmpty || slot.StackId == null || slot.ItemId == null)
        {
            throw new ArgumentException("An occupied inventory slot is required.", nameof(slot));
        }
    }

    private void ClearSelectedInventoryStack()
    {
        _selectedInventoryStackId = null;
        _selectedInventoryItemUsable = false;
        _selectedInventoryItemIsBuildingBox = false;
        _selectedInventoryItemCanUse = false;
        _selectedInventoryItemCanDrop = false;
    }

    private readonly struct AgentViewModelFacts
    {
        public AgentViewModelFacts(bool isAlive, int cellX, int cellY, int cellZ)
        {
            IsAlive = isAlive;
            CellX = cellX;
            CellY = cellY;
            CellZ = cellZ;
        }

        public bool IsAlive { get; }

        public int CellX { get; }

        public int CellY { get; }

        public int CellZ { get; }
    }
}

}
