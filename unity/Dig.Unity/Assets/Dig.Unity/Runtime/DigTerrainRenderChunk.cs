using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dig.Unity
{
    internal sealed class DigTerrainRenderChunk
    {
        internal DigTerrainRenderChunk(
            DigTerrainChunkKey key,
            long version,
            IReadOnlyCollection<DigTerrainRenderCell> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            Key = key;
            Version = version;
            Cells = new ReadOnlyCollection<DigTerrainRenderCell>(
                new List<DigTerrainRenderCell>(cells));
        }

        internal DigTerrainChunkKey Key { get; }
        internal long Version { get; }
        internal IReadOnlyList<DigTerrainRenderCell> Cells { get; }
    }
}
