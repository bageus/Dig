using Dig.Domain.World;

namespace Dig.Domain.Navigation;

public readonly struct TraversalProfileId : IEquatable<TraversalProfileId>
{
    private readonly string? _value;

    public TraversalProfileId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Traversal profile id is required.", nameof(value));
        }

        _value = value;
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public bool Equals(TraversalProfileId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is TraversalProfileId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(TraversalProfileId left, TraversalProfileId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TraversalProfileId left, TraversalProfileId right)
    {
        return !left.Equals(right);
    }
}

public enum TraversalMode
{
    Grounded = 0,
    Free = 1,
}

public sealed class TraversalProfile
{
    public TraversalProfile(
        TraversalProfileId id,
        TraversalMode mode,
        int maxStepUp,
        int maxStepDown,
        bool canUseLadders,
        bool canUseElevators,
        int orthogonalCost = 10,
        int stepCost = 14)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Traversal profile id cannot be empty.", nameof(id));
        }

        if (mode is not TraversalMode.Grounded and not TraversalMode.Free)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (maxStepUp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStepUp));
        }

        if (maxStepDown < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStepDown));
        }

        if (orthogonalCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orthogonalCost));
        }

        if (stepCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepCost));
        }

        Id = id;
        Mode = mode;
        MaxStepUp = maxStepUp;
        MaxStepDown = maxStepDown;
        CanUseLadders = canUseLadders;
        CanUseElevators = canUseElevators;
        OrthogonalCost = orthogonalCost;
        StepCost = stepCost;
    }

    public TraversalProfileId Id { get; }

    public TraversalMode Mode { get; }

    public int MaxStepUp { get; }

    public int MaxStepDown { get; }

    public bool CanUseLadders { get; }

    public bool CanUseElevators { get; }

    public int OrthogonalCost { get; }

    public int StepCost { get; }

    public bool Allows(TraversalLinkKind kind)
    {
        return kind switch
        {
            TraversalLinkKind.Ladder => CanUseLadders,
            TraversalLinkKind.Elevator => CanUseElevators,
            _ => false,
        };
    }

    public static TraversalProfile CreateGroundedDwarf()
    {
        return new TraversalProfile(
            new TraversalProfileId("grounded-dwarf"),
            TraversalMode.Grounded,
            maxStepUp: 1,
            maxStepDown: 1,
            canUseLadders: true,
            canUseElevators: true);
    }

    public static TraversalProfile CreateFreeMover()
    {
        return new TraversalProfile(
            new TraversalProfileId("free-mover"),
            TraversalMode.Free,
            maxStepUp: 1,
            maxStepDown: 1,
            canUseLadders: false,
            canUseElevators: false);
    }
}

public enum TraversalLinkKind
{
    Ladder = 1,
    Elevator = 2,
}

public sealed class TraversalLink
{
    public TraversalLink(
        string id,
        CellId from,
        CellId to,
        TraversalLinkKind kind,
        int cost,
        bool bidirectional,
        long sourceVersion)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Traversal link id is required.", nameof(id));
        }

        if (from == to)
        {
            throw new ArgumentException("Traversal link endpoints must differ.", nameof(to));
        }

        if (kind is not TraversalLinkKind.Ladder and not TraversalLinkKind.Elevator)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (cost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost));
        }

        if (sourceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceVersion));
        }

        Id = id;
        From = from;
        To = to;
        Kind = kind;
        Cost = cost;
        Bidirectional = bidirectional;
        SourceVersion = sourceVersion;
    }

    public string Id { get; }

    public CellId From { get; }

    public CellId To { get; }

    public TraversalLinkKind Kind { get; }

    public int Cost { get; }

    public bool Bidirectional { get; }

    public long SourceVersion { get; }
}
