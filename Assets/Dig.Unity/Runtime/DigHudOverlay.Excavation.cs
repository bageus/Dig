using Dig.Application.World;
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

            _excavationControls.EnsureDefaultExcavationDrawingMode();
            GUILayout.BeginHorizontal();
            GUI.enabled = _excavationControls.CanActivateExcavationDrawing;
            if (GUILayout.Button("Tunnel", GUILayout.Width(76f)))
            {
                _excavationControls.SetExcavationDrawingMode(
                    DigExcavationDrawingMode.Tunnel);
            }

            if (GUILayout.Button("Depth", GUILayout.Width(66f)))
            {
                _excavationControls.SetExcavationDrawingMode(
                    DigExcavationDrawingMode.Depth);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(68f)))
            {
                _excavationControls.SetExcavationDrawingMode(
                    DigExcavationDrawingMode.Delete);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("□", GUILayout.Width(58f)))
            {
                _excavationControls.SetCaveRoomPlanningPreset(
                    CaveRoomPresetKind.Small);
            }

            if (GUILayout.Button("▭", GUILayout.Width(58f)))
            {
                _excavationControls.SetCaveRoomPlanningPreset(
                    CaveRoomPresetKind.Medium);
            }

            if (GUILayout.Button("▰", GUILayout.Width(58f)))
            {
                _excavationControls.SetCaveRoomPlanningPreset(
                    CaveRoomPresetKind.Large);
            }

            if (GUILayout.Button("▯", GUILayout.Width(58f)))
            {
                _excavationControls.SetCaveRoomPlanningPreset(
                    CaveRoomPresetKind.Tall);
            }

            GUILayout.EndHorizontal();
            GUI.enabled = true;
            if (!_excavationControls.CanActivateExcavationDrawing)
            {
                GUILayout.Label("Clear the dwarf selection to edit excavation plans.");
            }
            else if (_excavationControls.CaveRoomPreset.HasValue)
            {
                GUILayout.Label("Move over a horizontal tunnel; LMB places a room plan.");
            }
            else if (_excavationControls.ExcavationModeLabel == "Depth")
            {
                GUILayout.Label(
                    "Depth: click an open tunnel or room cell to create a Z+1 Dig job, up to Z=3.");
            }
            else
            {
                GUILayout.Label("Tunnel/Delete: hold LMB and move over cells.");
            }
        }
    }
}