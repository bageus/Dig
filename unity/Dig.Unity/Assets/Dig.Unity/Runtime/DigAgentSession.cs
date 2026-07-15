using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed class DigAgentSession
    {
        private readonly AgentAutonomySystem _autonomy;
        private readonly MoveAgentCommandHandler _movementHandler;
        private readonly AgentPresenter _presenter;
        private readonly InMemoryAgentRepository _repository;
        private readonly SimulationState _simulationState;
        private readonly WorldCellViewModel[] _walkableCells;
        private readonly Dictionary<EntityId, int> _routeIndices;
        private long _tick;

        private DigAgentSession(
            AgentAutonomySystem autonomy,
            MoveAgentCommandHandler movementHandler,
            AgentPresenter presenter,
            InMemoryAgentRepository repository,
            SimulationState simulationState,
            WorldCellViewModel[] walkableCells,
            Dictionary<EntityId, int> routeIndices)
        {
            _autonomy = autonomy;
            _movementHandler = movementHandler;
            _presenter = presenter;
            _repository = repository;
            _simulationState = simulationState;
            _walkableCells = walkableCells;
            _routeIndices = routeIndices;
        }

        public long Tick => _tick;

        public static DigAgentSession CreateDemo(
            WorldViewModel world,
            InMemoryExecutionJournal journal)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (journal == null)
            {
                throw new ArgumentNullException(nameof(journal));
            }

            WorldCellViewModel[] walkable = world.Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => !cell.IsSolid)
                .OrderBy(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .ToArray();
            if (walkable.Length < 8)
            {
                throw new InvalidOperationException(
                    "The resident demo requires at least eight walkable cells.");
            }

            InMemoryAgentRepository repository = new InMemoryAgentRepository();
            Dictionary<EntityId, int> routeIndices = new Dictionary<EntityId, int>();
            AddDemoAgents(repository, routeIndices, walkable);
            AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
            InMemoryAgentDecisionContextProvider contexts =
                new InMemoryAgentDecisionContextProvider(
                    AgentDecisionContext.AllAvailable());
            AgentAutonomySystem autonomy = new AgentAutonomySystem(
                repository,
                contexts,
                journal,
                new AgentDecisionSystem(),
                policy);
            return new DigAgentSession(
                autonomy,
                new MoveAgentCommandHandler(repository, journal),
                new AgentPresenter(new GetAgentSnapshotsQueryHandler(repository)),
                repository,
                SimulationState.Create(
                    worldSeed: 0xD1661EUL,
                    tickDuration: TimeSpan.FromMilliseconds(500)),
                walkable,
                routeIndices);
        }

        public IReadOnlyList<AgentViewModel> LoadView()
        {
            return _presenter.Load(_tick);
        }

        public Result MoveResident(string residentId, CellId destination)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            return _movementHandler.Handle(new MoveAgentCommand(
                EntityId.Parse(residentId),
                destination,
                _tick));
        }

        public Result Advance()
        {
            return Advance(new Dictionary<string, CellId>(StringComparer.Ordinal));
        }

        public Result Advance(IReadOnlyDictionary<string, CellId> movementTargets)
        {
            if (movementTargets == null)
            {
                throw new ArgumentNullException(nameof(movementTargets));
            }

            _tick = checked(_tick + 1);
            _autonomy.Execute(new SimulationContext(_tick, _simulationState));
            IReadOnlyList<AgentState> agents = _repository.GetAll();
            for (int index = 0; index < agents.Count; index++)
            {
                AgentState agent = agents[index];
                if (!agent.IsAlive)
                {
                    continue;
                }

                CellId destination;
                if (!movementTargets.TryGetValue(agent.Id.ToString(), out destination))
                {
                    int routeIndex = SelectNextRouteIndex(agent);
                    WorldCellViewModel cell = _walkableCells[routeIndex];
                    destination = new CellId(cell.X, cell.Y);
                }

                Result result = _movementHandler.Handle(new MoveAgentCommand(
                    agent.Id,
                    destination,
                    _tick));
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }

        private int SelectNextRouteIndex(AgentState agent)
        {
            AgentIntentKind intent = agent.LastDecision?.SelectedIntent
                ?? AgentIntentKind.Idle;
            int step = 1 + ((int)intent % 3);
            int next = (_routeIndices[agent.Id] + step) % _walkableCells.Length;
            _routeIndices[agent.Id] = next;
            return next;
        }

        private static void AddDemoAgents(
            InMemoryAgentRepository repository,
            Dictionary<EntityId, int> routeIndices,
            IReadOnlyList<WorldCellViewModel> walkable)
        {
            string[] ids =
            {
                "10000000000000000000000000000001",
                "10000000000000000000000000000002",
                "10000000000000000000000000000003",
                "10000000000000000000000000000004",
            };
            string[] names = { "Borin", "Dora", "Einar", "Fara" };
            for (int index = 0; index < names.Length; index++)
            {
                int routeIndex = (index * walkable.Count) / names.Length;
                WorldCellViewModel cell = walkable[routeIndex];
                AgentState agent = new AgentState(
                    EntityId.Parse(ids[index]),
                    names[index],
                    new AgentNeedsSnapshot(
                        new NeedValue(7_800 - (index * 900)),
                        new NeedValue(6_600 + (index * 500)),
                        new NeedValue(7_000 - (index * 350)),
                        new NeedValue(10_000)),
                    DailySchedule.CreateBalanced(24),
                    skills: null,
                    traits: null,
                    initialPosition: new CellId(cell.X, cell.Y));
                Result added = repository.Add(agent);
                if (added.IsFailure)
                {
                    throw new InvalidOperationException(added.Error!.ToString());
                }

                routeIndices.Add(agent.Id, routeIndex);
            }
        }
    }
}