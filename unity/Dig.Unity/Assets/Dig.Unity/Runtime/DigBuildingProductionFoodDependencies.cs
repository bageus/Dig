using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Application.Production;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private const int ProductionFoodDependencyPriority = 625;

        private void CreateEligibleFoodDependencyJobs(
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            if (_mushroomRepository == null
                || _startMushroomChop == null
                || _cancelMushroomChop == null
                || _buildingSupplyRepository == null
                || _buildingInventoryRepository == null
                || _createDeferredBuildingSupply == null)
            {
                return;
            }

            HashSet<CellId> revealed = GetProductionRevealedCells().ToHashSet();
            HashSet<CellId> reachable = GetProductionReachableCells().ToHashSet();
            InventorySnapshot inventory = _buildingInventoryRepository.Get().CreateSnapshot();
            BuildingSupplyState supplies = _buildingSupplyRepository.Get();

            ItemId[] supportedItems =
            {
                CampfireProductionContent.MushroomCapItemId,
                CampfireProductionContent.MushroomLegItemId,
            };
            foreach (BuildingSupplySnapshot supply in supplies.GetAll(inventory))
            {
                if (supply.HasActiveSupply
                    || HasNonTerminalBuildingSupplyJob(supply.BuildingId))
                {
                    continue;
                }

                ItemConsumptionRequest? request =
                    BuildingSupplyDependencyPlanner.PlanSingleExtractionRequest(
                        supply,
                        inventory.Stacks,
                        revealed,
                        reachable,
                        supportedItems);
                if (!request.HasValue)
                {
                    continue;
                }

                FoodDependencyCandidate? candidate = ResolveFoodDependencyCandidate(
                    agents,
                    revealed,
                    reachable);
                if (!candidate.HasValue)
                {
                    continue;
                }

                EntityId jobId = DemoId('9', checked(++_nextMushroomJobSequence));
                Result<MushroomChopStartedResult> started = _startMushroomChop.Handle(
                    new StartDirectMushroomChopCommand(
                        jobId,
                        candidate.Value.Site.SiteId,
                        candidate.Value.ResidentId,
                        candidate.Value.WorkPosition,
                        ProductionFoodDependencyPriority,
                        tick));
                if (started.IsSuccess)
                {
                    EntityId supplyJobId = NextProductionEntityId(
                        'd',
                        ref _nextSupplyJobSequence);
                    EntityId[] transit = Enumerable.Range(0, 12)
                        .Select(_ => NextProductionEntityId(
                            'c',
                            ref _nextSupplyTransitSequence))
                        .ToArray();
                    EntityId[] deposits =
                    {
                        NextProductionEntityId(
                            'b',
                            ref _nextSupplyDepositSequence),
                    };
                    Result deferred = _createDeferredBuildingSupply.Handle(
                        new CreateDeferredBuildingSupplyJobCommand(
                            supplyJobId,
                            supply.BuildingId,
                            new[] { request.Value },
                            new[] { jobId },
                            transit,
                            deposits,
                            ProductionFoodDependencyPriority,
                            tick));
                    if (deferred.IsSuccess)
                    {
                        // Continuous refill owns one dependency pair at a time. The
                        // next synchronization first consumes any new world output.
                        return;
                    }

                    _cancelMushroomChop.Handle(new CancelMushroomChopCommand(
                        jobId,
                        "dependent_supply_creation_failed",
                        tick));
                }
            }
        }

        private FoodDependencyCandidate? ResolveFoodDependencyCandidate(
            IReadOnlyList<AgentViewModel> agents,
            HashSet<CellId> revealed,
            HashSet<CellId> reachable)
        {
            FoodDependencyCandidate? best = null;
            foreach (MushroomSiteSnapshot site in _mushroomRepository!.Get().GetAll()
                .Where(value => value.Stage == MushroomStage.Large
                    && !value.IsChopActive
                    && revealed.Contains(value.Cell))
                .OrderBy(value => value.Cell.Y)
                .ThenBy(value => value.Cell.X)
                .ThenBy(value => value.Cell.Z)
                .ThenBy(value => value.SiteId.ToString(), StringComparer.Ordinal))
            {
                foreach (AgentViewModel agent in agents
                    .Where(IsAvailableForAutomaticWork)
                    .OrderBy(value => value.Id, StringComparer.Ordinal))
                {
                    CellId workerCell = new CellId(agent.CellX, agent.CellY, agent.CellZ);
                    if (!TryResolveMushroomWorkPosition(
                            site.Cell,
                            workerCell,
                            out CellId workPosition)
                        || !reachable.Contains(workPosition))
                    {
                        continue;
                    }

                    FoodDependencyCandidate current = new FoodDependencyCandidate(
                        site,
                        EntityId.Parse(agent.Id),
                        workPosition,
                        Distance(agent, workPosition));
                    if (!best.HasValue || current.CompareTo(best.Value) < 0)
                    {
                        best = current;
                    }
                }
            }

            return best;
        }

        private readonly struct FoodDependencyCandidate
            : IComparable<FoodDependencyCandidate>
        {
            internal FoodDependencyCandidate(
                MushroomSiteSnapshot site,
                EntityId residentId,
                CellId workPosition,
                int distance)
            {
                Site = site;
                ResidentId = residentId;
                WorkPosition = workPosition;
                DistanceCost = distance;
            }

            internal MushroomSiteSnapshot Site { get; }
            internal EntityId ResidentId { get; }
            internal CellId WorkPosition { get; }
            internal int DistanceCost { get; }

            public int CompareTo(FoodDependencyCandidate other)
            {
                int distance = DistanceCost.CompareTo(other.DistanceCost);
                if (distance != 0)
                {
                    return distance;
                }

                int site = string.Compare(
                    Site.SiteId.ToString(),
                    other.Site.SiteId.ToString(),
                    StringComparison.Ordinal);
                return site != 0
                    ? site
                    : string.Compare(
                        ResidentId.ToString(),
                        other.ResidentId.ToString(),
                        StringComparison.Ordinal);
            }
        }
    }
}