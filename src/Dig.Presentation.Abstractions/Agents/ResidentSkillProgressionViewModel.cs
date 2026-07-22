using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;

namespace Dig.Presentation.Agents
{

public sealed partial class ResidentSkillSetViewModel
{
    public ResidentSkillSetViewModel(
        IEnumerable<ResidentSkillViewModel> skills,
        int totalCapacityUnits,
        int precisionVersion,
        SkillRedistributionReport? lastReport)
    {
        if (skills is null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        ResidentSkillViewModel[] all = skills
            .OrderBy(item => item.SkillId, StringComparer.Ordinal)
            .ToArray();
        if (all.Select(item => item.SkillId)
            .Distinct(StringComparer.Ordinal)
            .Count() != all.Length)
        {
            throw new ArgumentException("Skill ids must be unique.", nameof(skills));
        }

        if (totalCapacityUnits < AgentSkillCatalog.BaseCapacityUnits
            || totalCapacityUnits > AgentSkillCatalog.UniversityCapacityUnits
            || precisionVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCapacityUnits));
        }

        All = new ReadOnlyCollection<ResidentSkillViewModel>(all);
        TopFive = new ReadOnlyCollection<ResidentSkillViewModel>(all
            .Where(item => item.Level > 0)
            .OrderByDescending(item => item.Level)
            .ThenBy(item => item.SkillId, StringComparer.Ordinal)
            .Take(5)
            .ToArray());
        TotalCapacityUnits = totalCapacityUnits;
        PrecisionVersion = precisionVersion;
        LastReport = lastReport;
    }

    public int TotalCapacityUnits { get; }
    public int UsedCapacityUnits => All.Sum(value => value.Level);
    public int PrecisionVersion { get; }
    public SkillRedistributionReport? LastReport { get; }
    public int UniversityProgressUnits =>
        TotalCapacityUnits - AgentSkillCatalog.BaseCapacityUnits;
}

}
