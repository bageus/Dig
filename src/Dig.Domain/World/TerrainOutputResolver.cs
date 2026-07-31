using System;
using System.Collections.Generic;

namespace Dig.Domain.World
{

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

        ulong rootRoll = Hash(
            worldSeed,
            generatorVersion,
            cell,
            profile.Version,
            itemKey: string.Empty,
            stream: 0);
        List<TerrainOutputResult>? outputs = null;
        foreach (TerrainOutputEntry entry in profile.Entries)
        {
            string itemKey = entry.ItemId.ToString();
            ulong probabilityRoll = Hash(
                worldSeed,
                generatorVersion,
                cell,
                profile.Version,
                itemKey,
                stream: 1);
            if ((int)(probabilityRoll % 1_000UL) >= entry.ProbabilityPermille)
            {
                continue;
            }

            ulong quantityRoll = Hash(
                worldSeed,
                generatorVersion,
                cell,
                profile.Version,
                itemKey,
                stream: 2);
            int range = entry.MaximumQuantity - entry.MinimumQuantity + 1;
            int quantity = entry.MinimumQuantity
                + (int)(quantityRoll % (ulong)range);
            (outputs ??= new List<TerrainOutputResult>()).Add(
                new TerrainOutputResult(
                    entry.ItemId,
                    quantity,
                    probabilityRoll,
                    quantityRoll));
        }

        return new TerrainOutputRoll(
            profile.Id,
            profile.Version,
            rootRoll,
            outputs ?? (IEnumerable<TerrainOutputResult>)Array.Empty<TerrainOutputResult>());
    }

    private static ulong Hash(
        int worldSeed,
        int generatorVersion,
        CellId cell,
        int profileVersion,
        string itemKey,
        uint stream)
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
        Mix(ref hash, stream, prime);
        for (int index = 0; index < itemKey.Length; index++)
        {
            Mix(ref hash, itemKey[index], prime);
        }

        return hash;
    }

    private static void Mix(ref ulong hash, uint value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }
}

}
