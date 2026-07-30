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
    public void Upper_layer_preview_uses_rotated_footprint_and_work_position()
    {
        CellId origin = new CellId(2, 2, 1);

        BuildingBoxGhostViewModel north = Preview(
            origin,
            BuildingOrientation.North,
            reachable: new[] { new CellId(2, 1, 1) });
        BuildingBoxGhostViewModel east = Preview(
            origin,
            BuildingOrientation.East,
            reachable: new[] { new CellId(3, 2, 1) });

        Assert.True(north.IsValid);
        Assert.True(north.IsVisible);
        Assert.Equal(BuildingBoxPlacementKind.AssembleBuilding, north.PlacementKind);
        Assert.Equal(BuildingBoxGhostStyle.Valid, north.Style);
        Assert.Equal(
            new[] { new CellId(2, 2, 1), new CellId(3, 2, 1) },
            north.Footprint.ToArray());
        Assert.Equal(new CellId(2, 1, 1), north.WorkPosition);
        Assert.True(east.IsValid);
        Assert.True(east.IsVisible);
        Assert.Equal(
            new[] { new CellId(2, 2, 1), new CellId(2, 3, 1) },
            east.Footprint.ToArray());
        Assert.Equal(new CellId(3, 2, 1), east.WorkPosition);
    }

    [Fact]
    public void Z0_preview_is_single_cell_box_relocation()
    {
        CellId target = new CellId(4, 3, 0);

        BuildingBoxGhostViewModel preview = Preview(
            target,
            BuildingOrientation.West,
            reachable: new[] { target });
        Result<BuildingBoxPlacementConfirmationDraft> drafted =
            _presenter.CreateConfirmationDraft(preview);

        Assert.True(preview.IsValid);
        Assert.True(preview.IsVisible);
        Assert.Equal(BuildingBoxPlacementKind.RelocateBox, preview.PlacementKind);
        Assert.Equal(new[] { target }, preview.Footprint);
        Assert.Equal(target, preview.WorkPosition);
        Assert.True(drafted.IsSuccess);
        Assert.Equal(BuildingBoxPlacementKind.RelocateBox, drafted.Value.PlacementKind);
    }

    [Fact]
    public void Unsupported_air_hides_box_and_building_ghosts()
    {
        CellId boxTarget = new CellId(4, 3, 0);
        CellId buildingOrigin = new CellId(2, 2, 1);
        WorldSnapshot air = BuildingBoxPlacementTestWorld.Empty();

        BuildingBoxGhostViewModel box = _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            boxTarget,
            BuildingOrientation.North,
            air,
            Array.Empty<CellId>(),
            new[] { boxTarget });
        BuildingBoxGhostViewModel building = _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            buildingOrigin,
            BuildingOrientation.North,
            air,
            Array.Empty<CellId>(),
            new[] { new CellId(2, 1, 1) });

        Assert.False(box.IsValid);
        Assert.False(box.IsVisible);
        Assert.Equal(PackableBuildingPlacementErrors.SurfaceMissing.Code, box.ReasonCode);
        Assert.True(_presenter.CreateConfirmationDraft(box).IsFailure);
        Assert.False(building.IsValid);
        Assert.False(building.IsVisible);
        Assert.Equal(PackableBuildingPlacementErrors.SurfaceMissing.Code, building.ReasonCode);
        Assert.True(_presenter.CreateConfirmationDraft(building).IsFailure);
    }

    [Fact]
    public void Z0_relocation_ignores_non_building_occupants_by_contract()
    {
        CellId target = new CellId(3, 3, 0);

        BuildingBoxGhostViewModel preview = _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            target,
            BuildingOrientation.North,
            BuildingBoxPlacementTestWorld.Supported(
                Definition(),
                target,
                BuildingOrientation.North,
                new[] { target }),
            occupiedCells: Array.Empty<CellId>(),
            reachableCells: new[] { target });

        Assert.True(preview.IsValid);
        Assert.True(preview.IsVisible);
        Assert.Equal(BuildingBoxPlacementKind.RelocateBox, preview.PlacementKind);
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
        CellId origin = new CellId(2, 2, 1);
        CellId[] reachable = { new CellId(2, 1, 1) };
        WorldSnapshot world = BuildingBoxPlacementTestWorld.Supported(
            Definition(),
            origin,
            BuildingOrientation.North,
            reachable);

        BuildingBoxGhostViewModel mismatch = _presenter.Preview(
            Stack(),
            wrongItem,
            Definition(),
            origin,
            BuildingOrientation.North,
            world,
            Array.Empty<CellId>(),
            reachable);
        BuildingBoxGhostViewModel unavailable = _presenter.Preview(
            reserved,
            Item(),
            Definition(),
            origin,
            BuildingOrientation.North,
            world,
            Array.Empty<CellId>(),
            reachable);

        Assert.False(mismatch.IsValid);
        Assert.Equal(BuildingBoxPreviewReasons.ItemMismatch, mismatch.ReasonCode);
        Assert.False(unavailable.IsValid);
        Assert.Equal(BuildingBoxPreviewReasons.BoxUnavailable, unavailable.ReasonCode);
    }

    [Fact]
    public void Authoritative_placement_reason_is_preserved_in_invalid_ghost()
    {
        CellId origin = new CellId(2, 2, 1);
        BuildingBoxGhostViewModel occupied = Preview(
            origin,
            BuildingOrientation.North,
            occupied: new[] { origin },
            reachable: new[] { new CellId(2, 1, 1) });
        BuildingBoxGhostViewModel unreachable = Preview(
            origin,
            BuildingOrientation.North,
            reachable: Array.Empty<CellId>());

        Assert.Equal(BuildingBoxGhostStyle.Invalid, occupied.Style);
        Assert.Equal(BuildingErrors.PlacementOccupied.Code, occupied.ReasonCode);
        Assert.Equal(
            BuildingErrors.NoReachableWorkPosition.Code,
            unreachable.ReasonCode);
    }

    [Fact]
    public void Confirmation_draft_exists_only_for_valid_preview()
    {
        BuildingBoxGhostViewModel valid = Preview(
            new CellId(2, 2, 1),
            BuildingOrientation.East,
            reachable: new[] { new CellId(3, 2, 1) });
        BuildingBoxGhostViewModel invalid = _presenter.Preview(
            sourceStack: null,
            sourceItem: null,
            Definition(),
            new CellId(2, 2, 1),
            BuildingOrientation.North,
            BuildingBoxPlacementTestWorld.Empty(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 1, 1) });

        Result<BuildingBoxPlacementConfirmationDraft> draft =
            _presenter.CreateConfirmationDraft(valid);
        Result<BuildingBoxPlacementConfirmationDraft> rejected =
            _presenter.CreateConfirmationDraft(invalid);

        Assert.True(draft.IsSuccess);
        Assert.Equal(StackId, draft.Value.SourceStackId);
        Assert.Equal(Definition().Id, draft.Value.DefinitionId);
        Assert.Equal(BuildingOrientation.East, draft.Value.Orientation);
        Assert.Equal(new CellId(3, 2, 1), draft.Value.WorkPosition);
        Assert.Equal(BuildingBoxPlacementKind.AssembleBuilding, draft.Value.PlacementKind);
        Assert.True(rejected.IsFailure);
    }

    private BuildingBoxGhostViewModel Preview(
        CellId origin,
        BuildingOrientation orientation,
        CellId[]? occupied = null,
        CellId[]? reachable = null)
    {
        CellId[] routes = reachable ?? Array.Empty<CellId>();
        return _presenter.Preview(
            Stack(),
            Item(),
            Definition(),
            origin,
            orientation,
            BuildingBoxPlacementTestWorld.Supported(
                Definition(),
                origin,
                orientation,
                routes),
            occupied ?? Array.Empty<CellId>(),
            routes);
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

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
