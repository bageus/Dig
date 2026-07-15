using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Buildings
{

public sealed partial class BuildingsState
{
    public static Result<BuildingsState> RestoreWithPacking(
        IEnumerable<BuildingSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        BuildingSnapshot[] values = snapshots
            .OrderBy(item => item.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        Result<BuildingsState> restored = Restore(values);
        if (restored.IsFailure)
        {
            return restored;
        }

        BuildingsState buildings = restored.Value;
        foreach (BuildingSnapshot snapshot in values)
        {
            if (snapshot.PackingPlan is null)
            {
                continue;
            }

            Result<BuildingPackingPlanState> packing =
                BuildingPackingPlanState.Restore(snapshot.PackingPlan, snapshot);
            if (packing.IsFailure)
            {
                return Result<BuildingsState>.Failure(packing.Error!);
            }

            buildings._packingPlans.Add(snapshot.Id, packing.Value);
        }

        return Result<BuildingsState>.Success(buildings);
    }
}
}
