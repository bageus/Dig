using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.Agents
{

public sealed class AgentSkillProgressionSnapshot
{
    public AgentSkillProgressionSnapshot(
        int schemaVersion,
        int precisionVersion,
        int totalCapacityUnits,
        IEnumerable<AgentSkillValue> values,
        IEnumerable<string>? appliedSourceKeys = null,
        SkillRedistributionReport? lastReport = null,
        IEnumerable<string>? migrationSteps = null)
    {
        if (schemaVersion != AgentSkillCatalog.SchemaVersion
            || precisionVersion != AgentSkillCatalog.PrecisionVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (totalCapacityUnits < AgentSkillCatalog.BaseCapacityUnits
            || totalCapacityUnits > AgentSkillCatalog.UniversityCapacityUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCapacityUnits));
        }

        AgentSkillValue[] ordered = (values
            ?? throw new ArgumentNullException(nameof(values)))
            .OrderBy(value => value.Id)
            .ToArray();
        if (ordered.Length != 12
            || ordered.Select(value => value.Id).Distinct().Count() != 12
            || ordered.Any(value => !AgentSkillCatalog.Contains(value.Id))
            || ordered.Sum(value => value.Level) > totalCapacityUnits)
        {
            throw new ArgumentException(
                "A progression snapshot must contain the 12 catalog skills within capacity.",
                nameof(values));
        }

        string[] sources = (appliedSourceKeys ?? Array.Empty<string>())
            .Select(NormalizeSource)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        SchemaVersion = schemaVersion;
        PrecisionVersion = precisionVersion;
        TotalCapacityUnits = totalCapacityUnits;
        Values = new ReadOnlyCollection<AgentSkillValue>(ordered);
        AppliedSourceKeys = new ReadOnlyCollection<string>(sources);
        LastReport = lastReport;
        MigrationSteps = new ReadOnlyCollection<string>(
            (migrationSteps ?? Array.Empty<string>())
                .Select(NormalizeSource)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
    }

    public int SchemaVersion { get; }
    public int PrecisionVersion { get; }
    public int TotalCapacityUnits { get; }
    public IReadOnlyList<AgentSkillValue> Values { get; }
    public IReadOnlyList<string> AppliedSourceKeys { get; }
    public SkillRedistributionReport? LastReport { get; }
    public IReadOnlyList<string> MigrationSteps { get; }
    public int UsedCapacityUnits => Values.Sum(value => value.Level);

    public int GetLevel(AgentSkillId id)
    {
        AgentSkillValue value = Values.FirstOrDefault(item => item.Id == id);
        return value.Id.IsEmpty ? 0 : value.Level;
    }

    public static AgentSkillProgressionSnapshot Empty()
    {
        return new AgentSkillProgressionSnapshot(
            AgentSkillCatalog.SchemaVersion,
            AgentSkillCatalog.PrecisionVersion,
            AgentSkillCatalog.BaseCapacityUnits,
            AgentSkillCatalog.All.Select(value => new AgentSkillValue(value.Id, 0)));
    }

    private static string NormalizeSource(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Applied source keys cannot be empty.");
        }

        return value.Trim();
    }
}

}
