using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public sealed class WorldPresenter
{
    private readonly IQueryHandler<GetWorldSnapshotQuery, WorldSnapshot> _queryHandler;

    public WorldPresenter(
        IQueryHandler<GetWorldSnapshotQuery, WorldSnapshot> queryHandler)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
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

    private static WorldChunkViewModel PresentChunk(ChunkSnapshot chunk)
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
                cell.WorldVersion));
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
