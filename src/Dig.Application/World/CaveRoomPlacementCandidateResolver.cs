using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public static class CaveRoomPlacementCandidateResolver
{
    public static IReadOnlyList<CellId> Resolve(
        CaveRoomPreset preset,
        CellId pointerCell)
    {
        if (preset == null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        List<Candidate> values = new List<Candidate>();
        for (int level = 0; level < preset.Height; level++)
        {
            int entranceY = checked(pointerCell.Y + level);
            int minimumAnchor = pointerCell.X - preset.BaseWidth;
            int maximumAnchor = pointerCell.X + preset.BaseWidth;
            for (int anchorX = minimumAnchor; anchorX <= maximumAnchor; anchorX++)
            {
                CaveRoomRowProfile row = CaveRoomPlanner.ResolveRowProfile(
                    preset,
                    anchorX,
                    level);
                if (!row.RequiredQuartersByX.ContainsKey(pointerCell.X))
                {
                    continue;
                }

                int centerDistance2 = Math.Abs(
                    row.LeftBoundary2 + row.RightBoundary2 - (pointerCell.X * 4));
                values.Add(new Candidate(
                    new CellId(anchorX, entranceY, CellId.MinimumDepth),
                    level,
                    centerDistance2));
            }
        }

        return new ReadOnlyCollection<CellId>(values
            .OrderBy(value => value.VerticalOffset)
            .ThenBy(value => value.CenterDistance2)
            .ThenBy(value => value.Entrance.X)
            .Select(value => value.Entrance)
            .Distinct()
            .ToArray());
    }

    private readonly struct Candidate
    {
        public Candidate(CellId entrance, int verticalOffset, int centerDistance2)
        {
            Entrance = entrance;
            VerticalOffset = verticalOffset;
            CenterDistance2 = centerDistance2;
        }

        public CellId Entrance { get; }
        public int VerticalOffset { get; }
        public int CenterDistance2 { get; }
    }
}

}
