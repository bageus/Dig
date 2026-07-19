using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private AssignAvailableJobsHandler CreateHaulingAssignment(
            InMemoryExecutionJournal journal)
        {
            IJobCandidateProvider candidates =
                new InventoryTravelCostJobCandidateProvider(
                    _haulingCandidates!,
                    _inventoryRepository);
            HaulingResidentSlotClaimService slotClaims =
                new HaulingResidentSlotClaimService(
                    _inventoryRepository,
                    journal);
            return new AssignAvailableJobsHandler(
                _jobRepository,
                candidates,
                journal,
                haulingResidentSlotClaims: slotClaims);
        }
    }
}
