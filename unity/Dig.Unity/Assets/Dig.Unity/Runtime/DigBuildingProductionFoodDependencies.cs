using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
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
                || _buildingSupplyRepository == null
                || _productionRepository == null
                || _buildingInventoryRepository == null)
            {
                return;
            }

            HashSet<CellId> revealed = GetProductionRevealedCells().ToHashSet();
            HashSet<CellId> reachable = GetProductionReachableCells().ToHashSet();
            InventorySnapshot inventory = _buildingInventoryRepository.Get().CreateSnapshot();
            BuildingSupplyState supplies = _buildingSupplyRepository.Get();

            foreach (BuildingSupplySnapshot supply in supplies.GetAll(inventory))
            {
                ProductionOrderSnapshot? order = _productionRepository.Get()
                    .GetNextQueued(supply.BuildingId);
                if (order == null
                    || order.Recipe.Id != CampfireProductionContent.GrilledMushroomRecipeId
                    || supply.HasActiveSupply
                    || _productionRepository.Get().HasActiveOrder(supply.BuildingId))
                {
                    continue;
                }

                BuildingStockSnapshot capStock = supply.Stocks.First(
                    value => value.ItemId == CampfireProductionContent.MushroomCapItemId);
                if (capStock.Current > 0 || capStock.Incoming > 0)
                {
                    continue;
                }

                bool eligibleWorldCap = inventory.Stacks.Any(stack =>
                    stack.ItemId == CampfireProductionContent.MushroomCapItemId
                    && stack.Location.Kind == ItemLocationKind.World
                    && stack.Location.HasCell
                    && stack.AvailableQuantity > 0
                    && revealed.Contains(stack.Location.CellId)
                    && reachable.Contains(stack.Location.CellId));
                if (eligibleWorldCap)
                {
                    // The ordinary BuildingSupply pipeline owns an existing cap. Do not
                    // create extra biological work merely because no worker is free yet.
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
                    // One new biological dependency per synchronization tick is enough;
                    // its world drops are reconciled by the normal supply chain.
                    return;
                }
            }
        }

        private FoodDependencyCandidate? ResolveFoodDependencyCandidate(
            IReadOnlyList<AgentViewModel> agents,
            IReadOnlySet<CellId> revealed,
            IReadOnlySet<CellId> reachable)
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