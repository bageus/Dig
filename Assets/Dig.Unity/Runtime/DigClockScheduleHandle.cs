using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigClockScheduleHandle :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private DigGameHudCanvas? _owner;
    private RectTransform? _face;
    private bool _adjustStart;

    internal void Initialize(
        DigGameHudCanvas owner,
        RectTransform face,
        bool adjustStart)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _face = face ?? throw new ArgumentNullException(nameof(face));
        _adjustStart = adjustStart;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Apply(eventData, announce: false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Apply(eventData, announce: false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Apply(eventData, announce: true);
    }

    private void Apply(PointerEventData eventData, bool announce)
    {
        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        if (_owner == null || _face == null)
        {
            return;
        }

        _owner.DragScheduleHandle(
            _adjustStart,
            _face,
            eventData.position,
            eventData.pressEventCamera,
            announce);
    }
}

}
