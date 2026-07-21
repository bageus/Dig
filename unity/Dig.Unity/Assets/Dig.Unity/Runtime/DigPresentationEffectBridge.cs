using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigPresentationEffectBridge : MonoBehaviour
{
    private readonly PresentationEffectPresenter _presenter =
        new PresentationEffectPresenter();
    private DigPooledVfxPlayer? _vfxPlayer;
    private DigRealtimeLightPool? _lightPool;

    public int ActiveEffectCount => _vfxPlayer == null ? 0 : _vfxPlayer.ActiveCount;
    public int ActiveParticleCount => _vfxPlayer == null
        ? 0 : _vfxPlayer.ActiveParticleCount;
    public int ActiveLightCount => _lightPool == null ? 0 : _lightPool.ActiveCount;

    private void Awake()
    {
        Render(PresentationEffectFrame.Empty, camera: null);
    }

    public PresentationEffectFrame Present(
        IReadOnlyList<PresentationEffectFact> facts, Camera? camera)
    {
        if (facts == null) throw new ArgumentNullException(nameof(facts));
        PresentationEffectFrame frame = _presenter.Present(facts);
        Render(frame, camera);
        return frame;
    }

    public void Render(PresentationEffectFrame frame, Camera? camera)
    {
        if (frame == null) throw new ArgumentNullException(nameof(frame));
        EnsureResources();
        _vfxPlayer!.Play(frame.Effects, camera);
        _lightPool!.Render(frame.Lights, camera);
    }

    public void SetBudget(RenderFrameBudget budget)
    {
        if (budget == null) throw new ArgumentNullException(nameof(budget));
        EnsureResources();
        _vfxPlayer!.SetBudget(budget);
        _lightPool!.SetBudget(budget);
    }

    private void EnsureResources()
    {
        if (_vfxPlayer == null)
            _vfxPlayer = GetComponent<DigPooledVfxPlayer>()
                ?? gameObject.AddComponent<DigPooledVfxPlayer>();
        if (_lightPool == null)
            _lightPool = GetComponent<DigRealtimeLightPool>()
                ?? gameObject.AddComponent<DigRealtimeLightPool>();
    }
}
}