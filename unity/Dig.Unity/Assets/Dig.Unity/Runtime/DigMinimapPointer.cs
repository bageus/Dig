using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigMinimapPointer : MonoBehaviour, IScrollHandler
{
    internal Action<float>? Scrolled { get; set; }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        Scrolled?.Invoke(eventData.scrollDelta.y);
    }
}

}