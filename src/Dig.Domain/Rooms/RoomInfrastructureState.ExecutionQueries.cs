using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Rooms
{

public sealed partial class RoomInfrastructureState
{
    public RoomInfrastructureProjectSnapshot? GetByActiveJob(EntityId jobId)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        return _rooms.Values
            .Select(room => room.CreateSnapshot())
            .Where(room => room.ActiveJobIds.Contains(jobId))
            .OrderBy(room => room.RoomInfrastructureId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public RoomMaterialUnitId? GetNextMaterialUnit(EntityId roomInfrastructureId)
    {
        RoomInfrastructureProjectSnapshot? room = Get(roomInfrastructureId);
        if (room == null)
        {
            return null;
        }

        foreach (RoomMaterialRequirement requirement in
            RoomUpgradeCostCatalog.Get(room.TemplateKind))
        {
            RoomMaterialLedgerSnapshot ledger = room.Materials.Single(
                value => value.ItemId == requirement.ItemId);
            if (ledger.Consumed < ledger.Required)
            {
                return new RoomMaterialUnitId(
                    requirement.ItemId,
                    ledger.Consumed + 1);
            }
        }

        return null;
    }
}

}
