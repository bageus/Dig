using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Presentation.Input
{

public enum PointerInputSurface
{
    World = 0,
    ResidentRoster = 1,
    ResidentInventory = 2,
}

public enum PointerButtonKind
{
    Left = 0,
    Right = 1,
}

public enum ContextWorldTargetKind
{
    None = 0,
    Ground = 1,
    Resident = 2,
    HostileResident = 3,
    BuildingBox = 4,
    CompletedBuilding = 5,
    GenericItem = 6,
}

public enum ContextPanelMode
{
    ExcavationPalette = 0,
    ResidentInventory = 1,
    BuildingFunctions = 2,
    BuildingPlacement = 3,
}

public enum ExcavationToolKind
{
    None = 0,
    Tunnel = 1,
    Room = 2,
    Erase = 3,
}

[Flags]
public enum PresentationInputEffect
{
    None = 0,
    SelectResident = 1,
    SelectBuilding = 2,
    SelectGround = 4,
    DeselectResident = 8,
    FocusResident = 16,
    StartBuildingPlacement = 32,
    CancelBuildingPlacement = 64,
    KeepBuildingPreview = 128,
    ShowReason = 256,
}

public enum ApplicationInputCommandKind
{
    None = 0,
    ConfirmBuildingPlacement = 1,
    UseInventoryItem = 2,
    DropInventoryStack = 3,
    PickupBuildingBox = 4,
    AttackTarget = 5,
    MoveResident = 6,
    ApplyExcavation = 7,
    PickupWorldItem = 8,
}

public readonly struct ContextPointerEvent
{
    public ContextPointerEvent(
        PointerInputSurface surface,
        PointerButtonKind button,
        int clickCount = 1,
        bool altPressed = false,
        bool isPointerOverBlockingUi = false)
    {
        if (!Enum.IsDefined(typeof(PointerInputSurface), surface))
        {
            throw new ArgumentOutOfRangeException(nameof(surface));
        }

        if (!Enum.IsDefined(typeof(PointerButtonKind), button))
        {
            throw new ArgumentOutOfRangeException(nameof(button));
        }

        if (clickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clickCount));
        }

        Surface = surface;
        Button = button;
        ClickCount = clickCount;
        AltPressed = altPressed;
        IsPointerOverBlockingUi = isPointerOverBlockingUi;
    }

    public PointerInputSurface Surface { get; }
    public PointerButtonKind Button { get; }
    public int ClickCount { get; }
    public bool AltPressed { get; }
    public bool IsPointerOverBlockingUi { get; }
}

public sealed class ContextInputState
{
    public ContextInputState(
        EntityId? selectedResidentId = null,
        bool selectedResidentAlive = true,
        EntityId? selectedCompletedBuildingId = null,
        EntityId? selectedInventoryStackId = null,
        bool selectedInventoryItemUsable = false,
        bool selectedInventoryItemIsBuildingBox = false,
        bool canUseSelectedInventoryItem = false,
        bool canDropSelectedInventoryItem = false,
        bool buildingPlacementActive = false,
        bool buildingPlacementValid = false,
        string? buildingPlacementReasonCode = null,
        ExcavationToolKind excavationTool = ExcavationToolKind.None)
    {
        if (selectedResidentId.HasValue && selectedResidentId.Value.IsEmpty)
        {
            throw new ArgumentException("Selected resident id cannot be empty.", nameof(selectedResidentId));
        }

        if (selectedCompletedBuildingId.HasValue && selectedCompletedBuildingId.Value.IsEmpty)
        {
            throw new ArgumentException(
                "Selected building id cannot be empty.",
                nameof(selectedCompletedBuildingId));
        }

        if (selectedInventoryStackId.HasValue && selectedInventoryStackId.Value.IsEmpty)
        {
            throw new ArgumentException(
                "Selected stack id cannot be empty.",
                nameof(selectedInventoryStackId));
        }

        if (!Enum.IsDefined(typeof(ExcavationToolKind), excavationTool))
        {
            throw new ArgumentOutOfRangeException(nameof(excavationTool));
        }

        SelectedResidentId = selectedResidentId;
        SelectedResidentAlive = selectedResidentAlive;
        SelectedCompletedBuildingId = selectedCompletedBuildingId;
        SelectedInventoryStackId = selectedInventoryStackId;
        SelectedInventoryItemUsable = selectedInventoryItemUsable;
        SelectedInventoryItemIsBuildingBox = selectedInventoryItemIsBuildingBox;
        CanUseSelectedInventoryItem = canUseSelectedInventoryItem;
        CanDropSelectedInventoryItem = canDropSelectedInventoryItem;
        BuildingPlacementActive = buildingPlacementActive;
        BuildingPlacementValid = buildingPlacementValid;
        BuildingPlacementReasonCode = Normalize(buildingPlacementReasonCode);
        ExcavationTool = excavationTool;
    }

    public EntityId? SelectedResidentId { get; }
    public bool SelectedResidentAlive { get; }
    public EntityId? SelectedCompletedBuildingId { get; }
    public EntityId? SelectedInventoryStackId { get; }
    public bool SelectedInventoryItemUsable { get; }
    public bool SelectedInventoryItemIsBuildingBox { get; }
    public bool CanUseSelectedInventoryItem { get; }
    public bool CanDropSelectedInventoryItem { get; }
    public bool BuildingPlacementActive { get; }
    public bool BuildingPlacementValid { get; }
    public string? BuildingPlacementReasonCode { get; }
    public ExcavationToolKind ExcavationTool { get; }

    public bool HasUsableResidentSelection =>
        SelectedResidentId.HasValue && SelectedResidentAlive;

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class ContextPointerTarget
{
    public ContextPointerTarget(
        ContextWorldTargetKind kind,
        EntityId? entityId = null,
        CellId? cell = null,
        bool reachable = false,
        bool supportsAltInteraction = false,
        bool isAlive = true)
    {
        if (!Enum.IsDefined(typeof(ContextWorldTargetKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (entityId.HasValue && entityId.Value.IsEmpty)
        {
            throw new ArgumentException("Target entity id cannot be empty.", nameof(entityId));
        }

        Kind = kind;
        EntityId = entityId;
        Cell = cell;
        Reachable = reachable;
        SupportsAltInteraction = supportsAltInteraction;
        IsAlive = isAlive;
    }

    public ContextWorldTargetKind Kind { get; }
    public EntityId? EntityId { get; }
    public CellId? Cell { get; }
    public bool Reachable { get; }
    public bool SupportsAltInteraction { get; }
    public bool IsAlive { get; }
}

public sealed class ContextInputDecision
{
    public ContextInputDecision(
        PresentationInputEffect effects,
        ApplicationInputCommandKind commandKind,
        bool consumesPointer,
        EntityId? actorId = null,
        EntityId? targetEntityId = null,
        CellId? targetCell = null,
        ExcavationToolKind excavationTool = ExcavationToolKind.None,
        string? reasonCode = null)
    {
        if (actorId.HasValue && actorId.Value.IsEmpty)
        {
            throw new ArgumentException("Decision actor id cannot be empty.", nameof(actorId));
        }

        if (targetEntityId.HasValue && targetEntityId.Value.IsEmpty)
        {
            throw new ArgumentException(
                "Decision target id cannot be empty.",
                nameof(targetEntityId));
        }

        Effects = effects;
        CommandKind = commandKind;
        ConsumesPointer = consumesPointer;
        ActorId = actorId;
        TargetEntityId = targetEntityId;
        TargetCell = targetCell;
        ExcavationTool = excavationTool;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim();
    }

    public PresentationInputEffect Effects { get; }
    public ApplicationInputCommandKind CommandKind { get; }
    public bool ConsumesPointer { get; }
    public EntityId? ActorId { get; }
    public EntityId? TargetEntityId { get; }
    public CellId? TargetCell { get; }
    public ExcavationToolKind ExcavationTool { get; }
    public string? ReasonCode { get; }
    public bool HasApplicationCommand => CommandKind != ApplicationInputCommandKind.None;
}
}
