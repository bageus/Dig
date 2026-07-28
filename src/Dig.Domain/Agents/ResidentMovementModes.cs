using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.Agents
{

public enum ResidentMovementMode
{
    Normal = 0,
    Tired = 1,
    ForcedFast = 2,
    Fleeing = 3,
    Carrying = 4,
    Mobility = 5,
    Climbing = 6,
}

public enum ResidentMobilityKind
{
    None = 0,
    RideHamster = 1,
    Hoverboard = 2,
}

public enum ResidentMovementCommandSource
{
    Automatic = 0,
    Manual = 1,
    SpatialWork = 2,
    Recovery = 3,
}

public enum ResidentMovementModeReason
{
    Normal = 0,
    CriticalAlertness = 1,
    RepeatedManualCommand = 2,
    FleeIntent = 3,
    BuildingBoxCarried = 4,
    RideHamsterAvailable = 5,
    HoverboardAvailable = 6,
    VerticalTraversal = 7,
    ShaftGapTraversal = 8,
}

public enum ResidentMovementInterruptionReason
{
    None = 0,
    Completed = 1,
    ReplacedByCommand = 2,
    RepeatedCommand = 3,
    HigherPriorityAction = 4,
    AgentDead = 5,
    RouteUnavailable = 6,
    TraversalRejected = 7,
    MovementRejected = 8,
}

public sealed class ResidentMovementModeDefinition
{
    public ResidentMovementModeDefinition(
        ResidentMovementMode mode,
        double speedMultiplier,
        double transitionDurationMultiplier)
    {
        if (!Enum.IsDefined(typeof(ResidentMovementMode), mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (speedMultiplier <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
        }

        if (transitionDurationMultiplier <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionDurationMultiplier));
        }

        Mode = mode;
        SpeedMultiplier = speedMultiplier;
        TransitionDurationMultiplier = transitionDurationMultiplier;
    }

    public ResidentMovementMode Mode { get; }
    public double SpeedMultiplier { get; }
    public double TransitionDurationMultiplier { get; }
}

public sealed class ResidentMovementModeCatalog
{
    private readonly IReadOnlyDictionary<ResidentMovementMode, ResidentMovementModeDefinition>
        _definitions;

    public ResidentMovementModeCatalog(
        IReadOnlyCollection<ResidentMovementModeDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        Dictionary<ResidentMovementMode, ResidentMovementModeDefinition> values =
            definitions.ToDictionary(definition => definition.Mode);
        foreach (ResidentMovementMode mode in Enum.GetValues(typeof(ResidentMovementMode)))
        {
            if (!values.ContainsKey(mode))
            {
                throw new ArgumentException(
                    $"Movement mode catalog is missing '{mode}'.",
                    nameof(definitions));
            }
        }

        _definitions = new ReadOnlyDictionary<
            ResidentMovementMode,
            ResidentMovementModeDefinition>(values);
    }

    public ResidentMovementModeDefinition Get(ResidentMovementMode mode)
    {
        return _definitions[mode];
    }

    public static ResidentMovementModeCatalog CreateNeutralDefaults()
    {
        return new ResidentMovementModeCatalog(
            Enum.GetValues(typeof(ResidentMovementMode))
                .Cast<ResidentMovementMode>()
                .Select(mode => new ResidentMovementModeDefinition(
                    mode,
                    speedMultiplier: 1d,
                    transitionDurationMultiplier: 1d))
                .ToArray());
    }
}

}
