using System.Collections.Generic;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

public static class LivingMaterialContent
{
    public static IReadOnlyList<ItemDefinition> CreateItems()
    {
        ItemCategoryId category = ResidentInventoryExpansionContent.RawMaterialCategoryId;
        return new[]
        {
            new ItemDefinition(
                new ItemId("creature.hamster"),
                "Hamster",
                maximumStackSize: 1,
                isTool: false,
                new[] { category }),
            new ItemDefinition(
                new ItemId("creature.grub"),
                "Grub",
                maximumStackSize: 1,
                isTool: false,
                new[] { category }),
        };
    }
}

}
