using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;

namespace Dig.Presentation.Management
{

public sealed class SettlementResidentManagementRowViewModel
{
    public SettlementResidentManagementRowViewModel(
        ResidentRosterRowViewModel resident,
        long age,
        string partner,
        string mother,
        string father,
        IEnumerable<string> children,
        ResidentInventoryLayoutViewModel inventory)
    {
        if (age < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(age));
        }

        Resident = resident ?? throw new ArgumentNullException(nameof(resident));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        if (!string.Equals(resident.Id, inventory.ResidentId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Resident and inventory identifiers must match.",
                nameof(inventory));
        }

        Age = age;
        Partner = Normalize(partner);
        Mother = Normalize(mother);
        Father = Normalize(father);
        Children = new ReadOnlyCollection<string>((children ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray());
    }

    public ResidentRosterRowViewModel Resident { get; }
    public long Age { get; }
    public string Partner { get; }
    public string Mother { get; }
    public string Father { get; }
    public IReadOnlyList<string> Children { get; }
    public ResidentInventoryLayoutViewModel Inventory { get; }

    public int ProductionTotal => SumSkills(AgentSkillCatalog.All
        .Where(definition => definition.Category == AgentSkillCategory.Work)
        .Select(definition => definition.Id));

    public int CombatTotal => SumSkills(AgentSkillCatalog.All
        .Where(definition => definition.Category == AgentSkillCategory.Combat)
        .Select(definition => definition.Id));

    public int GetSkill(AgentSkillId id)
    {
        string value = id.ToString();
        for (int index = 0; index < Resident.Skills.All.Count; index++)
        {
            ResidentSkillViewModel skill = Resident.Skills.All[index];
            if (string.Equals(skill.SkillId, value, StringComparison.Ordinal))
            {
                return skill.Level;
            }
        }

        return 0;
    }

    private int SumSkills(IEnumerable<AgentSkillId> ids)
    {
        return ids.Sum(GetSkill);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}

public sealed class SettlementResidentManagementViewModel
{
    public SettlementResidentManagementViewModel(
        long societyVersion,
        IEnumerable<SettlementResidentManagementRowViewModel> rows)
    {
        if (societyVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(societyVersion));
        }

        SocietyVersion = societyVersion;
        Rows = new ReadOnlyCollection<SettlementResidentManagementRowViewModel>(
            (rows ?? throw new ArgumentNullException(nameof(rows))).ToArray());
    }

    public long SocietyVersion { get; }
    public IReadOnlyList<SettlementResidentManagementRowViewModel> Rows { get; }
}

}
