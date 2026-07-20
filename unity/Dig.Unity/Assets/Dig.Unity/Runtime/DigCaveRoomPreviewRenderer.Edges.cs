using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private static Vector2Int[] CreateBoxConnections()
        {
            return new[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 0),
                new Vector2Int(4, 5),
                new Vector2Int(5, 6),
                new Vector2Int(6, 7),
                new Vector2Int(7, 4),
                new Vector2Int(0, 4),
                new Vector2Int(1, 5),
                new Vector2Int(2, 6),
                new Vector2Int(3, 7),
            };
        }
    }
}
