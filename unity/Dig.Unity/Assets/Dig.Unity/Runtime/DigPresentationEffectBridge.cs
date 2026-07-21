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