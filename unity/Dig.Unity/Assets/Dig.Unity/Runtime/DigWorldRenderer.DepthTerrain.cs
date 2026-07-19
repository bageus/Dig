using System;
using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.World;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private readonly TerrainDepthVolumePresenter _terrainDepthPresenter =
            new TerrainDepthVolumePresenter();

        internal void SetTerrainDepthVolume(
            TunnelNavigationVolume volume,
            string solidMaterialId,
            int hardness,
            IReadOnlyCollection<SpatialCellId> excavatedCells)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            if (excavatedCells == null)
            {
                throw new ArgumentNullException(nameof(excavatedCells));
            }

            _terrainDepthVolume = _terrainDepthPresenter.Present(
                volume,
                solidMaterialId,
                hardness,
                excavatedCells);
            RefreshChunkedTerrain();
        }
    }
}
