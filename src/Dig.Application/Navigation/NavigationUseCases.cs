using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Navigation
{

public interface INavigationRepository
{
    NavigationMap? Get(TraversalProfileId profileId);

    void Save(NavigationMap map);
}

public sealed class RebuildNavigationCommand
    : ICommand<Result<NavigationUpdateDiagnostics>>
{
    public RebuildNavigationCommand(
        TraversalProfile profile,
        WorldSnapshot world,
        IReadOnlyCollection<TraversalLink> links)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public TraversalProfile Profile { get; }

    public WorldSnapshot World { get; }

    public IReadOnlyCollection<TraversalLink> Links { get; }
}

public sealed class RebuildNavigationCommandHandler
    : ICommandHandler<RebuildNavigationCommand, Result<NavigationUpdateDiagnostics>>
{
    private readonly INavigationRepository _repository;

    public RebuildNavigationCommandHandler(INavigationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<NavigationUpdateDiagnostics> Handle(
        RebuildNavigationCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        NavigationMap map = new NavigationMap(command.Profile);
        Result<NavigationUpdateDiagnostics> result = map.Rebuild(
            command.World,
            command.Links);
        if (result.IsSuccess)
        {
            _repository.Save(map);
        }

        return result;
    }
}

public sealed class RefreshNavigationCommand
    : ICommand<Result<NavigationUpdateDiagnostics>>
{
    public RefreshNavigationCommand(
        TraversalProfileId profileId,
        WorldSnapshot world,
        IReadOnlyCollection<ChunkId> invalidatedChunks,
        IReadOnlyCollection<TraversalLink> links)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException("Traversal profile id cannot be empty.", nameof(profileId));
        }

        ProfileId = profileId;
        World = world ?? throw new ArgumentNullException(nameof(world));
        InvalidatedChunks = invalidatedChunks
            ?? throw new ArgumentNullException(nameof(invalidatedChunks));
        Links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public TraversalProfileId ProfileId { get; }

    public WorldSnapshot World { get; }

    public IReadOnlyCollection<ChunkId> InvalidatedChunks { get; }

    public IReadOnlyCollection<TraversalLink> Links { get; }
}

public sealed class RefreshNavigationCommandHandler
    : ICommandHandler<RefreshNavigationCommand, Result<NavigationUpdateDiagnostics>>
{
    private readonly INavigationRepository _repository;

    public RefreshNavigationCommandHandler(INavigationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<NavigationUpdateDiagnostics> Handle(
        RefreshNavigationCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        NavigationMap? map = _repository.Get(command.ProfileId);
        if (map is null)
        {
            return Result<NavigationUpdateDiagnostics>.Failure(
                NavigationErrors.MapNotFound);
        }

        Result<NavigationUpdateDiagnostics> result = map.Refresh(
            command.World,
            command.InvalidatedChunks,
            command.Links);
        if (result.IsSuccess)
        {
            _repository.Save(map);
        }

        return result;
    }
}

public sealed class FindPathQuery : IQuery<Result<PathResult>>
{
    public FindPathQuery(
        TraversalProfileId profileId,
        PathRequest request)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException("Traversal profile id cannot be empty.", nameof(profileId));
        }

        ProfileId = profileId;
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public TraversalProfileId ProfileId { get; }

    public PathRequest Request { get; }
}

public sealed class FindPathQueryHandler
    : IQueryHandler<FindPathQuery, Result<PathResult>>
{
    private readonly INavigationRepository _repository;
    private readonly NavigationPathfinder _pathfinder;

    public FindPathQueryHandler(
        INavigationRepository repository,
        NavigationPathfinder pathfinder)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
    }

    public Result<PathResult> Handle(FindPathQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        NavigationMap? map = _repository.Get(query.ProfileId);
        if (map is null)
        {
            return Result<PathResult>.Failure(NavigationErrors.MapNotFound);
        }

        Result<NavigationSnapshot> snapshot = map.GetSnapshot();
        if (snapshot.IsFailure)
        {
            return Result<PathResult>.Failure(snapshot.Error!);
        }

        return Result<PathResult>.Success(
            _pathfinder.FindPath(snapshot.Value, query.Request));
    }
}

public sealed class GetNavigationSnapshotQuery
    : IQuery<Result<NavigationSnapshot>>
{
    public GetNavigationSnapshotQuery(TraversalProfileId profileId)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException("Traversal profile id cannot be empty.", nameof(profileId));
        }

        ProfileId = profileId;
    }

    public TraversalProfileId ProfileId { get; }
}

public sealed class GetNavigationSnapshotQueryHandler
    : IQueryHandler<GetNavigationSnapshotQuery, Result<NavigationSnapshot>>
{
    private readonly INavigationRepository _repository;

    public GetNavigationSnapshotQueryHandler(INavigationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result<NavigationSnapshot> Handle(GetNavigationSnapshotQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        NavigationMap? map = _repository.Get(query.ProfileId);
        return map is null
            ? Result<NavigationSnapshot>.Failure(NavigationErrors.MapNotFound)
            : map.GetSnapshot();
    }
}
}
