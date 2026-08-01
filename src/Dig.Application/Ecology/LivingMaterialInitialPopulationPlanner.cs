using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Ecology
{

public sealed class LivingMaterialInitialPlacement
{
    public LivingMaterialInitialPlacement(
        LivingMaterialSpecies species,
        CellId cell,
        LivingMaterialPlaneKey planeKey)
    {
        if (!Enum.IsDefined(typeof(LivingMaterialSpecies), species))
        {
            throw new ArgumentOutOfRangeException(nameof(species));
        }

        Species = species;
        Cell = cell;
        PlaneKey = planeKey;
    }

    public LivingMaterialSpecies Species { get; }

    public CellId Cell { get; }

    public LivingMaterialPlaneKey PlaneKey { get; }
}

public sealed class LivingMaterialInitialPopulationPlan
{
    public LivingMaterialInitialPopulationPlan(
        IReadOnlyCollection<LivingMaterialInitialPlacement> placements)
    {
        if (placements == null)
        {
            throw new ArgumentNullException(nameof(placements));
        }

        Placements = new ReadOnlyCollection<LivingMaterialInitialPlacement>(
            placements.ToArray());
    }

    public IReadOnlyList<LivingMaterialInitialPlacement> Placements { get; }
}

public static class LivingMaterialInitialPopulationErrors
{
    public static readonly DomainError NoSuitablePlane = new DomainError(
        "ecology.initial_population.no_suitable_plane",
        "The world has no supported flat plane for two hamster and one grub seeds.");
}

public sealed class LivingMaterialInitialPopulationPlanner
{
    public Result<LivingMaterialInitialPopulationPlan> Plan(
        NavigationSnapshot navigation,
        IReadOnlyCollection<CellId> occupiedWorldCells)
    {
        if (navigation == null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        if (occupiedWorldCells == null)
        {
            throw new ArgumentNullException(nameof(occupiedWorldCells));
        }

        HashSet<CellId> occupied = new HashSet<CellId>(occupiedWorldCells);
        LivingMaterialPlaneResolver resolver = new LivingMaterialPlaneResolver(navigation);
        Dictionary<LivingMaterialPlaneKey, LivingMaterialPlane> planes =
            new Dictionary<LivingMaterialPlaneKey, LivingMaterialPlane>();

        for (int z = CellId.MinimumDepth; z < navigation.WorldSize.Depth; z++)
        {
            for (int y = 0; y < navigation.WorldSize.Height; y++)
            {
                for (int x = 0; x < navigation.WorldSize.Width; x++)
                {
                    CellId cell = new CellId(x, y, z);
                    if (!navigation.IsWalkable(cell)
                        || !resolver.TryResolve(cell, out LivingMaterialPlane plane))
                    {
                        continue;
                    }

                    if (!planes.ContainsKey(plane.Key))
                    {
                        planes.Add(plane.Key, plane);
                    }
                }
            }
        }

        CandidatePlane[] candidates = planes.Values
            .Select(plane => new CandidatePlane(
                plane.Key,
                plane.Cells.Where(cell => !occupied.Contains(cell))))
            .Where(value => value.Cells.Count > 0)
            .OrderBy(value => value.Key)
            .ToArray();
        CandidatePlane? hamsterPlane = candidates.FirstOrDefault(
            value => value.Cells.Count >= 2);
        if (hamsterPlane == null)
        {
            return Result<LivingMaterialInitialPopulationPlan>.Failure(
                LivingMaterialInitialPopulationErrors.NoSuitablePlane);
        }

        CellId firstHamster = hamsterPlane.Cells[hamsterPlane.Cells.Count / 3];
        CellId secondHamster = hamsterPlane.Cells[(hamsterPlane.Cells.Count * 2) / 3];
        if (secondHamster == firstHamster)
        {
            secondHamster = hamsterPlane.Cells[hamsterPlane.Cells.Count - 1];
        }

        CandidatePlane? grubPlane = candidates.FirstOrDefault(
            value => value.Key != hamsterPlane.Key);
        CellId grubCell;
        LivingMaterialPlaneKey grubPlaneKey;
        if (grubPlane != null)
        {
            grubCell = grubPlane.Cells[grubPlane.Cells.Count / 2];
            grubPlaneKey = grubPlane.Key;
        }
        else
        {
            CellId? fallback = hamsterPlane.Cells
                .Where(cell => cell != firstHamster && cell != secondHamster)
                .Select(cell => (CellId?)cell)
                .FirstOrDefault();
            if (!fallback.HasValue)
            {
                return Result<LivingMaterialInitialPopulationPlan>.Failure(
                    LivingMaterialInitialPopulationErrors.NoSuitablePlane);
            }

            grubCell = fallback.Value;
            grubPlaneKey = hamsterPlane.Key;
        }

        LivingMaterialInitialPlacement[] placements =
        {
            new LivingMaterialInitialPlacement(
                LivingMaterialSpecies.Hamster,
                firstHamster,
                hamsterPlane.Key),
            new LivingMaterialInitialPlacement(
                LivingMaterialSpecies.Hamster,
                secondHamster,
                hamsterPlane.Key),
            new LivingMaterialInitialPlacement(
                LivingMaterialSpecies.Grub,
                grubCell,
                grubPlaneKey),
        };
        return Result<LivingMaterialInitialPopulationPlan>.Success(
            new LivingMaterialInitialPopulationPlan(placements));
    }

    private sealed class CandidatePlane
    {
        public CandidatePlane(
            LivingMaterialPlaneKey key,
            IEnumerable<CellId> cells)
        {
            Key = key;
            Cells = cells.OrderBy(value => value).ToArray();
        }

        public LivingMaterialPlaneKey Key { get; }

        public IReadOnlyList<CellId> Cells { get; }
    }
}

}
