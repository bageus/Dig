using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPlacementPresenterTests
{
    private static readonly ItemId BoxItem = new ItemId("building_box.preview");
    private static readonly EntityId StackId = Id(1);
    private static readonly EntityId ReservationJobId = Id(2);
    private readonly BuildingBoxPlacementPresenter _presenter =
        new BuildingBoxPlacementPresenter(new BuildingPlacementValidator());

    [Fact]
    public void Valid_preview_uses_rotated_footprint_and_work_position()
    {
        BuildingDefinition definition = Definition();
        ItemDefinition item = Item();
        ItemStackSnapshot stack = Stack();
        WorldSnapshot world = World();
        CellId origin = new CellId(2, 2);

        BuildingBoxGhostViewModel north = _presenter.Preview(
            stack,
            item,
            definition,
            origin,
            BuildingOrientation.North,
            world,
            occupiedCells: Array.Empty<CellId>(),
            reachableCells: new[] { new CellId(2, 1) });
        BuildingBoxGhostViewModel east = _presenter.Preview(
            stack,
            item,
            definition,
            origin,
            BuildingOrientation.East,
            world,
            occupiedCells: Array.Empty<CellId>(),
            reachableCells: new[] { new CellId(3, 2) });

        Assert.True(north.IsValid);
        Assert.Equal(BuildingBoxGhostStyle.Valid, north.Style);
        Assert.Equal(
            new[] { new CellId(2, 2), new CellId(3, 2) },
            north.Footprint.ToArray());
        Assert.Equal(new CellId(2, 1), north.WorkPosition);
        Assert.True(east.IsValid);
        Assert.Equal(
            new[] { new CellId(2, 2), new CellId(2, 3) },
            east.Footprint.ToArray());
        Assert.Equal(new CellId(3, 2), east.WorkPosition);
    }

    [Fact]
    public void Placement_mode_rotation_is_stable_and_reversible()
    {
        BuildingBoxPlacementModeState initial = new BuildingBoxPlacementModeState(
            StackId,
            Definition().Id);

        BuildingBoxPlacementModeState clockwise = initial
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise();
        BuildingBoxPlacementModeState counter = initial
            .RotateCounterClockwise()
            .RotateClockwise();

        Assert.Equal(BuildingOrientation.North, clockwise.Orientation);
        Assert.Equal(BuildingOrientation.North, counter.Orientation);
        Assert.Equal(StackId, clockwise.SourceStackId);
    }

    [Fact]
    public void Source_mismatch_and_reservation_have_typed_reasons()
    {
        BuildingDefinition definition = Definition();
        ItemDefinition wrongItem = new ItemDefinition(
            new ItemId("other"),
            "Other",
            maximumStackSize: 1,
            isTool: false);
        ItemStackSnapshot reserved = new ItemStackSnapshot(
            StackId,
            BoxItem,
            quantity: 1,
            ItemLocation.InWorld(new CellId(1, 1)),
            new[] { new ItemQuantityReservationSnapshot(ReservationJobId, 1) });

        BuildingBoxGhostViewModel mismatch = _presenter.Preview(
            Stack(),
            wrongItem,
            definition,
            new CellId(2, 2),
            BuildingOrientation.North,
            World(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 1) });
        BuildingBoxGhostViewModel unavailable = _presenter.Preview(
            reserved,
            Item(),
            definition,
            new CellId(2, 2),
            BuildingOrientation.North,
            World(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 1) });

        Assert.False(mismatch.IsValid);
        Assert.Equal(BuildingBoxPreviewReasons.ItemMismatch, mismatch.ReasonCode);
        Assert.False(unavailable.IsValid);
        Assert.Equal(BuildingBoxPreviewReasons.BoxUnavailable, unavailable.ReasonCode);
    }

    [Fact]
    public void Authoritative_placement_reason_is_preserved_in_invalid_ghost()
    {
        CellId origin = new CellId(2, 2);
        BuildingBoxGhostViewModel occupied = _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            origin,
            BuildingOrientation.North,
            World(),
            occupiedCells: new[] { origin },
            reachableCells: new[] { new CellId(2, 1) });
        BuildingBoxGhostViewModel unreachable = _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            origin,
            BuildingOrientation.North,
            World(),
            occupiedCells: Array.Empty<CellId>(),
            reachableCells: Array.Empty<CellId>());

        Assert.Equal(BuildingBoxGhostStyle.Invalid, occupied.Style);
        Assert.Equal(BuildingErrors.PlacementOccupied.Code, occupied.ReasonCode);
        Assert.Equal(
            BuildingErrors.NoReachableWorkPosition.Code,
            unreachable.ReasonCode);
    }

    [Fact]
    public void Confirmation_draft_exists_only_for_valid_preview()
    {
        BuildingBoxGhostViewModel valid = _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            new CellId(2, 2),
            BuildingOrientation.East,
            World(),
            Array.Empty<CellId>(),
            new[] { new CellId(3, 2) });
        BuildingBoxGhostViewModel invalid = _presenter.Preview(
            sourceStack: null,
            sourceItem: null,
            Definition(),
            new CellId(2, 2),
            BuildingOrientation.North,
            World(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 1) });

        Result<BuildingBoxPlacementConfirmationDraft> draft =
            _presenter.CreateConfirmationDraft(valid);
        Result<BuildingBoxPlacementConfirmationDraft> rejected =
            _presenter.CreateConfirmationDraft(invalid);

        Assert.True(draft.IsSuccess);
        Assert.Equal(StackId, draft.Value.SourceStackId);
        Assert.Equal(Definition().Id, draft.Value.DefinitionId);
        Assert.Equal(BuildingOrientation.East, draft.Value.Orientation);
        Assert.Equal(new CellId(3, 2), draft.Value.WorkPosition);
        Assert.True(rejected.IsFailure);
    }

    private static BuildingDefinition Definition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("preview.workshop"),
            "Preview Workshop",
            new[] { new CellOffset(0, 0), new CellOffset(1, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 100,
            boxPolicy: new BuildingBoxPolicy(BoxItem, packingWork: 2));
    }

    private static ItemDefinition Item()
    {
        return new ItemDefinition(
            BoxItem,
            "Preview Box",
            maximumStackSize: 1,
            isTool: false);
    }

    private static ItemStackSnapshot Stack()
    {
        return new ItemStackSnapshot(
            StackId,
            BoxItem,
            quantity: 1,
            ItemLocation.InWorld(new CellId(1, 1)),
            Array.Empty<ItemQuantityReservationSnapshot>());
    }

    private static WorldSnapshot World()
    {
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            air,
            explored: true).Value.CreateSnapshot();
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}