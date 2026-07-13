using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Agents;

public sealed class AgentSnapshot
{
    private readonly Dictionary<AgentSkillId, int> _skillLevels;
    private readonly HashSet<AgentTraitId> _traits;

    public AgentSnapshot(
        EntityId id,
        string name,
        long version,
        bool isAlive,
        AgentNeedsSnapshot needs,
        ScheduleActivity scheduledActivity,
        AgentActionSnapshot? activeAction,
        PlayerOrder? playerOrder,
        long lastActionSwitchTick,
        AgentDecision? lastDecision,
        IReadOnlyCollection<AgentSkillValue> skills,
        IReadOnlyCollection<AgentTraitId> traits)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Agent name is required.", nameof(name));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (lastActionSwitchTick < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(lastActionSwitchTick));
        }

        if (skills is null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        if (traits is null)
        {
            throw new ArgumentNullException(nameof(traits));
        }

        Id = id;
        Name = name.Trim();
        Version = version;
        IsAlive = isAlive;
        Needs = needs;
        ScheduledActivity = scheduledActivity;
        ActiveAction = activeAction;
        PlayerOrder = playerOrder;
        LastActionSwitchTick = lastActionSwitchTick;
        LastDecision = lastDecision;

        AgentSkillValue[] orderedSkills = skills.OrderBy(skill => skill.Id).ToArray();
        AgentTraitId[] orderedTraits = traits.OrderBy(trait => trait).ToArray();
        Skills = new ReadOnlyCollection<AgentSkillValue>(orderedSkills);
        Traits = new ReadOnlyCollection<AgentTraitId>(orderedTraits);
        _skillLevels = orderedSkills.ToDictionary(skill => skill.Id, skill => skill.Level);
        _traits = new HashSet<AgentTraitId>(orderedTraits);
    }

    public EntityId Id { get; }

    public string Name { get; }

    public long Version { get; }

    public bool IsAlive { get; }

    public AgentNeedsSnapshot Needs { get; }

    public ScheduleActivity ScheduledActivity { get; }

    public AgentActionSnapshot? ActiveAction { get; }

    public PlayerOrder? PlayerOrder { get; }

    public long LastActionSwitchTick { get; }

    public AgentDecision? LastDecision { get; }

    public IReadOnlyList<AgentSkillValue> Skills { get; }

    public IReadOnlyList<AgentTraitId> Traits { get; }

    public int GetSkillLevel(AgentSkillId skillId)
    {
        return _skillLevels.TryGetValue(skillId, out int level) ? level : 0;
    }

    public bool HasTrait(AgentTraitId traitId)
    {
        return _traits.Contains(traitId);
    }
}
