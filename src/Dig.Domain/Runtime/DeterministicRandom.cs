using System.Collections.ObjectModel;
using System.Text;

namespace Dig.Domain.Runtime;

public readonly struct RandomStreamSnapshot
{
    public RandomStreamSnapshot(string name, ulong state)
    {
        Name = name;
        State = state;
    }

    public string Name { get; }

    public ulong State { get; }
}

public sealed class DeterministicRandomStream
{
    private ulong _state;

    public DeterministicRandomStream(ulong state)
    {
        _state = state;
    }

    public ulong State => _state;

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    public int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        }

        return (int)(NextUInt64() % (uint)exclusiveMaximum);
    }

    public int NextInt(int inclusiveMinimum, int exclusiveMaximum)
    {
        if (inclusiveMinimum >= exclusiveMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        }

        int width = checked(exclusiveMaximum - inclusiveMinimum);
        return checked(inclusiveMinimum + NextInt(width));
    }
}

public sealed class RandomStreamCatalog
{
    private readonly ulong _worldSeed;
    private readonly Dictionary<string, DeterministicRandomStream> _streams;

    public RandomStreamCatalog(ulong worldSeed)
    {
        _worldSeed = worldSeed;
        _streams = new Dictionary<string, DeterministicRandomStream>(StringComparer.Ordinal);
    }

    private RandomStreamCatalog(
        ulong worldSeed,
        Dictionary<string, DeterministicRandomStream> streams)
    {
        _worldSeed = worldSeed;
        _streams = streams;
    }

    public ulong WorldSeed => _worldSeed;

    public DeterministicRandomStream GetOrCreate(string name)
    {
        ValidateName(name);

        if (_streams.TryGetValue(name, out DeterministicRandomStream? existing))
        {
            return existing;
        }

        ulong initialState = DeriveStreamSeed(_worldSeed, name);
        DeterministicRandomStream created = new DeterministicRandomStream(initialState);
        _streams.Add(name, created);
        return created;
    }

    public IReadOnlyList<RandomStreamSnapshot> CaptureSnapshots()
    {
        RandomStreamSnapshot[] snapshots = _streams
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new RandomStreamSnapshot(pair.Key, pair.Value.State))
            .ToArray();

        return new ReadOnlyCollection<RandomStreamSnapshot>(snapshots);
    }

    public static RandomStreamCatalog Restore(
        ulong worldSeed,
        IEnumerable<RandomStreamSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        Dictionary<string, DeterministicRandomStream> streams =
            new Dictionary<string, DeterministicRandomStream>(StringComparer.Ordinal);

        foreach (RandomStreamSnapshot snapshot in snapshots)
        {
            ValidateName(snapshot.Name);
            if (!streams.TryAdd(snapshot.Name, new DeterministicRandomStream(snapshot.State)))
            {
                throw new ArgumentException(
                    $"Duplicate random stream '{snapshot.Name}'.",
                    nameof(snapshots));
            }
        }

        return new RandomStreamCatalog(worldSeed, streams);
    }

    private static ulong DeriveStreamSeed(ulong worldSeed, string name)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offset;
        foreach (byte value in Encoding.UTF8.GetBytes(name))
        {
            hash ^= value;
            hash *= prime;
        }

        ulong combined = hash ^ worldSeed;
        combined ^= combined >> 30;
        combined *= 0xBF58476D1CE4E5B9UL;
        combined ^= combined >> 27;
        combined *= 0x94D049BB133111EBUL;
        return combined ^ (combined >> 31);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Random stream name is required.", nameof(name));
        }
    }
}
