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

        [UnityTest]
        public IEnumerator Tint_target_rebinds_after_cached_renderer_is_destroyed()
        {
            GameObject root = new GameObject("Tint target root");
            DigVisualTintTarget tint = root.AddComponent<DigVisualTintTarget>();
            GameObject original = GameObject.CreatePrimitive(PrimitiveType.Cube);
            original.name = "Original tint geometry";
            original.transform.SetParent(root.transform, worldPositionStays: false);
            Renderer originalRenderer = original.GetComponent<Renderer>();
            tint.Configure(originalRenderer.sharedMaterial, Color.gray);

            Object.DestroyImmediate(original);
            GameObject replacement = GameObject.CreatePrimitive(PrimitiveType.Cube);
            replacement.name = "Replacement tint geometry";
            replacement.transform.SetParent(root.transform, worldPositionStays: false);
            Renderer replacementRenderer = replacement.GetComponent<Renderer>();
            Color replacementTint = new Color(0.24f, 0.76f, 0.42f, 1f);

            Assert.DoesNotThrow(() => tint.SetTint(replacementTint));
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            replacementRenderer.GetPropertyBlock(properties);
            Assert.AreEqual(replacementTint, properties.GetColor("_Color"));

            Object.DestroyImmediate(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hover_tint_recaptures_after_target_visual_rebuild()
        {
            GameObject targetRoot = new GameObject("Hovered building root");
            GameObject oldVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            oldVisual.transform.SetParent(targetRoot.transform, worldPositionStays: false);
            DigVisualTintTarget oldTint = oldVisual.AddComponent<DigVisualTintTarget>();
            Color baseTint = new Color(0.62f, 0.42f, 0.20f, 1f);
            oldTint.Configure(oldVisual.GetComponent<Renderer>().sharedMaterial, baseTint);

            GameObject interactionRoot = new GameObject("World interaction");
            DigWorldInteraction interaction = interactionRoot.AddComponent<DigWorldInteraction>();
            MethodInfo capture = PrivateInteractionMethod("CaptureHoverTints");
            MethodInfo refresh = PrivateInteractionMethod("RefreshHoverTintsIfStale");
            MethodInfo apply = PrivateInteractionMethod("ApplyHoverTints");
            capture.Invoke(interaction, new object[] { targetRoot.transform });

            Object.DestroyImmediate(oldVisual);
            GameObject replacement = GameObject.CreatePrimitive(PrimitiveType.Cube);
            replacement.transform.SetParent(targetRoot.transform, worldPositionStays: false);
            DigVisualTintTarget replacementTint =
                replacement.AddComponent<DigVisualTintTarget>();
            replacementTint.Configure(
                replacement.GetComponent<Renderer>().sharedMaterial,
                baseTint);

            Assert.DoesNotThrow(() =>
                refresh.Invoke(interaction, new object[] { targetRoot.transform }));
            Assert.DoesNotThrow(() => apply.Invoke(interaction, null));
            Assert.AreNotEqual(baseTint, ReadCurrentTint(replacementTint));

            Object.DestroyImmediate(interactionRoot);
            Object.DestroyImmediate(targetRoot);
            yield return null;
        }

        private static MethodInfo PrivateInteractionMethod(string name)
        {
            return typeof(DigWorldInteraction).GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!;
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
