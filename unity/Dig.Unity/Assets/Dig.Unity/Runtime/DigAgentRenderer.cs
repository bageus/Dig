using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigAgentRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigAgentVisual> _agents =
            new Dictionary<string, DigAgentVisual>();
        private Transform? _visualRoot;
        private Material? _normalMaterial;
        private Material? _selectedMaterial;
        private DigAgentVisual? _selected;

        public string? SelectedAgentId => _selected?.Model.Id;

        public AgentViewModel? SelectedModel => _selected?.Model;

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

            foreach (string id in removed)
            {
                DigAgentVisual visual = _agents[id];
                if (_selected == visual)
                {
                    _selected = null;
                }

                _agents.Remove(id);
                Destroy(visual.gameObject);
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

        public DigAgentVisual? Select(DigAgentVisual? agent)
        {
            if (_selected != null)
            {
                _selected.SetSelected(false);
            }

            _selected = agent;
            if (_selected != null)
            {
                _selected.SetSelected(true);
            }

            return _selected;
        }

        public DigAgentVisual? SelectById(string id)
        {
            return _agents.TryGetValue(id, out DigAgentVisual? agent)
                ? Select(agent)
                : Select(null);
        }

        private void CreateAgent(AgentViewModel model)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = $"Resident {model.Name}";
            visual.transform.SetParent(_visualRoot, worldPositionStays: false);
            visual.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);
            DigAgentVisual agentVisual = visual.AddComponent<DigAgentVisual>();
            agentVisual.Initialize(model, _normalMaterial!, _selectedMaterial!);
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
        }
    }
}