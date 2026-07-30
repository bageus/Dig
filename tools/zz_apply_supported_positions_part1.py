from pathlib import Path


def replace_required(path: str, old: str, new: str) -> None:
    p = Path(path)
    text = p.read_text()
    if old not in text:
        raise SystemExit(f"Required text not found in {path}: {old[:120]!r}")
    p.write_text(text.replace(old, new, 1))


def append_once(path: str, marker: str, content: str) -> None:
    p = Path(path)
    text = p.read_text()
    if marker not in text:
        p.write_text(text.rstrip() + "\n\n" + content.strip() + "\n")


meal = "src/Dig.Application/Agents/ResidentFoodMealUseCases.cs"
replace_required(meal, "using Dig.Domain.Inventory;\n", "using Dig.Domain.Inventory;\nusing Dig.Domain.World;\n")
replace_required(
    meal,
    "namespace Dig.Application.Agents\n{\n    public static class ResidentFoodMealErrors",
    """namespace Dig.Application.Agents
{
    public interface IResidentStandingSupportQuery
    {
        bool HasFullStandingSupport(CellId cell);
    }

    public static class ResidentFoodMealErrors""")
replace_required(
    meal,
    """        public static readonly DomainError UnsupportedFood = new DomainError(
            "resident.food_meal.unsupported_food",
            "The carried item is not supported food.");
""",
    """        public static readonly DomainError UnsupportedFood = new DomainError(
            "resident.food_meal.unsupported_food",
            "The carried item is not supported food.");

        public static readonly DomainError UnsupportedStandingPosition = new DomainError(
            "resident.food_meal.unsupported_standing_position",
            "The resident must stand on a fully supported flat cell to eat.");
""")
replace_required(
    meal,
    """        private readonly IAgentRepository _agents;
        private readonly IInventoryRepository _inventory;
        private readonly IEventSink _events;
""",
    """        private readonly IAgentRepository _agents;
        private readonly IInventoryRepository _inventory;
        private readonly IResidentStandingSupportQuery _standingSupport;
        private readonly IEventSink _events;
""")
replace_required(
    meal,
    """        public StartResidentFoodMealHandler(
            IAgentRepository agents,
            IInventoryRepository inventory,
            IEventSink events)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }
""",
    """        public StartResidentFoodMealHandler(
            IAgentRepository agents,
            IInventoryRepository inventory,
            IResidentStandingSupportQuery standingSupport,
            IEventSink events)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _standingSupport = standingSupport
                ?? throw new ArgumentNullException(nameof(standingSupport));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }
""")
replace_required(
    meal,
    """            if (agent.HasActiveFoodMeal)
            {
                return Result.Failure(FoodMealErrors.AlreadyActive);
            }
""",
    """            if (!_standingSupport.HasFullStandingSupport(agent.Position))
            {
                return Result.Failure(
                    ResidentFoodMealErrors.UnsupportedStandingPosition);
            }

            if (agent.HasActiveFoodMeal)
            {
                return Result.Failure(FoodMealErrors.AlreadyActive);
            }
""")

Path("unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.SupportedActionPositions.cs").write_text(r"""using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private static CellId[] GetSameHeightActionCandidates(CellId target)
    {
        List<CellId> candidates = new List<CellId>
        {
            new CellId(target.X - 1, target.Y, target.Z),
            new CellId(target.X + 1, target.Y, target.Z),
        };
        if (target.Z > CellId.MinimumDepth)
        {
            candidates.Add(new CellId(target.X, target.Y, target.Z - 1));
        }

        if (target.Z < CellId.MaximumDepth)
        {
            candidates.Add(new CellId(target.X, target.Y, target.Z + 1));
        }

        return candidates.Distinct().OrderBy(value => value).ToArray();
    }

    private bool IsSupportedStationaryActionPath(
        NavigationSnapshot navigation,
        NavigationPath path)
    {
        if (path.Cells.Any(cell => !HasFullStandingSupport(cell)))
        {
            return false;
        }

        for (int index = 0; index + 1 < path.Cells.Count; index++)
        {
            CellId from = path.Cells[index];
            CellId to = path.Cells[index + 1];
            bool supported = navigation.GetTransitions(from).Any(
                transition => transition.Target == to
                    && (transition.TraversalKind == TunnelTraversalKind.SupportedWalk
                        || transition.TraversalKind == TunnelTraversalKind.DepthTraverse));
            if (!supported)
            {
                return false;
            }
        }

        return true;
    }

    internal bool HasFullStandingSupport(CellId cell)
    {
        CellId support = new CellId(cell.X, cell.Y + 1, cell.Z);
        return _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Any(value => value.X == support.X
                && value.Y == support.Y
                && value.Z == support.Z
                && value.HasFullActorSupport);
    }

    internal Result InterruptUnsupportedStationaryActions(long tick)
    {
        if (_productionAgents == null)
        {
            return Result.Success();
        }

        foreach (AgentState resident in _productionAgents.GetAll()
            .Where(value => value.HasActiveFoodMeal)
            .OrderBy(value => value.Id))
        {
            if (HasFullStandingSupport(resident.Position))
            {
                continue;
            }

            Result interrupted = resident.InterruptFoodMeal(
                "unsupported_standing_position",
                tick);
            if (interrupted.IsFailure)
            {
                return interrupted;
            }

            _productionAgents.Save(resident);
            _journal.Append(resident.DequeueUncommittedEvents());
        }

        return Result.Success();
    }
}

internal sealed class DigTerrainResidentStandingSupportQuery
    : IResidentStandingSupportQuery
{
    private readonly DigTerrainWorkSession _session;

    internal DigTerrainResidentStandingSupportQuery(DigTerrainWorkSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public bool HasFullStandingSupport(CellId cell) =>
        _session.HasFullStandingSupport(cell);
}
}
""")

mushnav = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.MushroomNavigation.cs"
replace_required(
    mushnav,
    """        if (!path.Succeeded || path.Path == null)
        {
            return true;
        }
""",
    """        if (!path.Succeeded
            || path.Path == null
            || !HasFullStandingSupport(definition.WorkPosition)
            || !IsSupportedStationaryActionPath(navigation, path.Path))
        {
            return true;
        }
""")
replace_required(
    mushnav,
    """        CellId[] candidates =
        {
            new CellId(target.X - 1, target.Y, target.Z),
            new CellId(target.X + 1, target.Y, target.Z),
            new CellId(target.X, target.Y - 1, target.Z),
            new CellId(target.X, target.Y + 1, target.Z),
        };
""",
    """        CellId[] candidates = GetSameHeightActionCandidates(target);
""")
replace_required(
    mushnav,
    """        foreach (CellId candidate in candidates
            .Where(navigation.IsWalkable)
            .Distinct()
            .OrderBy(value => value))
""",
    """        foreach (CellId candidate in candidates
            .Where(navigation.IsWalkable)
            .Where(HasFullStandingSupport)
            .Distinct()
            .OrderBy(value => value))
""")
replace_required(
    mushnav,
    """            if (!path.Succeeded || path.Path == null)
            {
                continue;
            }
""",
    """            if (!path.Succeeded
                || path.Path == null
                || !IsSupportedStationaryActionPath(navigation, path.Path))
            {
                continue;
            }
""")

mushrooms = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Mushrooms.cs"
replace_required(
    mushrooms,
    """            if (!atWork)
            {
                continue;
            }

            Result result = AdvanceMushroomJob(job, definition, tick);
""",
    """            if (!atWork)
            {
                continue;
            }

            if (!HasFullStandingSupport(definition.WorkPosition))
            {
                Result cancelled = _cancelMushroomChop!.Handle(
                    new CancelMushroomChopCommand(
                        job.Id,
                        "mushroom_work_position_unsupported",
                        tick));
                if (cancelled.IsFailure)
                {
                    return cancelled;
                }

                continue;
            }

            Result result = AdvanceMushroomJob(job, definition, tick);
""")

replace_required(
    "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldItemPickupSession.cs",
    """            _startResidentFoodMeal = new StartResidentFoodMealHandler(
                _productionAgents,
                _buildingInventoryRepository,
                journal);
""",
    """            _startResidentFoodMeal = new StartResidentFoodMealHandler(
                _productionAgents,
                _buildingInventoryRepository,
                new DigTerrainResidentStandingSupportQuery(this),
                journal);
""")
replace_required(
    "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigResidentInventory.Consumables.cs",
    """                return new StartResidentFoodMealHandler(
                    _productionAgents,
                    repository,
                    _worldSession.Journal).Handle(
""",
    """                return new StartResidentFoodMealHandler(
                    _productionAgents,
                    repository,
                    new DigTerrainResidentStandingSupportQuery(this),
                    _worldSession.Journal).Handle(
""")

loop = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentSimulationDriverBase.Loop.cs"
replace_required(
    loop,
    """            if (result.IsSuccess)
            {
                IReadOnlyDictionary<string, CellId> movement =
                    TerrainSession.PlanMovement(before, nextTick);
""",
    """            if (result.IsSuccess)
            {
                result = TerrainSession.InterruptUnsupportedStationaryActions(nextTick);
            }

            if (result.IsSuccess)
            {
                IReadOnlyDictionary<string, CellId> movement =
                    TerrainSession.PlanMovement(before, nextTick);
""")
