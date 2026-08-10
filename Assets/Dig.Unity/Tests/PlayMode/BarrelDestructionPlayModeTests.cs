using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dig.Application.WorldObjects;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.PlayModeTests
{

public sealed class BarrelDestructionPlayModeTests
{
    [UnityTest]
    public IEnumerator Four_supported_barrels_render_below_resident_inside_their_depth_slabs()
    {
        GameObject root = new GameObject("Barrel Play Mode fixture");
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        root.AddComponent<DigRenderMaterialLibrary>();
        DigBarrelRenderer renderer = root.AddComponent<DigBarrelRenderer>();
        BarrelDefinitionId definitionId = new BarrelDefinitionId("world.barrel.wooden");
        ItemId stone = new ItemId("material.stone");
        BarrelSnapshot[] barrels =
        {
            Snapshot("f1000000000000000000000000000001", definitionId, stone, new CellId(2, 3, 0)),
            Snapshot("f1000000000000000000000000000002", definitionId, stone, new CellId(5, 3, 0)),
            Snapshot("f1000000000000000000000000000003", definitionId, stone, new CellId(2, 8, 2)),
            Snapshot("f1000000000000000000000000000004", definitionId, stone, new CellId(5, 8, 2)),
        };

        Invoke(renderer, "Render", (object)barrels);
        yield return null;

        DigBarrelVisual[] visuals = root.GetComponentsInChildren<DigBarrelVisual>();
        Assert.That(visuals, Has.Length.EqualTo(4));
        const float residentWorldHeight = 1.52f * 0.5f;
        float depthOrigin = GetProjectionConstant("DepthOrigin");
        float depthSpacing = GetProjectionConstant("DepthSpacing");
        foreach (DigBarrelVisual visual in visuals)
        {
            BoxCollider collider = visual.GetComponent<BoxCollider>();
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.size.y, Is.EqualTo(0.49f).Within(0.0001f));
            Assert.That(collider.size.y, Is.LessThan(residentWorldHeight));
            Assert.That(Vector3.Dot(visual.transform.up, Vector3.up),
                Is.EqualTo(1f).Within(0.0001f));
            BarrelSnapshot model = (BarrelSnapshot)GetProperty(visual, "Model");
            float expectedDepth = depthOrigin + (model.Cell.Z * depthSpacing);
            Assert.That(visual.transform.position.z, Is.EqualTo(expectedDepth).Within(0.0001f));
        }

        EntityId highlightedId = barrels[0].BarrelId;
        Invoke(renderer, "SetHighlighted", highlightedId);
        DigBarrelVisual highlighted = visuals.Single(value =>
            ((BarrelSnapshot)GetProperty(value, "Model")).BarrelId == highlightedId);
        Assert.That((bool)GetProperty(highlighted, "IsHighlighted"), Is.True);

        Invoke(renderer, "Render", (object)new[] { barrels[1], barrels[2], barrels[3] });
        yield return null;
        Assert.That(root.GetComponentsInChildren<DigBarrelVisual>(), Has.Length.EqualTo(3));

        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Direct_attack_destroys_visual_and_materializes_one_world_item()
    {
        GameObject root = new GameObject("Barrel attack workflow fixture");
        root.AddComponent<DigRenderMaterialLibrary>();
        DigBarrelRenderer renderer = root.AddComponent<DigBarrelRenderer>();
        BarrelDefinitionId definitionId = new BarrelDefinitionId("world.barrel.wooden");
        ItemId stone = new ItemId("material.stone");
        EntityId barrelId = EntityId.Parse("f2000000000000000000000000000001");
        EntityId jobId = EntityId.Parse("f2000000000000000000000000000002");
        EntityId workerId = EntityId.Parse("f2000000000000000000000000000003");
        EntityId outputId = EntityId.Parse("f2000000000000000000000000000004");
        CellId target = new CellId(4, 4, 1);
        CellId work = new CellId(3, 4, 1);
        BarrelState barrels = new BarrelState(new BarrelCatalog(new[]
        {
            new BarrelDefinition(definitionId, new[] { stone }),
        }));
        Assert.That(barrels.Add(barrelId, definitionId, target, stone, 0).IsSuccess, Is.True);
        JobSystem jobs = new JobSystem();
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(stone, "Stone", 100, isTool: false),
        }));
        InMemoryBarrelRepository barrelRepository = new InMemoryBarrelRepository(barrels);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository(jobs);
        InMemoryInventoryRepository inventoryRepository = new InMemoryInventoryRepository(inventory);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        StartDirectBarrelAttackCommandHandler start = new StartDirectBarrelAttackCommandHandler(
            barrelRepository,
            jobRepository,
            journal);
        ArriveAtBarrelCommandHandler arrive = new ArriveAtBarrelCommandHandler(
            jobRepository,
            journal);
        CompleteBarrelHitCommandHandler hit = new CompleteBarrelHitCommandHandler(
            jobRepository,
            journal);
        CompleteBarrelDestructionCommandHandler complete =
            new CompleteBarrelDestructionCommandHandler(
                barrelRepository,
                jobRepository,
                inventoryRepository,
                journal);

        Invoke(renderer, "Render", (object)barrels.GetAll());
        yield return null;
        Assert.That(root.GetComponentsInChildren<DigBarrelVisual>(), Has.Length.EqualTo(1));
        Assert.That(start.Handle(new StartDirectBarrelAttackCommand(
            jobId,
            barrelId,
            workerId,
            work,
            priority: 900,
            tick: 1)).IsSuccess, Is.True);
        Assert.That(arrive.Handle(new ArriveAtBarrelCommand(jobId, 2)).IsSuccess, Is.True);
        Assert.That(hit.Handle(new CompleteBarrelHitCommand(jobId, 3)).IsSuccess, Is.True);
        Assert.That(complete.Handle(new CompleteBarrelDestructionCommand(
            jobId,
            outputId,
            tick: 4)).IsSuccess, Is.True);

        Invoke(renderer, "Render", (object)barrels.GetAll());
        yield return null;
        Assert.That(root.GetComponentsInChildren<DigBarrelVisual>(), Is.Empty);
        ItemStackSnapshot output = inventory.GetStack(outputId)!;
        Assert.That(output.ItemId, Is.EqualTo(stone));
        Assert.That(output.Quantity, Is.EqualTo(1));
        Assert.That(output.Location, Is.EqualTo(ItemLocation.InWorld(target)));
        Assert.That(inventory.CreateSnapshot().Stacks, Has.Count.EqualTo(1));

        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Unsupported_barrel_lands_without_breaking_or_releasing_contents()
    {
        GameObject root = new GameObject("Barrel landing workflow fixture");
        root.AddComponent<DigRenderMaterialLibrary>();
        DigBarrelRenderer renderer = root.AddComponent<DigBarrelRenderer>();
        BarrelDefinitionId definitionId = new BarrelDefinitionId("world.barrel.wooden");
        ItemId ore = new ItemId("ore.iron");
        EntityId barrelId = EntityId.Parse("f3000000000000000000000000000001");
        CellId source = new CellId(6, 2, 2);
        CellId landing = new CellId(6, 7, 2);
        BarrelState barrels = new BarrelState(new BarrelCatalog(new[]
        {
            new BarrelDefinition(definitionId, new[] { ore }),
        }));
        Assert.That(barrels.Add(barrelId, definitionId, source, ore, 0).IsSuccess, Is.True);
        Invoke(renderer, "Render", (object)barrels.GetAll());
        yield return null;
        float sourceY = root.GetComponentInChildren<DigBarrelVisual>().transform.position.y;

        Assert.That(barrels.BeginFall(barrelId, landing, 1).IsSuccess, Is.True);
        Assert.That(barrels.Land(barrelId, 1).IsSuccess, Is.True);
        Invoke(renderer, "Render", (object)barrels.GetAll());
        yield return null;

        DigBarrelVisual visual = root.GetComponentInChildren<DigBarrelVisual>();
        BarrelSnapshot landed = barrels.Get(barrelId)!;
        Assert.That(landed.Lifecycle, Is.EqualTo(BarrelLifecycle.Supported));
        Assert.That(landed.Cell, Is.EqualTo(landing));
        Assert.That(landed.ContentsItemId, Is.EqualTo(ore));
        Assert.That(landed.ContentsMaterialized, Is.False);
        Assert.That(visual.transform.position.y, Is.LessThan(sourceY));

        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    private static BarrelSnapshot Snapshot(
        string id,
        BarrelDefinitionId definitionId,
        ItemId contents,
        CellId cell)
    {
        return new BarrelSnapshot(
            EntityId.Parse(id),
            definitionId,
            cell,
            BarrelLifecycle.Supported,
            contents,
            contentsGeneration: 0,
            contentsMaterialized: false,
            fallSourceCell: null,
            fallLandingCell: null,
            version: 0);
    }

    private static float GetProjectionConstant(string fieldName)
    {
        Type projection = typeof(DigBarrelRenderer).Assembly.GetType(
            "Dig.Unity.DigTunnelProjection")
            ?? throw new TypeLoadException("Dig.Unity.DigTunnelProjection");
        FieldInfo field = projection.GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(projection.FullName, fieldName);
        object value = field.IsLiteral
            ? field.GetRawConstantValue()!
            : field.GetValue(null)!;
        return (float)value;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, name);
        return method.Invoke(target, arguments)!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(target.GetType().FullName, name);
        return property.GetValue(target)!;
    }
}

}
