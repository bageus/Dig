using System;
using System.Collections.Generic;
using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigJobRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigJobVisual> _jobs =
            new Dictionary<string, DigJobVisual>();
        private readonly Dictionary<string, Material> _statusMaterials =
            new Dictionary<string, Material>();
        private Transform? _visualRoot;
        private DigAgentRenderer? _agents;
        private Material? _selectedMaterial;
        private Material? _attentionMaterial;
        private Material? _lineMaterial;
        private DigJobVisual? _selected;
        private bool _overlayVisible = true;

        public string? SelectedJobId => _selected?.Model.Id;

        public JobOverlayViewModel? SelectedModel => _selected?.Model;

        internal void Initialize(DigAgentRenderer agents)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        }

        public void Render(IReadOnlyList<JobOverlayViewModel> jobs)
        {
            EnsureResources();
            HashSet<string> visibleIds = new HashSet<string>();
            for (int index = 0; index < jobs.Count; index++)
            {
                JobOverlayViewModel model = jobs[index];
                visibleIds.Add(model.Id);
                Material statusMaterial = ResolveMaterial(model);
                if (_jobs.TryGetValue(model.Id, out DigJobVisual? visual))
                {
                    visual.ApplyModel(model, statusMaterial);
                    continue;
                }

                CreateJob(model, statusMaterial);
            }

            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, DigJobVisual> pair in _jobs)
            {
                if (!visibleIds.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            foreach (string id in removed)
            {
                DigJobVisual visual = _jobs[id];
                if (_selected == visual)
                {
                    _selected = null;
                }

                _jobs.Remove(id);
                Destroy(visual.gameObject);
            }
        }

        public bool TryGetJob(RaycastHit hit, out DigJobVisual job)
        {
            job = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigJobVisual>();
            return job != null;
        }

        public DigJobVisual? Select(DigJobVisual? job)
        {
            if (_selected != null)
            {
                _selected.SetSelected(false);
            }

            _selected = job;
            if (_selected != null)
            {
                _selected.SetSelected(true);
            }

            return _selected;
        }

        public DigJobVisual? SelectById(string id)
        {
            return _jobs.TryGetValue(id, out DigJobVisual? job)
                ? Select(job)
                : Select(null);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F3) || _visualRoot == null)
            {
                return;
            }

            _overlayVisible = !_overlayVisible;
            _visualRoot.gameObject.SetActive(_overlayVisible);
        }

        private void LateUpdate()
        {
            if (!_overlayVisible || _agents == null)
            {
                return;
            }

            foreach (DigJobVisual visual in _jobs.Values)
            {
                Vector3 workerPosition = default;
                string? agentId = visual.Model.AssignedAgentId;
                bool showLink = agentId != null
                    && visual.Model.Reservations.Count > 0
                    && _agents.TryGetWorldPosition(agentId, out workerPosition);
                visual.SetWorkerLink(showLink, workerPosition);
            }
        }

        private void CreateJob(JobOverlayViewModel model, Material statusMaterial)
        {
            if (!model.HasTarget)
            {
                return;
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Job {model.Description}";
            marker.transform.SetParent(_visualRoot, worldPositionStays: false);
            marker.transform.localScale = new Vector3(0.34f, 0.08f, 0.34f);
            DigJobVisual jobVisual = marker.AddComponent<DigJobVisual>();
            jobVisual.Initialize(
                model,
                statusMaterial,
                _selectedMaterial!,
                _lineMaterial!);
            _jobs.Add(model.Id, jobVisual);
        }

        private void EnsureResources()
        {
            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("Job Diagnostic Overlay [F3]").transform;
                _visualRoot.SetParent(transform, worldPositionStays: false);
            }

            if (_selectedMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No supported diagnostic shader was found.");
            }

            _selectedMaterial = CreateMaterial(shader, "Job Selected", Color.white);
            _attentionMaterial = CreateMaterial(
                shader,
                "Job Waiting For Decision",
                new Color(1f, 0.32f, 0.78f, 1f));
            _lineMaterial = CreateMaterial(
                shader,
                "Job Worker Link",
                new Color(0.42f, 0.85f, 1f, 1f));
            _statusMaterials.Add("Available", CreateMaterial(
                shader,
                "Job Available",
                new Color(1f, 0.75f, 0.18f, 1f)));
            _statusMaterials.Add("Claimed", CreateMaterial(
                shader,
                "Job Claimed",
                new Color(0.28f, 0.78f, 1f, 1f)));
            _statusMaterials.Add("InProgress", CreateMaterial(
                shader,
                "Job In Progress",
                new Color(0.20f, 0.92f, 0.68f, 1f)));
            _statusMaterials.Add("Blocked", CreateMaterial(
                shader,
                "Job Blocked",
                new Color(1f, 0.32f, 0.22f, 1f)));
            _statusMaterials.Add("Completed", CreateMaterial(
                shader,
                "Job Completed",
                new Color(0.42f, 0.48f, 0.42f, 1f)));
            _statusMaterials.Add("Cancelled", CreateMaterial(
                shader,
                "Job Cancelled",
                new Color(0.48f, 0.42f, 0.42f, 1f)));
            _statusMaterials.Add("Failed", CreateMaterial(
                shader,
                "Job Failed",
                new Color(0.82f, 0.18f, 0.18f, 1f)));
        }

        private Material ResolveMaterial(JobOverlayViewModel model)
        {
            if (!model.ExecutionReadiness.IsReady)
            {
                return _attentionMaterial!;
            }

            return _statusMaterials.TryGetValue(model.Status, out Material? material)
                ? material
                : _statusMaterials["Available"];
        }

        private static Material CreateMaterial(Shader shader, string name, Color color)
        {
            return new Material(shader)
            {
                name = name,
                color = color,
            };
        }

        private void OnDestroy()
        {
            foreach (Material material in _statusMaterials.Values)
            {
                Destroy(material);
            }

            if (_selectedMaterial != null)
            {
                Destroy(_selectedMaterial);
            }

            if (_attentionMaterial != null)
            {
                Destroy(_attentionMaterial);
            }

            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
        }
    }
}