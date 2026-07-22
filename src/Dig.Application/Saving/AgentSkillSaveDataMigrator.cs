using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;

namespace Dig.Application.Saving
{

internal static class AgentSkillSaveDataMigrator
{
    private const int LegacyPrecisionVersion = 0;

    public static IReadOnlyList<string> Apply(AgentSkillsSaveData data)
    {
        if (data?.Agents is null)
        {
            throw new InvalidOperationException("Agent skills save data is missing.");
        }

        AgentSkillProgressionSaveData[] savedAgents = data.Agents.ToArray();
        if (savedAgents.Any(value => value is null))
        {
            throw new InvalidOperationException("Agent skills save entry is missing.");
        }

        List<string> applied = new List<string>();
        foreach (AgentSkillProgressionSaveData saved in savedAgents
            .OrderBy(value => value.AgentId, StringComparer.Ordinal))
        {
            if (saved.PrecisionVersion == AgentSkillCatalog.PrecisionVersion
                && saved.UnitsPerPoint == AgentSkillCatalog.UnitsPerPoint)
            {
                continue;
            }

            MigrateLegacyPrecision(saved);
            string step = "agent-skills.precision-v0-to-v1:" + saved.AgentId;
            saved.MigrationSteps ??= new List<string>();
            if (!saved.MigrationSteps.Contains(step, StringComparer.Ordinal))
            {
                saved.MigrationSteps.Add(step);
                saved.MigrationSteps.Sort(StringComparer.Ordinal);
            }

            applied.Add(step);
        }

        return applied;
    }

    private static void MigrateLegacyPrecision(AgentSkillProgressionSaveData saved)
    {
        if (saved is null
            || saved.SchemaVersion != AgentSkillCatalog.SchemaVersion
            || saved.PrecisionVersion != LegacyPrecisionVersion
            || saved.UnitsPerPoint <= 0
            || saved.Values is null)
        {
            throw new InvalidOperationException("Unsupported agent skill precision.");
        }

        AgentSkillValueSaveData[] values = saved.Values.ToArray();
        if (values.Any(value => value is null))
        {
            throw new InvalidOperationException("Legacy agent skill value is missing.");
        }

        values = values
            .OrderBy(value => value.SkillId, StringComparer.Ordinal)
            .ToArray();
        if (values.Length != AgentSkillCatalog.All.Count
            || values.Select(value => value.SkillId).Distinct(StringComparer.Ordinal).Count()
                != AgentSkillCatalog.All.Count
            || values.Any(value => string.IsNullOrWhiteSpace(value.SkillId))
            || values.Any(value => !AgentSkillCatalog.Contains(
                new AgentSkillId(value.SkillId)))
            || values.Any(value => value.Units < 0))
        {
            throw new InvalidOperationException("Legacy agent skill values are invalid.");
        }

        int sourceUnitsPerPoint = saved.UnitsPerPoint;
        int targetTotal = ScaleFloor(
            values.Sum(value => (long)value.Units),
            sourceUnitsPerPoint);
        Dictionary<string, long> remainders = new Dictionary<string, long>(
            StringComparer.Ordinal);
        int allocated = 0;
        foreach (AgentSkillValueSaveData value in values)
        {
            long numerator = checked((long)value.Units * AgentSkillCatalog.UnitsPerPoint);
            value.Units = checked((int)(numerator / sourceUnitsPerPoint));
            remainders.Add(value.SkillId, numerator % sourceUnitsPerPoint);
            allocated = checked(allocated + value.Units);
        }

        int missing = targetTotal - allocated;
        foreach (AgentSkillValueSaveData value in values
            .OrderByDescending(value => remainders[value.SkillId])
            .ThenBy(value => value.SkillId, StringComparer.Ordinal)
            .Take(missing))
        {
            value.Units = checked(value.Units + 1);
        }

        int scaledCapacity = ScaleFloor(saved.TotalCapacityUnits, sourceUnitsPerPoint);
        if (scaledCapacity < AgentSkillCatalog.BaseCapacityUnits
            || scaledCapacity > AgentSkillCatalog.UniversityCapacityUnits
            || targetTotal > scaledCapacity
            || values.Any(value => value.Units > AgentSkillCatalog.IndividualMaximumUnits))
        {
            throw new InvalidOperationException("Migrated agent skills exceed current limits.");
        }

        saved.TotalCapacityUnits = scaledCapacity;
        saved.Values = values.ToList();
        saved.PrecisionVersion = AgentSkillCatalog.PrecisionVersion;
        saved.UnitsPerPoint = AgentSkillCatalog.UnitsPerPoint;
        saved.LastReport = null;
    }

    private static int ScaleFloor(long sourceUnits, int sourceUnitsPerPoint)
    {
        return checked((int)(checked(sourceUnits * AgentSkillCatalog.UnitsPerPoint)
            / sourceUnitsPerPoint));
    }
}

}
