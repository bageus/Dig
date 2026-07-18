using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCaveRoomFloorRenderer : MonoBehaviour
    {
        private readonly HashSet<SpatialCellId> _cells = new HashSet<SpatialCellId>();
        private readonly Dictionary<string, GameObject> _backWalls =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Material?[] _materials = new Material?[4];
        private Transform? _root;
        private Material? _backWallMaterial;

        internal void AddRoomFloor(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            EnsureResources();
            int minX = plan.Entrance.X - ((plan.Preset.BaseWidth - 1) / 2);
            for (int z = 1; z < plan.Preset.Depth; z++)
            {
                for (int offset = 0; offset < plan.Preset.BaseWidth; offset++)
                {
                    SpatialCellId cell = new SpatialCellId(
                        minX + offset,
                        plan.Entrance.Y,
                        z);
                    if (!_cells.Add(cell))
                    {
                        continue;
                    }

                    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.name = $"Cave room floor {cell}";
                    floor.transform.SetParent(_root, worldPositionStays: true);
                    floor.transform.SetPositionAndRotation(
                        DigTunnelProjection.FloorWorldPosition(cell),
                        Quaternion.identity);
                    floor.transform.localScale = new Vector3(
                        0.84f,
                        DigTunnelProjection.FloorThickness,
                        DigTunnelProjection.FloorDepth);
                    DigTunnelCellVisual visual = floor.AddComponent<DigTunnelCellVisual>();
                    visual.Configure(
                        cell,
                        isVerticalTunnel: false,
                        material: _materials[z]!);
                }
            }

            ReplaceBackWall(plan);
        }

        internal bool TryGetCell(RaycastHit hit, out DigTunnelCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigTunnelCellVisual>();
            return cell != null && _cells.Contains(cell.Cell);
        }

        private void ReplaceBackWall(CaveRoomPlan plan)
        {
            string key = $"{plan.Entrance.X}:{plan.Entrance.Y}";
            if (_backWalls.TryGetValue(key, out GameObject? existing))
            {
                Destroy(existing);
            }

            int minX = plan.Entrance.X - ((plan.Preset.BaseWidth - 1) / 2);
            float centerX = minX + ((plan.Preset.BaseWidth - 1) * 0.5f);
            float floorY = DigTunnelProjection.WalkSurfaceY(plan.Entrance.Y);
            float ceilingY = DigTunnelProjection.WalkSurfaceY(
                plan.Entrance.Y - plan.Preset.Height);
            float centerY = (floorY + ceilingY) * 0.5f;
            float wallHeight = Mathf.Abs(floorY - ceilingY);
            Vector3 deepest = DigTunnelProjection.CellWorldPosition(
                new SpatialCellId(
                    plan.Entrance.X,
                    plan.Entrance.Y,
                    plan.Preset.Depth - 1));
            float backZ = deepest.z + (DigTunnelProjection.DepthSpacing * 0.55f);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Cave room back wall {key}";
            wall.transform.SetParent(_root, worldPositionStays: true);
            wall.transform.SetPositionAndRotation(
                new Vector3(centerX, centerY, backZ),
                Quaternion.identity);
            wall.transform.localScale = new Vector3(
                plan.Preset.BaseWidth,
                wallHeight,
                0.10f);
            wall.GetComponent<Renderer>().sharedMaterial = _backWallMaterial;
            Collider collider = wall.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _backWalls[key] = wall;
        }

        private void EnsureResources()
        {
            if (_root == null)
            {
                _root = new GameObject("Completed Cave Room Floors").transform;
                _root.SetParent(transform, worldPositionStays: true);
                _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            if (_materials[0] != null)
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
                    "Cave room floors require a supported lit shader.");
            }

            Color[] colors =
            {
                new Color(0.20f, 0.52f, 0.66f, 1f),
                new Color(0.25f, 0.62f, 0.46f, 1f),
                new Color(0.70f, 0.50f, 0.22f, 1f),
                new Color(0.55f, 0.34f, 0.68f, 1f),
            };
            for (int index = 0; index < _materials.Length; index++)
            {
                _materials[index] = new Material(shader)
                {
                    name = $"Cave room depth {index}",
                    color = colors[index],
                };
            }

            _backWallMaterial = new Material(shader)
            {
                name = "Excavated cave room back wall",
                color = new Color(0.30f, 0.27f, 0.25f, 1f),
            };
        }

        private void OnDestroy()
        {
            for (int index = 0; index < _materials.Length; index++)
            {
                if (_materials[index] != null)
                {
                    Destroy(_materials[index]);
                }
            }

            if (_backWallMaterial != null)
            {
                Destroy(_backWallMaterial);
            }
        }
    }
}
