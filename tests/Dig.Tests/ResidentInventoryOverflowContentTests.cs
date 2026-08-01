using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentInventoryOverflowContentTests
{
    [Fact]
    public void Cargo_expansions_accept_weapon_and_shield_overflow()
    {
        ResidentInventoryExpansionContent content =
            new ResidentInventoryExpansionContent();
        var items = content.Items.ToDictionary(item => item.Id);
        ItemDefinition weapon = new ItemDefinition(
            new ItemId("weapon.test_sword"),
            "Sword",
            1,
            false,
            new[] { ResidentInventoryExpansionContent.WeaponCategoryId });
        ItemDefinition shield = new ItemDefinition(
            new ItemId("shield.test_round"),
            "Shield",
            1,
            false,
            new[] { ResidentInventoryExpansionContent.ShieldCategoryId });

        Assert.True(items[ResidentInventoryExpansionContent.BasketItemId]
            .InventoryExpansion!.Accepts(weapon));
        Assert.True(items[ResidentInventoryExpansionContent.BasketItemId]
            .InventoryExpansion!.Accepts(shield));
        Assert.True(items[ResidentInventoryExpansionContent.LargeBasketItemId]
            .InventoryExpansion!.Accepts(weapon));
        Assert.True(items[ResidentInventoryExpansionContent.LargeBasketItemId]
            .InventoryExpansion!.Accepts(shield));
    }
}

}
