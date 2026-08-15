using System.Collections.Generic;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public static class LivingMaterialContent
{
    public static readonly ItemId HamsterItemId = new ItemId("creature.hamster");
    public static readonly ItemId GrubItemId = new ItemId("creature.grub");

    public static IReadOnlyList<ItemDefinition> CreateItems()
    {
        ItemCategoryId category = ResidentInventoryExpansionContent.RawMaterialCategoryId;
        return new[]
        {
            new ItemDefinition(
                HamsterItemId,
                "Hamster",
                maximumStackSize: 1,
                isTool: false,
                new[] { category }),
            new ItemDefinition(
                GrubItemId,
                "Grub",
                maximumStackSize: 1,
                isTool: false,
                new[] { category }),
        };
    }
}

}
