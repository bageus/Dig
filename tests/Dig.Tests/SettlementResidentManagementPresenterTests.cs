using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Society;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Management;
using Xunit;

namespace Dig.Tests
{

public sealed class SettlementResidentManagementPresenterTests
{
    [Fact]
    public void Presenter_resolves_age_family_skills_and_inventory()
    {
        EntityId residentId = Id('a');
        EntityId partnerId = Id('b');
        EntityId motherId = Id('c');
        EntityId fatherId = Id('d');
        EntityId childId = Id('e');
        AgentSnapshot agent = new AgentSnapshot(
            residentId,
            "Dora",
            version: 4,
            isAlive: true,
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(7_000),
                new NeedValue(6_000),
                new NeedValue(10_000)),
            ScheduleActivity.Work,
            activeAction: null,
            playerOrder: null,
            lastActionSwitchTick: 0,
            lastDecision: null,
            new[]
            {
                new AgentSkillValue(AgentSkillCatalog.Stonework, 1_000),
                new AgentSkillValue(AgentSkillCatalog.UnarmedCombat, 2_000),
            },
            Array.Empty<AgentTraitId>());
        SocietySnapshot society = new SocietySnapshot(
            version: 7,
            new[]
            {
                Social(residentId, "Dora", ResidentSex.Female, partnerId, motherId, fatherId),
                Social(partnerId, "Borin", ResidentSex.Male),
                Social(motherId, "Mira", ResidentSex.Female),
                Social(fatherId, "Torin", ResidentSex.Male),
                Social(childId, "Runa", ResidentSex.Female, motherId: residentId),
            },
            Array.Empty<SocialBondSnapshot>());
        ResidentRosterViewModel roster = new ResidentRosterPresenter().Present(
            new[] { agent },
            society,
            Array.Empty<JobSnapshot>());
        ItemId boxId = new ItemId("building.box");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(boxId, "Box", 1, isTool: false),
        }));
        Assert.True(inventory.NormalizeResidentInventory(residentId, tick: 0).IsSuccess);
        ResidentInventoryLayoutViewModel layout =
            new ResidentInventoryLayoutPresenter(boxId).Present(inventory, residentId);

        SettlementResidentManagementViewModel result =
            new SettlementResidentManagementPresenter().Present(
                roster,
                society,
                new Dictionary<string, ResidentInventoryLayoutViewModel>
                {
                    [residentId.ToString()] = layout,
                },
                currentTick: 48,
                ticksPerAgeUnit: 24);

        SettlementResidentManagementRowViewModel row = Assert.Single(result.Rows);
        Assert.Equal(7, result.SocietyVersion);
        Assert.Equal(2, row.Age);
        Assert.Equal("Borin", row.Partner);
        Assert.Equal("Mira", row.Mother);
        Assert.Equal("Torin", row.Father);
        Assert.Equal("Runa", Assert.Single(row.Children));
        Assert.Equal(1_000, row.ProductionTotal);
        Assert.Equal(2_000, row.CombatTotal);
        Assert.Equal(ResidentSexIndicator.Female, row.Resident.Sex);
        Assert.Equal(residentId.ToString(), row.Inventory.ResidentId);
    }

    private static ResidentSocietySnapshot Social(
        EntityId id,
        string name,
        ResidentSex sex,
        EntityId? partnerId = null,
        EntityId? motherId = null,
        EntityId? fatherId = null)
    {
        return new ResidentSocietySnapshot(
            id,
            name,
            sex,
            birthTick: 0,
            ResidentLifeStage.Adult,
            isAlive: true,
            motherId,
            fatherId,
            partnerId,
            pregnancy: null,
            new CellId(1, 1),
            deathCause: null,
            deathTick: null,
            new ResidentHeritage(7_500));
    }

    private static EntityId Id(char prefix)
    {
        return EntityId.Parse(prefix + new string('0', 31));
    }
}

}
