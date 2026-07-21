using System;
using Dig.Domain.World;
using Dig.Presentation.Jobs;
using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class DigJobVisual : MonoBehaviour
    {
        private Renderer? _renderer;
        private Collider? _interactionCollider;
        private LineRenderer? _workerLink;
        private DigOverlayManager? _overlays;
        private OverlaySemanticKind _semantic;
        private bool _selected;

        public JobOverlayViewModel Model { get; private set; } = null!;

        internal void Initialize(
            JobOverlayViewModel model,
            DigOverlayManager overlays,
            OverlaySemanticKind semantic)
        {
            Model = model;
            _overlays = overlays;
            _semantic = semantic;
            _renderer = GetComponent<Renderer>();
            _interactionCollider = GetComponent<Collider>();
            _workerLink = gameObject.AddComponent<LineRenderer>();
            _workerLink.positionCount = 2;
            _workerLink.startWidth = 0.055f;
            _workerLink.endWidth = 0.025f;
            _workerLink.useWorldSpace = true;
            _workerLink.enabled = false;
            _overlays.ConfigureLineRenderer(
                _workerLink,
                OverlayLayerKind.Jobs,
                OverlaySemanticKind.Diagnostic);
            ApplyModel(model, semantic);
        }

        internal void ApplyModel(
            JobOverlayViewModel model,
            OverlaySemanticKind semantic)
        {
            Model = model;
            _semantic = semantic;
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = !IsTerminalStatus(model.Status);
            }

            if (model.HasTarget)
            {
                Vector3 position = DigTunnelProjection.CellWorldPosition(
                    new SpatialCellId(
                        model.TargetX!.Value,
                        model.TargetY!.Value,
                        model.TargetZ!.Value));
                position.y += 0.72f;
                position.z += 0.18f;
                transform.position = position;
            }

            RefreshAppearance();
        }

        internal void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshAppearance();
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

        private void RefreshAppearance()
        {
            if (_renderer == null || _overlays == null)
            {
                return;
            }

            OverlaySemanticKind semantic = _selected
                ? OverlaySemanticKind.Selection
                : _semantic;
            DigOverlayAppearance appearance = _overlays.ConfigureRenderer(
                _renderer,
                OverlayLayerKind.Jobs,
                semantic);
            float horizontal = 0.34f * appearance.Scale;
            float vertical = appearance.Shape == OverlayShapeKind.Cross
                ? 0.14f
                : 0.08f;
            transform.localScale = new Vector3(horizontal, vertical, horizontal);
            float yaw = appearance.Shape == OverlayShapeKind.Diamond ? 45f : 0f;
            transform.localRotation = Quaternion.Euler(
                0f,
                yaw,
                appearance.TiltDegrees);
        }

        private static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, "Completed", StringComparison.Ordinal)
                || string.Equals(status, "Cancelled", StringComparison.Ordinal)
                || string.Equals(status, "Failed", StringComparison.Ordinal);
        }
    }
}
