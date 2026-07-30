using System.Collections.Generic;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public static class CombatEquipmentContent
{
    public static readonly ItemId ClubItemId = new ItemId("weapon.club");

    public static IReadOnlyList<ItemDefinition> CreateItems()
    {
        return new[]
        {
            new ItemDefinition(
                ClubItemId,
                "Club",
                maximumStackSize: 1,
                isTool: true,
                new[]
                {
                    ResidentInventoryExpansionContent.WeaponCategoryId,
                }),
        };
    }
}

}
