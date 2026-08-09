using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class DigAuthoredResidentRigConfiguratorTests
{
    [Test]
    public void Namespaced_imported_bones_drive_pose_fallback_and_hand_sockets()
    {
        GameObject root = new GameObject("Namespaced rig root");
        GameObject modelRoot = new GameObject("Namespaced model");
        modelRoot.transform.SetParent(root.transform, worldPositionStays: false);
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "Body";
        mesh.transform.SetParent(modelRoot.transform, worldPositionStays: false);

        Transform leftArm = AddBone(modelRoot.transform, "mixamorig:LeftArm");
        Transform rightArm = AddBone(modelRoot.transform, "mixamorig:RightArm");
        AddBone(modelRoot.transform, "mixamorig:LeftUpLeg");
        AddBone(modelRoot.transform, "mixamorig:RightUpLeg");
        Transform leftHand = AddBone(leftArm, "mixamorig:LeftHand");
        Transform rightHand = AddBone(rightArm, "mixamorig:RightHand");

        try
        {
            Assert.IsTrue(DigAuthoredResidentRigConfigurator.TryConfigure(
                root,
                modelRoot,
                DigResidentAnimatedModel.StableId,
                maximumRenderers: 24,
                out DigResidentRig rig,
                configureAnimation: false));

            Assert.AreSame(
                leftHand,
                rig.ResolveSocket(DigResidentSocketKind.LeftHand).parent);
            Assert.AreSame(
                rightHand,
                rig.ResolveSocket(DigResidentSocketKind.RightHand).parent);

            Quaternion initialLeftArm = leftArm.localRotation;
            rig.ApplyAction(new ResidentActionVisualViewModel(
                "resident.namespaced.test",
                ResidentActionVisualState.Dig,
                0.25d,
                true,
                1));
            Assert.AreNotEqual(initialLeftArm, leftArm.localRotation);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Transform AddBone(Transform parent, string name)
    {
        Transform bone = new GameObject(name).transform;
        bone.SetParent(parent, worldPositionStays: false);
        return bone;
    }
}
}
