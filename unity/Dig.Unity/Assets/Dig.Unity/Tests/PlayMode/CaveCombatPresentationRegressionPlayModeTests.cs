using System.Linq;
using Dig.Presentation.Creatures;
using Dig.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class CaveCombatPresentationRegressionPlayModeTests
{
    [Test]
    public void Combat_vfx_request_uses_world_position_under_translated_parent()
    {
        GameObject parent = new GameObject("Translated VFX Parent");
        GameObject root = new GameObject("Combat VFX");
        Material material = CreateMaterial();
        try
        {
            parent.transform.position = new Vector3(11f, 7f, -4f);
            root.transform.SetParent(parent.transform, worldPositionStays: false);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            DigPooledVfxInstance instance = root.AddComponent<DigPooledVfxInstance>();
            instance.Initialize(particles);
            EffectSpawnRequest request = new EffectSpawnRequest(
                "combat:test",
                "vfx.combat.impact",
                VfxCategory.Combat,
                VfxPriority.Critical,
                worldX: 2.5d,
                worldY: -3.5d,
                worldZ: -1.5d,
                durationSeconds: 0.5d,
                particleBudget: 8,
                scale: 1d,
                version: 1);

            instance.Play(request, profile: null, material: material, now: 0f);

            Assert.That(root.transform.position.x, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(root.transform.position.y, Is.EqualTo(-3.5f).Within(0.001f));
            Assert.That(root.transform.position.z, Is.EqualTo(-1.5f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(parent);
        }
    }

    [Test]
    public void Vuker_fallback_silhouette_fits_one_cell_tunnel_height()
    {
        GameObject parent = new GameObject("Vuker Tunnel Fit");
        Material material = CreateMaterial();
        try
        {
            CreatureVisualSnapshot snapshot = new CreatureVisualSnapshot(
                "vuker-test",
                "enemy.vuker",
                CreatureLifecycleVisualStage.Adult,
                CreatureDisposition.Hostile,
                isAlive: true,
                cellX: 1,
                cellY: 1,
                cellZ: 1,
                isMoving: false,
                isAttacking: false,
                showImpact: false,
                isGrowing: false,
                isSpecialAction: false,
                actionProgress: 0d,
                version: 1);
            CreatureAppearanceViewModel appearance =
                new CreatureVisualPresenter().PresentAppearance(snapshot);
            DigVisualAsset asset = DigVisualAsset.CreateRuntimeFallback(
                "enemy.vuker",
                Color.white);
            DigCreatureVisualResolution resolution =
                new DigCreatureVisualResolution(
                    asset,
                    appearance.RigId,
                    appearance.Family,
                    Vector3.one * DigCreatureRenderer.VukerTunnelFitScale,
                    maximumRenderers: 12,
                    hasProfile: false);

            DigCreatureRig rig = DigCreatureRigFactory.Create(
                parent.transform,
                resolution,
                material,
                appearance);
            Renderer[] renderers = rig.GetComponentsInChildren<Renderer>()
                .Where(value => value.enabled && value.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(renderers.Length, Is.GreaterThan(0));
            float minimum = renderers.Min(value => value.bounds.min.y);
            float maximum = renderers.Max(value => value.bounds.max.y);

            Assert.That(maximum - minimum, Is.LessThanOrEqualTo(1.0f));
        }
        finally
        {
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(parent);
        }
    }

    private static Material CreateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");
        Assert.That(shader, Is.Not.Null);
        return new Material(shader!);
    }
}

}
