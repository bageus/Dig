using System;
using System.Collections;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Dig.Presentation.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class LivingMaterialPhysicsPassThroughPlayModeTests
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
    public IEnumerator HamsterAndGrubRemainRaycastableWithoutPushingMovableItems()
    {
        _root = new GameObject("Living material pass-through test");
        DigCreatureRenderer renderer = _root.AddComponent<DigCreatureRenderer>();
        LivingMaterialCreatureVisualProjector projector =
            new LivingMaterialCreatureVisualProjector();
        LivingMaterialSnapshot[] snapshots =
        {
            Snapshot(Id(1), LivingMaterialSpecies.Hamster, new CellId(4, 3, 0), 1),
            Snapshot(Id(2), LivingMaterialSpecies.Grub, new CellId(6, 3, 0), 2),
        };

        renderer.Render(
            projector.Project(snapshots),
            camera: null,
            movementDuration: 0f);
        yield return null;

        DigCreatureVisual[] visuals = _root
            .GetComponentsInChildren<DigCreatureVisual>(includeInactive: true)
            .OrderBy(value => value.Model.SpeciesId, StringComparer.Ordinal)
            .ToArray();
        Assert.That(visuals, Has.Length.EqualTo(2));

        Rigidbody[] movableItems = new Rigidbody[visuals.Length];
        Vector3[] initialPositions = new Vector3[visuals.Length];
        Quaternion[] initialRotations = new Quaternion[visuals.Length];
        for (int index = 0; index < visuals.Length; index++)
        {
            DigCreatureVisual visual = visuals[index];
            SphereCollider interaction = visual.GetComponent<SphereCollider>();
            Assert.That(interaction.enabled, Is.True, visual.Model.SpeciesId);
            Assert.That(interaction.isTrigger, Is.True, visual.Model.SpeciesId);

            Vector3 center = visual.transform.TransformPoint(interaction.center);
            Ray pointerRay = new Ray(center + Vector3.up, Vector3.down);
            Assert.That(
                interaction.Raycast(pointerRay, out RaycastHit hit, 2f),
                Is.True,
                visual.Model.SpeciesId);
            Assert.That(hit.collider, Is.SameAs(interaction));

            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "Movable item proxy " + visual.Model.SpeciesId;
            item.transform.SetParent(_root.transform, worldPositionStays: true);
            item.transform.position = center;
            item.transform.localScale = Vector3.one * 0.08f;
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.WakeUp();
            movableItems[index] = body;
            initialPositions[index] = body.position;
            initialRotations[index] = body.rotation;
        }

        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        for (int index = 0; index < movableItems.Length; index++)
        {
            Rigidbody body = movableItems[index];
            Assert.That(
                Vector3.Distance(body.position, initialPositions[index]),
                Is.LessThan(0.0001f));
            Assert.That(
                Quaternion.Angle(body.rotation, initialRotations[index]),
                Is.LessThan(0.001f));
            Assert.That(body.velocity.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
        }
    }

    private static LivingMaterialSnapshot Snapshot(
        EntityId id,
        LivingMaterialSpecies species,
        CellId cell,
        long version)
    {
        return new LivingMaterialSnapshot(
            id,
            id,
            species,
            LivingMaterialContainment.Free,
            cell,
            cell,
            new LivingMaterialPlaneKey(cell),
            direction: 1,
            activity: LivingMaterialActivity.Moving,
            activityStepsRemaining: 0,
            movementCredit: 0,
            successfulMovementSteps: 0,
            nextSearchAtStep: 4,
            nextSleepAtStep: 16,
            reproductionCyclesCompleted: 0,
            nextReproductionStep: 96,
            deterministicSequence: 0,
            blockedReason: null,
            version: version);
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "7600000000000000000000000000" + suffix.ToString("D4"));
}

}
