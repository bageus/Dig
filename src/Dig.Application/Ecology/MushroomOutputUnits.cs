using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;

namespace Dig.Application.Ecology
{

internal sealed class MushroomOutputUnits
{
    private MushroomOutputUnits(EntityId[] caps, EntityId[] legs)
    {
        Caps = caps;
        Legs = legs;
    }

    public EntityId[] Caps { get; }
    public EntityId[] Legs { get; }

    public static MushroomOutputUnits Create(EntityId seedId, MushroomDropProfile drops)
    {
        if (seedId.IsEmpty)
        {
            throw new ArgumentException("Mushroom output seed id cannot be empty.", nameof(seedId));
        }

        EntityId[] ids = new EntityId[drops.TotalCount];
        for (int index = 0; index < ids.Length; index++)
        {
            ids[index] = index == 0
                ? seedId
                : EntityId.Parse(CreateDerivedEntityId(seedId.ToString(), index));
        }

        EntityId[] caps = new EntityId[drops.CapCount];
        EntityId[] legs = new EntityId[drops.LegCount];
        Array.Copy(ids, 0, caps, 0, caps.Length);
        Array.Copy(ids, caps.Length, legs, 0, legs.Length);
        return new MushroomOutputUnits(caps, legs);
    }

    public Result Validate(
        InventoryState inventory,
        ItemId capItemId,
        ItemId legItemId)
    {
        if (!inventory.Catalog.Contains(capItemId)
            || (Legs.Length > 0 && !inventory.Catalog.Contains(legItemId)))
        {
            return Result.Failure(MushroomApplicationErrors.UnknownDropItem);
        }

        foreach (EntityId id in EnumerateAll())
        {
            if (inventory.GetStack(id) is not null)
            {
                return Result.Failure(InventoryErrors.StackAlreadyExists);
            }
        }

        return Result.Success();
    }

    public IEnumerable<EntityId> EnumerateAll()
    {
        foreach (EntityId cap in Caps)
        {
            yield return cap;
        }

        foreach (EntityId leg in Legs)
        {
            yield return leg;
        }
    }

    private static string CreateDerivedEntityId(string seed, int index)
    {
        const int suffixLength = 8;
        string prefix = seed.Substring(0, seed.Length - suffixLength);
        uint seedSuffix = Convert.ToUInt32(seed.Substring(seed.Length - suffixLength), 16);
        uint derivedSuffix = checked(seedSuffix + (uint)index);
        return prefix + derivedSuffix.ToString("x8");
    }
}

}
