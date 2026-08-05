using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
internal static class DigResidentRigFactory
{
    private const float AuthoredResidentHeight = 1.50f;

    internal static DigResidentRig Create(
        Transform parent,
        DigVisualAsset asset,
        Material fallbackMaterial,
        ResidentAppearanceViewModel appearance,
        int maximumRenderers)
    {
        int rendererBudget = Mathf.Clamp(maximumRenderers, 10, 24);
        Material material = asset.Material ?? fallbackMaterial;
        GameObject root;
        DigResidentRig rig;
        if (asset.Prefab != null)
        {
            if (DigResidentAnimatedModel.IsDefaultAsset(asset.StableId))
            {
                root = new GameObject("Authored Resident Rig");
                GameObject modelRoot = UnityEngine.Object.Instantiate(asset.Prefab);
                modelRoot.name = asset.Prefab.name;
                modelRoot.transform.SetParent(root.transform, worldPositionStays: false);
                modelRoot.transform.localPosition = Vector3.zero;
                modelRoot.transform.localRotation = Quaternion.identity;
                modelRoot.transform.localScale = Vector3.one;
                if (!TryConfigureAuthoredRig(
                        root,
                        modelRoot,
                        asset.StableId,
                        rendererBudget,
                        out rig))
                {
                    UnityEngine.Object.Destroy(root);
                    root = BuildRepresentative(material);
                    rig = root.GetComponent<DigResidentRig>();
                }
            }
            else
            {
                root = UnityEngine.Object.Instantiate(asset.Prefab);
                rig = root.GetComponent<DigResidentRig>();
                if (rig == null && !TryConfigureAuthoredRig(
                        root,
                        root,
                        asset.StableId,
                        rendererBudget,
                        out rig))
                {
                    UnityEngine.Object.Destroy(root);
                    root = BuildRepresentative(material);
                    rig = root.GetComponent<DigResidentRig>();
                }
            }
        }
        else
        {
            root = BuildRepresentative(material);
            rig = root.GetComponent<DigResidentRig>();
        }

        root.name = "Resident Rig " + appearance.ResidentId;
        root.transform.SetParent(parent, worldPositionStays: false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        DisableChildColliders(root);
        rig.ApplyAppearance(appearance);
        return rig;
    }

    private static GameObject BuildRepresentative(Material material)
    {
        GameObject root = new GameObject("Low Poly Resident Rig");
        root.AddComponent<DigVisualPrefabRoot>();
        List<Renderer> renderers = new List<Renderer>();
        CreatePart(root.transform, "Body", new Vector3(0f, 0.70f, 0f),
            new Vector3(0.44f, 0.56f, 0.30f), material, renderers);
        CreatePart(root.transform, "Head", new Vector3(0f, 1.18f, 0f),
            new Vector3(0.36f, 0.34f, 0.34f), material, renderers);
        CreatePart(root.transform, "Hair", new Vector3(0f, 1.36f, -0.01f),
            new Vector3(0.38f, 0.10f, 0.32f), material, renderers);
        CreatePart(root.transform, "Headwear", new Vector3(0f, 1.44f, 0f),
            new Vector3(0.46f, 0.08f, 0.40f), material, renderers);
        Transform leftArm = CreateLimb(root.transform, "Left Arm", -0.30f, 0.88f,
            new Vector3(0.14f, 0.48f, 0.14f), material, renderers);
        Transform rightArm = CreateLimb(root.transform, "Right Arm", 0.30f, 0.88f,
            new Vector3(0.14f, 0.48f, 0.14f), material, renderers);
        Transform leftLeg = CreateLimb(root.transform, "Left Leg", -0.13f, 0.42f,
            new Vector3(0.17f, 0.52f, 0.18f), material, renderers);
        Transform rightLeg = CreateLimb(root.transform, "Right Leg", 0.13f, 0.42f,
            new Vector3(0.17f, 0.52f, 0.18f), material, renderers);
        CreatePart(leftArm, "Left Hand", new Vector3(0f, -0.30f, 0f),
            new Vector3(0.16f, 0.16f, 0.16f), material, renderers);
        CreatePart(rightArm, "Right Hand", new Vector3(0f, -0.30f, 0f),
            new Vector3(0.16f, 0.16f, 0.16f), material, renderers);
        Transform[] sockets = CreateSockets(root.transform, leftArm, rightArm);
        DigResidentRig rig = root.AddComponent<DigResidentRig>();
        rig.Initialize(renderers.ToArray(), leftArm, rightArm, leftLeg, rightLeg, sockets);
        return root;
    }

    private static bool TryConfigureAuthoredRig(
        GameObject root,
        GameObject modelRoot,
        string stableId,
        int maximumRenderers,
        out DigResidentRig rig)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(
            includeInactive: true);
        if (renderers.Length < 1 || renderers.Length > maximumRenderers)
        {
            rig = null!;
            return false;
        }

        DigResidentAnimationPlayer? animationPlayer = null;
        if (DigResidentAnimatedModel.IsDefaultAsset(stableId))
        {
            if (!DigResidentAnimationPlayer.TryConfigure(
                    modelRoot,
                    stableId,
                    out DigResidentAnimationPlayer configuredPlayer))
            {
                rig = null!;
                return false;
            }

            animationPlayer = configuredPlayer;
        }

        if (animationPlayer != null)
        {
            NormalizeAuthoredModel(modelRoot.transform, renderers);
        }

        Transform leftArm = FindDescendantAny(
            modelRoot.transform,
            "LeftArm",
            "Left Arm",
            "arm_l",
            "upperarm_l")
            ?? FindOrCreate(modelRoot.transform, "Left Arm");
        Transform rightArm = FindDescendantAny(
            modelRoot.transform,
            "RightArm",
            "Right Arm",
            "arm_r",
            "upperarm_r")
            ?? FindOrCreate(modelRoot.transform, "Right Arm");
        Transform leftLeg = FindDescendantAny(
            modelRoot.transform,
            "LeftLeg",
            "Left Leg",
            "leg_l",
            "thigh_l")
            ?? FindOrCreate(modelRoot.transform, "Left Leg");
        Transform rightLeg = FindDescendantAny(
            modelRoot.transform,
            "RightLeg",
            "Right Leg",
            "leg_r",
            "thigh_r")
            ?? FindOrCreate(modelRoot.transform, "Right Leg");
        Transform[] sockets = CreateAuthoredSockets(
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
            preserveAuthoredMaterials: animationPlayer != null);
        return true;
    }

    private static void NormalizeAuthoredModel(
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

    private static Transform[] CreateAuthoredSockets(
        Transform root,
        Transform leftArm,
        Transform rightArm)
    {
        return new[]
        {
            FindDescendant(root, "HeadAccessory")
                ?? CreateSocket(root, "Socket Head", new Vector3(0f, 1.48f, 0f)),
            FindDescendant(root, "LeftHandTool")
                ?? CreateSocket(leftArm, "Socket Left Hand", new Vector3(0f, -0.38f, 0f)),
            FindDescendant(root, "RightHandTool")
                ?? CreateSocket(rightArm, "Socket Right Hand", new Vector3(0f, -0.38f, 0f)),
            FindDescendant(root, "BackAttachment")
                ?? CreateSocket(root, "Socket Back", new Vector3(0f, 0.82f, -0.22f)),
            FindDescendant(root, "CarryAnchor")
                ?? CreateSocket(root, "Socket Cargo", new Vector3(0f, 0.66f, -0.30f)),
            CreateSocket(root, "Socket VFX", new Vector3(0f, 1.06f, 0f)),
        };
    }

    private static Transform CreateLimb(Transform parent, string name, float x, float y,
        Vector3 scale, Material material, List<Renderer> renderers)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, worldPositionStays: false);
        pivot.localPosition = new Vector3(x, y, 0f);
        CreatePart(pivot, name + " Mesh", new Vector3(0f, -scale.y * 0.45f, 0f),
            scale, material, renderers);
        return pivot;
    }

    private static void CreatePart(Transform parent, string name, Vector3 position,
        Vector3 scale, Material material, List<Renderer> renderers)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.layer = 2;
        part.transform.SetParent(parent, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderers.Add(renderer);
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.Destroy(collider);
    }

    private static Transform[] CreateSockets(
        Transform root,
        Transform leftArm,
        Transform rightArm)
    {
        return new[]
        {
            CreateSocket(root, "Socket Head", new Vector3(0f, 1.48f, 0f)),
            CreateSocket(leftArm, "Socket Left Hand", new Vector3(0f, -0.38f, 0f)),
            CreateSocket(rightArm, "Socket Right Hand", new Vector3(0f, -0.38f, 0f)),
            CreateSocket(root, "Socket Back", new Vector3(0f, 0.82f, -0.22f)),
            CreateSocket(root, "Socket Cargo", new Vector3(0f, 0.66f, -0.30f)),
            CreateSocket(root, "Socket VFX", new Vector3(0f, 1.06f, 0f)),
        };
    }

    private static Transform CreateSocket(Transform parent, string name, Vector3 position)
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

    private static Transform? FindDescendant(Transform root, string name)
    {
        if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase))
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

    private static void DisableChildColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
        for (int index = 0; index < colliders.Length; index++)
        {
            colliders[index].enabled = false;
        }
    }
}
}
