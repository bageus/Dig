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
        private HaulingResidentSlotClaimService? _haulingSlotClaims;

        private AssignAvailableJobsHandler CreateHaulingAssignment(
            InMemoryExecutionJournal journal)
        {
            IJobCandidateProvider candidates =
                new InventoryTravelCostJobCandidateProvider(
                    _haulingCandidates!,
                    _inventoryRepository);
            _haulingSlotClaims = new HaulingResidentSlotClaimService(
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
                haulingResidentSlotClaims: _haulingSlotClaims);
        }

        private Result AdvanceHaulingTransitAtTarget(JobSnapshot job, long tick)
        {
            if (job.Status == JobStatus.Claimed
                || job.Stage == JobStageKind.TravelToTarget)
            {
                Result started = _advanceHandler.Handle(
                    new AdvanceJobCommand(job.Id, tick));
                if (started.IsFailure)
                {
                    return started;
                }

                JobSnapshot? refreshed = _jobRepository.Get().Get(job.Id);
                if (refreshed == null
                    || (refreshed.Status == job.Status
                        && refreshed.Stage == job.Stage))
                {
                    return Result.Success();
                }

                job = refreshed;
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
