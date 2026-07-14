using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigJobVisual : MonoBehaviour
    {
        private Renderer? _renderer;
        private LineRenderer? _workerLink;
        private Material? _statusMaterial;
        private Material? _selectedMaterial;
        private bool _selected;

        public JobOverlayViewModel Model { get; private set; } = null!;

        internal void Initialize(
            JobOverlayViewModel model,
            Material statusMaterial,
            Material selectedMaterial,
            Material lineMaterial)
        {
            Model = model;
            _statusMaterial = statusMaterial;
            _selectedMaterial = selectedMaterial;
            _renderer = GetComponent<Renderer>();
            _workerLink = gameObject.AddComponent<LineRenderer>();
            _workerLink.sharedMaterial = lineMaterial;
            _workerLink.positionCount = 2;
            _workerLink.startWidth = 0.055f;
            _workerLink.endWidth = 0.025f;
            _workerLink.useWorldSpace = true;
            _workerLink.enabled = false;
            ApplyModel(model, statusMaterial);
        }

        internal void ApplyModel(JobOverlayViewModel model, Material statusMaterial)
        {
            Model = model;
            _statusMaterial = statusMaterial;
            if (model.HasTarget)
            {
                transform.position = new Vector3(
                    model.TargetX!.Value,
                    1.05f,
                    model.TargetY!.Value);
            }

            RefreshMaterial();
        }

        internal void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshMaterial();
        }

        internal void SetWorkerLink(bool visible, Vector3 workerPosition)
        {
            if (_workerLink == null)
            {
                return;
            }

            _workerLink.enabled = visible;
            if (!visible)
            {
                return;
            }

            _workerLink.SetPosition(0, transform.position + (Vector3.up * 0.25f));
            _workerLink.SetPosition(1, workerPosition + (Vector3.up * 0.45f));
        }

        private void RefreshMaterial()
        {
            if (_renderer != null)
            {
                _renderer.sharedMaterial = _selected
                    ? _selectedMaterial
                    : _statusMaterial;
            }
        }
    }
}