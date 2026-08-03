using System.Collections;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class TunnelInfrastructureVisualProjectionPlayModeTests
{
    [UnityTest]
    public IEnumerator Completed_support_and_trim_render_and_remove_without_colliders()
    {
        GameObject host = new GameObject("Tunnel infrastructure projection test");
        DigWorldRenderer worldRenderer = host.AddComponent<DigWorldRenderer>();
        CellId supportCell = new CellId(5, 7, 1);
        CellId trimCell = new CellId(9, 7, 2);
        const string supportId = "tunnel:wooden-support:5:7:1";
        const string trimId = "tunnel:junction-stone-trim:9:7:2";
        worldRenderer.SetTunnelInfrastructureVisuals(
            new TunnelInfrastructureVisualVolumeViewModel(
                version: 4,
                new[]
                {
                    new TunnelInfrastructureVisualViewModel(
                        trimId,
                        TunnelInfrastructureVisualKind.JunctionStoneTrim,
                        trimCell),
                    new TunnelInfrastructureVisualViewModel(
                        supportId,
                        TunnelInfrastructureVisualKind.WoodenSupport,
                        supportCell),
                }));

        yield return null;

        DigTunnelInfrastructureRenderer projection =
            host.GetComponent<DigTunnelInfrastructureRenderer>();
        Assert.That(projection, Is.Not.Null);
        Assert.That(projection.InstanceCount, Is.EqualTo(2));
        Assert.That(projection.TryGetVisual(supportId, out GameObject support), Is.True);
        Assert.That(projection.TryGetVisual(trimId, out GameObject trim), Is.True);
        Assert.That(support.transform.childCount, Is.EqualTo(1));
        Assert.That(trim.transform.childCount, Is.EqualTo(4));
        Assert.That(
            support.GetComponentsInChildren<Collider>(includeInactive: true),
            Is.Empty);
        Assert.That(
            trim.GetComponentsInChildren<Collider>(includeInactive: true),
            Is.Empty);
        Vector3 expectedSupport = new Vector3(
            supportCell.X,
            DigTunnelProjection.WalkSurfaceY(supportCell.Y),
            DigTunnelProjection.DepthOrigin
                + (supportCell.Z * DigTunnelProjection.DepthSpacing));
        Assert.That(
            Vector3.Distance(support.transform.localPosition, expectedSupport),
            Is.LessThan(0.0001f));

        worldRenderer.SetTunnelInfrastructureVisuals(
            TunnelInfrastructureVisualVolumeViewModel.Empty());
        yield return null;

        Assert.That(projection.InstanceCount, Is.Zero);
        UnityEngine.Object.Destroy(host);
        yield return null;
    }
}

}
