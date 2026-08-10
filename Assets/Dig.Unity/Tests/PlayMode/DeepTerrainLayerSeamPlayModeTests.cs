using System;
using System.Reflection;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class DeepTerrainLayerSeamPlayModeTests
{
    [Test]
    public void Adjacent_deep_terrain_slices_share_the_exact_boundary()
    {
        Type? builder = typeof(DigWorldRenderer).Assembly.GetType(
            "Dig.Unity.DigTerrainChunkMeshBuilder");
        Assert.That(builder, Is.Not.Null);
        MethodInfo? resolve = builder!.GetMethod(
            "ResolveDepthExtents",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);

        (float firstMin, float firstMax) = Resolve(resolve!, z: 1);
        (float secondMin, float secondMax) = Resolve(resolve, z: 2);
        (float thirdMin, float thirdMax) = Resolve(resolve, z: 3);

        Assert.That(firstMin, Is.EqualTo(secondMax).Within(0.00001f));
        Assert.That(secondMin, Is.EqualTo(thirdMax).Within(0.00001f));
        Assert.That(firstMax - firstMin,
            Is.EqualTo(Math.Abs(DigTunnelProjection.DepthSpacing)).Within(0.00001f));
        Assert.That(secondMax - secondMin,
            Is.EqualTo(Math.Abs(DigTunnelProjection.DepthSpacing)).Within(0.00001f));
        Assert.That(thirdMax - thirdMin,
            Is.EqualTo(Math.Abs(DigTunnelProjection.DepthSpacing)).Within(0.00001f));
    }

    private static (float Min, float Max) Resolve(MethodInfo method, int z)
    {
        object[] arguments = { z, 0f, 0f };
        method.Invoke(null, arguments);
        return ((float)arguments[1], (float)arguments[2]);
    }
}

}
