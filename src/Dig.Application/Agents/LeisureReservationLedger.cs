using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Agents
{

public sealed class LeisureReservationLedger
{
    private readonly Dictionary<EntityId, EntityId> _participants =
        new Dictionary<EntityId, EntityId>();
    private readonly Dictionary<CellId, EntityId> _meetingCells =
        new Dictionary<CellId, EntityId>();

    public bool TryReservePair(EntityId first, EntityId second, CellId meetingCell)
    {
        if (first.IsEmpty || second.IsEmpty || first == second)
        {
            throw new ArgumentException("A social reservation requires two residents.");
        }

        if (_participants.ContainsKey(first)
            || _participants.ContainsKey(second)
            || _meetingCells.ContainsKey(meetingCell))
        {
            return false;
        }

        _participants.Add(first, second);
        _participants.Add(second, first);
        _meetingCells.Add(meetingCell, first);
        return true;
    }

    public EntityId? GetPartner(EntityId residentId)
    {
        return _participants.TryGetValue(residentId, out EntityId partner)
            ? partner
            : null;
    }

    public void Release(EntityId residentId)
    {
        if (!_participants.TryGetValue(residentId, out EntityId partner)) return;
        _participants.Remove(residentId);
        _participants.Remove(partner);
        CellId? cell = null;
        foreach (KeyValuePair<CellId, EntityId> entry in _meetingCells)
        {
            if (entry.Value == residentId || entry.Value == partner)
            {
                cell = entry.Key;
                break;
            }
        }

        if (cell.HasValue) _meetingCells.Remove(cell.Value);
    }

    public void Clear()
    {
        _participants.Clear();
        _meetingCells.Clear();
    }
}

}
