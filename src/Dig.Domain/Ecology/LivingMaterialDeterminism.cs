using System;
using System.Text;
using Dig.Domain.Core;

namespace Dig.Domain.Ecology
{

public static class LivingMaterialDeterminism
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static int SelectInclusive(
        ulong worldSeed,
        EntityId creatureId,
        long sequence,
        string purpose,
        int minimum,
        int maximum)
    {
        if (creatureId.IsEmpty)
        {
            throw new ArgumentException("Creature id cannot be empty.", nameof(creatureId));
        }

        if (sequence < 0 || string.IsNullOrWhiteSpace(purpose) || minimum > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        ulong hash = Offset ^ worldSeed;
        hash = Add(hash, creatureId.ToString());
        hash = Add(hash, sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        hash = Add(hash, purpose);
        int width = checked(maximum - minimum + 1);
        return checked(minimum + (int)(Mix(hash) % (uint)width));
    }

    public static EntityId CreateOffspringId(
        EntityId parentId,
        LivingMaterialSpecies species,
        int successfulCycleNumber)
    {
        if (parentId.IsEmpty || successfulCycleNumber <= 0)
        {
            throw new ArgumentException("Parent id and positive cycle number are required.");
        }

        ulong first = Mix(Add(Offset, parentId + ":offspring:" + species + ":" + successfulCycleNumber));
        ulong second = Mix(first ^ 0x9E3779B97F4A7C15UL);
        byte[] bytes = new byte[16];
        Array.Copy(BitConverter.GetBytes(first), 0, bytes, 0, 8);
        Array.Copy(BitConverter.GetBytes(second), 0, bytes, 8, 8);
        Guid guid = new Guid(bytes);
        if (guid == Guid.Empty)
        {
            bytes[0] = 1;
            guid = new Guid(bytes);
        }

        return new EntityId(guid);
    }

    private static ulong Add(ulong hash, string value)
    {
        foreach (byte item in Encoding.UTF8.GetBytes(value))
        {
            hash ^= item;
            hash *= Prime;
        }

        return hash;
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}

}
