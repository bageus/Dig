using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Presentation.Agents
{

public sealed class AgentViewModel
{
    public AgentViewModel(
        string id,
        string name,
        long version,
        bool isAlive,
        int cellX,
        int cellY,
        int nutrition,
        int alertness,
        int mood,
        int health,
        string scheduledActivity,
        string activeIntent,
        int actionElapsedTicks,
        int actionRequiredTicks,
        string decisionReason,
        string decisionExplanation,
        IReadOnlyCollection<AgentUtilityOptionViewModel> utilityOptions,
        int cellZ = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Agent id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Agent name is required.", nameof(name));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (cellX < 0 || cellY < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX));
        }

        if (cellZ < 0 || cellZ > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(cellZ));
        }

        if (utilityOptions is null)
        {
            throw new ArgumentNullException(nameof(utilityOptions));
        }

        Id = id.Trim();
        Name = name.Trim();
        Version = version;
        IsAlive = isAlive;
        CellX = cellX;
        CellY = cellY;
        CellZ = cellZ;
        Nutrition = nutrition;
        Alertness = alertness;
        Mood = mood;
        Health = health;
        ScheduledActivity = scheduledActivity;
        ActiveIntent = activeIntent;
        ActionElapsedTicks = actionElapsedTicks;
        ActionRequiredTicks = actionRequiredTicks;
        DecisionReason = decisionReason;
        DecisionExplanation = decisionExplanation;
        UtilityOptions = new ReadOnlyCollection<AgentUtilityOptionViewModel>(utilityOptions.ToArray());
    }

    public string Id { get; }
    public string Name { get; }
    public long Version { get; }
    public bool IsAlive { get; }
    public int CellX { get; }
    public int CellY { get; }
    public int CellZ { get; }
    public int Nutrition { get; }
    public int Alertness { get; }
    public int Mood { get; }
    public int Health { get; }
    public string ScheduledActivity { get; }
    public string ActiveIntent { get; }
    public int ActionElapsedTicks { get; }
    public int ActionRequiredTicks { get; }
    public string DecisionReason { get; }
    public string DecisionExplanation { get; }
    public IReadOnlyList<AgentUtilityOptionViewModel> UtilityOptions { get; }

    public double ActionProgress => ActionRequiredTicks <= 0
        ? 0d
        : Math.Min(1d, (double)ActionElapsedTicks / ActionRequiredTicks);
}

}
