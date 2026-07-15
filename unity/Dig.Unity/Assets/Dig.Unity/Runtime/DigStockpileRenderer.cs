using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigStockpileRenderer : MonoBehaviour
    {
        private const int MaximumPileVisuals = 12;
        private readonly List<GameObject> _pileVisuals =
            new List<GameObject>(MaximumPileVisuals);
        private Transform? _root;
        private GameObject? _baseVisual;
        private GameObject? _incomingVisual;
        private Material? _baseMaterial;
        private Material? _itemMaterial;
        private Material? _incomingMaterial;

        internal void Render(DigStorageStatus status)
        {
            EnsureResources();
            ApplyTransform(
                _baseVisual!,
                new Vector3(status.Cell.X, 0.04f, status.Cell.Y),
                new Vector3(0.92f, 0.08f, 0.92f));

            int pileCount = Mathf.Min(
                MaximumPileVisuals,
                Mathf.CeilToInt(status.StoredQuantity / 12f));
            for (int index = 0; index < _pileVisuals.Count; index++)
            {
                GameObject pile = _pileVisuals[index];
                bool visible = index < pileCount;
                pile.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                int column = index % 3;
                int row = (index / 3) % 2;
                int layer = index / 6;
                ApplyTransform(
                    pile,
                    new Vector3(
                        status.Cell.X - 0.28f + (column * 0.28f),
                        0.16f + (layer * 0.18f),
                        status.Cell.Y - 0.18f + (row * 0.36f)),
                    new Vector3(0.22f, 0.16f, 0.26f));
            }

            bool hasIncoming = status.ReservedIncomingQuantity > 0;
            _incomingVisual!.SetActive(hasIncoming);
            if (hasIncoming)
            {
                ApplyTransform(
                    _incomingVisual,
                    new Vector3(status.Cell.X, 0.28f, status.Cell.Y),
                    new Vector3(0.72f, 0.04f, 0.72f));
            }
        }

        private void EnsureResources()
        {
            EnsureMaterials();
            if (_root != null)
            {
                return;
            }

            GameObject root = new GameObject("Dig Stockpile Visuals");
            root.transform.SetParent(transform, worldPositionStays: false);
            _root = root.transform;
            _baseVisual = CreateCube("Stockpile Base", _baseMaterial!);
            for (int index = 0; index < MaximumPileVisuals; index++)
            {
                GameObject pile = CreateCube(
                    $"Stored Rock {index + 1}",
                    _itemMaterial!);
                pile.SetActive(false);
                _pileVisuals.Add(pile);
            }

            _incomingVisual = CreateCube(
                "Incoming Reservation",
                _incomingMaterial!);
            _incomingVisual.SetActive(false);
        }

        private void EnsureMaterials()
        {
            if (_baseMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No supported stockpile shader was found.");
            }

            _baseMaterial = CreateMaterial(
                shader,
                "Dig Stockpile Base",
                new Color(0.18f, 0.38f, 0.24f, 1f));
            _itemMaterial = CreateMaterial(
                shader,
                "Dig Stored Resource",
                new Color(0.42f, 0.48f, 0.56f, 1f));
            _incomingMaterial = CreateMaterial(
                shader,
                "Dig Incoming Storage",
                new Color(0.92f, 0.70f, 0.18f, 1f));
        }

        private GameObject CreateCube(string objectName, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(_root, worldPositionStays: true);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return cube;
        }

        private static void ApplyTransform(
            GameObject visual,
            Vector3 position,
            Vector3 scale)
        {
            visual.transform.position = position;
            visual.transform.localScale = scale;
        }

        private static Material CreateMaterial(
            Shader shader,
            string materialName,
            Color color)
        {
            return new Material(shader)
            {
                name = materialName,
                color = color,
            };
        }

        private void OnDestroy()
        {
            DestroyMaterial(_baseMaterial);
            DestroyMaterial(_itemMaterial);
            DestroyMaterial(_incomingMaterial);
        }

        private static void DestroyMaterial(Material? material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
