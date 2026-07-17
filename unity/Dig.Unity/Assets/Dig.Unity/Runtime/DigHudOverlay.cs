using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Presentation.Agents;
using Dig.Presentation.Runtime;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigHudOverlay : MonoBehaviour
    {
        private const float HudX = 16f;
        private const float HudY = 16f;
        private const float HudWidth = 540f;
        private const float HudHeight = 800f;

        private WorldViewModel? _world;
        private IReadOnlyList<AgentViewModel> _agents =
            System.Array.Empty<AgentViewModel>();
        private WorldCellViewModel? _selectedCell;
        private AgentViewModel? _selectedAgent;
        private DigStorageStatus? _storageStatus;
        private DigAgentSimulationDriver? _simulation;
        private long _tick;
        private string _status = "Starting runtime...";

        public void SetWorld(WorldViewModel world)
        {
            _world = world;
        }

        public void SetAgents(IReadOnlyList<AgentViewModel> agents, long tick)
        {
            _agents = agents;
            _tick = tick;
        }

        internal void SetStorageStatus(DigStorageStatus status)
        {
            _storageStatus = status;
        }

        internal void SetSimulationControls(DigAgentSimulationDriver simulation)
        {
            _simulation = simulation;
        }

        internal bool ContainsScreenPoint(Vector3 screenPoint)
        {
            Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return new Rect(HudX, HudY, HudWidth, HudHeight).Contains(guiPoint);
        }

        public void SetSelection(WorldCellViewModel? selected)
        {
            _selectedCell = selected;
            _selectedAgent = null;
            _residentInventory = null;
            ClearJobSelection();
            ClearBuildingSelection();
        }

        public void SetAgentSelection(AgentViewModel? selected)
        {
            _selectedAgent = selected;
            _selectedCell = null;
            if (selected == null
                || _residentInventory == null
                || !string.Equals(
                    _residentInventory.ResidentId,
                    selected.Id,
                    System.StringComparison.Ordinal))
            {
                _residentInventory = null;
            }

            ClearJobSelection();
            ClearBuildingSelection();
        }

        public void SetCommandResult(Result result)
        {
            _status = result.IsSuccess ? "Command accepted." : result.Error!.ToString();
        }

        public void SetStatus(string status)
        {
            _status = status;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(
                new Rect(HudX, HudY, HudWidth, HudHeight),
                GUI.skin.box);
            GUILayout.Label("DIG — Interactive Settlement Slice");
            if (_world != null)
            {
                GUILayout.Label($"World: {_world.Width}×{_world.Height} | v{_world.Version}");
            }

            GUILayout.Label($"Residents: {_agents.Count} | jobs: {JobCount} | tick: {_tick}");
            DrawTimeControls();
            DrawToolAssignmentControls();
            DrawJobAttentionSummary();
            DrawStorageStatus();
            GUILayout.Space(6f);
            GUILayout.Label("Click inside Game view before using controls");
            GUILayout.Label("WASD / arrows pan | wheel zoom | Q/E rotate");
            GUILayout.Label("Space pause/resume | . step | -/+ speed");
            GUILayout.Label("3 jobs/reservations | 4 navigation routes");
            GUILayout.Label("5 place empty stockpile on selected open cell");
            GUILayout.Label("Box: LMB placement | Alt+LMB pickup | RMB cancel preview");
            GUILayout.Label("Left click select | right click toggle digging");
            GUILayout.Space(8f);
            DrawBuildingPlacement();
            if (!HasBuildingPlacement)
            {
                DrawCellSelection();
                DrawAgentSelection();
                DrawJobSelection();
                DrawBuildingSelection();
            }

            GUILayout.Space(8f);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void DrawTimeControls()
        {
            if (_simulation == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Time: {_simulation.PlaybackLabel}", GUILayout.Width(92f));
            if (GUILayout.Button(
                _simulation.IsPaused ? "Resume" : "Pause",
                GUILayout.Width(68f)))
            {
                _simulation.TogglePause();
            }

            if (GUILayout.Button("Step", GUILayout.Width(52f)))
            {
                _simulation.StepOnce();
            }

            if (GUILayout.Button("1x", GUILayout.Width(42f)))
            {
                _simulation.SetSpeed(SimulationPlaybackSpeed.Normal);
            }

            if (GUILayout.Button("2x", GUILayout.Width(42f)))
            {
                _simulation.SetSpeed(SimulationPlaybackSpeed.Fast);
            }

            if (GUILayout.Button("4x", GUILayout.Width(42f)))
            {
                _simulation.SetSpeed(SimulationPlaybackSpeed.VeryFast);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStorageStatus()
        {
            if (!_storageStatus.HasValue)
            {
                return;
            }

            DigStorageStatus value = _storageStatus.Value;
            GUILayout.Label(
                $"Stockpile {value.Cell.X},{value.Cell.Y}: " +
                $"{value.StoredQuantity}/{value.Capacity} stored | " +
                $"{value.ReservedIncomingQuantity} incoming");
        }

        private void DrawCellSelection()
        {
            if (!_selectedCell.HasValue)
            {
                return;
            }

            WorldCellViewModel cell = _selectedCell.Value;
            GUILayout.Label("SELECTED CELL");
            GUILayout.Label($"Cell: {cell.X},{cell.Y} | {cell.MaterialId}");
            GUILayout.Label($"Solid: {cell.IsSolid} | marked: {cell.IsDesignated}");
            GUILayout.Label($"Hardness: {cell.Hardness} | temperature: {cell.Temperature}");
        }

        private void DrawAgentSelection()
        {
            if (_selectedAgent == null)
            {
                if (!_selectedCell.HasValue
                    && !HasJobSelection
                    && !HasBuildingSelection
                    && !HasBuildingPlacement)
                {
                    GUILayout.Label("Selected object: none");
                }

                return;
            }

            AgentViewModel agent = _selectedAgent;
            GUILayout.Label("SELECTED RESIDENT");
            GUILayout.Label($"{agent.Name} | cell {agent.CellX},{agent.CellY} | v{agent.Version}");
            GUILayout.Label($"Alive: {agent.IsAlive} | schedule: {agent.ScheduledActivity}");
            GUILayout.Label($"Intent: {agent.ActiveIntent} | action {agent.ActionElapsedTicks}/{agent.ActionRequiredTicks}");
            GUILayout.Label($"Nutrition {agent.Nutrition} | alertness {agent.Alertness}");
            GUILayout.Label($"Mood {agent.Mood} | health {agent.Health}");
            DrawResidentInventory();
            GUILayout.Space(5f);
            GUILayout.Label($"Decision: {agent.DecisionReason}");
            GUILayout.Label(agent.DecisionExplanation);
            GUILayout.Space(5f);
            GUILayout.Label("UTILITY OPTIONS");
            foreach (AgentUtilityOptionViewModel option in agent.UtilityOptions)
            {
                string marker = option.Selected ? ">" : " ";
                string availability = option.Available ? "available" : "blocked";
                string critical = option.Critical ? " critical" : string.Empty;
                GUILayout.Label(
                    $"{marker} {option.Intent}: {option.Score} | {availability}{critical} | {option.ReasonCode}");
            }
        }
    }
}