using System;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldItemVisual
    {
        private static readonly Color SelectionColor =
            new Color(0.18f, 0.58f, 1f, 1f);

        private ItemReservationVisualState _reservationState;
        private bool _selectionHighlighted;

        internal bool IsSelectionHighlighted => _selectionHighlighted;

        internal void SetSelectionHighlighted(bool highlighted)
        {
            if (_selectionHighlighted == highlighted)
            {
                return;
            }

            _selectionHighlighted = highlighted;
            ApplyCurrentTint();
        }

        private void ApplyCurrentTint()
        {
            Color tint = ResolveCurrentTint();
            int visible = Math.Min(VisibleInstanceCount, _tints.Count);
            for (int index = 0; index < visible; index++)
            {
                if (_instances[index].activeSelf)
                {
                    _tints[index].SetTint(tint);
                }
            }
        }

        private Color ResolveCurrentTint()
        {
            Color tint = ResolveReservationTint(_reservationState);
            return _selectionHighlighted
                ? Color.Lerp(tint, SelectionColor, 0.48f)
                : tint;
        }
    }
}
