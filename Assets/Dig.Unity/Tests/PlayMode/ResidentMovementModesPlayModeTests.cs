using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class ResidentMovementModesPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [UnityTest]
    public IEnumerator Movement_mode_controls_transition_duration_and_carry_projection()
    {
        _root = new GameObject("Resident Movement Mode Test");
        DigAgentRenderer renderer = _root.AddComponent<DigAgentRenderer>();
        renderer.Render(new[] { Resident(cellX: 1) }, movementDuration: 0.1f);
        yield return null;

        ResidentMovementModeViewModel carrying = new ResidentMovementModeViewModel(
            ResolveCarryingMode(transitionDurationMultiplier: 2d));
        InvokeMovementRender(
            renderer,
            new[] { Resident(cellX: 2) },
            0.1f,
            new Dictionary<string, ResidentMovementModeViewModel>
            {
                ["resident.mode"] = carrying,
            });
        yield return null;

        DigAgentVisual visual = _root.GetComponentInChildren<DigAgentVisual>();
        Assert.That(visual, Is.Not.Null);
        Assert.That(ReadField<float>(visual, "_duration"), Is.EqualTo(0.2f).Within(0.001f));
        ResidentMovementModeViewModel? mode =
            ReadField<ResidentMovementModeViewModel?>(visual, "_movementMode");
        Assert.That(mode, Is.Not.Null);
        Assert.That(mode!.Mode, Is.EqualTo(ResidentMovementMode.Carrying));
        Assert.That(mode.IsCarrying, Is.True);
    }

    private static ResidentMovementModeResolution ResolveCarryingMode(
        double transitionDurationMultiplier)
    {
        ResidentMovementModeCatalog catalog = new ResidentMovementModeCatalog(
            Enum.GetValues(typeof(ResidentMovementMode))
                .Cast<ResidentMovementMode>()
                .Select(mode => new ResidentMovementModeDefinition(
                    mode,
                    speedMultiplier: 1d,
                    transitionDurationMultiplier:
                        mode == ResidentMovementMode.Carrying
                            ? transitionDurationMultiplier
                            : 1d))
                .ToArray());
        ResidentMovementModeResolver resolver = new ResidentMovementModeResolver(
            new ResidentMovementModePolicy(2_000, null, catalog));
        return resolver.Resolve(new ResidentMovementModeRequest(
            EntityId.Parse("40000000000000000000000000000042"),
            alertness: 8_000,
            activeIntent: AgentIntentKind.Work,
            commandSource: ResidentMovementCommandSource.Manual,
            traversalKind: TunnelTraversalKind.SupportedWalk,
            repeatedManualCommand: false,
            remainingPathSteps: 1,
            inventorySpeedMultiplier: 1d,
            carriesBuildingBox: true,
            hasRideHamster: false,
            hasHoverboard: false));
    }

    private static void InvokeMovementRender(
        DigAgentRenderer renderer,
        IReadOnlyList<AgentViewModel> agents,
        float movementDuration,
        IReadOnlyDictionary<string, ResidentMovementModeViewModel> modes)
    {
        MethodInfo? method = typeof(DigAgentRenderer).GetMethod(
            "RenderWithMovementModes",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(IReadOnlyList<AgentViewModel>),
                typeof(float),
                typeof(IReadOnlyDictionary<string, ResidentMovementModeViewModel>),
            },
            modifiers: null);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(renderer, new object[] { agents, movementDuration, modes });
    }

    private static T ReadField<T>(object target, string fieldName)
    {
        FieldInfo? field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field!.GetValue(target)!;
    }

    private static AgentViewModel Resident(int cellX)
    {
        return new AgentViewModel(
            "resident.mode",
            "Movement Mode",
            version: cellX,
            isAlive: true,
            cellX,
            cellY: 1,
            nutrition: 8_000,
            alertness: 8_000,
            mood: 8_000,
            health: 10_000,
            scheduledActivity: "Work",
            activeIntent: "Deliver",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}

}
