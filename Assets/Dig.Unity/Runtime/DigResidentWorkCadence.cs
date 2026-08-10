using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private const int DefaultMiningIntervalTicks = 3;

        internal long ResolveTerrainAdvanceTick(string residentId, long tick)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick));
            }

            EntityId resident = EntityId.Parse(residentId);
            JobSnapshot? activeDigJob = _jobRepository.Get().GetAll().FirstOrDefault(job =>
                (job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                && job.AssignedAgentId == resident
                && job.Definition is DigJobDefinition);
            if (activeDigJob == null)
            {
                return tick;
            }

            int intervalTicks = ResolveMiningWorkInterval(
                residentId,
                DefaultMiningIntervalTicks);
            if (intervalTicks >= DefaultMiningIntervalTicks)
            {
                return tick;
            }

            long scaledTick = checked(tick * DefaultMiningIntervalTicks);
            return tick % intervalTicks == 0
                ? scaledTick
                : checked(scaledTick + 1);
        }
    }
}
