using System;
using System.Collections.Generic;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Presentation.Buildings
{

public static class BuildingBoxPreviewReasons
{
    public const string MissingSource = "building_box.preview.source_missing";
    public const string DefinitionNotEnabled = "building_box.preview.definition_not_enabled";
    public const string ItemMismatch = "building_box.preview.item_mismatch";
    public const string BoxNotSingle = "building_box.preview.box_not_single";
    public const string BoxUnavailable = "building_box.preview.box_unavailable";
}

public sealed class BuildingBoxPlacementPresenter
{
    private static readonly DomainError InvalidPreview = new DomainError(
        "building_box.preview.invalid",
        "The BuildingBox placement preview is not valid for confirmation.");

    private readonly BuildingPlacementValidator _validator;

    public BuildingBoxPlacementPresenter(BuildingPlacementValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public BuildingBoxGhostViewModel Preview(
        ItemStackSnapshot? sourceStack,
        ItemDefinition? sourceItem,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> occupiedCells,
        IReadOnlyCollection<CellId> reachableCells)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (occupiedCells is null || reachableCells is null)
        {
            throw new ArgumentNullException(nameof(occupiedCells));
        }

        IReadOnlyList<CellId> footprint = definition.ResolveFootprint(origin, orientation);
        string? sourceReason = ValidateSource(sourceStack, sourceItem, definition);
        if (sourceReason is not null)
        {
            return Invalid(
                sourceStack?.StackId,
                definition,
                origin,
                orientation,
                footprint,
                sourceReason);
        }

        BuildingPlacementResult placement = _validator.Validate(
            definition,
            origin,
            orientation,
            world,
            occupiedCells,
            reachableCells);
        if (!placement.Succeeded)
        {
            return Invalid(
                sourceStack!.StackId,
                definition,
                origin,
                orientation,
                placement.Footprint,
                placement.Error!.Code);
        }

        return new BuildingBoxGhostViewModel(
            sourceStack!.StackId,
            definition.Id,
            origin,
            orientation,
            placement.Footprint,
            placement.WorkPosition,
            isValid: true,
            reasonCode: null);
    }

    public Result<BuildingBoxPlacementConfirmationDraft> CreateConfirmationDraft(
        BuildingBoxGhostViewModel preview)
    {
        if (preview is null)
        {
            throw new ArgumentNullException(nameof(preview));
        }

        if (!preview.IsValid
            || !preview.SourceStackId.HasValue
            || !preview.WorkPosition.HasValue)
        {
            return Result<BuildingBoxPlacementConfirmationDraft>.Failure(InvalidPreview);
        }

        return Result<BuildingBoxPlacementConfirmationDraft>.Success(
            new BuildingBoxPlacementConfirmationDraft(
                preview.SourceStackId.Value,
                preview.DefinitionId,
                preview.Origin,
                preview.Orientation,
                preview.WorkPosition.Value));
    }

    private static string? ValidateSource(
        ItemStackSnapshot? sourceStack,
        ItemDefinition? sourceItem,
        BuildingDefinition definition)
    {
        if (definition.BoxPolicy is null)
        {
            return BuildingBoxPreviewReasons.DefinitionNotEnabled;
        }

        if (sourceStack is null || sourceItem is null)
        {
            return BuildingBoxPreviewReasons.MissingSource;
        }

        if (sourceStack.ItemId != definition.BoxPolicy.BoxItemId
            || sourceItem.Id != sourceStack.ItemId)
        {
            return BuildingBoxPreviewReasons.ItemMismatch;
        }

        if (sourceItem.MaximumStackSize != 1 || sourceStack.Quantity != 1)
        {
            return BuildingBoxPreviewReasons.BoxNotSingle;
        }

        return sourceStack.AvailableQuantity == 1
            ? null
            : BuildingBoxPreviewReasons.BoxUnavailable;
    }

    private static BuildingBoxGhostViewModel Invalid(
        EntityId? sourceStackId,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        IEnumerable<CellId> footprint,
        string reasonCode)
    {
        return new BuildingBoxGhostViewModel(
            sourceStackId,
            definition.Id,
            origin,
            orientation,
            footprint,
            workPosition: null,
            isValid: false,
            reasonCode);
    }
}
}