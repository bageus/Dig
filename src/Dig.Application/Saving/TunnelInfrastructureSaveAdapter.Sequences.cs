using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Jobs;

namespace Dig.Application.Saving
{

public static partial class TunnelInfrastructureSaveAdapter
{
    public static ulong ResolveLegacyNextSequence(JobsSaveData? jobs)
    {
        ulong next = 1UL;
        IEnumerable<JobSaveData> savedJobs = jobs == null
            ? Array.Empty<JobSaveData>()
            : jobs.Jobs;
        foreach (JobSaveData job in savedJobs)
        {
            JobDefinitionSaveData? definition = job?.Definition;
            if (definition == null
                || !string.Equals(
                    definition.TypeId,
                    new TunnelAutomaticWorkJobSaveCodec().TypeId,
                    StringComparison.Ordinal)
                || !TryParseAutomaticJobSequence(
                    definition.JobId,
                    out ulong sequence))
            {
                continue;
            }

            next = Math.Max(next, checked(sequence + 1UL));
        }

        return next;
    }

    private static bool IsNextSequenceValid(ulong next, JobSystem jobs)
    {
        if (next == 0)
        {
            return false;
        }

        return !jobs.GetAll().Any(job =>
            job.Definition is TunnelAutomaticWorkJobDefinition
            && TryParseAutomaticJobSequence(job.Id.ToString(), out ulong sequence)
            && sequence >= next);
    }


    private static bool TryParseAutomaticJobSequence(
        string? value,
        out ulong sequence)
    {
        sequence = 0;
        return value != null
            && value.Length == 32
            && value[0] == 'a'
            && ulong.TryParse(
                value.Substring(1),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out sequence)
            && sequence > 0;
    }


}

}
