using System;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

public static class WorldGenerationFingerprint
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Compute(
        WorldState world,
        ulong worldSeed,
        int generatorVersion,
        string profileId)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        }

        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile id is required.", nameof(profileId));
        }

        ulong hash = Offset;
        AddUInt64(ref hash, worldSeed);
        AddInt32(ref hash, generatorVersion);
        AddString(ref hash, profileId);
        AddInt32(ref hash, world.Size.Width);
        AddInt32(ref hash, world.Size.Height);
        AddInt32(ref hash, world.Layout.ChunkSize);

        for (int y = 0; y < world.Size.Height; y++)
        {
            for (int x = 0; x < world.Size.Width; x++)
            {
                CellState state = world.GetCell(new CellId(x, y)).Value.State;
                AddString(ref hash, state.MaterialId.ToString());
                AddInt32(ref hash, (int)state.Designation);
                AddByte(ref hash, state.IsExplored ? (byte)1 : (byte)0);
                AddInt32(ref hash, state.Damage);
                AddInt32(ref hash, state.Temperature);
            }
        }

        return hash;
    }

    private static void AddString(ref ulong hash, string value)
    {
        AddInt32(ref hash, value.Length);
        foreach (char character in value)
        {
            AddByte(ref hash, (byte)character);
            AddByte(ref hash, (byte)(character >> 8));
        }
    }

    private static void AddInt32(ref ulong hash, int value)
    {
        unchecked
        {
            AddByte(ref hash, (byte)value);
            AddByte(ref hash, (byte)(value >> 8));
            AddByte(ref hash, (byte)(value >> 16));
            AddByte(ref hash, (byte)(value >> 24));
        }
    }

    private static void AddUInt64(ref ulong hash, ulong value)
    {
        for (int shift = 0; shift < 64; shift += 8)
        {
            AddByte(ref hash, (byte)(value >> shift));
        }
    }

    private static void AddByte(ref ulong hash, byte value)
    {
        hash ^= value;
        hash *= Prime;
    }
}

}
