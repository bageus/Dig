using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Inventory;

namespace Dig.Domain.World
{

public sealed class TerrainOutputEntry
{
    public TerrainOutputEntry(
        ItemId itemId,
        int probabilityPermille,
        int minimumQuantity,
        int maximumQuantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Output item id is required.", nameof(itemId));
        }

        if (probabilityPermille <= 0 || probabilityPermille > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(probabilityPermille));
        }

        if (minimumQuantity <= 0 || maximumQuantity < minimumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumQuantity));
        }

        ItemId = itemId;
        ProbabilityPermille = probabilityPermille;
        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
    }

    public ItemId ItemId { get; }
    public int ProbabilityPermille { get; }
    public int MinimumQuantity { get; }
    public int MaximumQuantity { get; }
}

public sealed class TerrainOutputProfile
{
    private readonly IReadOnlyList<TerrainOutputEntry> _entries;

    public TerrainOutputProfile(
        string id,
        int version,
        IEnumerable<TerrainOutputEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Output profile id is required.", nameof(id));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        TerrainOutputEntry[] values = entries
            .OrderBy(entry => entry.ItemId)
            .ToArray();
        if (values.Select(entry => entry.ItemId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Output profile item ids must be unique.",
                nameof(entries));
        }

        Id = id.Trim();
        Version = version;
        _entries = new ReadOnlyCollection<TerrainOutputEntry>(values);
    }

    public string Id { get; }
    public int Version { get; }
    public IReadOnlyList<TerrainOutputEntry> Entries => _entries;
}

public sealed class TerrainOutputResult
{
    public TerrainOutputResult(
        ItemId itemId,
        int quantity,
        ulong probabilityRoll,
        ulong quantityRoll)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Output item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ItemId = itemId;
        Quantity = quantity;
        ProbabilityRoll = probabilityRoll;
        QuantityRoll = quantityRoll;
    }

    public ItemId ItemId { get; }
    public int Quantity { get; }
    public ulong ProbabilityRoll { get; }
    public ulong QuantityRoll { get; }
}

public sealed class TerrainOutputRoll
{
    private readonly IReadOnlyList<TerrainOutputResult> _outputs;

    public TerrainOutputRoll(
        string profileId,
        int profileVersion,
        ulong roll,
        IEnumerable<TerrainOutputResult> outputs)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile id is required.", nameof(profileId));
        }

        if (profileVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(profileVersion));
        }

        TerrainOutputResult[] values = (outputs
            ?? throw new ArgumentNullException(nameof(outputs)))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (values.Select(value => value.ItemId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Resolved terrain outputs must be unique by item id.",
                nameof(outputs));
        }

        ProfileId = profileId.Trim();
        ProfileVersion = profileVersion;
        Roll = roll;
        _outputs = new ReadOnlyCollection<TerrainOutputResult>(values);
    }

    public string ProfileId { get; }
    public int ProfileVersion { get; }
    public ulong Roll { get; }
    public IReadOnlyList<TerrainOutputResult> Outputs => _outputs;
    public bool IsEmpty => _outputs.Count == 0;
    public ItemId ItemId => _outputs.Count == 1 ? _outputs[0].ItemId : default;
    public int Quantity => _outputs.Count == 1 ? _outputs[0].Quantity : 0;
}

}
