using System;
using UnityEngine;

namespace Dig.Unity
{

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class DigClockHoverDriver : MonoBehaviour
{
    private DigGameHudCanvas? _owner;

    internal void Initialize(DigGameHudCanvas owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    private void LateUpdate()
    {
        _owner?.RefreshClockInteractionFrame();
    }
}

}
