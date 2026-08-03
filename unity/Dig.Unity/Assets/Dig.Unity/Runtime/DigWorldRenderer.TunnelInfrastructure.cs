using System;
using Dig.Presentation.World;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private TunnelInfrastructureVisualVolumeViewModel _tunnelInfrastructureVisuals =
            TunnelInfrastructureVisualVolumeViewModel.Empty();
        private DigTunnelInfrastructureRenderer? _tunnelInfrastructureRenderer;

        internal void SetTunnelInfrastructureVisuals(
            TunnelInfrastructureVisualVolumeViewModel visuals)
        {
            _tunnelInfrastructureVisuals = visuals
                ?? throw new ArgumentNullException(nameof(visuals));
            RefreshTunnelInfrastructureVisuals();
        }

        private void RefreshTunnelInfrastructureVisuals()
        {
            if (_tunnelInfrastructureRenderer == null
                && _tunnelInfrastructureVisuals.Instances.Count == 0)
            {
                return;
            }

            EnsureTunnelInfrastructureRenderer().Render(
                _tunnelInfrastructureVisuals);
        }

        private DigTunnelInfrastructureRenderer EnsureTunnelInfrastructureRenderer()
        {
            if (_tunnelInfrastructureRenderer != null)
            {
                return _tunnelInfrastructureRenderer;
            }

            _tunnelInfrastructureRenderer =
                GetComponent<DigTunnelInfrastructureRenderer>();
            if (_tunnelInfrastructureRenderer == null)
            {
                _tunnelInfrastructureRenderer =
                    gameObject.AddComponent<DigTunnelInfrastructureRenderer>();
            }

            return _tunnelInfrastructureRenderer;
        }
    }
}
