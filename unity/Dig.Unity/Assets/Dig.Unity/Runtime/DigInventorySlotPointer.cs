using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigInventorySlotPointer : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    internal Action<PointerEventData>? Clicked { get; set; }
    internal Action? Hovered { get; set; }
    internal Action? Exited { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Exited?.Invoke();
    }
}

}
