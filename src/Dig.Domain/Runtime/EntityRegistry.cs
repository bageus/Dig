using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Runtime
{

public static class EntityRegistryErrors
{
    public static readonly DomainError EmptyId = new DomainError(
        "runtime.entity.empty_id",
        "An empty entity id cannot be registered.");

    public static readonly DomainError AlreadyRegistered = new DomainError(
        "runtime.entity.already_registered",
        "The entity id is already registered.");

    public static readonly DomainError NotRegistered = new DomainError(
        "runtime.entity.not_registered",
        "The entity id is not registered.");

    public static readonly DomainError GenerationExhausted = new DomainError(
        "runtime.entity.generation_exhausted",
        "A unique entity id could not be generated.");
}

public readonly struct EntityRegistrySnapshot
{
    public EntityRegistrySnapshot(IReadOnlyList<EntityId> entityIds)
    {
        EntityIds = entityIds;
    }

    public IReadOnlyList<EntityId> EntityIds { get; }
}

public sealed class EntityRegistry
{
    private const string IdStreamName = "runtime.entity_ids";
    private readonly HashSet<EntityId> _entityIds;
    private readonly DeterministicRandomStream _idStream;

    public EntityRegistry(RandomStreamCatalog randomStreams)
    {
        if (randomStreams is null)
        {
            throw new ArgumentNullException(nameof(randomStreams));
        }

        _entityIds = new HashSet<EntityId>();
        _idStream = randomStreams.GetOrCreate(IdStreamName);
    }

    private EntityRegistry(
        RandomStreamCatalog randomStreams,
        IEnumerable<EntityId> entityIds)
        : this(randomStreams)
    {
        foreach (EntityId entityId in entityIds)
        {
            Result result = Register(entityId);
            if (result.IsFailure)
            {
                throw new ArgumentException(
                    result.Error!.Message,
                    nameof(entityIds));
            }
        }
    }

    public int Count => _entityIds.Count;

    public bool Contains(EntityId entityId)
    {
        return _entityIds.Contains(entityId);
    }

    public Result<EntityId> RegisterNew()
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            EntityId entityId = GenerateId();
            if (_entityIds.Add(entityId))
            {
                return Result<EntityId>.Success(entityId);
            }
        }

        return Result<EntityId>.Failure(EntityRegistryErrors.GenerationExhausted);
    }

    public Result Register(EntityId entityId)
    {
        if (entityId.IsEmpty)
        {
            return Result.Failure(EntityRegistryErrors.EmptyId);
        }

        if (!_entityIds.Add(entityId))
        {
            return Result.Failure(EntityRegistryErrors.AlreadyRegistered);
        }

        return Result.Success();
    }

    public Result Remove(EntityId entityId)
    {
        if (!_entityIds.Remove(entityId))
        {
            return Result.Failure(EntityRegistryErrors.NotRegistered);
        }

        return Result.Success();
    }

    public EntityRegistrySnapshot CaptureSnapshot()
    {
        EntityId[] orderedIds = _entityIds
            .OrderBy(entityId => entityId.ToString(), StringComparer.Ordinal)
            .ToArray();

        return new EntityRegistrySnapshot(
            new ReadOnlyCollection<EntityId>(orderedIds));
    }

    public static EntityRegistry Restore(
        RandomStreamCatalog randomStreams,
        EntityRegistrySnapshot snapshot)
    {
        if (snapshot.EntityIds is null)
        {
            throw new ArgumentException("Entity ids are required.", nameof(snapshot));
        }

        return new EntityRegistry(randomStreams, snapshot.EntityIds);
    }

    private EntityId GenerateId()
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[..8], _idStream.NextUInt64());
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[8..], _idStream.NextUInt64());

        Guid value = new Guid(bytes);
        return value == Guid.Empty
            ? GenerateId()
            : new EntityId(value);
    }
}
}
