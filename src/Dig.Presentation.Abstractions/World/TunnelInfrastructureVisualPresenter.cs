using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public sealed class TunnelInfrastructureVisualPresenter
{
    public TunnelInfrastructureVisualVolumeViewModel Present(
        TunnelInfrastructureSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        HashSet<CellId> supportCells = new HashSet<CellId>();
        for (int segmentIndex = 0;
            segmentIndex < snapshot.Segments.Count;
            segmentIndex++)
        {
            HorizontalTunnelSegmentSnapshot segment = snapshot.Segments[segmentIndex];
            for (int anchorIndex = 0;
                anchorIndex < segment.StructuralAnchors.Count;
                anchorIndex++)
            {
                TunnelStructuralAnchorSnapshot anchor =
                    segment.StructuralAnchors[anchorIndex];
                if (anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport)
                {
                    supportCells.Add(anchor.Cell);
                }
            }
        }

        List<TunnelInfrastructureVisualViewModel> instances =
            new List<TunnelInfrastructureVisualViewModel>(
                supportCells.Count
                + snapshot.CompletedJunctionStoneTrimCells.Count
                + snapshot.CompletedStoneFloorTrimCells.Count);
        foreach (CellId cell in supportCells)
        {
            instances.Add(new TunnelInfrastructureVisualViewModel(
                CreateInstanceId("wooden-support", cell),
                TunnelInfrastructureVisualKind.WoodenSupport,
                cell));
        }

        HashSet<CellId> trimCells = new HashSet<CellId>(
            snapshot.CompletedJunctionStoneTrimCells);
        foreach (CellId cell in trimCells)
        {
            instances.Add(new TunnelInfrastructureVisualViewModel(
                CreateInstanceId("junction-stone-trim", cell),
                TunnelInfrastructureVisualKind.JunctionStoneTrim,
                cell));
        }

        HashSet<CellId> floorTrimCells = new HashSet<CellId>(
            snapshot.CompletedStoneFloorTrimCells);
        foreach (CellId cell in floorTrimCells)
        {
            instances.Add(new TunnelInfrastructureVisualViewModel(
                CreateInstanceId("stone-floor-trim", cell),
                TunnelInfrastructureVisualKind.StoneFloorTrim,
                cell));
        }

        return new TunnelInfrastructureVisualVolumeViewModel(
            snapshot.Version,
            instances);
    }

    private static string CreateInstanceId(string kind, CellId cell)
    {
        return $"tunnel:{kind}:{cell.X}:{cell.Y}:{cell.Z}";
    }
}

}
