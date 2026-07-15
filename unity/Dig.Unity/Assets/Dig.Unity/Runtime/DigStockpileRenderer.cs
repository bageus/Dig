using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigStockpileRenderer : MonoBehaviour
    {
        private Transform? _root;
        private Material? _baseMaterial;
        private Material? _itemMaterial;
        private Material? _incomingMaterial;

        internal void Render(DigStorageStatus status)
        {
            EnsureMaterials();
            RebuildRoot();
            CreateCube(
                "Stockpile Base",
                new Vector3(status.Cell.X, 0.04f, status.Cell.Y),
                new Vector3(0.92f, 0.08f, 0.92f),
                _baseMaterial!);

            int pileCount = Mathf.Min(12, Mathf.CeilToInt(status.StoredQuantity / 12f));
            for (int index = 0; index < pileCount; index++)
            {
                int column = index % 3;
                int row = (index / 3) % 2;
                int layer = index / 6;
                CreateCube(
                    $"Stored Rock {index + 1}",
                    new Vector3(
                        status.Cell.X - 0.28f + (column * 0.28f),
                        0.16f + (layer * 0.18f),
                        status.Cell.Y - 0.18f + (row * 0.36f)),
                    new Vector3(0.22f, 0.16f, 0.26f),
                    _itemMaterial!);
            }

            if (status.ReservedIncomingQuantity > 0)
            {
                CreateCube(
                    "Incoming Reservation",
                    new Vector3(status.Cell.X, 0.28f, status.Cell.Y),
                    new Vector3(0.72f, 0.04f, 0.72f),
                    _incomingMaterial!);
            }
        }

        private void EnsureMaterials()
        {
            if (_baseMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Standard");
            _baseMaterial = new Material(shader)
            {
                color = new Color(0.18f, 0.38f, 0.24f, 1f),
            };
            _itemMaterial = new Material(shader)
            {
                color = new Color(0.42f, 0.48f, 0.56f, 1f),
            };
            _incomingMaterial = new Material(shader)
            {
                color = new Color(0.92f, 0.70f, 0.18f, 1f),
            };
        }

        private void RebuildRoot()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            GameObject root = new GameObject("Dig Stockpile Visuals");
            root.transform.SetParent(transform, worldPositionStays: false);
            _root = root.transform;
        }

        private void CreateCube(
            string objectName,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(_root, worldPositionStays: true);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
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
