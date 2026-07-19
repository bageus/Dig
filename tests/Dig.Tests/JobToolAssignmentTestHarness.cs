using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Tests
{

public sealed partial class JobToolAssignmentTests
{
    private static Harness CreateHarness(
        JobToolSwitchMode switchMode,
        bool includeSecondAgent = false,
        IJobToolPreparationService? preparationService = null)
    {
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        Require(jobs.Get().Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(5, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default)));
        Require(jobs.Get().MakeAvailable(JobId, tick: 0));
        jobs.Save(jobs.Get());

        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(PickaxeItemId, "Pickaxe", 1, isTool: true),
            new ItemDefinition(HammerItemId, "Hammer", 1, isTool: true),
        }));
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobToolSwitchPolicy policy = new InMemoryJobToolSwitchPolicy(switchMode);
        EquipmentRates rates = new EquipmentRates(new[]
        {
            new EquipmentProfile(
                PickaxeItemId,
                EquipmentAppearanceKind.Pickaxe,
                EquipmentWorkKind.Mining,
                workIntervalTicks: 1),
            new EquipmentProfile(
                HammerItemId,
                EquipmentAppearanceKind.Hammer,
                EquipmentWorkKind.Construction,
                workIntervalTicks: 1),
        });
        IJobCandidateProvider provider = new InventoryAwareJobCandidateProvider(
            new FixedCandidateProvider(includeSecondAgent),
            inventoryRepository,
            rates);
        IJobToolPreparationService toolPreparation = preparationService
            ?? new InventoryJobToolPreparationService(inventoryRepository, journal);
        AssignAvailableJobsHandler handler = new AssignAvailableJobsHandler(
            jobs,
            provider,
            journal,
            journal,
            toolPreparation,
            policy);
        return new Harness(jobs, inventory, journal, handler);
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new System.InvalidOperationException(result.Error!.ToString());
        }
    }

    private sealed class FixedCandidateProvider : IJobCandidateProvider
    {
        private readonly bool _includeSecondAgent;

        public FixedCandidateProvider(bool includeSecondAgent)
        {
            _includeSecondAgent = includeSecondAgent;
        }

        public System.Collections.Generic.IReadOnlyCollection<JobCandidate> GetCandidates(
            JobSnapshot job,
            long tick)
        {
            AgentSnapshot first = CreateAgent(FirstAgentId, "First");
            AgentSnapshot second = CreateAgent(SecondAgentId, "Second");
            return _includeSecondAgent
                ? new[]
                {
                    new JobCandidate(first, null, 0, 0, 0),
                    new JobCandidate(second, null, 0, 0, 0),
                }
                : new[] { new JobCandidate(first, null, 0, 0, 0) };
        }

        private static AgentSnapshot CreateAgent(EntityId id, string name)
        {
            return AgentSnapshot.Restore(
                id,
                name,
                new CellId(1, 1),
                isAlive: true,
                ScheduleActivity.Work,
                NeedsSnapshot.CreateDefault(),
                intent: null,
                action: null,
                version: 1);
        }
    }

    private sealed class FailingPreparationService : IJobToolPreparationService
    {
        public Result Prepare(EntityId agentId, EntityId toolStackId, long tick)
        {
            return Result.Failure(InventoryErrors.ToolSwitchUnsafe);
        }
    }

    private sealed class Harness
    {
        public Harness(
            InMemoryJobRepository jobs,
            InventoryState inventory,
            InMemoryExecutionJournal journal,
            AssignAvailableJobsHandler handler)
        {
            Jobs = jobs;
            Inventory = inventory;
            Journal = journal;
            Handler = handler;
        }

        public InMemoryJobRepository Jobs { get; }
        public InventoryState Inventory { get; }
        public InMemoryExecutionJournal Journal { get; }
        public AssignAvailableJobsHandler Handler { get; }

        public Result Assign(long tick)
        {
            return Handler.Handle(new AssignAvailableJobsCommand(tick));
        }
    }
}

}