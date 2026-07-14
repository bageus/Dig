using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public sealed class ReservationSnapshot
{
    public ReservationSnapshot(
        ReservationKey key,
        EntityId jobId,
        EntityId agentId,
        long acquiredTick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (acquiredTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acquiredTick));
        }

        Key = key;
        JobId = jobId;
        AgentId = agentId;
        AcquiredTick = acquiredTick;
    }

    public ReservationKey Key { get; }

    public EntityId JobId { get; }

    public EntityId AgentId { get; }

    public long AcquiredTick { get; }
}

public sealed class ReservationLedger
{
    private readonly Dictionary<ReservationKey, ReservationSnapshot> _reservations =
        new Dictionary<ReservationKey, ReservationSnapshot>();

    public Result ReserveAll(
        EntityId jobId,
        EntityId agentId,
        IEnumerable<ReservationKey> keys,
        long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ReservationKey[] orderedKeys = keys.Distinct().OrderBy(key => key).ToArray();
        if (orderedKeys.Length == 0)
        {
            throw new ArgumentException("At least one reservation key is required.", nameof(keys));
        }

        foreach (ReservationKey key in orderedKeys)
        {
            if (_reservations.TryGetValue(key, out ReservationSnapshot? existing)
                && existing.JobId != jobId)
            {
                return Result.Failure(
                    key.Kind == ReservationKind.Agent
                        ? JobErrors.AgentUnavailable
                        : JobErrors.ReservationConflict);
            }
        }

        foreach (ReservationKey key in orderedKeys)
        {
            if (!_reservations.ContainsKey(key))
            {
                _reservations.Add(
                    key,
                    new ReservationSnapshot(key, jobId, agentId, tick));
            }
        }

        return Result.Success();
    }

    public int ReleaseForJob(EntityId jobId)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        ReservationKey[] keys = _reservations
            .Where(pair => pair.Value.JobId == jobId)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (ReservationKey key in keys)
        {
            _reservations.Remove(key);
        }

        return keys.Length;
    }

    public bool IsReserved(ReservationKey key)
    {
        return _reservations.ContainsKey(key);
    }

    public ReservationSnapshot? Find(ReservationKey key)
    {
        return _reservations.TryGetValue(key, out ReservationSnapshot? reservation)
            ? reservation
            : null;
    }

    internal void Visit(IJobInspectionVisitor visitor)
    {
        foreach (ReservationSnapshot reservation in _reservations.Values)
        {
            visitor.VisitJobReservation(reservation);
        }
    }

    public IReadOnlyList<ReservationSnapshot> CreateSnapshot()
    {
        ReservationSnapshot[] snapshot = _reservations.Values
            .OrderBy(item => item.Key)
            .ToArray();
        return new ReadOnlyCollection<ReservationSnapshot>(snapshot);
    }
}
