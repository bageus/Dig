using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigAgentRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigAgentVisual> _agents =
            new Dictionary<string, DigAgentVisual>();
        private readonly Dictionary<string, ResidentEquipmentViewModel> _equipment =
            new Dictionary<string, ResidentEquipmentViewModel>(StringComparer.Ordinal);
        private readonly HashSet<string> _selectedIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _selectionOrder = new List<string>();
        private IReadOnlyList<string> _selectedSnapshot = Array.Empty<string>();
        private Transform? _visualRoot;
        private Material? _normalMaterial;
        private Material? _selectedMaterial;
        private Material? _equipmentMaterial;
        private DigAgentVisual? _primarySelected;

        public string? SelectedAgentId => _primarySelected?.Model.Id;

        public IReadOnlyList<string> SelectedAgentIds => _selectedSnapshot;

        public int SelectedCount => _selectedSnapshot.Count;

        public AgentViewModel? SelectedModel => _primarySelected?.Model;

        public void Render(IReadOnlyList<AgentViewModel> agents, float movementDuration)
        {
            EnsureResources();
            HashSet<string> visibleIds = new HashSet<string>();
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel model = agents[index];
                visibleIds.Add(model.Id);
                if (_agents.TryGetValue(model.Id, out DigAgentVisual? visual))
                {
                    visual.SetModel(model, movementDuration);
                    visual.SetSelected(_selectedIds.Contains(model.Id));
                    continue;
                }

                CreateAgent(model);
            }

            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                if (!visibleIds.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            bool selectionChanged = false;
            foreach (string id in removed)
            {
                DigAgentVisual visual = _agents[id];
                if (_selectedIds.Remove(id))
                {
                    _selectionOrder.Remove(id);
                    selectionChanged = true;
                }

                if (_primarySelected == visual)
                {
                    _primarySelected = null;
                    selectionChanged = true;
                }

                _agents.Remove(id);
                Destroy(visual.gameObject);
            }

            if (selectionChanged || (_primarySelected == null && _selectedIds.Count > 0))
            {
                ResolvePrimarySelection();
                PublishSelectionSnapshot();
            }
        }

        public void RenderEquipment(IReadOnlyList<ResidentEquipmentViewModel> equipment)
        {
            if (equipment == null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            EnsureResources();
            _equipment.Clear();
            for (int index = 0; index < equipment.Count; index++)
            {
                ResidentEquipmentViewModel model = equipment[index];
                if (_equipment.ContainsKey(model.ResidentId))
                {
                    throw new InvalidOperationException(
                        "A resident cannot have more than one equipment visual.");
                }

                _equipment.Add(model.ResidentId, model);
            }

            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                _equipment.TryGetValue(pair.Key, out ResidentEquipmentViewModel? model);
                pair.Value.SetEquipment(model, _equipmentMaterial!);
            }
        }

        public bool TryGetAgent(RaycastHit hit, out DigAgentVisual agent)
        {
            agent = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigAgentVisual>();
            return agent != null;
        }

        public bool TryGetWorldPosition(string agentId, out Vector3 position)
        {
            if (_agents.TryGetValue(agentId, out DigAgentVisual? agent))
            {
                position = agent.transform.position;
                return true;
            }

            position = default;
            return false;
        }

        internal bool AnimateRoute(
            string agentId,
            IReadOnlyList<SpatialCellId> route,
            float stepDuration)
        {
            if (!_agents.TryGetValue(agentId, out DigAgentVisual? agent))
            {
                return false;
            }

            agent.PlayRoute(route, stepDuration);
            return true;
        }

        public DigAgentVisual? Select(DigAgentVisual? agent)
        {
            ClearSelection();
            if (agent == null)
            {
                return null;
            }

            AddSelection(agent);
            return _primarySelected;
        }

        public DigAgentVisual? ToggleSelection(DigAgentVisual agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            string id = agent.Model.Id;
            if (_selectedIds.Remove(id))
            {
                _selectionOrder.Remove(id);
                agent.SetSelected(false);
                if (_primarySelected == agent)
                {
                    _primarySelected = null;
                    ResolvePrimarySelection();
                }
            }
            else
            {
                AddSelection(agent);
            }

            PublishSelectionSnapshot();
            return _primarySelected;
        }

        public void ClearSelection()
        {
            for (int index = 0; index < _selectionOrder.Count; index++)
            {
                if (_agents.TryGetValue(
                    _selectionOrder[index],
                    out DigAgentVisual? selected))
                {
                    selected.SetSelected(false);
                }
            }

            _selectedIds.Clear();
            _selectionOrder.Clear();
            _primarySelected = null;
            PublishSelectionSnapshot();
        }

        public DigAgentVisual? SelectById(string id)
        {
            return _agents.TryGetValue(id, out DigAgentVisual? agent)
                ? Select(agent)
                : Select(null);
        }

        private void AddSelection(DigAgentVisual agent)
        {
            string id = agent.Model.Id;
            if (_selectedIds.Add(id))
            {
                _selectionOrder.Add(id);
                agent.SetSelected(true);
            }

            _primarySelected = agent;
            PublishSelectionSnapshot();
        }

        private void ResolvePrimarySelection()
        {
            for (int index = _selectionOrder.Count - 1; index >= 0; index--)
            {
                if (_agents.TryGetValue(
                    _selectionOrder[index],
                    out DigAgentVisual? selected))
                {
                    _primarySelected = selected;
                    return;
                }
            }

            _primarySelected = null;
        }

        private void PublishSelectionSnapshot()
        {
            _selectedSnapshot = _selectionOrder.Count == 0
                ? Array.Empty<string>()
                : new ReadOnlyCollection<string>(_selectionOrder.ToArray());
        }

        private void CreateAgent(AgentViewModel model)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = $"Resident {model.Name}";
            visual.transform.SetParent(_visualRoot, worldPositionStays: false);
            visual.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);
            DigAgentVisual agentVisual = visual.AddComponent<DigAgentVisual>();
            agentVisual.Initialize(model, _normalMaterial!, _selectedMaterial!);
            agentVisual.SetSelected(_selectedIds.Contains(model.Id));
            _equipment.TryGetValue(model.Id, out ResidentEquipmentViewModel? equipment);
            agentVisual.SetEquipment(equipment, _equipmentMaterial!);
            _agents.Add(model.Id, agentVisual);
        }

        private void EnsureResources()
        {
            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("Resident Visuals").transform;
                _visualRoot.SetParent(transform, worldPositionStays: false);
            }

            if (_normalMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No supported resident shader was found.");
            }

            _normalMaterial = new Material(shader)
            {
                name = "Dig Resident",
                color = new Color(0.25f, 0.72f, 0.82f, 1f),
            };
            _selectedMaterial = new Material(shader)
            {
                name = "Dig Resident Selected",
                color = new Color(1f, 0.78f, 0.22f, 1f),
            };
            _equipmentMaterial = new Material(shader)
            {
                name = "Dig Resident Equipment",
                color = new Color(0.38f, 0.28f, 0.18f, 1f),
            };
        }

        private void OnDestroy()
        {
            if (_normalMaterial != null)
            {
                Destroy(_normalMaterial);
            }

            if (_selectedMaterial != null)
            {
                Destroy(_selectedMaterial);
            }

            if (_equipmentMaterial != null)
            {
                Destroy(_equipmentMaterial);
            }
        }
    }
}
