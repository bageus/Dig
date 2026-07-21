using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.Agents
{

internal sealed partial class AgentSkillSet
{
    private readonly Dictionary<AgentSkillId, int> _levels;
    private readonly HashSet<string> _appliedSources =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _migrationSteps =
        new HashSet<string>(StringComparer.Ordinal);
    private IReadOnlyList<AgentSkillValue>? _snapshot;
    private bool _usesCatalog;

    public AgentSkillSet(IEnumerable<AgentSkillValue> skills)
    {
        if (skills is null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        AgentSkillValue[] initial = skills.ToArray();
        if (initial.Select(value => value.Id).Distinct().Count() != initial.Length)
        {
            throw new ArgumentException("Skill ids must be unique.", nameof(skills));
        }

        _usesCatalog = initial.Length == 0
            || initial.Any(value => AgentSkillCatalog.Contains(value.Id));
        TotalCapacityUnits = AgentSkillCatalog.BaseCapacityUnits;
        _levels = new Dictionary<AgentSkillId, int>();
        if (_usesCatalog)
        {
            foreach (AgentSkillDefinition definition in AgentSkillCatalog.All)
            {
                _levels.Add(definition.Id, 0);
            }

            foreach (AgentSkillValue value in initial)
            {
                if (!AgentSkillCatalog.Contains(value.Id))
                {
                    throw new ArgumentException(
                        $"Unknown authoritative skill '{value.Id}'.",
                        nameof(skills));
                }

                _levels[value.Id] = value.Level;
            }

            if (_levels.Values.Sum() > TotalCapacityUnits)
            {
                throw new ArgumentException(
                    "Initial catalog skills exceed total capacity.",
                    nameof(skills));
            }
        }
        else
        {
            foreach (AgentSkillValue value in initial)
            {
                _levels.Add(value.Id, value.Level);
            }
        }
    }

    public int TotalCapacityUnits { get; private set; }
    public SkillRedistributionReport? LastReport { get; private set; }
    public bool UsesCatalog => _usesCatalog;

    public int GetLevel(AgentSkillId id)
    {
        return _levels.TryGetValue(id, out int level) ? level : 0;
    }

    public void SetLevel(AgentSkillId id, int level)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Skill id cannot be empty.", nameof(id));
        }

        if (level < 0 || level > AgentSkillCatalog.IndividualMaximumUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (!_usesCatalog && AgentSkillCatalog.Contains(id))
        {
            EnsureCatalogProgression();
        }

        if (_usesCatalog && !AgentSkillCatalog.Contains(id))
        {
            throw new ArgumentException($"Unknown authoritative skill '{id}'.", nameof(id));
        }

        if (_usesCatalog)
        {
            int currentLevel = _levels.TryGetValue(id, out int existing) ? existing : 0;
            int resultingSum = checked(_levels.Values.Sum() - currentLevel + level);
            if (resultingSum > TotalCapacityUnits)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    "Skill value would exceed total capacity.");
            }
        }

        bool changed = !_levels.TryGetValue(id, out int current) || current != level;
        _levels[id] = level;
        if (changed)
        {
            _snapshot = null;
        }
    }

    public void SetTotalCapacity(int capacityUnits)
    {
        EnsureCatalogProgression();
        if (capacityUnits < TotalCapacityUnits
            || capacityUnits > AgentSkillCatalog.UniversityCapacityUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityUnits));
        }

        TotalCapacityUnits = capacityUnits;
    }

    public IReadOnlyList<AgentSkillValue> CreateSnapshot()
    {
        if (_snapshot is not null)
        {
            return _snapshot;
        }

        AgentSkillValue[] values = _levels
            .OrderBy(pair => pair.Key)
            .Select(pair => new AgentSkillValue(pair.Key, pair.Value))
            .ToArray();
        _snapshot = new ReadOnlyCollection<AgentSkillValue>(values);
        return _snapshot;
    }

    public AgentSkillProgressionSnapshot CreateProgressionSnapshot()
    {
        if (!_usesCatalog)
        {
            return CreateLegacyProgressionSnapshot();
        }

        return new AgentSkillProgressionSnapshot(
            AgentSkillCatalog.SchemaVersion,
            AgentSkillCatalog.PrecisionVersion,
            TotalCapacityUnits,
            CreateSnapshot(),
            _appliedSources,
            LastReport,
            _migrationSteps);
    }

    public AgentSkillProgressionSnapshot? TryCreateProgressionSnapshot()
    {
        return CreateProgressionSnapshot();
    }

    public void Restore(AgentSkillProgressionSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        _levels.Clear();
        foreach (AgentSkillValue value in snapshot.Values)
        {
            _levels.Add(value.Id, value.Level);
        }

        _appliedSources.Clear();
        foreach (string source in snapshot.AppliedSourceKeys)
        {
            _appliedSources.Add(source);
        }

        _migrationSteps.Clear();
        foreach (string step in snapshot.MigrationSteps)
        {
            _migrationSteps.Add(step);
        }

        _usesCatalog = true;
        TotalCapacityUnits = snapshot.TotalCapacityUnits;
        LastReport = snapshot.LastReport;
        _snapshot = null;
    }

    private void EnsureCatalogProgression()
    {
        if (_usesCatalog)
        {
            return;
        }

        IReadOnlyDictionary<AgentSkillId, int> migrated = CreateLegacyLevels();
        _levels.Clear();
        foreach (KeyValuePair<AgentSkillId, int> value in migrated)
        {
            _levels.Add(value.Key, value.Value);
        }

        NormalizeLegacyCapacity();
        _migrationSteps.Add("agent-skills.legacy-capabilities-to-catalog");
        _usesCatalog = true;
        _snapshot = null;
    }

    private AgentSkillProgressionSnapshot CreateLegacyProgressionSnapshot()
    {
        Dictionary<AgentSkillId, int> migrated = CreateLegacyLevels()
            .ToDictionary(value => value.Key, value => value.Value);
        int sum = migrated.Values.Sum();
        List<string> migrationSteps = new List<string>
        {
            "agent-skills.legacy-capabilities-to-catalog",
        };
        if (sum > TotalCapacityUnits)
        {
            IReadOnlyDictionary<AgentSkillId, SkillAllocation> normalized =
                ProportionalSkillAllocator.Allocate(
                    TotalCapacityUnits,
                    migrated.Where(value => value.Value > 0)
                        .Select(value => new SkillAllocationWeight(
                            value.Key,
                            value.Value)));
            foreach (SkillAllocation allocation in normalized.Values)
            {
                migrated[allocation.SkillId] = allocation.Units;
            }

            migrationSteps.Add("agent-skills.legacy-capacity-normalized");
        }

        return new AgentSkillProgressionSnapshot(
            AgentSkillCatalog.SchemaVersion,
            AgentSkillCatalog.PrecisionVersion,
            TotalCapacityUnits,
            migrated.Select(value => new AgentSkillValue(value.Key, value.Value)),
            migrationSteps: migrationSteps);
    }

    private IReadOnlyDictionary<AgentSkillId, int> CreateLegacyLevels()
    {
        Dictionary<AgentSkillId, int> migrated = AgentSkillCatalog.All
            .ToDictionary(value => value.Id, _ => 0);
        CopyLegacy(_levels, migrated, "mining", AgentSkillCatalog.Stonework);
        CopyLegacy(_levels, migrated, "building", AgentSkillCatalog.Woodworking);
        CopyLegacy(_levels, migrated, "general.work", AgentSkillCatalog.Logistics);
        return migrated;
    }

    private void NormalizeLegacyCapacity()
    {
        int sum = _levels.Values.Sum();
        if (sum <= TotalCapacityUnits)
        {
            return;
        }


        _migrationSteps.Add("agent-skills.legacy-capacity-normalized");

        IReadOnlyDictionary<AgentSkillId, SkillAllocation> normalized =
            ProportionalSkillAllocator.Allocate(
                TotalCapacityUnits,
                _levels.Where(value => value.Value > 0)
                    .Select(value => new SkillAllocationWeight(
                        value.Key,
                        value.Value)));
        foreach (SkillAllocation allocation in normalized.Values)
        {
            _levels[allocation.SkillId] = allocation.Units;
        }
    }

    private static void CopyLegacy(
        IReadOnlyDictionary<AgentSkillId, int> legacy,
        IDictionary<AgentSkillId, int> targetValues,
        string legacyId,
        AgentSkillId target)
    {
        if (legacy.TryGetValue(new AgentSkillId(legacyId), out int value))
        {
            targetValues[target] = value;
        }
    }
}

}
