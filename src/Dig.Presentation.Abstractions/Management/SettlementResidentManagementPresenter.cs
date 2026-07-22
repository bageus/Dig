using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;

namespace Dig.Presentation.Management
{

public sealed class SettlementResidentManagementPresenter
{
    public SettlementResidentManagementViewModel Present(
        ResidentRosterViewModel roster,
        SocietySnapshot society,
        IReadOnlyDictionary<string, ResidentInventoryLayoutViewModel> inventories,
        long currentTick,
        long ticksPerAgeUnit)
    {
        if (roster is null)
        {
            throw new ArgumentNullException(nameof(roster));
        }

        if (society is null)
        {
            throw new ArgumentNullException(nameof(society));
        }

        if (inventories is null)
        {
            throw new ArgumentNullException(nameof(inventories));
        }

        if (currentTick < 0 || ticksPerAgeUnit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentTick));
        }

        Dictionary<EntityId, ResidentSocietySnapshot> societyById = society.Residents
            .ToDictionary(value => value.Id);
        Dictionary<EntityId, string> names = society.Residents
            .ToDictionary(value => value.Id, value => value.Name);
        SettlementResidentManagementRowViewModel[] rows = roster.Rows.Select(resident =>
        {
            if (!inventories.TryGetValue(
                resident.Id,
                out ResidentInventoryLayoutViewModel inventory))
            {
                throw new ArgumentException(
                    $"Inventory for resident '{resident.Id}' is missing.",
                    nameof(inventories));
            }

            EntityId id = EntityId.Parse(resident.Id);
            societyById.TryGetValue(id, out ResidentSocietySnapshot? social);
            long age = social == null
                ? 0
                : Math.Max(0, currentTick - social.BirthTick) / ticksPerAgeUnit;
            string[] children = society.Residents
                .Where(value => value.MotherId == id || value.FatherId == id)
                .Select(value => value.Name)
                .ToArray();
            return new SettlementResidentManagementRowViewModel(
                resident,
                age,
                ResolveName(social?.PartnerId, names),
                ResolveName(social?.MotherId, names),
                ResolveName(social?.FatherId, names),
                children,
                inventory);
        }).ToArray();
        return new SettlementResidentManagementViewModel(society.Version, rows);
    }

    private static string ResolveName(
        EntityId? id,
        IReadOnlyDictionary<EntityId, string> names)
    {
        return id.HasValue && names.TryGetValue(id.Value, out string value)
            ? value
            : "-";
    }
}

}
