using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.Agents
{

public sealed class LeisureActivitySelector
{
    public LeisureActivityDefinition Select(
        IReadOnlyList<LeisureActivityDefinition> candidates,
        IReadOnlyList<LeisureVarietyId> history,
        ulong worldSeed,
        long decisionId)
    {
        if (candidates is null || candidates.Count == 0)
        {
            throw new ArgumentException("At least one leisure candidate is required.", nameof(candidates));
        }

        if (history is null) throw new ArgumentNullException(nameof(history));
        LeisureActivityDefinition[] ordered = candidates
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        int[] weights = ordered
            .Select(value => Math.Max(1, 11 - history.Count(item => item.Equals(value.Id))))
            .ToArray();
        int total = weights.Sum();
        ulong mixed = Mix(worldSeed ^ unchecked((ulong)decisionId));
        int roll = (int)(mixed % (ulong)total);
        for (int index = 0; index < ordered.Length; index++)
        {
            if (roll < weights[index]) return ordered[index];
            roll -= weights[index];
        }

        return ordered[ordered.Length - 1];
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9UL;
        value ^= value >> 27;
        value *= 0x94d049bb133111ebUL;
        return value ^ (value >> 31);
    }
}

}
