using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Agents
{

public sealed class TunnelTrafficCoordinator
{
    private readonly List<TunnelTrafficTransition> _committed =
        new List<TunnelTrafficTransition>();
    private long _tick = -1;

    public void BeginTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (_tick == tick)
        {
            return;
        }

        _tick = tick;
        _committed.Clear();
    }

    public bool CanMove(
        EntityId residentId,
        CellId from,
        CellId to,
        long tick)
    {
        ValidateResident(residentId);
        BeginTick(tick);
        if (from == to)
        {
            return true;
        }

        for (int index = 0; index < _committed.Count; index++)
        {
            TunnelTrafficTransition transition = _committed[index];
            if (transition.ResidentId != residentId
                && transition.From == to
                && transition.To == from)
            {
                return false;
            }
        }

        return true;
    }

    public void RecordMove(
        EntityId residentId,
        CellId from,
        CellId to,
        long tick)
    {
        ValidateResident(residentId);
        BeginTick(tick);
        if (from == to)
        {
            return;
        }

        if (!CanMove(residentId, from, to, tick))
        {
            throw new InvalidOperationException(
                "Residents cannot exchange tunnel cells in the same simulation tick.");
        }

        _committed.Add(new TunnelTrafficTransition(residentId, from, to));
    }

    private static void ValidateResident(EntityId residentId)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }
    }

    private readonly struct TunnelTrafficTransition
    {
        internal TunnelTrafficTransition(EntityId residentId, CellId from, CellId to)
        {
            ResidentId = residentId;
            From = from;
            To = to;
        }

        internal EntityId ResidentId { get; }
        internal CellId From { get; }
        internal CellId To { get; }
    }
}

}
