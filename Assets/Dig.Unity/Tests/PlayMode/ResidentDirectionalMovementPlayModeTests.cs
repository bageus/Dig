using System;
using System.Collections;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class ResidentDirectionalMovementPlayModeTests
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
    public IEnumerator Opposite_horizontal_movement_uses_opposite_visual_preferences()
    {
        _root = new GameObject("Directional Movement Test");
        DigAgentRenderer renderer = _root.AddComponent<DigAgentRenderer>();
        renderer.Render(new[]
        {
            Resident("right", 1),
            Resident("left", 3),
        }, movementDuration: 0.25f);

        renderer.Render(new[]
        {
            Resident("right", 2),
            Resident("left", 2),
        }, movementDuration: 0.25f);

        yield return new WaitForSeconds(0.3f);

        Transform visualRoot = _root.transform.Find("Resident Visuals");
        Assert.That(visualRoot, Is.Not.Null);
        Transform right = FindResident(visualRoot, "right");
        Transform left = FindResident(visualRoot, "left");
        Assert.That(right.position.x, Is.GreaterThan(left.position.x));
    }

    private static Transform FindResident(Transform root, string id)
    {
        for (int index = 0; index < root.childCount; index++)
        {
            DigAgentVisual visual = root.GetChild(index).GetComponent<DigAgentVisual>();
            if (visual != null && string.Equals(visual.Model.Id, id, StringComparison.Ordinal))
            {
                return visual.transform;
            }
        }

        throw new InvalidOperationException($"Resident '{id}' was not rendered.");
    }

    private static AgentViewModel Resident(string id, int cellX)
    {
        return new AgentViewModel(
            id,
            id,
            version: cellX,
            isAlive: true,
            cellX,
            cellY: 1,
            nutrition: 100,
            alertness: 100,
            mood: 100,
            health: 100,
            scheduledActivity: "FreeTime",
            activeIntent: "Move",
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}
}
