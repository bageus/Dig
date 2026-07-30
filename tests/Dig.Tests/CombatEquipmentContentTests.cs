using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatEquipmentContentTests
{
    [Fact]
    public void Club_is_a_stable_non_stackable_weapon_item()
    {
        ItemDefinition club = Assert.Single(CombatEquipmentContent.CreateItems());

        Assert.Equal(new ItemId("weapon.club"), club.Id);
        Assert.Equal("Club", club.DisplayName);
        Assert.Equal(1, club.MaximumStackSize);
        Assert.True(club.IsTool);
        Assert.True(club.HasCategory(
            ResidentInventoryExpansionContent.WeaponCategoryId));
        Assert.Null(club.InventoryExpansion);
        Assert.Single(club.Categories);
    }

    [Fact]
    public void Club_content_can_join_the_expansion_catalog_without_duplicate_ids()
    {
        ResidentInventoryExpansionContent expansions =
            new ResidentInventoryExpansionContent();
        ItemCatalog catalog = new ItemCatalog(
            expansions.Items.Concat(CombatEquipmentContent.CreateItems()));

        Assert.True(catalog.Contains(CombatEquipmentContent.ClubItemId));
        Assert.True(catalog.Contains(
            ResidentInventoryExpansionContent.SheathItemId));
        Assert.True(catalog.Contains(
            ResidentInventoryExpansionContent.WeaponHarnessItemId));
    }
}

}
