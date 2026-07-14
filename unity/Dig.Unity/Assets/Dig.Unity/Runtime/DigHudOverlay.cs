using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigHudOverlay : MonoBehaviour
    {
        private WorldViewModel? _world;
        private IReadOnlyList<AgentViewModel> _agents =
            System.Array.Empty<AgentViewModel>();
        private WorldCellViewModel? _selectedCell;
        private AgentViewModel? _selectedAgent;
        private long _tick;
        private string _status = "Ready";

        public void SetWorld(WorldViewModel world)
        {
            _world = world;
        }

        public void SetAgents(IReadOnlyList<AgentViewModel> agents, long tick)
        {
            _agents = agents;
            _tick = tick;
        }

        public void SetSelection(WorldCellViewModel? selected)
        {
            _selectedCell = selected;
            _selectedAgent = null;
        }

        public void SetAgentSelection(AgentViewModel? selected)
        {
            _selectedAgent = selected;
            _selectedCell = null;
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
            GUILayout.BeginArea(new Rect(16f, 16f, 470f, 720f), GUI.skin.box);
            GUILayout.Label("DIG — Interactive Settlement Slice");
            if (_world != null)
            {
                GUILayout.Label($"World: {_world.Width}×{_world.Height} | v{_world.Version}");
            }

            GUILayout.Label($"Residents: {_agents.Count} | simulation tick: {_tick}");
            GUILayout.Space(6f);
            GUILayout.Label("WASD pan | wheel zoom | Q/E rotate");
            GUILayout.Label("Left click select | right click toggle digging");
            GUILayout.Space(8f);
            DrawCellSelection();
            DrawAgentSelection();
            GUILayout.Space(8f);
            GUILayout.Label(_status);
            GUILayout.EndArea();
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
                if (!_selectedCell.HasValue)
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