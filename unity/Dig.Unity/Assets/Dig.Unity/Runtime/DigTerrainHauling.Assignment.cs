using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private AcquireHaulingItemHandler? _haulingAcquisition;

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
            _haulingAcquisition = new AcquireHaulingItemHandler(
                _inventoryRepository,
                _jobRepository,
                journal);
            return new AssignAvailableJobsHandler(
                _jobRepository,
                candidates,
                journal,
                haulingResidentSlotClaims: slotClaims);
        }

        private Result AdvanceHaulingTransitAtTarget(JobSnapshot job, long tick)
        {
            if (job.Status == JobStatus.Claimed)
            {
                return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
            }

            if (job.Stage == JobStageKind.AcquireItem)
            {
                return _haulingAcquisition!.Handle(new AcquireHaulingItemCommand(
                    job.Id,
                    _haulingIds!.NextSplitStackId(),
                    tick));
            }

            return job.Stage == JobStageKind.TravelToDestination
                ? _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick))
                : Result.Success();
        }
    }
}
