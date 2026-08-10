using System;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class ResidentWorldScalePlayModeTests
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

    [Test]
    public void Rendered_resident_root_is_half_scale_without_moving_its_feet()
    {
        _root = new GameObject("Resident Scale Test");
        DigAgentRenderer renderer = _root.AddComponent<DigAgentRenderer>();

        renderer.Render(new[] { Resident() }, movementDuration: 0.1f);

        Transform visualRoot = _root.transform.Find("Resident Visuals");
        Assert.That(visualRoot, Is.Not.Null);
        Assert.That(visualRoot.childCount, Is.EqualTo(1));
        Transform resident = visualRoot.GetChild(0);
        Assert.That(resident.localScale, Is.EqualTo(Vector3.one * 0.5f));
        Assert.That(resident.position, Is.EqualTo(new Vector3(1f, -1.54f, 0.41f)));
        Assert.That(resident.GetComponent<CapsuleCollider>(), Is.Not.Null);
    }

    private static AgentViewModel Resident()
    {
        return new AgentViewModel(
            id: "resident.scale-test",
            name: "Scale Test",
            version: 1,
            isAlive: true,
            cellX: 1,
            cellY: 1,
            nutrition: 100,
            alertness: 100,
            mood: 100,
            health: 100,
            scheduledActivity: "FreeTime",
            activeIntent: "Wait",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}
}
