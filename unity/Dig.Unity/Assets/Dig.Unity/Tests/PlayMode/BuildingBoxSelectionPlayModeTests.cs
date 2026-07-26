using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
    public sealed class BuildingBoxSelectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Selection_tints_only_the_existing_box_geometry()
        {
            GameObject root = new GameObject("BuildingBox visual root");
            root.AddComponent<BoxCollider>();
            DigWorldItemVisual visual = root.AddComponent<DigWorldItemVisual>();
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Physical BuildingBox";
            box.transform.SetParent(root.transform, worldPositionStays: false);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            DigVisualTintTarget tint = box.AddComponent<DigVisualTintTarget>();
            Color baseTint = new Color(0.66f, 0.38f, 0.16f, 1f);
            tint.Configure(box.GetComponent<Renderer>().sharedMaterial, baseTint);

            GetField<List<GameObject>>(visual, "_instances").Add(box);
            GetField<List<DigVisualTintTarget>>(visual, "_tints").Add(tint);
            SetField(visual, "_baseTint", baseTint);
            SetField(visual, "_reservationState", default(ItemReservationVisualState));
            SetProperty(visual, "VisibleInstanceCount", 1);
            MethodInfo setHighlighted = typeof(DigWorldItemVisual).GetMethod(
                "SetSelectionHighlighted",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            int childCountBefore = root.transform.childCount;
            setHighlighted.Invoke(visual, new object[] { true });
            yield return null;

            Assert.AreEqual(childCountBefore, root.transform.childCount);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.AreNotEqual(baseTint, ReadCurrentTint(tint));

            setHighlighted.Invoke(visual, new object[] { false });
            yield return null;

            Assert.AreEqual(baseTint, ReadCurrentTint(tint));
            Assert.AreEqual("Physical BuildingBox", root.transform.GetChild(0).name);
            Object.DestroyImmediate(root);
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        }

        private static void SetProperty(object target, string name, object value)
        {
            target.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        }

        private static Color ReadCurrentTint(DigVisualTintTarget tint)
        {
            return (Color)typeof(DigVisualTintTarget).GetProperty(
                "CurrentTint",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tint)!;
        }
    }
}
