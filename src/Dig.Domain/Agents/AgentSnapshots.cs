using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Agents
{

public sealed class AgentSnapshot
{
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
        IReadOnlyCollection<AgentTraitId> traits,
        CellId? position = null)
        : this(
            id,
            name,
            version,
            isAlive,
            needs,
            scheduledActivity,
            activeAction,
            playerOrder,
            lastActionSwitchTick,
            lastDecision,
            CopySkills(skills),
            CopyTraits(traits),
            position ?? new CellId(0, 0))
    {
    }

    private AgentSnapshot(
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
        IReadOnlyList<AgentSkillValue> skills,
        IReadOnlyList<AgentTraitId> traits,
        CellId position)
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

        if (position.X < 0 || position.Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        Skills = skills ?? throw new ArgumentNullException(nameof(skills));
        Traits = traits ?? throw new ArgumentNullException(nameof(traits));
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
        Position = position;
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
    public CellId Position { get; }

    public int GetSkillLevel(AgentSkillId skillId)
    {
        for (int index = 0; index < Skills.Count; index++)
        {
            if (Skills[index].Id == skillId)
            {
                return Skills[index].Level;
            }
        }

        return 0;
    }

    public bool HasTrait(AgentTraitId traitId)
    {
        for (int index = 0; index < Traits.Count; index++)
        {
            if (Traits[index] == traitId)
            {
                return true;
            }
        }

        return false;
    }

    internal static AgentSnapshot FromNormalizedCapabilities(
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
        IReadOnlyList<AgentSkillValue> skills,
        IReadOnlyList<AgentTraitId> traits,
        CellId position)
    {
        return new AgentSnapshot(
            id,
            name,
            version,
            isAlive,
            needs,
            scheduledActivity,
            activeAction,
            playerOrder,
            lastActionSwitchTick,
            lastDecision,
            skills,
            traits,
            position);
    }

    private static IReadOnlyList<AgentSkillValue> CopySkills(
        IReadOnlyCollection<AgentSkillValue> skills)
    {
        if (skills is null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        AgentSkillValue[] ordered = skills.OrderBy(skill => skill.Id).ToArray();
        return new ReadOnlyCollection<AgentSkillValue>(ordered);
    }

    private static IReadOnlyList<AgentTraitId> CopyTraits(
        IReadOnlyCollection<AgentTraitId> traits)
    {
        if (traits is null)
        {
            throw new ArgumentNullException(nameof(traits));
        }

        AgentTraitId[] ordered = traits.OrderBy(trait => trait).ToArray();
        return new ReadOnlyCollection<AgentTraitId>(ordered);
    }
}
}