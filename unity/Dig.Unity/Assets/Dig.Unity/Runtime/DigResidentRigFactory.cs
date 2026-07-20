using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
internal static partial class DigResidentRigFactory
{
    internal static DigResidentRig Create(
        Transform parent,
        DigVisualAsset asset,
        Material fallbackMaterial,
        ResidentAppearanceViewModel appearance)
    {
        GameObject root = asset.Prefab == null
            ? BuildRepresentative(fallbackMaterial)
            : UnityEngine.Object.Instantiate(asset.Prefab);
        root.name = "Resident Rig " + appearance.ResidentId;
        root.transform.SetParent(parent, worldPositionStays: false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        DigResidentRig rig = root.GetComponent<DigResidentRig>()
            ?? ConfigureAuthoredRig(root, asset.Material ?? fallbackMaterial);
        DisableChildColliders(root);
        rig.ApplyAppearance(appearance);
        return rig;
    }

    private static GameObject BuildRepresentative(Material material)
    {
        GameObject root = new GameObject("Low Poly Resident Rig");
        root.AddComponent<DigVisualPrefabRoot>();
        List<Renderer> renderers = new List<Renderer>();
        CreateCoreParts(root.transform, material, renderers);
        Transform leftArm = CreateLimb(root.transform, "Left Arm", -0.30f, 0.88f,
            new Vector3(0.14f, 0.48f, 0.14f), material, renderers);
        Transform rightArm = CreateLimb(root.transform, "Right Arm", 0.30f, 0.88f,
            new Vector3(0.14f, 0.48f, 0.14f), material, renderers);
        Transform leftLeg = CreateLimb(root.transform, "Left Leg", -0.13f, 0.42f,
            new Vector3(0.17f, 0.52f, 0.18f), material, renderers);
        Transform rightLeg = CreateLimb(root.transform, "Right Leg", 0.13f, 0.42f,
            new Vector3(0.17f, 0.52f, 0.18f), material, renderers);
        Transform[] sockets = CreateSockets(root.transform, leftArm, rightArm);
        DigResidentRig rig = root.AddComponent<DigResidentRig>();
        rig.Initialize(renderers.ToArray(), leftArm, rightArm, leftLeg, rightLeg, sockets);
        return root;
    }
}
}
