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
        private readonly Material?[] _materials = new Material?[4];
        private Transform? _root;

        internal void AddRoomFloor(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            EnsureResources();
            int minX = plan.Entrance.X - ((plan.Preset.BaseWidth - 1) / 2);
            for (int z = 0; z < plan.Preset.Depth; z++)
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
                    floor.name = z == 0
                        ? $"Cave room hit target {cell}"
                        : $"Cave room floor {cell}";
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
                    ConfigureInteractionCollider(floor, plan, cell);
                    floor.GetComponent<Renderer>().enabled = z > 0;
                }
            }
        }

        internal bool TryGetCell(RaycastHit hit, out DigTunnelCellVisual cell)
        {
            cell = hit.collider == null
                ? null!
                : hit.collider.GetComponent<DigTunnelCellVisual>();
            return cell != null && _cells.Contains(cell.Cell);
        }

        private static void ConfigureInteractionCollider(
            GameObject floor,
            CaveRoomPlan plan,
            SpatialCellId destination)
        {
            int topY = destination.Y;
            for (int index = 0; index < plan.VolumeCells.Count; index++)
            {
                SpatialCellId volumeCell = plan.VolumeCells[index];
                if (volumeCell.X == destination.X
                    && volumeCell.Z == destination.Z
                    && volumeCell.Y < topY)
                {
                    topY = volumeCell.Y;
                }
            }

            float top = DigTunnelProjection.CellWorldPosition(
                new SpatialCellId(destination.X, topY, destination.Z)).y
                + DigTunnelProjection.RockCellHalfExtent;
            float bottom = DigTunnelProjection.WalkSurfaceY(destination.Y);
            Vector3 center = DigTunnelProjection.CellWorldPosition(destination);
            center.y = (top + bottom) * 0.5f;
            Vector3 size = new Vector3(
                0.94f,
                Mathf.Max(0.94f, top - bottom),
                0.50f);
            BoxCollider collider = floor.GetComponent<BoxCollider>();
            Vector3 scale = floor.transform.lossyScale;
            collider.center = floor.transform.InverseTransformPoint(center);
            collider.size = new Vector3(
                size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
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
        }
    }
}
