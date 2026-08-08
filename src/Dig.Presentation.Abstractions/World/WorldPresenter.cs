using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.World;
using Dig.Domain.Exploration;

namespace Dig.Presentation.World
{

public sealed class WorldPresenter
{
    private readonly IQueryHandler<GetWorldSnapshotQuery, WorldSnapshot> _queryHandler;
    private readonly Func<CellId, CellVisibility>? _visibility;

    public WorldPresenter(
        IQueryHandler<GetWorldSnapshotQuery, WorldSnapshot> queryHandler,
        Func<CellId, CellVisibility>? visibility = null)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        _visibility = visibility;
    }

    public WorldViewModel Load()
    {
        WorldSnapshot snapshot = _queryHandler.Handle(new GetWorldSnapshotQuery());
        List<WorldChunkViewModel> chunks = new List<WorldChunkViewModel>(snapshot.Chunks.Count);
        foreach (ChunkSnapshot chunk in snapshot.Chunks)
        {
            chunks.Add(PresentChunk(chunk));
        }

        return new WorldViewModel(
            snapshot.Size.Width,
            snapshot.Size.Height,
            snapshot.Size.Depth,
            snapshot.ChunkSize,
            snapshot.Version,
            chunks);
    }

    private WorldChunkViewModel PresentChunk(ChunkSnapshot chunk)
    {
        List<WorldCellViewModel> cells = new List<WorldCellViewModel>(chunk.Cells.Count);
        foreach (CellSnapshot cell in chunk.Cells)
        {
            cells.Add(new WorldCellViewModel(
                cell.Id.X,
                cell.Id.Y,
                cell.Id.Z,
                cell.State.MaterialId.ToString(),
                cell.IsSolid,
                cell.State.IsExplored,
                cell.State.Designation != CellDesignation.None,
                cell.Hardness,
                cell.State.Damage,
                cell.State.Temperature,
                cell.WorldVersion,
                cell.State.CompletedExcavationQuarters,
                cell.State.ExcavationCutPattern,
                _visibility?.Invoke(cell.Id) ?? (cell.State.IsExplored
                    ? CellVisibility.Visible : CellVisibility.Unexplored)));
        }

        return new WorldChunkViewModel(
            chunk.Id.X,
            chunk.Id.Y,
            chunk.Id.Z,
            chunk.ChunkVersion,
            cells);
    }
}
}
