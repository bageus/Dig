using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private Result<JobSystem> BuildJobSystem(JobsSaveData data)
    {
        if (data is null || data.Jobs is null || data.Reservations is null)
        {
            throw new InvalidOperationException("Jobs save data is missing.");
        }

        List<JobSnapshot> jobs = new List<JobSnapshot>();
        foreach (JobSaveData savedJob in data.Jobs
            .OrderBy(item => item.Definition.JobId, StringComparer.Ordinal))
        {
            if (savedJob is null
                || savedJob.Definition is null
                || !Enum.IsDefined(typeof(JobStatus), savedJob.Status)
                || !Enum.IsDefined(typeof(JobStageKind), savedJob.Stage))
            {
                throw new InvalidOperationException("Saved job state is invalid.");
            }

            JobDefinition definition = _jobDefinitions.Decode(savedJob.Definition);
            EntityId? assignedAgentId = string.IsNullOrWhiteSpace(savedJob.AssignedAgentId)
                ? null
                : EntityId.Parse(savedJob.AssignedAgentId);
            JobBlockReason? reason = ParseReason(savedJob);
            jobs.Add(new JobSnapshot(
                definition,
                (JobStatus)savedJob.Status,
                (JobStageKind)savedJob.Stage,
                assignedAgentId,
                savedJob.RetryCount,
                savedJob.NextRetryTick,
                savedJob.Version,
                reason));
        }

        List<ReservationSnapshot> reservations = new List<ReservationSnapshot>();
        foreach (JobReservationSaveData saved in data.Reservations
            .OrderBy(item => item.JobId, StringComparer.Ordinal)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Value, StringComparer.Ordinal))
        {
            if (saved is null
                || !Enum.IsDefined(typeof(ReservationKind), saved.Kind)
                || saved.AcquiredTick < 0)
            {
                throw new InvalidOperationException("Saved reservation is invalid.");
            }

            reservations.Add(new ReservationSnapshot(
                ParseReservationKey((ReservationKind)saved.Kind, saved.Value),
                EntityId.Parse(saved.JobId),
                EntityId.Parse(saved.AgentId),
                saved.AcquiredTick));
        }

        return JobSystem.Restore(jobs, reservations);
    }

    private static JobBlockReason? ParseReason(JobSaveData data)
    {
        bool hasCode = !string.IsNullOrWhiteSpace(data.ReasonCode);
        bool hasMessage = !string.IsNullOrWhiteSpace(data.ReasonMessage);
        if (hasCode != hasMessage)
        {
            throw new InvalidOperationException("Saved job reason is incomplete.");
        }

        return hasCode
            ? new JobBlockReason(data.ReasonCode!, data.ReasonMessage!)
            : null;
    }

    private static ReservationKey ParseReservationKey(
        ReservationKind kind,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Saved reservation key is empty.");
        }

        return kind switch
        {
            ReservationKind.Job => ReservationKey.ForJob(EntityId.Parse(value)),
            ReservationKind.Agent => ReservationKey.ForAgent(EntityId.Parse(value)),
            ReservationKind.Item => ReservationKey.ForItem(EntityId.Parse(value)),
            ReservationKind.Tool => ReservationKey.ForTool(EntityId.Parse(value)),
            ReservationKind.Position => ReservationKey.ForPosition(ParseCell(value)),
            ReservationKind.Designation => ReservationKey.ForDesignation(ParseCell(value)),
            ReservationKind.Destination => ReservationKey.ForDestination(EntityId.Parse(value)),
            _ => throw new InvalidOperationException("Unsupported reservation kind."),
        };
    }

    private static CellId ParseCell(string value)
    {
        string[] parts = value.Split(',');
        if ((parts.Length != 2 && parts.Length != 3)
            || !int.TryParse(
                parts[0],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int x)
            || !int.TryParse(
                parts[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int y)
            || (parts.Length == 3
                && !int.TryParse(
                    parts[2],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out _)))
        {
            throw new InvalidOperationException("Saved reservation cell is invalid.");
        }

        int z = parts.Length == 3
            ? int.Parse(parts[2], CultureInfo.InvariantCulture)
            : CellId.MinimumDepth;
        return new CellId(x, y, z);
    }

}
}