using Dig.Application.Inventory;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private CellId ResolveBuildingBoxRelocationTarget(
            JobSnapshot job,
            BuildingBoxPickupJobDefinition relocation,
            CellId start,
            NavigationSnapshot navigation,
            out PathResult path)
        {
            ItemStackSnapshot? box = _buildingInventoryRepository!.Get().GetStack(
                relocation.StackId);
            bool carriedByWorker = job.AssignedAgentId.HasValue
                && box != null
                && DropResidentInventoryStackHandler.IsOwnedByResident(
                    box.Location,
                    job.AssignedAgentId.Value);
            bool delivering = relocation.StartsHeld
                || carriedByWorker
                || job.Stage == JobStageKind.TravelToDestination
                || job.Stage == JobStageKind.DepositItem;
            if (!delivering)
            {
                path = FindBuildingBoxRelocationPath(
                    start,
                    relocation.SourceCell,
                    navigation);
                return relocation.SourceCell;
            }

            return ResolveBuildingBoxRelocationWorkTarget(
                start,
                relocation.DestinationCell!.Value,
                navigation,
                out path);
        }

        private CellId ResolveBuildingBoxRelocationWorkTarget(
            CellId start,
            CellId destination,
            NavigationSnapshot navigation,
            out PathResult path)
        {
            CellId[] candidates =
            {
                new CellId(destination.X - 1, destination.Y, destination.Z),
                new CellId(destination.X + 1, destination.Y, destination.Z),
                new CellId(destination.X, destination.Y - 1, destination.Z),
                new CellId(destination.X, destination.Y + 1, destination.Z),
            };
            CellId? bestTarget = null;
            PathResult? bestPath = null;
            for (int index = 0; index < candidates.Length; index++)
            {
                CellId candidate = candidates[index];
                if (!navigation.IsWalkable(candidate)
                    || !HasFullStandingSupport(candidate))
                {
                    continue;
                }

                PathResult candidatePath = FindBuildingBoxRelocationPath(
                    start,
                    candidate,
                    navigation);
                if (!candidatePath.Succeeded || candidatePath.Path == null)
                {
                    continue;
                }

                int candidateCost = candidatePath.Path!.TotalCost;
                int bestCost = bestPath?.Path?.TotalCost ?? int.MaxValue;
                if (bestPath == null
                    || candidateCost < bestCost
                    || (candidateCost == bestCost
                        && candidate.CompareTo(
                            bestTarget.GetValueOrDefault()) < 0))
                {
                    bestTarget = candidate;
                    bestPath = candidatePath;
                }
            }

            if (bestTarget.HasValue)
            {
                path = bestPath!;
                return bestTarget.Value;
            }

            if (navigation.IsWalkable(destination)
                && HasFullStandingSupport(destination))
            {
                path = FindBuildingBoxRelocationPath(start, destination, navigation);
                return destination;
            }

            path = PathResult.Failure(
                new PathSearchDiagnostics(
                    PathFailureReason.InvalidGoal,
                    expandedNodes: 0,
                    startRegion: null,
                    goalRegion: null,
                    navigation.NavigationVersion,
                    "BuildingBox relocation has no supported deposit work position."));
            return destination;
        }

        private PathResult FindBuildingBoxRelocationPath(
            CellId start,
            CellId target,
            NavigationSnapshot navigation)
        {
            return _buildingBoxPickupPathfinder!.FindPath(
                navigation,
                new PathRequest(start, target, navigation.NavigationVersion));
        }

    }
}
