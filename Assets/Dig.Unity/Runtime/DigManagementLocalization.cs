using System;
using System.Collections.Generic;

namespace Dig.Unity
{

internal static class DigManagementLocalization
{
    private static readonly IReadOnlyDictionary<string, string> English =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["resident.need.health"] = "Health",
            ["resident.need.alertness.vigor"] = "Vigor",
            ["resident.need.mood"] = "Mood",
            ["resident.sex.female"] = "F",
            ["resident.sex.male"] = "M",
            ["resident.sex.unknown"] = "?",
            ["resident.skill.stonework"] = "Stonework",
            ["resident.skill.woodworking"] = "Woodworking",
            ["resident.skill.cooking"] = "Cooking",
            ["resident.skill.metallurgy"] = "Metallurgy",
            ["resident.skill.logistics"] = "Logistics",
            ["resident.skill.alchemy"] = "Alchemy",
            ["resident.skill.service"] = "Service",
            ["resident.skill.unarmed_combat"] = "Unarmed combat",
            ["resident.skill.one_handed_combat"] = "One-handed weapons",
            ["resident.skill.two_handed_combat"] = "Two-handed weapons",
            ["resident.skill.defense"] = "Defense",
            ["resident.skill.ranged_combat"] = "Ranged weapons",
            ["management.name"] = "Name",
            ["management.sex"] = "Sex",
            ["management.hunger"] = "Hunger",
            ["management.age"] = "Age",
            ["management.schedule"] = "Schedule",
            ["management.schedule.work"] = "Work time",
            ["management.schedule.free"] = "Free time",
            ["management.schedule.sleep"] = "Sleep",
            ["management.schedule.rest"] = "Rest",
            ["management.total"] = "Total",
            ["management.partner"] = "Partner",
            ["management.father"] = "Father",
            ["management.mother"] = "Mother",
            ["management.children"] = "Children",
            ["management.item"] = "Item",
            ["management.type"] = "Type",
            ["management.status"] = "Status",
            ["management.position"] = "Position",
            ["management.condition"] = "Condition",
            ["management.progress"] = "Progress",
            ["management.items.empty"] = "There are no items in this category",
            ["management.buildings.empty"] = "There are no buildings in the current zone",
            ["management.building.status.awaitingmaterials"] = "Awaiting materials",
            ["management.building.status.readytobuild"] = "Ready to build",
            ["management.building.status.underconstruction"] = "Under construction",
            ["management.building.status.readytocomplete"] = "Ready to complete",
            ["management.building.status.completed"] = "Completed",
            ["management.building.status.damaged"] = "Damaged",
            ["management.building.status.cancelled"] = "Cancelled",
            ["management.building.status.removed"] = "Removed",
            ["management.building.status.awaitingbox"] = "Awaiting box",
        };

    internal static string Resolve(string key)
    {
        return English.TryGetValue(key, out string? value) ? value : key;
    }
}

}
