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

        int totalProbability = values.Sum(entry => entry.ProbabilityPermille);
        if (totalProbability > 1_000)
        {
            throw new ArgumentException(
                "Output probabilities cannot exceed 1000 permille.",
                nameof(entries));
        }

        Id = id.Trim();
        Version = version;
        _entries = new ReadOnlyCollection<TerrainOutputEntry>(values);
    }

    public string Id { get; }
    public int Version { get; }
    public IReadOnlyList<TerrainOutputEntry> Entries => _entries;
    public int EmptyProbabilityPermille =>
        1_000 - _entries.Sum(entry => entry.ProbabilityPermille);
}

public readonly struct TerrainOutputRoll
{
    public TerrainOutputRoll(
        string profileId,
        int profileVersion,
        ulong roll,
        ItemId itemId,
        int quantity)
    {
        ProfileId = profileId ?? throw new ArgumentNullException(nameof(profileId));
        ProfileVersion = profileVersion;
        Roll = roll;
        ItemId = itemId;
        Quantity = quantity;
    }

    public string ProfileId { get; }
    public int ProfileVersion { get; }
    public ulong Roll { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
    public bool IsEmpty => Quantity == 0;
}

public sealed class TerrainOutputResolver
{
    public TerrainOutputRoll Resolve(
        int worldSeed,
        int generatorVersion,
        CellId cell,
        TerrainOutputProfile profile)
    {
        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        }

        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        ulong roll = Hash(worldSeed, generatorVersion, cell, profile.Version);
        int bucket = (int)(roll % 1_000UL);
        int cursor = 0;
        for (int index = 0; index < profile.Entries.Count; index++)
        {
            TerrainOutputEntry entry = profile.Entries[index];
            cursor += entry.ProbabilityPermille;
            if (bucket >= cursor)
            {
                continue;
            }

            int range = entry.MaximumQuantity - entry.MinimumQuantity + 1;
            int quantity = entry.MinimumQuantity
                + (int)((roll / 1_000UL) % (ulong)range);
            return new TerrainOutputRoll(
                profile.Id,
                profile.Version,
                roll,
                entry.ItemId,
                quantity);
        }

        return new TerrainOutputRoll(
            profile.Id,
            profile.Version,
            roll,
            default,
            quantity: 0);
    }

    private static ulong Hash(
        int worldSeed,
        int generatorVersion,
        CellId cell,
        int profileVersion)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        Mix(ref hash, unchecked((uint)worldSeed), prime);
        Mix(ref hash, unchecked((uint)generatorVersion), prime);
        Mix(ref hash, unchecked((uint)cell.X), prime);
        Mix(ref hash, unchecked((uint)cell.Y), prime);
        Mix(ref hash, unchecked((uint)cell.Z), prime);
        Mix(ref hash, unchecked((uint)profileVersion), prime);
        return hash;
    }

    private static void Mix(ref ulong hash, uint value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }
}

public static class DefaultTerrainMaterials
{
    public static readonly MaterialId Sand = new MaterialId("terrain.sand");
    public static readonly MaterialId StoneRock = new MaterialId("terrain.stone_rock");
    public static readonly MaterialId MetalBearingRock =
        new MaterialId("terrain.metal_bearing_rock");
    public static readonly MaterialId CrystallineRock =
        new MaterialId("terrain.crystalline_rock");
    public static readonly MaterialId LavaRock = new MaterialId("terrain.lava_rock");
    public static readonly MaterialId Unmineable = new MaterialId("terrain.unmineable");

    public static MaterialCatalog CreateCatalog()
    {
        ItemId stone = new ItemId("material.stone");
        ItemId iron = new ItemId("material.iron");
        ItemId gold = new ItemId("material.gold");
        ItemId crystal = new ItemId("material.crystal");
        ItemId coal = new ItemId("material.coal");

        return new MaterialCatalog(new[]
        {
            Material(Sand, "Песчаный грунт", 25, Profile("terrain-output.sand", 1)),
            Material(StoneRock, "Каменная порода", 100,
                Profile("terrain-output.stone-rock", 1,
                    Entry(stone, 1_000, 1, 3))),
            Material(MetalBearingRock, "Рудная порода", 140,
                Profile("terrain-output.metal-bearing-rock", 1,
                    Entry(stone, 550, 1, 3),
                    Entry(iron, 220, 1, 2),
                    Entry(gold, 20, 1, 1),
                    Entry(coal, 110, 1, 2))),
            Material(CrystallineRock, "Кристаллическая порода", 170,
                Profile("terrain-output.crystalline-rock", 1,
                    Entry(stone, 180, 1, 2),
                    Entry(iron, 260, 1, 2),
                    Entry(crystal, 360, 1, 2),
                    Entry(gold, 40, 1, 1))),
            Material(LavaRock, "Лавовая порода", 220,
                Profile("terrain-output.lava-rock", 1,
                    Entry(gold, 120, 1, 2),
                    Entry(stone, 130, 1, 2),
                    Entry(crystal, 170, 1, 2),
                    Entry(iron, 230, 1, 2),
                    Entry(coal, 210, 1, 3))),
            new MaterialDefinition(
                Unmineable,
                "Недобываемая порода",
                isSolid: true,
                hardness: int.MaxValue,
                isMineable: false,
                outputProfile: null),
        });
    }

    private static MaterialDefinition Material(
        MaterialId id,
        string displayName,
        int hardness,
        TerrainOutputProfile profile)
    {
        return new MaterialDefinition(
            id,
            displayName,
            isSolid: true,
            hardness: hardness,
            isMineable: true,
            outputProfile: profile);
    }

    private static TerrainOutputProfile Profile(
        string id,
        int version,
        params TerrainOutputEntry[] entries)
    {
        return new TerrainOutputProfile(id, version, entries);
    }

    private static TerrainOutputEntry Entry(
        ItemId itemId,
        int probabilityPermille,
        int minimumQuantity,
        int maximumQuantity)
    {
        return new TerrainOutputEntry(
            itemId,
            probabilityPermille,
            minimumQuantity,
            maximumQuantity);
    }
}

}