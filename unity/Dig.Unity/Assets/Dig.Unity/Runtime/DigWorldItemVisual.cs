using System;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigWorldItemVisual : MonoBehaviour
    {
        public WorldItemViewModel Model { get; private set; } = null!;

        public void Configure(WorldItemViewModel model, Material material)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            Renderer targetRenderer = GetComponent<Renderer>();
            targetRenderer.sharedMaterial = material;
            gameObject.layer = model.IsInteractive ? 0 : 2;
            float scale = model.IsBuildingBox
                ? 0.58f
                : 0.22f + (Mathf.Clamp(model.Quantity, 1, 50) * 0.006f);
            name = model.IsBuildingBox
                ? $"BuildingBox {model.StackId}"
                : $"World item {model.ItemId} x{model.Quantity}";
            transform.localPosition = new Vector3(model.CellX, 0.24f, model.CellY);
            transform.localScale = model.IsBuildingBox
                ? new Vector3(scale, scale * 0.72f, scale)
                : new Vector3(scale, scale, scale);
        }
    }
}
