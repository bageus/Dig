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
        private Transform? _root;
        private bool _digInteractionActive;

        internal void AddRoomFloor(CaveRoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            EnsureRoot();
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

                    GameObject floor = new GameObject($"Cave room dig proxy {cell}");
                    floor.transform.SetParent(_root, worldPositionStays: true);
                    floor.transform.SetPositionAndRotation(
                        DigTunnelProjection.FloorWorldPosition(cell),
                        Quaternion.identity);
                    BoxCollider collider = floor.AddComponent<BoxCollider>();
                    DigTunnelCellVisual visual = floor.AddComponent<DigTunnelCellVisual>();
                    visual.Configure(
                        cell,
                        isVerticalTunnel: false);
                    ConfigureInteractionCollider(floor, plan, cell);
                    collider.enabled = _digInteractionActive;
                }
            }
        }

        internal void SetDigInteractionActive(bool active)
        {
            _digInteractionActive = active;
            if (_root == null)
            {
                return;
            }

            BoxCollider[] colliders = _root.GetComponentsInChildren<BoxCollider>();
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = active;
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

        private void EnsureRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("Completed Cave Room Floors").transform;
                _root.SetParent(transform, worldPositionStays: true);
                _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
    }
}
