using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigInventorySlotPointer : MonoBehaviour, IPointerClickHandler
{
    internal Action<PointerEventData>? Clicked { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(eventData);
    }
}

}
