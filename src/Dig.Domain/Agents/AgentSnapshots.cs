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
        CellId? position = null,
        int positionZ = 0,
        AgentSkillProgressionSnapshot? skillProgression = null,
        bool automaticPlanningEnabled = true)
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
            new CellId(
                (position ?? new CellId(0, 0, 0)).X,
                (position ?? new CellId(0, 0, 0)).Y,
                positionZ),
            skillProgression,
            automaticPlanningEnabled)
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
        CellId position,
        AgentSkillProgressionSnapshot? skillProgression,
        bool automaticPlanningEnabled)
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

        if (position.X < 0
            || position.Y < 0
            || position.Z < 0
            || position.Z > AgentState.MaximumDepthIndex)
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
        SkillProgression = skillProgression;
        AutomaticPlanningEnabled = automaticPlanningEnabled;
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
    public AgentSkillProgressionSnapshot? SkillProgression { get; }
    public bool AutomaticPlanningEnabled { get; }
    public CellId Position { get; }
    public int PositionZ => Position.Z;

    public int GetSkillLevel(AgentSkillId skillId)
    {
        for (int index = 0; index < Skills.Count; index++)
        {
            if (Skills[index].Id == skillId)
            {
                return Skills[index].Level;
            }
        }

        AgentSkillId alias = LegacyAlias(skillId);
        if (!alias.IsEmpty && alias != skillId)
        {
            for (int index = 0; index < Skills.Count; index++)
            {
                if (Skills[index].Id == alias)
                {
                    return Skills[index].Level;
                }
            }
        }

        return 0;
    }

    private static AgentSkillId LegacyAlias(AgentSkillId skillId)
    {
        string value = skillId.ToString();
        if (value == "general.work") return AgentSkillCatalog.Logistics;
        if (value == "mining") return AgentSkillCatalog.Stonework;
        if (value == "building") return AgentSkillCatalog.Woodworking;
        if (skillId == AgentSkillCatalog.Logistics) return new AgentSkillId("general.work");
        if (skillId == AgentSkillCatalog.Stonework) return new AgentSkillId("mining");
        if (skillId == AgentSkillCatalog.Woodworking) return new AgentSkillId("building");
        return default;
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
        CellId position,
        AgentSkillProgressionSnapshot? skillProgression = null,
        bool automaticPlanningEnabled = true)
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
            position,
            skillProgression,
            automaticPlanningEnabled);
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
