using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Presentation.Buildings
{

public enum BuildingBoxGhostStyle
{
    Valid = 0,
    Invalid = 1,
}

public enum BuildingBoxPlacementKind
{
    RelocateBox = 0,
    AssembleBuilding = 1,
}

public sealed class BuildingBoxGhostViewModel
{
    public BuildingBoxGhostViewModel(
        EntityId? sourceStackId,
        BuildingDefinitionId definitionId,
        CellId origin,
        BuildingOrientation orientation,
        IEnumerable<CellId> footprint,
        CellId? workPosition,
        bool isValid,
        string? reasonCode,
        BuildingBoxPlacementKind placementKind = BuildingBoxPlacementKind.AssembleBuilding,
        bool isVisible = true,
        ItemId? sourceItemId = null)
    {
        if (sourceStackId.HasValue && sourceStackId.Value.IsEmpty)
        {
            throw new ArgumentException("Source stack id cannot be empty.", nameof(sourceStackId));
        }

        if (definitionId.IsEmpty)
        {
            throw new ArgumentException("Building definition id cannot be empty.", nameof(definitionId));
        }

        if (footprint is null)
        {
            throw new ArgumentNullException(nameof(footprint));
        }

        if (!Enum.IsDefined(typeof(BuildingBoxPlacementKind), placementKind))
        {
            throw new ArgumentOutOfRangeException(nameof(placementKind));
        }

        CellId[] ordered = footprint.Distinct().OrderBy(cell => cell).ToArray();
        if (ordered.Length == 0)
        {
            throw new ArgumentException("Ghost footprint cannot be empty.", nameof(footprint));
        }

        if (!isValid && string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException(
                "Invalid ghost preview requires a reason code.",
                nameof(reasonCode));
        }

        SourceStackId = sourceStackId;
        SourceItemId = sourceItemId;
        DefinitionId = definitionId;
        Origin = origin;
        Orientation = orientation;
        Footprint = new ReadOnlyCollection<CellId>(ordered);
        WorkPosition = workPosition;
        IsValid = isValid;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim();
        PlacementKind = placementKind;
        IsVisible = isVisible
            && !string.Equals(
                ReasonCode,
                PackableBuildingPlacementErrors.SurfaceMissing.Code,
                StringComparison.Ordinal);
    }

    public EntityId? SourceStackId { get; }

    public ItemId? SourceItemId { get; }

    public BuildingDefinitionId DefinitionId { get; }

    public CellId Origin { get; }

    public BuildingOrientation Orientation { get; }

    public IReadOnlyList<CellId> Footprint { get; }

    public CellId? WorkPosition { get; }

    public bool IsValid { get; }

    public string? ReasonCode { get; }

    public BuildingBoxPlacementKind PlacementKind { get; }

    public bool IsVisible { get; }

    public BuildingBoxGhostStyle Style => IsValid
        ? BuildingBoxGhostStyle.Valid
        : BuildingBoxGhostStyle.Invalid;
}

public readonly struct BuildingBoxPlacementConfirmationDraft
{
    public BuildingBoxPlacementConfirmationDraft(
        EntityId sourceStackId,
        BuildingDefinitionId definitionId,
        CellId origin,
        BuildingOrientation orientation,
        CellId workPosition,
        BuildingBoxPlacementKind placementKind = BuildingBoxPlacementKind.AssembleBuilding)
    {
        if (sourceStackId.IsEmpty)
        {
            throw new ArgumentException("Source stack id cannot be empty.", nameof(sourceStackId));
        }

        if (definitionId.IsEmpty)
        {
            throw new ArgumentException("Building definition id cannot be empty.", nameof(definitionId));
        }

        if (!Enum.IsDefined(typeof(BuildingBoxPlacementKind), placementKind))
        {
            throw new ArgumentOutOfRangeException(nameof(placementKind));
        }

        SourceStackId = sourceStackId;
        DefinitionId = definitionId;
        Origin = origin;
        Orientation = orientation;
        WorkPosition = workPosition;
        PlacementKind = placementKind;
    }

    public EntityId SourceStackId { get; }

    public BuildingDefinitionId DefinitionId { get; }

    public CellId Origin { get; }

    public BuildingOrientation Orientation { get; }

    public CellId WorkPosition { get; }

    public BuildingBoxPlacementKind PlacementKind { get; }
}

public readonly struct BuildingBoxPlacementModeState
{
    public BuildingBoxPlacementModeState(
        EntityId sourceStackId,
        BuildingDefinitionId definitionId,
        BuildingOrientation orientation = BuildingOrientation.North)
    {
        if (sourceStackId.IsEmpty)
        {
            throw new ArgumentException("Source stack id cannot be empty.", nameof(sourceStackId));
        }

        if (definitionId.IsEmpty)
        {
            throw new ArgumentException("Building definition id cannot be empty.", nameof(definitionId));
        }

        SourceStackId = sourceStackId;
        DefinitionId = definitionId;
        Orientation = orientation;
    }

    public EntityId SourceStackId { get; }

    public BuildingDefinitionId DefinitionId { get; }

    public BuildingOrientation Orientation { get; }

    public BuildingBoxPlacementModeState RotateClockwise()
    {
        BuildingOrientation next = (BuildingOrientation)(((int)Orientation + 1) % 4);
        return new BuildingBoxPlacementModeState(SourceStackId, DefinitionId, next);
    }

    public BuildingBoxPlacementModeState RotateCounterClockwise()
    {
        BuildingOrientation next = (BuildingOrientation)(((int)Orientation + 3) % 4);
        return new BuildingBoxPlacementModeState(SourceStackId, DefinitionId, next);
    }
}

}
