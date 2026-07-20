using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigBuildingDetailGroup : MonoBehaviour
    {
        [SerializeField]
        private TerrainVisualDetailLevel minimumDetail =
            TerrainVisualDetailLevel.Marker;

        internal TerrainVisualDetailLevel MinimumDetail => minimumDetail;

        internal void Configure(TerrainVisualDetailLevel value)
        {
            minimumDetail = value;
        }

        internal void SetDetailLevel(TerrainVisualDetailLevel value)
        {
            gameObject.SetActive(value >= minimumDetail);
        }
    }
}
