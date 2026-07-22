using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Management;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const long ManagementTicksPerAgeUnit = 24 * 365;
    private readonly SettlementResidentManagementPresenter _residentManagementPresenter =
        new SettlementResidentManagementPresenter();

    private void RefreshDwarfManagement()
    {
        ResidentRosterViewModel roster = _simulation!.LoadResidentRoster(
            selectedResidentId: null);
        Dictionary<string, ResidentInventoryLayoutViewModel> inventories =
            roster.Rows.ToDictionary(
                row => row.Id,
                row => _terrainSession!.LoadResidentInventoryLayout(row.Id),
                StringComparer.Ordinal);
        SettlementResidentManagementViewModel model =
            _residentManagementPresenter.Present(
                roster,
                _simulation.LoadSocietySnapshot(),
                inventories,
                _simulation.CurrentSocietyTick,
                ManagementTicksPerAgeUnit);
        string signature = "dwarfs:" + _dwarfManagementTab + ":"
            + model.SocietyVersion + ":"
            + string.Join("|", model.Rows.Select(row =>
                row.Resident.Id + ":" + row.Resident.Version + ":"
                + row.Inventory.InventoryVersion));
        if (string.Equals(signature, _managementSignature, StringComparison.Ordinal))
        {
            return;
        }

        _managementSignature = signature;
        string[] tabs = { "Standard", "Production", "Fight", "Family", "Inventory" };
        BeginManagementOverlay(
            "Dwarfs",
            tabs,
            (int)_dwarfManagementTab,
            SelectDwarfManagementTab);
        switch (_dwarfManagementTab)
        {
            case DwarfManagementTab.Standard:
                BuildDwarfStandardTable(model);
                break;
            case DwarfManagementTab.Production:
                BuildDwarfProductionTable(model);
                break;
            case DwarfManagementTab.Fight:
                BuildDwarfFightTable(model);
                break;
            case DwarfManagementTab.Family:
                BuildDwarfFamilyTable(model);
                break;
            case DwarfManagementTab.Inventory:
                BuildDwarfInventoryTable(model);
                break;
        }
    }

    private void SelectDwarfManagementTab(int index)
    {
        _dwarfManagementTab = (DwarfManagementTab)index;
        _managementSignature = string.Empty;
        RefreshDwarfManagement();
    }

    private void BuildDwarfStandardTable(SettlementResidentManagementViewModel model)
    {
        ManagementColumn[] columns =
        {
            Column("management.name", 140f),
            Column("management.sex", 72f),
            Column("resident.need.health", 116f),
            Column("resident.need.alertness.vigor", 116f),
            Column("management.hunger", 116f),
            Column("resident.need.mood", 116f),
            Column("management.age", 70f),
            Column("management.schedule", 150f),
        };
        BuildManagementHeader(columns);
        foreach (SettlementResidentManagementRowViewModel row in model.Rows)
        {
            RectTransform visual = CreateManagementRow(row.Resident.Id, 34f);
            CreateManagementTextCell(visual, row.Resident.Name, columns[0].Width);
            CreateManagementTextCell(
                visual,
                SexLabel(row.Resident.Sex),
                columns[1].Width,
                TextAnchor.MiddleCenter);
            CreateNeedBar(visual, row.Resident.Health, columns[2].Width);
            CreateNeedBar(visual, row.Resident.Alertness, columns[3].Width);
            int hunger = 100 - row.Resident.Nutrition.Percent;
            CreateManagementBarCell(
                visual,
                hunger,
                100,
                columns[4].Width,
                NeedColor(row.Resident.Nutrition.Band));
            CreateNeedBar(visual, row.Resident.Mood, columns[5].Width);
            CreateManagementTextCell(
                visual,
                row.Age.ToString(),
                columns[6].Width,
                TextAnchor.MiddleCenter);
            CreateManagementTextCell(
                visual,
                ScheduleManagementLabel(row.Resident.ScheduledActivity),
                columns[7].Width,
                TextAnchor.MiddleCenter);
        }
    }

    private void BuildDwarfProductionTable(SettlementResidentManagementViewModel model)
    {
        AgentSkillId[] skills =
        {
            AgentSkillCatalog.Stonework,
            AgentSkillCatalog.Woodworking,
            AgentSkillCatalog.Cooking,
            AgentSkillCatalog.Metallurgy,
            AgentSkillCatalog.Logistics,
            AgentSkillCatalog.Alchemy,
            AgentSkillCatalog.Service,
        };
        string[] labels =
        {
            "resident.skill.stonework",
            "resident.skill.woodworking",
            "resident.skill.cooking",
            "resident.skill.metallurgy",
            "resident.skill.logistics",
            "resident.skill.alchemy",
            "resident.skill.service",
        };
        BuildSkillTable(model, skills, labels, combat: false);
    }

    private void BuildDwarfFightTable(SettlementResidentManagementViewModel model)
    {
        AgentSkillId[] skills =
        {
            AgentSkillCatalog.UnarmedCombat,
            AgentSkillCatalog.OneHandedCombat,
            AgentSkillCatalog.TwoHandedCombat,
            AgentSkillCatalog.Defense,
            AgentSkillCatalog.RangedCombat,
        };
        string[] labels =
        {
            "resident.skill.unarmed_combat",
            "resident.skill.one_handed_combat",
            "resident.skill.two_handed_combat",
            "resident.skill.defense",
            "resident.skill.ranged_combat",
        };
        BuildSkillTable(model, skills, labels, combat: true);
    }

    private void BuildSkillTable(
        SettlementResidentManagementViewModel model,
        IReadOnlyList<AgentSkillId> skills,
        IReadOnlyList<string> labels,
        bool combat)
    {
        List<ManagementColumn> columns = new List<ManagementColumn>
        {
            Column("management.name", 140f),
            Column("management.total", 142f),
        };
        columns.AddRange(labels.Select(value => Column(value, 126f)));
        BuildManagementHeader(columns);
        foreach (SettlementResidentManagementRowViewModel row in model.Rows)
        {
            RectTransform visual = CreateManagementRow(row.Resident.Id, 34f);
            CreateManagementTextCell(visual, row.Resident.Name, columns[0].Width);
            int total = SkillPoints(combat ? row.CombatTotal : row.ProductionTotal);
            int capacity = SkillPoints(row.Resident.Skills.TotalCapacityUnits);
            CreateManagementBarCell(
                visual,
                total,
                capacity,
                columns[1].Width,
                SkillColor(total));
            for (int index = 0; index < skills.Count; index++)
            {
                int value = SkillPoints(row.GetSkill(skills[index]));
                CreateManagementBarCell(
                    visual,
                    value,
                    100,
                    columns[index + 2].Width,
                    SkillColor(value));
            }
        }
    }

    private void BuildDwarfFamilyTable(SettlementResidentManagementViewModel model)
    {
        ManagementColumn[] columns =
        {
            Column("management.name", 160f),
            Column("management.partner", 180f),
            Column("management.father", 180f),
            Column("management.mother", 180f),
            Column("management.children", 300f),
        };
        BuildManagementHeader(columns);
        foreach (SettlementResidentManagementRowViewModel row in model.Rows)
        {
            RectTransform visual = CreateManagementRow(row.Resident.Id, 36f);
            CreateManagementTextCell(visual, row.Resident.Name, columns[0].Width);
            CreateManagementTextCell(visual, row.Partner, columns[1].Width);
            CreateManagementTextCell(visual, row.Father, columns[2].Width);
            CreateManagementTextCell(visual, row.Mother, columns[3].Width);
            CreateManagementTextCell(
                visual,
                row.Children.Count == 0 ? "-" : string.Join(", ", row.Children),
                columns[4].Width);
        }
    }

    private void BuildDwarfInventoryTable(SettlementResidentManagementViewModel model)
    {
        int maximumSlots = model.Rows.Select(row => row.Inventory.Slots.Count)
            .DefaultIfEmpty(0)
            .Max();
        List<ManagementColumn> columns = new List<ManagementColumn>
        {
            Column("management.name", 150f),
        };
        for (int index = 0; index < maximumSlots; index++)
        {
            columns.Add(new ManagementColumn("Slot " + (index + 1), 142f));
        }

        BuildManagementHeader(columns);
        foreach (SettlementResidentManagementRowViewModel row in model.Rows)
        {
            RectTransform visual = CreateManagementRow(row.Resident.Id, 46f);
            CreateManagementTextCell(visual, row.Resident.Name, columns[0].Width);
            for (int index = 0; index < maximumSlots; index++)
            {
                string value = index >= row.Inventory.Slots.Count
                    || row.Inventory.Slots[index].IsEmpty
                    ? "-"
                    : InventoryCell(row.Inventory.Slots[index]);
                CreateManagementTextCell(
                    visual,
                    value,
                    columns[index + 1].Width,
                    TextAnchor.MiddleCenter);
            }
        }
    }

    private static ManagementColumn Column(string key, float width)
    {
        return new ManagementColumn(DigHudLocalization.Resolve(key), width);
    }

    private static void CreateNeedBar(
        Transform parent,
        ResidentNeedViewModel need,
        float width)
    {
        CreateManagementBarCell(parent, need.Percent, 100, width, NeedColor(need.Band));
    }

    private static int SkillPoints(int units)
    {
        return units / AgentSkillCatalog.UnitsPerPoint;
    }

    private static Color SkillColor(int points)
    {
        return Color.Lerp(
            new Color(0.08f, 0.22f, 0.48f, 1f),
            new Color(0.12f, 0.78f, 0.22f, 1f),
            Mathf.Clamp01(points / 100f));
    }

    private static string SexLabel(ResidentSexIndicator sex)
    {
        return DigHudLocalization.Resolve(sex == ResidentSexIndicator.Female
            ? "resident.sex.female"
            : sex == ResidentSexIndicator.Male
                ? "resident.sex.male"
                : "resident.sex.unknown");
    }

    private static string ScheduleManagementLabel(ScheduleActivity activity)
    {
        return DigHudLocalization.Resolve(activity switch
        {
            ScheduleActivity.Work => "management.schedule.work",
            ScheduleActivity.Free => "management.schedule.free",
            ScheduleActivity.Sleep => "management.schedule.sleep",
            _ => "management.schedule.rest",
        });
    }

    private static string InventoryCell(ResidentInventoryLayoutSlotViewModel slot)
    {
        string quantity = slot.Quantity > 1 ? " x" + slot.Quantity : string.Empty;
        return slot.DisplayName + quantity;
    }
}

}
