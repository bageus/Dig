using System;
using System.Reflection;
using Dig.Presentation.Jobs;
using Dig.Presentation.Overlays;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class ServiceMarkerVisibilityPlayModeTests
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
    public void Tunnel_job_never_creates_world_marker_and_jobs_start_hidden()
    {
        OverlayVisibilitySnapshot release = OverlayVisibilityResolver.CreateDefault()
            .CreateSnapshot(OverlayVisibilityProfile.Release);
        Assert.That(release.IsVisible(OverlayLayerKind.Jobs), Is.False);

        _root = new GameObject("Service marker visibility test");
        DigOverlayManager manager = _root.AddComponent<DigOverlayManager>();
        DigAgentRenderer agents = _root.AddComponent<DigAgentRenderer>();
        DigJobRenderer renderer = _root.AddComponent<DigJobRenderer>();
        Invoke(renderer, "Initialize", agents, manager);

        renderer.Render(new[]
        {
            new JobOverlayViewModel(
                "00000000000000000000000000000001",
                "Tunnel support",
                "Created",
                "None",
                0,
                assignedAgentId: null,
                targetX: 4,
                targetY: 2,
                retryCount: 0,
                nextRetryTick: 0,
                reason: null,
                reservations: Array.Empty<JobReservationViewModel>(),
                targetZ: 0,
                isTunnelInfrastructure: true),
        });

        Transform? overlayRoot = _root.transform.Find("Job Overlay");
        Assert.That(overlayRoot, Is.Not.Null);
        Assert.That(overlayRoot!.childCount, Is.EqualTo(0));
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(target, arguments);
    }
}
}
