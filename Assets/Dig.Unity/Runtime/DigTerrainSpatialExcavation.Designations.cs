using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private Result EnsureSpatialExcavationDesignation(CellId target, long tick)
        {
            WorldState world = _worldSession.Repository.Get();
            Result<CellSnapshot> current = world.GetCell(target);
            if (current.IsFailure)
            {
                return Result.Failure(current.Error!);
            }

            if (current.Value.State.Designation == CellDesignation.Dig)
            {
                return Result.Success();
            }

            Result<WorldMutationResult> designated = world.SetDigDesignation(
                target,
                designated: true,
                tick);
            if (designated.IsFailure)
            {
                return Result.Failure(designated.Error!);
            }

            _worldSession.Repository.Save(world);
            _worldSession.Journal.Append(world.DequeueUncommittedEvents());
            MarkAuthoritativeWorldChanged();
            return Result.Success();
        }
    }
}
