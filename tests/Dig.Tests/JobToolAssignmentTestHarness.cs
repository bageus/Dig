using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Infrastructure.InMemory;

namespace Dig.Tests
{

public sealed partial class JobToolAssignmentTests
{
    private sealed class JobToolHarness
    {
        public JobToolHarness(
            InMemoryJobRepository jobs,
            InMemoryInventoryRepository inventory,
            InMemoryJobCandidateProvider candidates,
            InMemoryExecutionJournal journal)
        {
            Jobs = jobs;
            Inventory = inventory;
            Candidates = candidates;
            Journal = journal;
        }

        public InMemoryJobRepository Jobs { get; }
        public InMemoryInventoryRepository Inventory { get; }
        public InMemoryJobCandidateProvider Candidates { get; }
        public InMemoryExecutionJournal Journal { get; }

        public JobAssignmentReport Assign(
            JobToolPreparationMode mode,
            bool configurePreparation)
        {
            InventoryAwareJobCandidateProvider provider =
                new InventoryAwareJobCandidateProvider(
                    Candidates,
                    Inventory,
                    CreateRates());
            IJobToolPreparationService? preparation = configurePreparation
                ? new InventoryJobToolPreparationService(Inventory, Journal)
                : null;
            return new AssignAvailableJobsHandler(
                Jobs,
                provider,
                Journal,
                preparation).Handle(new AssignAvailableJobsCommand(
                    tick: 2,
                    toolPreparationMode: mode));
        }
    }
}

}