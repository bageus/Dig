using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private static readonly ResidentMovementModeResolver FallbackMovementResolver =
        new ResidentMovementModeResolver(ResidentMovementModePolicy.CreateDefault());
    private readonly Dictionary<string, ResidentMovementModeViewModel> _movementModes =
        new Dictionary<string, ResidentMovementModeViewModel>(StringComparer.Ordinal);
    private readonly Dictionary<string, ResidentMovementInterruptionViewModel>
        _movementInterruptions =
            new Dictionary<string, ResidentMovementInterruptionViewModel>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _movementStepBudgets =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _movementStepsConsumed =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private Func<ResidentMovementRuntimeRequest, ResidentMovementModeResolution>?
        _movementModeResolver;

    internal void SetMovementModeResolver(
        Func<ResidentMovementRuntimeRequest, ResidentMovementModeResolution> resolver)
    {
        _movementModeResolver = resolver
            ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal IReadOnlyDictionary<string, ResidentMovementModeViewModel>
        LoadMovementModes()
    {
        return new ReadOnlyDictionary<string, ResidentMovementModeViewModel>(
            new Dictionary<string, ResidentMovementModeViewModel>(
                _movementModes,
                StringComparer.Ordinal));
    }

    internal IReadOnlyDictionary<string, ResidentMovementInterruptionViewModel>
        LoadMovementInterruptions()
    {
        return new ReadOnlyDictionary<string, ResidentMovementInterruptionViewModel>(
            new Dictionary<string, ResidentMovementInterruptionViewModel>(
                _movementInterruptions,
                StringComparer.Ordinal));
    }

    private void BeginMovementModeTick()
    {
        _movementModes.Clear();
        _movementStepBudgets.Clear();
        _movementStepsConsumed.Clear();
    }

    private bool IsMovementStepDue(
        AgentState agent,
        CellId destination,
        ResidentMovementCommandSource source,
        bool repeatedManualCommand,
        int remainingPathSteps)
    {
        AgentSnapshot snapshot = agent.CreateSnapshot(_tick);
        TunnelTraversalKind traversal = _tunnelVolume?.ClassifyTraversal(
                agent.Position,
                destination)
            ?? TunnelTraversalKind.Invalid;
        AgentIntentKind intent = snapshot.ActiveAction?.IntentKind
            ?? AgentIntentKind.Idle;
        ResidentMovementRuntimeRequest runtimeRequest =
            new ResidentMovementRuntimeRequest(
                agent.Id,
                snapshot.Needs.Alertness.Points,
                intent,
                source,
                traversal,
                repeatedManualCommand,
                remainingPathSteps);
        ResidentMovementModeResolution resolution = _movementModeResolver == null
            ? ResolveFallback(runtimeRequest)
            : _movementModeResolver(runtimeRequest);
        string residentKey = agent.Id.ToString();
        _movementModes[residentKey] = new ResidentMovementModeViewModel(resolution);
        if (!_movementStepBudgets.TryGetValue(residentKey, out int budget))
        {
            budget = ResidentInventoryMovementCadence.ResolveStepCount(
                _tick,
                resolution.AuthoritativeCadenceMultiplier);
            _movementStepBudgets.Add(residentKey, budget);
        }

        _movementStepsConsumed.TryGetValue(residentKey, out int consumed);
        if (consumed >= budget)
        {
            return false;
        }

        _movementStepsConsumed[residentKey] = checked(consumed + 1);
        return true;
    }

    private void TryAdvanceAutomaticMovement(
        AgentState agent,
        CellId destination)
    {
        if (!IsMovementStepDue(
            agent,
            destination,
            ResidentMovementCommandSource.Automatic,
            repeatedManualCommand: false,
            remainingPathSteps: 1))
        {
            return;
        }

        Result moved = MoveThroughTunnelTraffic(agent, destination);
        if (moved.IsFailure)
        {
            CancelManualMovementWithWarning(
                agent.Id,
                moved.Error!,
                ResidentMovementInterruptionReason.MovementRejected);
        }
    }

    private static ResidentMovementModeResolution ResolveFallback(
        ResidentMovementRuntimeRequest request)
    {
        return FallbackMovementResolver.Resolve(new ResidentMovementModeRequest(
            request.ResidentId,
            request.Alertness,
            request.ActiveIntent,
            request.CommandSource,
            request.TraversalKind,
            request.RepeatedManualCommand,
            request.RemainingPathSteps,
            inventorySpeedMultiplier: 1d,
            carriesBuildingBox: false,
            hasRideHamster: false,
            hasHoverboard: false));
    }

    private void RecordMovementInterruption(
        EntityId residentId,
        ResidentMovementInterruptionReason reason,
        string detail)
    {
        _movementInterruptions[residentId.ToString()] =
            new ResidentMovementInterruptionViewModel(
                residentId.ToString(),
                reason,
                _tick,
                detail);
    }
}

}
