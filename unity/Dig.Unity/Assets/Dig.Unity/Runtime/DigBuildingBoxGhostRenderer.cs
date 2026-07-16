using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigBuildingBoxGhostRenderer : MonoBehaviour
    {
        private readonly Dictionary<CellId, GameObject> _cells =
            new Dictionary<CellId, GameObject>();
        private Transform? _root;
        private Material? _validMaterial;
        private Material? _invalidMaterial;
        private GameObject? _workMarker;

        public void Render(BuildingBoxGhostViewModel preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            EnsureResources();
            HashSet<CellId> visible = new HashSet<CellId>();
            for (int index = 0; index < preview.Footprint.Count; index++)
            {
                CellId cell = preview.Footprint[index];
                visible.Add(cell);
                if (!_cells.TryGetValue(cell, out GameObject? visual))
                {
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = $"Building ghost {cell}";
                    visual.layer = 2;
                    visual.transform.SetParent(_root, worldPositionStays: false);
                    _cells.Add(cell, visual);
                }

                visual.SetActive(true);
                visual.GetComponent<Renderer>().sharedMaterial = preview.IsValid
                    ? _validMaterial
                    : _invalidMaterial;
                visual.transform.localPosition = new Vector3(cell.X, 0.16f, cell.Y);
                visual.transform.localRotation = preview.IsValid
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 45f, 0f);
                visual.transform.localScale = preview.IsValid
                    ? new Vector3(0.88f, 0.18f, 0.88f)
                    : new Vector3(0.72f, 0.46f, 0.72f);
            }

            foreach (KeyValuePair<CellId, GameObject> pair in _cells)
            {
                if (!visible.Contains(pair.Key))
                {
                    pair.Value.SetActive(false);
                }
            }

            RenderWorkMarker(preview);
        }

        public void Clear()
        {
            foreach (GameObject visual in _cells.Values)
            {
                visual.SetActive(false);
            }

            if (_workMarker != null)
            {
                _workMarker.SetActive(false);
            }
        }

        private void RenderWorkMarker(BuildingBoxGhostViewModel preview)
        {
            if (!preview.WorkPosition.HasValue)
            {
                _workMarker!.SetActive(false);
                return;
            }

            CellId cell = preview.WorkPosition.Value;
            _workMarker!.SetActive(true);
            _workMarker.name = $"Building work position {cell}";
            _workMarker.GetComponent<Renderer>().sharedMaterial = preview.IsValid
                ? _validMaterial
                : _invalidMaterial;
            _workMarker.transform.localPosition = new Vector3(cell.X, 0.24f, cell.Y);
            _workMarker.transform.localScale = new Vector3(0.24f, 0.48f, 0.24f);
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Building Placement Ghost").transform;
                _root.SetParent(transform, worldPositionStays: false);
            }

            if (_validMaterial != null && _invalidMaterial != null && _workMarker != null)
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
                throw new InvalidOperationException("No supported ghost shader was found.");
            }

            _validMaterial = new Material(shader)
            {
                name = "Dig Valid Building Ghost",
                color = new Color(0.25f, 0.82f, 0.56f, 0.58f),
            };
            _invalidMaterial = new Material(shader)
            {
                name = "Dig Invalid Building Ghost",
                color = new Color(0.92f, 0.32f, 0.28f, 0.72f),
            };
            _workMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _workMarker.layer = 2;
            _workMarker.transform.SetParent(_root, worldPositionStays: false);
            _workMarker.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_validMaterial != null)
            {
                Destroy(_validMaterial);
            }

            if (_invalidMaterial != null)
            {
                Destroy(_invalidMaterial);
            }
        }
    }
}
