using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Presentation.Buildings
{
public sealed partial class BuildingBoxPlacementPresenter
{
    private static BuildingBoxGhostViewModel PreviewRelocation(
        ItemStackSnapshot sourceStack,
        BuildingDefinition definition,
        CellId origin,
        BuildingOrientation orientation,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> occupiedCells,
        IReadOnlyCollection<CellId> reachableCells)
    {
        CellId[] footprint = { origin };
        if (!world.Size.Contains(origin))
        {
            return Invalid(
                sourceStack.StackId,
                definition,
                origin,
                orientation,
                footprint,
                BuildingErrors.PlacementOutOfBounds.Code,
                BuildingBoxPlacementKind.RelocateBox,
                isVisible: false);
        }

        CellSnapshot cell = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .First(value => value.Id == origin);
        if (cell.IsSolid)
        {
            return Invalid(
                sourceStack.StackId,
                definition,
                origin,
                orientation,
                footprint,
                BuildingErrors.PlacementSolid.Code,
                BuildingBoxPlacementKind.RelocateBox);
        }

        if (!cell.State.IsExplored)
        {
            return Invalid(
                sourceStack.StackId,
                definition,
                origin,
                orientation,
                footprint,
                BuildingErrors.PlacementUnexplored.Code,
                BuildingBoxPlacementKind.RelocateBox,
                isVisible: false);
        }

        if (!BuildingPlacementSurfaceFactProjector.HasSupportingPlane(origin, world))
        {
            return Invalid(
                sourceStack.StackId,
                definition,
                origin,
                orientation,
                footprint,
                PackableBuildingPlacementErrors.SurfaceMissing.Code,
                BuildingBoxPlacementKind.RelocateBox,
                isVisible: false);
        }

        if (occupiedCells.Contains(origin))
        {
            return Invalid(
                sourceStack.StackId,
                definition,
                origin,
                orientation,
                footprint,
                BuildingErrors.PlacementOccupied.Code,
                BuildingBoxPlacementKind.RelocateBox);
        }

        if (!reachableCells.Contains(origin))
        {
            return Invalid(
                sourceStack.StackId,
                definition,
                origin,
                orientation,
                footprint,
                BuildingErrors.NoReachableWorkPosition.Code,
                BuildingBoxPlacementKind.RelocateBox);
        }

        return new BuildingBoxGhostViewModel(
            sourceStack.StackId,
            definition.Id,
            origin,
            orientation,
            footprint,
            origin,
            isValid: true,
            reasonCode: null,
            BuildingBoxPlacementKind.RelocateBox);
    }
}
}
