using System;
using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed class ResidentIdentity
{
    public ResidentIdentity(EntityId id, string name)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resident name is required.", nameof(name));
        }

        Id = id;
        Name = name;
    }

    public EntityId Id { get; }

    public string Name { get; }
}

public sealed class ResidentIdentityGenerator
{
    private static readonly string[] Prefixes =
    {
        "Bor", "Dor", "Ein", "Far", "Gim", "Hal", "Iri", "Kor",
    };

    private static readonly string[] Suffixes =
    {
        "in", "a", "ar", "ra", "or", "is", "un", "el",
    };

    public IReadOnlyList<ResidentIdentity> Generate(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        ResidentIdentity[] result = new ResidentIdentity[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = new ResidentIdentity(
                CreateId(index),
                CreateName(index));
        }

        return result;
    }

    private static EntityId CreateId(int index)
    {
        return EntityId.Parse("1" + (index + 1).ToString("x31"));
    }

    private static string CreateName(int index)
    {
        int combinations = Prefixes.Length * Suffixes.Length;
        int cycle = index / combinations;
        int combination = index % combinations;
        string name = Prefixes[combination / Suffixes.Length]
            + Suffixes[combination % Suffixes.Length];
        return cycle == 0 ? name : name + " " + (cycle + 1);
    }
}

}