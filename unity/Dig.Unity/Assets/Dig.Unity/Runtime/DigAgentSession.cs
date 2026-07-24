using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Runtime;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.Runtime;
using Dig.Domain.Society;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigAgentSession
    {
        private const int DemoResidentCount = 4;
        private const ulong DemoIdentitySeed = 0xD1661EUL;
        private readonly AgentAutonomySystem _autonomy;
        private readonly MoveAgentCommandHandler _movementHandler;
        private readonly AgentPresenter _presenter;
        private readonly InMemoryAgentRepository _repository;
        private readonly SimulationState _simulationState;
        private readonly WorldCellViewModel[] _walkableCells;
        private readonly Dictionary<EntityId, int> _routeIndices;
        private readonly Dictionary<EntityId, ResidentSex> _residentSexes;
        private readonly SocietyState _society;
        private readonly IAgentSkillGrantService _skillGrants;
        private long _tick;

        private DigAgentSession(
            AgentAutonomySystem autonomy,
            MoveAgentCommandHandler movementHandler,
            AgentPresenter presenter,
            InMemoryAgentRepository repository,
            SimulationState simulationState,
            WorldCellViewModel[] walkableCells,
            Dictionary<EntityId, int> routeIndices,
            Dictionary<EntityId, ResidentSex> residentSexes,
            SocietyState society,
            IAgentSkillGrantService skillGrants)
        {
            _autonomy = autonomy;
            _movementHandler = movementHandler;
            _presenter = presenter;
            _repository = repository;
            _simulationState = simulationState;
            _walkableCells = walkableCells;
            _routeIndices = routeIndices;
            _residentSexes = residentSexes;
            _society = society ?? throw new ArgumentNullException(nameof(society));
            _skillGrants = skillGrants
                ?? throw new ArgumentNullException(nameof(skillGrants));
        }

        public long Tick => _tick;

        internal IAgentSkillGrantService SkillGrants => _skillGrants;

        public static DigAgentSession CreateDemo(
            WorldViewModel world,
            TunnelNavigationVolume tunnelVolume,
            InMemoryExecutionJournal journal)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (tunnelVolume == null)
            {
                throw new ArgumentNullException(nameof(tunnelVolume));
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
            Dictionary<EntityId, ResidentSex> residentSexes =
                new Dictionary<EntityId, ResidentSex>();
            SocietyState society = CreateDemoSociety();
            AddDemoAgents(
                repository,
                routeIndices,
                residentSexes,
                society,
                walkable,
                tunnelVolume);
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
            DigAgentSession session = new DigAgentSession(
                autonomy,
                new MoveAgentCommandHandler(repository, journal),
                new AgentPresenter(new GetAgentSnapshotsQueryHandler(repository)),
                repository,
                SimulationState.Create(
                    worldSeed: DemoIdentitySeed,
                    tickDuration: TimeSpan.FromMilliseconds(500)),
                walkable,
                routeIndices,
                residentSexes,
                society,
                new AgentSkillGrantService(repository, journal));
            session.InitializeCombat(journal);
            session.InitializeTunnelMovement(tunnelVolume, journal);
            return session;
        }

        public IReadOnlyList<AgentViewModel> LoadView()
        {
            return _presenter.Load(_tick);
        }

        internal int GetMaximumSkillLevel(AgentSkillId skillId)
        {
            return _repository.GetAll()
                .Where(agent => agent.IsAlive)
                .Select(agent => agent.CreateSnapshot(_tick).GetSkillLevel(skillId))
                .DefaultIfEmpty(0)
                .Max();
        }

        internal ResidentSex ResolveResidentSex(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            EntityId id = EntityId.Parse(residentId);
            return _residentSexes.TryGetValue(id, out ResidentSex sex)
                ? sex
                : ResidentSex.Male;
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
            BeginTunnelTrafficTick(_tick);
            movementTargets = ApplyMovementTargetFilter(movementTargets, _tick);
            _autonomy.Execute(new SimulationContext(_tick, _simulationState));
            IReadOnlyList<AgentState> agents = _repository.GetAll();
            for (int index = 0; index < agents.Count; index++)
            {
                AgentState agent = agents[index];
                if (!agent.IsAlive)
                {
                    CancelManualTunnelMovement(agent.Id.ToString());
                    continue;
                }

                if (TryAdvanceManualTunnelMovement(agent, out Result manualMovement))
                {
                    if (manualMovement.IsFailure)
                    {
                        CancelManualMovementWithWarning(agent.Id, manualMovement.Error!);
                    }

                    continue;
                }

                if (TryAdvanceSpatialWorkMovement(agent, out Result spatialMovement))
                {
                    if (spatialMovement.IsFailure)
                    {
                        CancelManualMovementWithWarning(agent.Id, spatialMovement.Error!);
                    }

                    continue;
                }

                CellId destination;
                if (!movementTargets.TryGetValue(agent.Id.ToString(), out destination))
                {
                    if (_tunnelVolume != null)
                    {
                        continue;
                    }

                    int routeIndex = SelectNextRouteIndex(agent);
                    WorldCellViewModel cell = _walkableCells[routeIndex];
                    destination = new CellId(cell.X, cell.Y, cell.Z);
                }

                Result result = MoveThroughTunnelTraffic(agent, destination);
                if (result.IsFailure)
                {
                    CancelManualMovementWithWarning(agent.Id, result.Error!);
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
            Dictionary<EntityId, ResidentSex> residentSexes,
            SocietyState society,
            IReadOnlyList<WorldCellViewModel> walkable,
            TunnelNavigationVolume tunnelVolume)
        {
            TunnelDemoLayout layout = tunnelVolume.DemoLayout
                ?? throw new InvalidOperationException("The tunnel demo layout is required.");
            IReadOnlyList<ResidentBirthPlan> identities = CreateDemoIdentities(
                layout,
                DemoResidentCount);
            for (int index = 0; index < identities.Count; index++)
            {
                ResidentBirthPlan identity = identities[index];
                int routeIndex = (index * walkable.Count) / identities.Count;
                AgentState agent = new AgentState(
                    identity.Id,
                    identity.Name,
                    new AgentNeedsSnapshot(
                        new NeedValue(7_800 - (index * 900)),
                        new NeedValue(6_600 + (index * 500)),
                        new NeedValue(7_000 - (index * 350)),
                        new NeedValue(10_000)),
                    DailySchedule.CreateBalanced(24),
                    skills: null,
                    traits: identity.Heritage.Traits,
                    initialPosition: new CellId(
                        identity.Position.X,
                        identity.Position.Y,
                        0));
                Result added = repository.Add(agent);
                if (added.IsFailure)
                {
                    throw new InvalidOperationException(added.Error!.ToString());
                }

                routeIndices.Add(agent.Id, routeIndex);
                residentSexes.Add(agent.Id, identity.Sex);
                RegisterDemoResident(society, identity);
            }
        }

        private static IReadOnlyList<ResidentBirthPlan> CreateDemoIdentities(
            TunnelDemoLayout layout,
            int count)
        {
            ResidentNameCatalog names = new ResidentNameCatalog(
                new[] { "Dora", "Fara", "Hela", "Iria", "Kara", "Mira", "Nora", "Runa" },
                new[] { "Borin", "Doran", "Einar", "Gimli", "Haldor", "Korin", "Orin", "Torin" });
            ResidentHeritage founderHeritage = new ResidentHeritage(7_500);
            ResidentInheritancePolicy inheritance = new ResidentInheritancePolicy(
                potentialVariance: 0);
            Dig.Domain.Society.ResidentIdentityGenerator generator =
                new Dig.Domain.Society.ResidentIdentityGenerator();
            HashSet<string> usedNames = new HashSet<string>(StringComparer.Ordinal);
            List<ResidentBirthPlan> result = new List<ResidentBirthPlan>(count);
            long sequence = 0;
            int surfaceWidth = layout.SurfaceMaxX - layout.SurfaceMinX + 1;
            for (int index = 0; index < count; index++)
            {
                int x = layout.SurfaceMinX
                    + (((index + 1) * surfaceWidth) / (count + 1));
                ResidentBirthPlan identity;
                do
                {
                    identity = generator.CreateBirthPlan(
                        DemoIdentitySeed,
                        sequence++,
                        names,
                        founderHeritage,
                        founderHeritage,
                        inheritance,
                        new CellId(x, layout.SurfaceY, 0));
                }
                while (!usedNames.Add(identity.Name));
                result.Add(identity);
            }

            return result;
        }
    }
}
