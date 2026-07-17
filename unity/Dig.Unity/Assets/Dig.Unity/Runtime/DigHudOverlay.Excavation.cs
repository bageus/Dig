using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private DigWorldInteraction? _excavationControls;

        internal void SetExcavationControls(DigWorldInteraction controls)
        {
            _excavationControls = controls;
        }

        private void DrawExcavationControls()
        {
            if (_excavationControls == null)
            {
                return;
            }

            GUILayout.Label(
                $"Excavation: {_excavationControls.ExcavationModeLabel} | " +
                $"priority {_excavationControls.ExcavationPriority}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Off", GUILayout.Width(56f)))
            {
                _excavationControls.SetExcavationDrawingMode(
                    DigExcavationDrawingMode.None);
            }

            GUI.enabled = _excavationControls.CanActivateExcavationDrawing;
            if (GUILayout.Button("Horizontal", GUILayout.Width(92f)))
            {
                _excavationControls.SetExcavationDrawingMode(
                    DigExcavationDrawingMode.Horizontal);
            }

            if (GUILayout.Button("Vertical", GUILayout.Width(76f)))
            {
                _excavationControls.SetExcavationDrawingMode(
                    DigExcavationDrawingMode.Vertical);
            }

            GUI.enabled = true;
            if (GUILayout.Button("P-", GUILayout.Width(42f)))
            {
                _excavationControls.AdjustExcavationPriority(-50);
            }

            if (GUILayout.Button("P+", GUILayout.Width(42f)))
            {
                _excavationControls.AdjustExcavationPriority(50);
            }

            GUILayout.EndHorizontal();
            if (!_excavationControls.CanActivateExcavationDrawing)
            {
                GUILayout.Label("Clear the dwarf selection to draw or erase tunnels.");
            }
        }
    }
}
