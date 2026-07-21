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
            SpatialCellId cell,
            float offsetX,
            Vector3 worldPosition)
        {
            Cell = cell;
            OffsetX = offsetX;
            WorldPosition = worldPosition;
        }

        internal SpatialCellId Cell { get; }
        internal float OffsetX { get; }
        internal Vector3 WorldPosition { get; }
    }

    [DisallowMultipleComponent]
    public sealed class DigTunnelMovementSurface : MonoBehaviour
    {
        private readonly TunnelMovementTargetResolver _resolver =
            new TunnelMovementTargetResolver();
        private IReadOnlyList<SpatialCellId> _cells =
            Array.Empty<SpatialCellId>();

        internal DigTunnelMovementSurfaceKind Kind { get; private set; }

        internal void Configure(
            DigTunnelMovementSurfaceKind kind,
            IReadOnlyCollection<SpatialCellId> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            SpatialCellId[] ordered = cells.OrderBy(cell => cell).ToArray();
            if (ordered.Length == 0)
            {
                throw new ArgumentException(
                    "A movement surface requires at least one hidden cell.",
                    nameof(cells));
            }

            Kind = kind;
            _cells = new ReadOnlyCollection<SpatialCellId>(ordered);
        }

        internal DigTunnelMovementDestination Resolve(Vector3 hitPoint)
        {
            double logicalY = ResolveLogicalY(hitPoint);
            TunnelMovementTarget target = _resolver.Resolve(
                _cells,
                hitPoint.x,
                logicalY);
            float offsetX = (float)target.OffsetX;
            Vector3 world = DigTunnelProjection.ResidentWorldPosition(
                target.Cell.X + offsetX,
                target.Cell.Y,
                target.Cell.Z);
            return new DigTunnelMovementDestination(
                target.Cell,
                offsetX,
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
