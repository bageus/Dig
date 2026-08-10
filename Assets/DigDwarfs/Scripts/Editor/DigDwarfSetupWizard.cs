#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DigDwarfs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DigDwarfSetupWizard
{
    private const string ModelsRoot = "Assets/DigDwarfs/Models";
    private const string GeneratedRoot = "Assets/DigDwarfs/Generated";

    [MenuItem("Tools/Dig Dwarfs/Generate Prefabs And Controllers")]
    public static void Generate()
    {
        EnsureFolder("Assets/DigDwarfs");
        EnsureFolder(GeneratedRoot);
        EnsureFolder(Path.Combine(GeneratedRoot, "Controllers").Replace(Path.DirectorySeparatorChar, '/'));
        EnsureFolder(Path.Combine(GeneratedRoot, "Prefabs").Replace(Path.DirectorySeparatorChar, '/'));

        var prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { ModelsRoot });
        int generated = 0;

        foreach (var guid in prefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            var clips = LoadClipsForAsset(prefabPath);
            if (clips.Count == 0)
            {
                Debug.LogWarning($"Skipping {prefabPath}: no animation clips found.");
                continue;
            }

            var controllerPath = $"{GeneratedRoot}/Controllers/{prefab.name}.controller";
            var controller = CreateOrUpdateController(controllerPath, clips);

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogWarning($"Could not instantiate {prefabPath}.");
                continue;
            }

            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                var sockets = instance.GetComponent<DwarfAttachmentSockets>();
                if (sockets == null)
                {
                    sockets = instance.AddComponent<DwarfAttachmentSockets>();
                }
                sockets.AutoBind();

                var bridge = instance.GetComponent<DwarfAnimatorBridge>();
                if (bridge == null)
                {
                    bridge = instance.AddComponent<DwarfAnimatorBridge>();
                }

                if (instance.GetComponent<DwarfSampleDriver>() == null)
                {
                    instance.AddComponent<DwarfSampleDriver>();
                }

                var prefabOut = $"{GeneratedRoot}/Prefabs/{prefab.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, prefabOut);
                generated++;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"DigDwarfSetupWizard finished. Generated/updated {generated} prefabs.");
    }

    private static Dictionary<string, AnimationClip> LoadClipsForAsset(string prefabPath)
    {
        var result = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        var all = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
        foreach (var asset in all)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            {
                result[clip.name] = clip;
            }
        }
        return result;
    }

    private static AnimatorController CreateOrUpdateController(string controllerPath, Dictionary<string, AnimationClip> clips)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        controller.layers = Array.Empty<AnimatorControllerLayer>();
        controller.parameters = Array.Empty<AnimatorControllerParameter>();

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCarrying", AnimatorControllerParameterType.Bool);
        controller.AddParameter("ActionKind", AnimatorControllerParameterType.Int);
        controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HitTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DieTrigger", AnimatorControllerParameterType.Trigger);

        var layer = new AnimatorControllerLayer
        {
            name = "Base Layer",
            defaultWeight = 1f,
            stateMachine = new AnimatorStateMachine { name = "Base Layer" }
        };
        controller.AddLayer(layer);

        var sm = controller.layers[0].stateMachine;
        var idle = AddState(sm, "Idle", clips);
        sm.defaultState = idle;
        var walk = AddState(sm, "Walk", clips);
        var run = AddState(sm, "Run", clips);
        var climb = AddState(sm, "Climb", clips);
        var carry = AddState(sm, "Carry", clips);
        var mine = AddState(sm, "Mine", clips);
        var chop = AddState(sm, "Chop", clips);
        var build = AddState(sm, "Build", clips);
        var eat = AddState(sm, "Eat", clips);
        var attack = AddState(sm, "Attack", clips);
        var hit = AddState(sm, "Hit", clips);
        var death = AddState(sm, "Death", clips);

        // Idle/Walk/Run locomotion
        AddTransition(idle, walk, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); });
        AddTransition(walk, idle, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });
        AddTransition(walk, run, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.Greater, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });
        AddTransition(run, walk, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.Less, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });
        AddTransition(idle, run, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.Greater, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });

        // Climb/carry overrides
        foreach (var st in new[] { idle, walk, run })
        {
            AddTransition(st, climb, false, 0.05f, t => t.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing"));
            AddTransition(st, carry, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.If, 0, "IsCarrying"); });
        }
        AddTransition(climb, idle, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });
        AddTransition(climb, walk, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); t.AddCondition(AnimatorConditionMode.Less, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });
        AddTransition(climb, run, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); t.AddCondition(AnimatorConditionMode.Greater, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); });
        AddTransition(carry, idle, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); });
        AddTransition(carry, walk, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); t.AddCondition(AnimatorConditionMode.Less, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); });
        AddTransition(carry, run, false, 0.05f, t => { t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCarrying"); t.AddCondition(AnimatorConditionMode.Greater, 0.65f, "Speed"); t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing"); });

        // action int states. Caller should set ActionKind and then reset to None.
        AddAnyAction(controller, mine, 1);
        AddAnyAction(controller, chop, 2);
        AddAnyAction(controller, build, 3);
        AddAnyAction(controller, eat, 4);

        // trigger states
        AddAnyTrigger(controller, attack, "AttackTrigger");
        AddAnyTrigger(controller, hit, "HitTrigger");
        AddAnyTrigger(controller, death, "DieTrigger");

        foreach (var state in new[] { mine, chop, build, eat, attack, hit })
        {
            AddTransition(state, idle, true, 0.05f, null);
        }
        // death has no exit

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, Dictionary<string, AnimationClip> clips)
    {
        var state = sm.AddState(name);
        if (clips.TryGetValue(name, out var clip))
        {
            state.motion = clip;
        }
        return state;
    }

    private static void AddAnyAction(AnimatorController controller, AnimatorState target, int actionKind)
    {
        var t = controller.layers[0].stateMachine.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.Equals, actionKind, "ActionKind");
    }

    private static void AddAnyTrigger(AnimatorController controller, AnimatorState target, string triggerName)
    {
        var t = controller.layers[0].stateMachine.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
    }

    private static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to, bool hasExitTime, float duration, Action<AnimatorStateTransition>? configure)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = hasExitTime;
        t.duration = duration;
        configure?.Invoke(t);
        return t;
    }

    private static void EnsureFolder(string assetPath)
    {
        assetPath = assetPath.Replace(Path.DirectorySeparatorChar, '/');
        var parts = assetPath.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
#endif