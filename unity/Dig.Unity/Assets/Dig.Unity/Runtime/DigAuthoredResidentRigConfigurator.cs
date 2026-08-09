using System;
using UnityEngine;

namespace Dig.Unity
{
internal static class DigAuthoredResidentRigConfigurator
{
    private const float AuthoredResidentHeight = 1.50f;

    internal static bool TryConfigure(
        GameObject root,
        GameObject modelRoot,
        string stableId,
        int maximumRenderers,
        out DigResidentRig rig,
        bool configureAnimation = true)
    {
        Renderer[] renderers = DigResidentRigFactory.CollectRenderers(modelRoot);
        bool isDefaultAuthoredModel =
            DigResidentAnimatedModel.IsDefaultAsset(stableId);
        if (renderers.Length < 1)
        {
            Debug.LogWarning(
                $"Resident visual '{stableId}' loaded an authored model without renderer components. "
                + "The procedural resident fallback will be used.",
                modelRoot);
            rig = null!;
            return false;
        }

        if (!isDefaultAuthoredModel
            && maximumRenderers > 0
            && renderers.Length > maximumRenderers)
        {
            Debug.LogWarning(
                $"Resident visual '{stableId}' uses {renderers.Length} renderer components, "
                + $"above the configured budget of {maximumRenderers}. The procedural "
                + "resident fallback will be used.",
                modelRoot);
            rig = null!;
            return false;
        }

        DigResidentAnimationPlayer? animationPlayer = null;
        if (isDefaultAuthoredModel && configureAnimation
            && DigResidentAnimatedModel.LoadAnimationClips().Length > 0)
        {
            if (DigResidentAnimationPlayer.TryConfigure(
                    modelRoot,
                    stableId,
                    out DigResidentAnimationPlayer configuredPlayer))
            {
                animationPlayer = configuredPlayer;
            }
            else
            {
                Debug.LogWarning(
                    $"Resident visual '{stableId}' loaded its authored mesh, but "
                    + "animation clips could not be configured. The authored model "
                    + "remains active and uses the resident rig pose fallback.",
                    modelRoot);
            }
        }

        if (isDefaultAuthoredModel)
        {
            NormalizeModel(modelRoot.transform, renderers);
        }

        Transform leftArm = FindDescendantAny(
            modelRoot.transform,
            "LeftArm",
            "Left Arm",
            "LeftUpperArm",
            "arm_l",
            "upperarm_l",
            "upper_arm.L",
            "upper_arm_l")
            ?? FindOrCreate(modelRoot.transform, "Left Arm");
        Transform rightArm = FindDescendantAny(
            modelRoot.transform,
            "RightArm",
            "Right Arm",
            "RightUpperArm",
            "arm_r",
            "upperarm_r",
            "upper_arm.R",
            "upper_arm_r")
            ?? FindOrCreate(modelRoot.transform, "Right Arm");
        Transform leftLeg = FindDescendantAny(
            modelRoot.transform,
            "LeftLeg",
            "Left Leg",
            "LeftUpLeg",
            "LeftUpperLeg",
            "leg_l",
            "thigh_l",
            "upper_leg.L",
            "upper_leg_l")
            ?? FindOrCreate(modelRoot.transform, "Left Leg");
        Transform rightLeg = FindDescendantAny(
            modelRoot.transform,
            "RightLeg",
            "Right Leg",
            "RightUpLeg",
            "RightUpperLeg",
            "leg_r",
            "thigh_r",
            "upper_leg.R",
            "upper_leg_r")
            ?? FindOrCreate(modelRoot.transform, "Right Leg");
        Transform[] sockets = CreateSockets(
            modelRoot.transform,
            leftArm,
            rightArm);

        rig = root.AddComponent<DigResidentRig>();
        rig.Initialize(
            renderers,
            leftArm,
            rightArm,
            leftLeg,
            rightLeg,
            sockets,
            animationPlayer,
            preserveAuthoredMaterials: isDefaultAuthoredModel);
        return true;
    }

    private static void NormalizeModel(
        Transform modelRoot,
        Renderer[] renderers)
    {
        if (!TryCalculateBounds(renderers, out Bounds bounds)
            || bounds.size.y <= 0.001f)
        {
            return;
        }

        float uniformScale = AuthoredResidentHeight / bounds.size.y;
        modelRoot.localScale = Vector3.one * uniformScale;
        if (!TryCalculateBounds(renderers, out bounds))
        {
            return;
        }

        Vector3 position = modelRoot.localPosition;
        position.x -= bounds.center.x;
        position.y -= bounds.min.y;
        position.z -= bounds.center.z;
        modelRoot.localPosition = position;
    }

    private static bool TryCalculateBounds(
        Renderer[] renderers,
        out Bounds bounds)
    {
        bool initialized = false;
        bounds = default;
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer renderer = renderers[index];
            if (!renderer.enabled)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return initialized;
    }

    private static Transform[] CreateSockets(
        Transform root,
        Transform leftArm,
        Transform rightArm)
    {
        Transform? leftHand = FindDescendantAny(
            root,
            "LeftHand",
            "Left Hand",
            "hand_l",
            "hand.L");
        Transform? rightHand = FindDescendantAny(
            root,
            "RightHand",
            "Right Hand",
            "hand_r",
            "hand.R");
        return new[]
        {
            FindDescendant(root, "HeadAccessory")
                ?? CreateSocket(root, "Socket Head", new Vector3(0f, 1.48f, 0f)),
            FindDescendant(root, "LeftHandTool")
                ?? CreateSocket(
                    leftHand ?? leftArm,
                    "Socket Left Hand",
                    leftHand == null ? new Vector3(0f, -0.38f, 0f) : Vector3.zero),
            FindDescendant(root, "RightHandTool")
                ?? CreateSocket(
                    rightHand ?? rightArm,
                    "Socket Right Hand",
                    rightHand == null ? new Vector3(0f, -0.38f, 0f) : Vector3.zero),
            FindDescendant(root, "BackAttachment")
                ?? CreateSocket(root, "Socket Back", new Vector3(0f, 0.82f, -0.22f)),
            FindDescendant(root, "CarryAnchor")
                ?? CreateSocket(root, "Socket Cargo", new Vector3(0f, 0.66f, -0.30f)),
            CreateSocket(root, "Socket VFX", new Vector3(0f, 1.06f, 0f)),
        };
    }

    private static Transform CreateSocket(
        Transform parent,
        string name,
        Vector3 position)
    {
        Transform socket = FindOrCreate(parent, name);
        socket.localPosition = position;
        socket.localRotation = Quaternion.identity;
        socket.localScale = Vector3.one;
        return socket;
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;
        Transform created = new GameObject(name).transform;
        created.SetParent(parent, worldPositionStays: false);
        return created;
    }

    private static Transform? FindDescendantAny(
        Transform root,
        params string[] names)
    {
        for (int index = 0; index < names.Length; index++)
        {
            Transform? found = FindDescendant(root, names[index]);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform? FindDescendant(
        Transform root,
        string name)
    {
        if (MatchesImportedName(root.name, name))
        {
            return root;
        }

        for (int index = 0; index < root.childCount; index++)
        {
            Transform? found = FindDescendant(root.GetChild(index), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool MatchesImportedName(string importedName, string expectedName)
    {
        if (string.Equals(importedName, expectedName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int separator = Math.Max(
            importedName.LastIndexOf('|'),
            importedName.LastIndexOf(':'));
        return separator >= 0
            && separator + 1 < importedName.Length
            && string.Equals(
                importedName.Substring(separator + 1),
                expectedName,
                StringComparison.OrdinalIgnoreCase);
    }
}
}
