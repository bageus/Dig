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
    public IEnumerator Blackbeard_default_model_plays_runtime_states_and_exposes_sockets()
    {
        Assert.IsTrue(
            DigResidentAnimatedModel.TryResolveDefault(out DigVisualAsset asset),
            "The runtime Blackbeard resident resource was not imported.");

        AnimationClip[] clips = DigResidentAnimatedModel.LoadAnimationClips();
        HashSet<string> clipNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < clips.Length; index++)
        {
            clipNames.Add(NormalizeClipName(clips[index].name));
        }

        string[] requiredClips =
        {
            "Idle", "Walk", "Run", "Climb", "Carry", "Mine",
            "Build", "Eat", "Rest", "Hit", "Death",
        };
        for (int index = 0; index < requiredClips.Length; index++)
        {
            Assert.IsTrue(
                clipNames.Contains(requiredClips[index]),
                $"Missing animation clip '{requiredClips[index]}'.");
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default");
        Assert.IsNotNull(shader, "No test fallback shader is available.");

        Material fallbackMaterial = new Material(shader);
        GameObject parent = new GameObject("Animated resident test parent");
        try
        {
            ResidentAppearanceViewModel appearance = new ResidentAppearanceViewModel(
                "resident.blackbeard.test",
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
                maximumRenderers: 12);

            yield return null;

            Assert.IsNotNull(
                rig.GetComponentInChildren<Animator>(includeInactive: true));
            Assert.Greater(
                rig.GetComponentsInChildren<SkinnedMeshRenderer>(
                    includeInactive: true).Length,
                0,
                "Blackbeard was replaced by the procedural resident fallback.");
            Assert.IsNotNull(
                rig.transform.Find(asset.Prefab!.name),
                "The instantiated Blackbeard model root is missing.");
            Assert.AreEqual("LeftHandTool",
                rig.ResolveSocket(DigResidentSocketKind.LeftHand).name);
            Assert.AreEqual("RightHandTool",
                rig.ResolveSocket(DigResidentSocketKind.RightHand).name);
            Assert.AreEqual("CarryAnchor",
                rig.ResolveSocket(DigResidentSocketKind.Cargo).name);
            Assert.AreEqual("BackAttachment",
                rig.ResolveSocket(DigResidentSocketKind.Back).name);
            Assert.AreEqual("HeadAccessory",
                rig.ResolveSocket(DigResidentSocketKind.Head).name);

            Apply(rig, ResidentActionVisualState.Idle, looping: true, version: 1);
            Assert.AreEqual("Idle", rig.CurrentAnimationClipName);

            Apply(rig, ResidentActionVisualState.Walk, looping: true, version: 2);
            yield return null;
            Assert.AreEqual("Walk", rig.CurrentAnimationClipName);

            Apply(rig, ResidentActionVisualState.Dig, looping: true, version: 3);
            yield return null;
            Assert.AreEqual("Mine", rig.CurrentAnimationClipName);

            rig.ApplyClimbPose(0.5f, ascending: true);
            yield return null;
            Assert.AreEqual("Climb", rig.CurrentAnimationClipName);

            Apply(
                rig,
                ResidentActionVisualState.Death,
                looping: false,
                version: 4,
                normalizedProgress: 1d);
            yield return null;
            Assert.AreEqual("Death", rig.CurrentAnimationClipName);

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
    public void Blackbeard_authored_mesh_does_not_require_animation_to_replace_fallback()
    {
        GameObject root = new GameObject("Blackbeard no-animation test root");
        GameObject modelRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        modelRoot.name = "Blackbeard authored mesh stand-in";
        modelRoot.transform.SetParent(root.transform, worldPositionStays: false);
        try
        {
            Assert.IsTrue(DigAuthoredResidentRigConfigurator.TryConfigure(
                root,
                modelRoot,
                DigResidentAnimatedModel.StableId,
                maximumRenderers: 12,
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
            "resident.blackbeard.test",
            state,
            normalizedProgress,
            looping,
            version));
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
