using System;
using System.Collections.Generic;
using System.Reflection;
using Dig.Domain.World;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class EmbeddedTerrainDepositReliefPlayModeTests
{
    [Test]
    public void Every_deposit_shape_is_embedded_and_only_slightly_protrudes()
    {
        Type builder = ResolveBuilder();
        MethodInfo cluster = ResolveMethod(builder, "AddDepositCluster");
        float inset = ReadConstant(builder, "DepositReliefInset");
        float maximumRelief = ReadConstant(
            builder,
            "DepositMaximumVisibleRelief");
        Vector3 normal = Vector3.forward;
        Vector3 center = -normal * inset;

        foreach (DigTerrainDepositShape shape in Enum.GetValues(
            typeof(DigTerrainDepositShape)))
        {
            List<Vector3> vertices = BuildCluster(
                cluster,
                center,
                normal,
                shape);
            ResolveNormalRange(
                vertices,
                normal,
                out float minimum,
                out float maximum);

            Assert.That(
                minimum,
                Is.LessThan(-0.001f),
                $"{shape} must begin inside the host-rock plane.");
            Assert.That(
                maximum,
                Is.GreaterThan(0f),
                $"{shape} must retain a visible low relief.");
            Assert.That(
                maximum,
                Is.LessThanOrEqualTo(maximumRelief + 0.00001f),
                $"{shape} protrudes beyond the approved relief budget.");
        }
    }

    [Test]
    public void Deposit_connector_remains_flush_with_the_wall()
    {
        Type builder = ResolveBuilder();
        MethodInfo connector = ResolveMethod(builder, "AddDepositConnector");
        float inset = ReadConstant(builder, "DepositReliefInset");
        float expectedRelief = ReadConstant(builder, "DepositConnectorRelief");
        Vector3 normal = Vector3.forward;
        Vector3 center = -normal * inset;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<List<int>> triangles = new List<List<int>>
        {
            new List<int>(),
        };
        object[] arguments =
        {
            center,
            normal,
            Vector3.right,
            Vector3.up,
            0.45f,
            0,
            vertices,
            normals,
            triangles,
        };

        connector.Invoke(null, arguments);
        ResolveNormalRange(
            vertices,
            normal,
            out float minimum,
            out float maximum);

        Assert.That(vertices, Is.Not.Empty);
        Assert.That(minimum, Is.EqualTo(expectedRelief).Within(0.00001f));
        Assert.That(maximum, Is.EqualTo(expectedRelief).Within(0.00001f));
    }

    private static List<Vector3> BuildCluster(
        MethodInfo method,
        Vector3 center,
        Vector3 normal,
        DigTerrainDepositShape shape)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<List<int>> triangles = new List<List<int>>
        {
            new List<int>(),
        };
        TerrainDepositDecorationCellViewModel decoration =
            new TerrainDepositDecorationCellViewModel(
                new CellId(0, 0, 0),
                "deposit.test",
                TerrainDepositVisualState.Revealed,
                damageBand: 0,
                TerrainDepositConnection.None,
                variant: 3,
                rotationQuarterTurns: 0,
                scaleBand: 3,
                offsetBandX: 0,
                offsetBandY: 0);
        object[] arguments =
        {
            center,
            normal,
            Vector3.right,
            Vector3.up,
            decoration,
            shape,
            TerrainVisualDetailLevel.Full,
            0,
            vertices,
            normals,
            triangles,
        };

        method.Invoke(null, arguments);
        Assert.That(vertices, Is.Not.Empty);
        return vertices;
    }

    private static void ResolveNormalRange(
        IReadOnlyList<Vector3> vertices,
        Vector3 normal,
        out float minimum,
        out float maximum)
    {
        minimum = float.PositiveInfinity;
        maximum = float.NegativeInfinity;
        for (int index = 0; index < vertices.Count; index++)
        {
            float distance = Vector3.Dot(vertices[index], normal);
            minimum = Mathf.Min(minimum, distance);
            maximum = Mathf.Max(maximum, distance);
        }
    }

    private static Type ResolveBuilder()
    {
        Type? builder = typeof(DigWorldRenderer).Assembly.GetType(
            "Dig.Unity.DigTerrainChunkMeshBuilder");
        Assert.That(builder, Is.Not.Null);
        return builder!;
    }

    private static MethodInfo ResolveMethod(Type builder, string name)
    {
        MethodInfo? method = builder.GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return method!;
    }

    private static float ReadConstant(Type builder, string name)
    {
        FieldInfo? field = builder.GetField(
            name,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return Convert.ToSingle(field!.GetRawConstantValue());
    }
}

}
