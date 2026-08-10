using System;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldItemVisual
    {
        private static readonly Color SelectionColor =
            new Color(0.18f, 0.58f, 1f, 1f);
        private static readonly Color InteractionHoverColor =
            new Color(1f, 0.78f, 0.18f, 1f);

        private ItemReservationVisualState _reservationState;
        private bool _selectionHighlighted;
        private bool _interactionHighlighted;

        internal bool IsSelectionHighlighted => _selectionHighlighted;
        internal bool IsInteractionHighlighted => _interactionHighlighted;

        internal void SetSelectionHighlighted(bool highlighted)
        {
            if (_selectionHighlighted == highlighted)
            {
                return;
            }

            _selectionHighlighted = highlighted;
            ApplyCurrentTint();
        }

        internal void SetInteractionHighlighted(bool highlighted)
        {
            if (_interactionHighlighted == highlighted)
            {
                return;
            }

            _interactionHighlighted = highlighted;
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
            if (_selectionHighlighted)
            {
                tint = Color.Lerp(tint, SelectionColor, 0.48f);
            }

            return _interactionHighlighted
                ? Color.Lerp(tint, InteractionHoverColor, 0.55f)
                : tint;
        }
    }
}
