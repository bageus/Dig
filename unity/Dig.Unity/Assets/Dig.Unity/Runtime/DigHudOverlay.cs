using Dig.Domain.Core;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigHudOverlay : MonoBehaviour
    {
        private WorldViewModel? _world;
        private WorldCellViewModel? _selected;
        private string _status = "Ready";

        public void SetWorld(WorldViewModel world)
        {
            _world = world;
        }

        public void SetSelection(WorldCellViewModel? selected)
        {
            _selected = selected;
        }

        public void SetCommandResult(Result result)
        {
            _status = result.IsSuccess ? "Cell updated." : result.Error!.ToString();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 390f, 280f), GUI.skin.box);
            GUILayout.Label("DIG — Unity World Vertical Slice");
            if (_world != null)
            {
                GUILayout.Label($"World: {_world.Width}×{_world.Height}");
                GUILayout.Label($"Authoritative version: {_world.Version}");
            }

            GUILayout.Space(6f);
            GUILayout.Label("WASD pan | wheel zoom | Q/E rotate");
            GUILayout.Label("Left click select | right click update");
            GUILayout.Space(6f);
            DrawSelection();
            GUILayout.Space(6f);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void DrawSelection()
        {
            if (!_selected.HasValue)
            {
                GUILayout.Label("Selected cell: none");
                return;
            }

            WorldCellViewModel cell = _selected.Value;
            GUILayout.Label($"Cell: {cell.X},{cell.Y} | {cell.MaterialId}");
            GUILayout.Label($"Solid: {cell.IsSolid} | marked: {cell.IsDesignated}");
            GUILayout.Label($"Hardness: {cell.Hardness} | temperature: {cell.Temperature}");
        }
    }
}
