using Dig.Domain.Core;

namespace Dig.Domain.Navigation;

public static class NavigationErrors
{
    public static readonly DomainError NotBuilt = new DomainError(
        "navigation.not_built",
        "The navigation map has not been built yet.");

    public static readonly DomainError WorldLayoutMismatch = new DomainError(
        "navigation.world.layout_mismatch",
        "The world snapshot layout does not match the navigation map.");

    public static readonly DomainError MissingWorldChunk = new DomainError(
        "navigation.world.missing_chunk",
        "The world snapshot does not contain every expected chunk.");

    public static readonly DomainError InvalidatedChunkOutOfBounds = new DomainError(
        "navigation.chunk.out_of_bounds",
        "An invalidated navigation chunk is outside the world bounds.");

    public static readonly DomainError DuplicateLink = new DomainError(
        "navigation.link.duplicate",
        "Traversal links must have unique identifiers.");

    public static readonly DomainError LinkOutOfBounds = new DomainError(
        "navigation.link.out_of_bounds",
        "A traversal link endpoint is outside the world bounds.");
}
