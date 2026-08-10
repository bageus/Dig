using System;
using System.Collections;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
public sealed class DigResidentAnimatedModelPlayModeTests
{
    [UnityTest]
    public IEnumerator V3_default_model_is_used_and_exposes_runtime_rig()
    {
        Assert.IsTrue(
            DigResidentAnimatedModel.TryResolveDefault(out DigVisualAsset asset),
            "The V3 dwarf resident resource was not imported.");
        Assert.AreEqual(
            "resident.dwarf.hi3d.lowpoly70k.rigged",
            asset.StableId);
        Assert.IsNotNull(asset.Prefab);
        Assert.AreEqual("Dwarf_Hi3D_LowPoly_70k_Rigged", asset.Prefab!.name);

        AnimationClip[] clips = DigResidentAnimatedModel.LoadAnimationClips();
        HashSet<string> clipNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < clips.Length; index++)
        {
            clipNames.Add(NormalizeClipName(clips[index].name));
        }
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "Idle", "Walk", "Run", "Climb", "Carry", "Mine",
                "Build", "Eat", "Rest", "Hit", "Death",
            },
            clipNames,
            "The V3 runtime resource must expose the authored resident animation set.");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default");
        Assert.IsNotNull(shader, "No test fallback shader is available.");

        Material fallbackMaterial = new Material(shader);
        GameObject parent = new GameObject("V3 resident test parent");
        try
        {
            ResidentAppearanceViewModel appearance = new ResidentAppearanceViewModel(
                "resident.v3.test",
                ResidentBodyVariant.Masculine,
                ResidentAgeVisualBand.Adult,
                ResidentHairVisualVariant.Long,
                ResidentHeadwearRole.None,
                clothingPaletteIndex: 0,
                hairPaletteIndex: 0,
                faceVariant: 0,
                version: 1);
            DigResidentRig rig = DigResidentRigFactory.Create(
                parent.transform,
                asset,
                fallbackMaterial,
                appearance,
                maximumRenderers: 24);

            yield return null;

            Renderer[] renderers = rig.GetComponentsInChildren<Renderer>(
                includeInactive: true);
            Assert.Greater(
                renderers.Length,
                0,
                "The V3 authored dwarf has no runtime renderers.");
            Assert.IsTrue(
                Array.TrueForAll(
                    renderers,
                    renderer => renderer.enabled
                        && renderer.gameObject.activeInHierarchy),
                "The V3 authored dwarf renderers must be active and visible after rig setup.");
            Assert.IsNotNull(
                rig.transform.Find(asset.Prefab.name),
                "The V3 authored dwarf was replaced by the procedural fallback.");
            Assert.IsNotNull(rig.ResolveSocket(DigResidentSocketKind.LeftHand));
            Assert.IsNotNull(rig.ResolveSocket(DigResidentSocketKind.RightHand));
            Assert.IsNotNull(rig.ResolveSocket(DigResidentSocketKind.Cargo));
            Assert.IsNotNull(rig.ResolveSocket(DigResidentSocketKind.Back));
            Assert.IsNotNull(rig.ResolveSocket(DigResidentSocketKind.Head));

            Apply(rig, ResidentActionVisualState.Idle, looping: true, version: 1);
            AssertClip(rig, "Idle");

            Apply(rig, ResidentActionVisualState.Walk, looping: true, version: 2);
            yield return null;
            AssertClip(rig, "Walk");

            Apply(rig, ResidentActionVisualState.Dig, looping: true, version: 3);
            yield return null;
            AssertClip(rig, "Mine");

            rig.ApplyClimbPose(0.5f, ascending: true);
            yield return null;
            AssertClip(rig, "Climb");

            Apply(
                rig,
                ResidentActionVisualState.Death,
                looping: false,
                version: 4,
                normalizedProgress: 1d);
            yield return null;
            AssertClip(rig, "Death");

            rig.SetSelected(true);
            rig.SetSelected(false);
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(parent);
            UnityEngine.Object.DestroyImmediate(fallbackMaterial);
        }
    }

    [Test]
    public void V3_authored_mesh_does_not_require_animation_to_replace_fallback()
    {
        GameObject root = new GameObject("V3 no-animation test root");
        GameObject modelRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        modelRoot.name = "V3 authored mesh stand-in";
        modelRoot.transform.SetParent(root.transform, worldPositionStays: false);
        try
        {
            Assert.IsTrue(DigAuthoredResidentRigConfigurator.TryConfigure(
                root,
                modelRoot,
                DigResidentAnimatedModel.StableId,
                maximumRenderers: 24,
                out DigResidentRig rig,
                configureAnimation: false));

            Assert.AreSame(root, rig.gameObject);
            Assert.AreEqual(string.Empty, rig.CurrentAnimationClipName);
            Assert.AreEqual(
                1,
                rig.GetComponentsInChildren<MeshRenderer>(
                    includeInactive: true).Length,
                "A valid authored mesh must remain active when animation setup is unavailable.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Missing_authored_model_uses_procedural_fallback()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default");
        Assert.IsNotNull(shader, "No test fallback shader is available.");

        Material fallbackMaterial = new Material(shader);
        GameObject parent = new GameObject("Procedural resident fallback test parent");
        try
        {
            DigVisualAsset missingAsset = DigVisualAsset.CreateRuntimeFallback(
                "resident.missing.test",
                Color.cyan);
            ResidentAppearanceViewModel appearance = new ResidentAppearanceViewModel(
                "resident.missing.test",
                ResidentBodyVariant.Neutral,
                ResidentAgeVisualBand.Adult,
                ResidentHairVisualVariant.Short,
                ResidentHeadwearRole.None,
                clothingPaletteIndex: 0,
                hairPaletteIndex: 0,
                faceVariant: 0,
                version: 1);

            DigResidentRig rig = DigResidentRigFactory.Create(
                parent.transform,
                missingAsset,
                fallbackMaterial,
                appearance,
                maximumRenderers: 12);

            Assert.AreEqual(string.Empty, rig.CurrentAnimationClipName);
            Assert.GreaterOrEqual(
                rig.GetComponentsInChildren<Renderer>(includeInactive: true).Length,
                10);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(parent);
            UnityEngine.Object.DestroyImmediate(fallbackMaterial);
        }
    }

    private static void Apply(
        DigResidentRig rig,
        ResidentActionVisualState state,
        bool looping,
        long version,
        double normalizedProgress = 0d)
    {
        rig.ApplyAction(new ResidentActionVisualViewModel(
            "resident.v3.test",
            state,
            normalizedProgress,
            looping,
            version));
    }

    private static void AssertClip(DigResidentRig rig, string expected)
    {
        Assert.AreEqual(expected, rig.CurrentAnimationClipName);
    }

    private static string NormalizeClipName(string name)
    {
        int separator = Math.Max(name.LastIndexOf('|'), name.LastIndexOf(':'));
        return separator >= 0 && separator + 1 < name.Length
            ? name.Substring(separator + 1)
            : name;
    }
}
}
