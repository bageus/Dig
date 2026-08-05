using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Dig.Unity
{
[DisallowMultipleComponent]
internal sealed class DigResidentAnimationPlayer : MonoBehaviour
{
    private readonly Dictionary<string, AnimationClip> _clips =
        new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
    private PlayableGraph _graph;
    private AnimationPlayableOutput _output;
    private AnimationClipPlayable _currentPlayable;
    private AnimationClip? _currentClip;
    private string _currentClipName = string.Empty;
    private long _currentVersion = -1;
    private bool _looping;

    internal string CurrentClipName => _currentClipName;

    internal static bool TryConfigure(
        GameObject modelRoot,
        string stableId,
        out DigResidentAnimationPlayer player)
    {
        if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
        if (!DigResidentAnimatedModel.IsDefaultAsset(stableId))
        {
            player = null!;
            return false;
        }

        AnimationClip[] clips = DigResidentAnimatedModel.LoadAnimationClips();
        if (clips.Length == 0)
        {
            player = null!;
            return false;
        }

        Animator animator = modelRoot.GetComponentInChildren<Animator>(includeInactive: true);
        if (animator == null)
        {
            animator = modelRoot.AddComponent<Animator>();
        }

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = null;

        player = modelRoot.AddComponent<DigResidentAnimationPlayer>();
        if (!player.Initialize(animator, clips))
        {
            UnityEngine.Object.Destroy(player);
            player = null!;
            return false;
        }

        return true;
    }

    internal void ApplyAction(ResidentActionVisualViewModel action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        string clipName = action.State switch
        {
            ResidentActionVisualState.Walk => "Walk",
            ResidentActionVisualState.Run => "Run",
            ResidentActionVisualState.Dig => "Mine",
            ResidentActionVisualState.Carry => "Carry",
            ResidentActionVisualState.Build => "Build",
            ResidentActionVisualState.Pickup => "Carry",
            ResidentActionVisualState.Drop => "Carry",
            ResidentActionVisualState.Hit => "Hit",
            ResidentActionVisualState.Death => "Death",
            ResidentActionVisualState.Sleep => "Rest",
            ResidentActionVisualState.Eat => "Eat",
            _ => "Idle",
        };

        bool holdLastFrame = action.State == ResidentActionVisualState.Death;
        Play(
            clipName,
            (float)action.NormalizedProgress,
            action.IsLooping,
            action.Version,
            holdLastFrame);
    }

    internal void ApplyClimb(float normalizedProgress)
    {
        Play(
            "Climb",
            Mathf.Clamp01(normalizedProgress),
            looping: true,
            version: 0,
            holdLastFrame: false);
    }

    private bool Initialize(Animator animator, AnimationClip[] clips)
    {
        for (int index = 0; index < clips.Length; index++)
        {
            AnimationClip clip = clips[index];
            string normalizedName = NormalizeClipName(clip.name);
            if (!_clips.ContainsKey(normalizedName))
            {
                _clips.Add(normalizedName, clip);
            }
        }

        if (!TryResolveClip("Idle", out _))
        {
            return false;
        }

        _graph = PlayableGraph.Create("Dig Resident Animation");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _output = AnimationPlayableOutput.Create(
            _graph,
            "Resident Animation",
            animator);
        _graph.Play();
        Play("Idle", 0f, looping: true, version: 0, holdLastFrame: false);
        return true;
    }

    private void Play(
        string clipName,
        float normalizedProgress,
        bool looping,
        long version,
        bool holdLastFrame)
    {
        if (!TryResolveClip(clipName, out AnimationClip clip))
        {
            if (!TryResolveClip("Idle", out clip))
            {
                return;
            }

            clipName = "Idle";
            looping = true;
            holdLastFrame = false;
        }

        bool restart = _currentClip != clip
            || (!looping && _currentVersion != version);
        _currentVersion = version;
        _looping = looping;
        if (!restart)
        {
            if (holdLastFrame)
            {
                HoldAt(clip.length);
            }

            return;
        }

        if (_currentPlayable.IsValid())
        {
            _currentPlayable.Destroy();
        }

        _currentClip = clip;
        _currentClipName = NormalizeClipName(clip.name);
        _currentPlayable = AnimationClipPlayable.Create(_graph, clip);
        _currentPlayable.SetApplyFootIK(false);
        _currentPlayable.SetApplyPlayableIK(false);
        _output.SetSourcePlayable(_currentPlayable);

        double startTime = holdLastFrame
            ? clip.length
            : Mathf.Clamp01(normalizedProgress) * clip.length;
        _currentPlayable.SetTime(startTime);
        _currentPlayable.SetSpeed(holdLastFrame ? 0d : 1d);
        _graph.Evaluate(0f);
    }

    private void Update()
    {
        if (!_looping || _currentClip == null || !_currentPlayable.IsValid())
        {
            return;
        }

        double length = _currentClip.length;
        if (length <= 0d)
        {
            return;
        }

        double time = _currentPlayable.GetTime();
        if (time < length)
        {
            return;
        }

        _currentPlayable.SetTime(time % length);
        _graph.Evaluate(0f);
    }

    private void HoldAt(double time)
    {
        if (!_currentPlayable.IsValid())
        {
            return;
        }

        _currentPlayable.SetTime(time);
        _currentPlayable.SetSpeed(0d);
        _graph.Evaluate(0f);
    }

    private bool TryResolveClip(string requestedName, out AnimationClip clip)
    {
        if (_clips.TryGetValue(requestedName, out clip!))
        {
            return true;
        }

        foreach (KeyValuePair<string, AnimationClip> pair in _clips)
        {
            if (pair.Key.EndsWith(requestedName, StringComparison.OrdinalIgnoreCase))
            {
                clip = pair.Value;
                return true;
            }
        }

        clip = null!;
        return false;
    }

    private static string NormalizeClipName(string name)
    {
        int separator = Math.Max(name.LastIndexOf('|'), name.LastIndexOf(':'));
        return separator >= 0 && separator + 1 < name.Length
            ? name.Substring(separator + 1)
            : name;
    }

    private void OnDestroy()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }
    }
}
}
