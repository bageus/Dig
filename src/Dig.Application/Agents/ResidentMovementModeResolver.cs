using System;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;

namespace Dig.Application.Agents
{

public sealed class ResidentMovementModePolicy
{
    public ResidentMovementModePolicy(
        int tiredAlertnessThreshold,
        int? automaticMobilityMinimumRemainingSteps,
        ResidentMovementModeCatalog modes)
    {
        if (tiredAlertnessThreshold < NeedValue.Minimum
            || tiredAlertnessThreshold > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(tiredAlertnessThreshold));
        }

        if (automaticMobilityMinimumRemainingSteps.HasValue
            && automaticMobilityMinimumRemainingSteps.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(automaticMobilityMinimumRemainingSteps));
        }

        TiredAlertnessThreshold = tiredAlertnessThreshold;
        AutomaticMobilityMinimumRemainingSteps =
            automaticMobilityMinimumRemainingSteps;
        Modes = modes ?? throw new ArgumentNullException(nameof(modes));
    }

    public int TiredAlertnessThreshold { get; }
    public int? AutomaticMobilityMinimumRemainingSteps { get; }
    public ResidentMovementModeCatalog Modes { get; }

    public static ResidentMovementModePolicy CreateDefault()
    {
        return new ResidentMovementModePolicy(
            tiredAlertnessThreshold: 2_000,
            automaticMobilityMinimumRemainingSteps: null,
            ResidentMovementModeCatalog.CreateGameplayDefaults());
    }
}

public sealed class ResidentMovementRuntimeRequest
{
    public ResidentMovementRuntimeRequest(
        EntityId residentId,
        int alertness,
        AgentIntentKind activeIntent,
        ResidentMovementCommandSource commandSource,
        TunnelTraversalKind traversalKind,
        bool repeatedManualCommand,
        int remainingPathSteps)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (alertness < NeedValue.Minimum || alertness > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(alertness));
        }

        if (remainingPathSteps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingPathSteps));
        }

        ResidentId = residentId;
        Alertness = alertness;
        ActiveIntent = activeIntent;
        CommandSource = commandSource;
        TraversalKind = traversalKind;
        RepeatedManualCommand = repeatedManualCommand;
        RemainingPathSteps = remainingPathSteps;
    }

    public EntityId ResidentId { get; }
    public int Alertness { get; }
    public AgentIntentKind ActiveIntent { get; }
    public ResidentMovementCommandSource CommandSource { get; }
    public TunnelTraversalKind TraversalKind { get; }
    public bool RepeatedManualCommand { get; }
    public int RemainingPathSteps { get; }
}

public sealed class ResidentMovementModeRequest
{
    public ResidentMovementModeRequest(
        EntityId residentId,
        int alertness,
        AgentIntentKind activeIntent,
        ResidentMovementCommandSource commandSource,
        TunnelTraversalKind traversalKind,
        bool repeatedManualCommand,
        int remainingPathSteps,
        double inventorySpeedMultiplier,
        bool carriesBuildingBox,
        bool hasRideHamster,
        bool hasHoverboard)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (alertness < NeedValue.Minimum || alertness > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(alertness));
        }

        if (!Enum.IsDefined(typeof(AgentIntentKind), activeIntent))
        {
            throw new ArgumentOutOfRangeException(nameof(activeIntent));
        }

        if (!Enum.IsDefined(typeof(ResidentMovementCommandSource), commandSource))
        {
            throw new ArgumentOutOfRangeException(nameof(commandSource));
        }

        if (!Enum.IsDefined(typeof(TunnelTraversalKind), traversalKind))
        {
            throw new ArgumentOutOfRangeException(nameof(traversalKind));
        }

        if (remainingPathSteps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingPathSteps));
        }

        if (inventorySpeedMultiplier <= 0d || inventorySpeedMultiplier > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(inventorySpeedMultiplier));
        }

        ResidentId = residentId;
        Alertness = alertness;
        ActiveIntent = activeIntent;
        CommandSource = commandSource;
        TraversalKind = traversalKind;
        RepeatedManualCommand = repeatedManualCommand;
        RemainingPathSteps = remainingPathSteps;
        InventorySpeedMultiplier = inventorySpeedMultiplier;
        CarriesBuildingBox = carriesBuildingBox;
        HasRideHamster = hasRideHamster;
        HasHoverboard = hasHoverboard;
    }

    public EntityId ResidentId { get; }
    public int Alertness { get; }
    public AgentIntentKind ActiveIntent { get; }
    public ResidentMovementCommandSource CommandSource { get; }
    public TunnelTraversalKind TraversalKind { get; }
    public bool RepeatedManualCommand { get; }
    public int RemainingPathSteps { get; }
    public double InventorySpeedMultiplier { get; }
    public bool CarriesBuildingBox { get; }
    public bool HasRideHamster { get; }
    public bool HasHoverboard { get; }
}

public sealed class ResidentMovementModeResolution
{
    public ResidentMovementModeResolution(
        EntityId residentId,
        ResidentMovementMode mode,
        ResidentMovementModeReason reason,
        ResidentMobilityKind mobility,
        ResidentMovementCommandSource commandSource,
        double effectiveSpeedMultiplier,
        double transitionDurationMultiplier,
        bool repeatedManualCommand)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (effectiveSpeedMultiplier <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveSpeedMultiplier));
        }

        if (transitionDurationMultiplier <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionDurationMultiplier));
        }

        ResidentId = residentId;
        Mode = mode;
        Reason = reason;
        Mobility = mobility;
        CommandSource = commandSource;
        EffectiveSpeedMultiplier = effectiveSpeedMultiplier;
        TransitionDurationMultiplier = transitionDurationMultiplier;
        RepeatedManualCommand = repeatedManualCommand;
    }

    public EntityId ResidentId { get; }
    public ResidentMovementMode Mode { get; }
    public ResidentMovementModeReason Reason { get; }
    public ResidentMobilityKind Mobility { get; }
    public ResidentMovementCommandSource CommandSource { get; }
    public double EffectiveSpeedMultiplier { get; }
    public double TransitionDurationMultiplier { get; }
    public bool RepeatedManualCommand { get; }
    public double AuthoritativeCadenceMultiplier => EffectiveSpeedMultiplier;
}

public sealed class ResidentMovementModeResolver
{
    private readonly ResidentMovementModePolicy _policy;

    public ResidentMovementModeResolver(ResidentMovementModePolicy policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public ResidentMovementModeResolution Resolve(ResidentMovementModeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ResidentMobilityKind mobility = ResolveMobility(request);
        (ResidentMovementMode mode, ResidentMovementModeReason reason) =
            ResolveMode(request, mobility);
        ResidentMovementModeDefinition definition = _policy.Modes.Get(mode);
        return new ResidentMovementModeResolution(
            request.ResidentId,
            mode,
            reason,
            mobility,
            request.CommandSource,
            definition.SpeedMultiplier * request.InventorySpeedMultiplier,
            definition.TransitionDurationMultiplier,
            request.RepeatedManualCommand);
    }

    private (ResidentMovementMode, ResidentMovementModeReason) ResolveMode(
        ResidentMovementModeRequest request,
        ResidentMobilityKind mobility)
    {
        if (request.TraversalKind == TunnelTraversalKind.VerticalClimb)
        {
            return (ResidentMovementMode.Climbing,
                ResidentMovementModeReason.VerticalTraversal);
        }

        if (request.TraversalKind == TunnelTraversalKind.ShaftGapTraverse)
        {
            return (ResidentMovementMode.Climbing,
                ResidentMovementModeReason.ShaftGapTraversal);
        }

        if (request.CarriesBuildingBox)
        {
            return (ResidentMovementMode.Carrying,
                ResidentMovementModeReason.BuildingBoxCarried);
        }

        if (ShouldUseMobility(request, mobility))
        {
            return (ResidentMovementMode.Mobility,
                mobility == ResidentMobilityKind.Hoverboard
                    ? ResidentMovementModeReason.HoverboardAvailable
                    : ResidentMovementModeReason.RideHamsterAvailable);
        }

        if (request.ActiveIntent == AgentIntentKind.Flee)
        {
            return (ResidentMovementMode.Fleeing,
                ResidentMovementModeReason.FleeIntent);
        }

        if (request.RepeatedManualCommand)
        {
            return (ResidentMovementMode.ForcedFast,
                ResidentMovementModeReason.RepeatedManualCommand);
        }

        if (request.Alertness <= _policy.TiredAlertnessThreshold)
        {
            return (ResidentMovementMode.Tired,
                ResidentMovementModeReason.CriticalAlertness);
        }

        return (ResidentMovementMode.Normal, ResidentMovementModeReason.Normal);
    }

    private bool ShouldUseMobility(
        ResidentMovementModeRequest request,
        ResidentMobilityKind mobility)
    {
        if (mobility == ResidentMobilityKind.None)
        {
            return false;
        }

        if (request.RepeatedManualCommand)
        {
            return true;
        }

        return _policy.AutomaticMobilityMinimumRemainingSteps.HasValue
            && request.RemainingPathSteps
                >= _policy.AutomaticMobilityMinimumRemainingSteps.Value;
    }

    private static ResidentMobilityKind ResolveMobility(
        ResidentMovementModeRequest request)
    {
        if (request.HasHoverboard)
        {
            return ResidentMobilityKind.Hoverboard;
        }

        return request.HasRideHamster
            ? ResidentMobilityKind.RideHamster
            : ResidentMobilityKind.None;
    }
}

}
