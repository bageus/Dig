using System;
using System.Collections.Generic;
using Dig.Presentation.Jobs;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigJobRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, DigJobVisual> _jobs =
            new Dictionary<string, DigJobVisual>();
        private Transform? _visualRoot;
        private DigAgentRenderer? _agents;
        private DigOverlayManager? _overlays;
        private DigJobVisual? _selected;

        public string? SelectedJobId => _selected?.Model.Id;

        public JobOverlayViewModel? SelectedModel => _selected?.Model;

        internal void Initialize(
            DigAgentRenderer agents,
            DigOverlayManager overlays)
        {
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _overlays = overlays ?? throw new ArgumentNullException(nameof(overlays));
        }

        public void Render(IReadOnlyList<JobOverlayViewModel> jobs)
        {
            EnsureResources();
            HashSet<string> visibleIds = new HashSet<string>();
            for (int index = 0; index < jobs.Count; index++)
            {
                JobOverlayViewModel model = jobs[index];
                visibleIds.Add(model.Id);
                OverlaySemanticKind semantic = ResolveSemantic(model);
                if (_jobs.TryGetValue(model.Id, out DigJobVisual? visual))
                {
                    visual.ApplyModel(model, semantic);
                    continue;
                }

                CreateJob(model, semantic);
            }

            RemoveMissing(visibleIds);
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

        private void LateUpdate()
        {
            if (_overlays == null
                || !_overlays.IsVisible(OverlayLayerKind.Jobs)
                || _agents == null)
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

        private void CreateJob(
            JobOverlayViewModel model,
            OverlaySemanticKind semantic)
        {
            if (!model.HasTarget)
            {
                return;
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Job {model.Description}";
            marker.transform.SetParent(_visualRoot, worldPositionStays: false);
            DigJobVisual jobVisual = marker.AddComponent<DigJobVisual>();
            jobVisual.Initialize(model, _overlays!, semantic);
            _jobs.Add(model.Id, jobVisual);
        }

        private void RemoveMissing(HashSet<string> visibleIds)
        {
            List<string> removed = new List<string>();
            foreach (KeyValuePair<string, DigJobVisual> pair in _jobs)
            {
                if (!visibleIds.Contains(pair.Key))
                {
                    removed.Add(pair.Key);
                }
            }

            for (int index = 0; index < removed.Count; index++)
            {
                string id = removed[index];
                DigJobVisual visual = _jobs[id];
                if (_selected == visual)
                {
                    _selected = null;
                }

                _jobs.Remove(id);
                Destroy(visual.gameObject);
            }
        }

        private void EnsureResources()
        {
            if (_overlays == null)
            {
                throw new InvalidOperationException(
                    "Job renderer requires DigOverlayManager.");
            }

            if (_visualRoot != null)
            {
                return;
            }

            _visualRoot = new GameObject("Job Overlay").transform;
            _visualRoot.SetParent(transform, worldPositionStays: false);
            _overlays.RegisterLayer(OverlayLayerKind.Jobs, _visualRoot);
        }

        private static OverlaySemanticKind ResolveSemantic(
            JobOverlayViewModel model)
        {
            if (!model.ExecutionReadiness.IsReady)
            {
                return OverlaySemanticKind.JobAttention;
            }

            return model.Status switch
            {
                "Claimed" => OverlaySemanticKind.JobClaimed,
                "InProgress" => OverlaySemanticKind.JobInProgress,
                "Blocked" => OverlaySemanticKind.JobBlocked,
                "Completed" => OverlaySemanticKind.JobTerminal,
                "Cancelled" => OverlaySemanticKind.JobTerminal,
                "Failed" => OverlaySemanticKind.JobTerminal,
                _ => OverlaySemanticKind.JobAvailable,
            };
        }

        private void OnDestroy()
        {
            if (_visualRoot != null && _overlays != null)
            {
                _overlays.UnregisterLayer(
                    OverlayLayerKind.Jobs,
                    _visualRoot);
            }
        }
    }
}
