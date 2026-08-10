using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;
using Dig.Presentation.Navigation;
using UnityEngine;

namespace Dig.Unity
{
    internal enum DigTunnelMovementSurfaceKind
    {
        Horizontal = 0,
        Vertical = 1,
    }

    internal readonly struct DigTunnelMovementDestination
    {
        internal DigTunnelMovementDestination(
            CellId cell,
            float offsetX,
            float offsetZ,
            Vector3 worldPosition)
        {
            Cell = cell;
            OffsetX = offsetX;
            OffsetZ = offsetZ;
            WorldPosition = worldPosition;
        }

        internal CellId Cell { get; }
        internal float OffsetX { get; }
        internal float OffsetZ { get; }
        internal Vector3 WorldPosition { get; }
    }

    [DisallowMultipleComponent]
    public sealed class DigTunnelMovementSurface : MonoBehaviour
    {
        private readonly TunnelMovementTargetResolver _resolver =
            new TunnelMovementTargetResolver();
        private IReadOnlyList<CellId> _cells =
            Array.Empty<CellId>();

        internal DigTunnelMovementSurfaceKind Kind { get; private set; }

        internal void Configure(
            DigTunnelMovementSurfaceKind kind,
            IReadOnlyCollection<CellId> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            CellId[] ordered = cells.OrderBy(cell => cell).ToArray();
            if (ordered.Length == 0)
            {
                throw new ArgumentException(
                    "A movement surface requires at least one hidden cell.",
                    nameof(cells));
            }

            Kind = kind;
            _cells = new ReadOnlyCollection<CellId>(ordered);
        }

        internal DigTunnelMovementDestination Resolve(Vector3 hitPoint)
        {
            double logicalY = ResolveLogicalY(hitPoint);
            TunnelMovementTarget target = _resolver.Resolve(
                _cells,
                hitPoint.x,
                logicalY);
            float offsetX = (float)target.OffsetX;
            float offsetZ = Kind == DigTunnelMovementSurfaceKind.Horizontal
                ? Mathf.Clamp(
                    DigTunnelProjection.CellWorldPosition(target.Cell).z - hitPoint.z,
                    -(float)TunnelMovementTargetResolver.MaximumOffsetX,
                    (float)TunnelMovementTargetResolver.MaximumOffsetX)
                : 0f;
            Vector3 world = DigTunnelProjection.ResidentWorldPosition(
                target.Cell.X + offsetX,
                target.Cell.Y,
                target.Cell.Z + offsetZ);
            return new DigTunnelMovementDestination(
                target.Cell,
                offsetX,
                offsetZ,
                world);
        }

        private double ResolveLogicalY(Vector3 hitPoint)
        {
            if (Kind == DigTunnelMovementSurfaceKind.Horizontal)
            {
                return _cells[0].Y;
            }

            return -hitPoint.y;
        }
    }
}
