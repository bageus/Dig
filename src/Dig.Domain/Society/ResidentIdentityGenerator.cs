using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Society
{

public sealed class TraitInheritanceDefinition
{
    public TraitInheritanceDefinition(
        AgentTraitId traitId,
        int chanceFromOneParent,
        int chanceFromBothParents)
    {
        if (traitId.IsEmpty)
        {
            throw new ArgumentException("Trait id cannot be empty.", nameof(traitId));
        }

        ValidateChance(chanceFromOneParent, nameof(chanceFromOneParent));
        ValidateChance(chanceFromBothParents, nameof(chanceFromBothParents));
        if (chanceFromBothParents < chanceFromOneParent)
        {
            throw new ArgumentException(
                "Both-parent inheritance chance cannot be lower than one-parent chance.",
                nameof(chanceFromBothParents));
        }

        TraitId = traitId;
        ChanceFromOneParent = chanceFromOneParent;
        ChanceFromBothParents = chanceFromBothParents;
    }

    public AgentTraitId TraitId { get; }
    public int ChanceFromOneParent { get; }
    public int ChanceFromBothParents { get; }

    private static void ValidateChance(int chance, string parameterName)
    {
        if (chance < 0 || chance > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class ResidentInheritancePolicy
{
    private readonly IReadOnlyDictionary<AgentTraitId, TraitInheritanceDefinition> _traits;

    public ResidentInheritancePolicy(
        int potentialVariance,
        IEnumerable<TraitInheritanceDefinition>? traits = null)
    {
        if (potentialVariance < 0 || potentialVariance > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(potentialVariance));
        }

        Dictionary<AgentTraitId, TraitInheritanceDefinition> definitions =
            new Dictionary<AgentTraitId, TraitInheritanceDefinition>();
        foreach (TraitInheritanceDefinition definition in
            traits ?? Array.Empty<TraitInheritanceDefinition>())
        {
            if (definition is null)
            {
                throw new ArgumentException(
                    "Trait inheritance definitions cannot contain null.",
                    nameof(traits));
            }

            if (!definitions.TryAdd(definition.TraitId, definition))
            {
                throw new ArgumentException(
                    $"Duplicate trait inheritance definition '{definition.TraitId}'.",
                    nameof(traits));
            }
        }

        PotentialVariance = potentialVariance;
        _traits = new ReadOnlyDictionary<AgentTraitId, TraitInheritanceDefinition>(definitions);
    }

    public int PotentialVariance { get; }

    public TraitInheritanceDefinition? GetTrait(AgentTraitId traitId)
    {
        return _traits.TryGetValue(traitId, out TraitInheritanceDefinition? definition)
            ? definition
            : null;
    }
}

public sealed class ResidentIdentityGenerator
{
    public ResidentBirthPlan CreateBirthPlan(
        ulong worldSeed,
        long birthSequence,
        ResidentNameCatalog names,
        ResidentHeritage mother,
        ResidentHeritage father,
        ResidentInheritancePolicy inheritance,
        CellId position)
    {
        if (birthSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(birthSequence));
        }

        if (names is null)
        {
            throw new ArgumentNullException(nameof(names));
        }

        if (mother is null)
        {
            throw new ArgumentNullException(nameof(mother));
        }

        if (father is null)
        {
            throw new ArgumentNullException(nameof(father));
        }

        if (inheritance is null)
        {
            throw new ArgumentNullException(nameof(inheritance));
        }

        ulong sequenceSeed = Mix(worldSeed, unchecked((ulong)birthSequence));
        RandomStreamCatalog streams = new RandomStreamCatalog(sequenceSeed);
        DeterministicRandomStream sexStream = streams.GetOrCreate("society.identity.sex");
        ResidentSex sex = sexStream.NextInt(2) == 0
            ? ResidentSex.Female
            : ResidentSex.Male;

        IReadOnlyList<string> availableNames = names.GetNames(sex);
        DeterministicRandomStream nameStream = streams.GetOrCreate("society.identity.name");
        string name = names.Select(sex, nameStream.NextInt(availableNames.Count));
        EntityId id = CreateId(streams.GetOrCreate("society.identity.id"));
        ResidentHeritage heritage = CreateHeritage(
            mother,
            father,
            inheritance,
            streams);
        return new ResidentBirthPlan(id, name, sex, heritage, position);
    }

    private static ResidentHeritage CreateHeritage(
        ResidentHeritage mother,
        ResidentHeritage father,
        ResidentInheritancePolicy policy,
        RandomStreamCatalog streams)
    {
        int averagePotential = checked((mother.Potential + father.Potential) / 2);
        int variance = policy.PotentialVariance;
        int potentialOffset = variance == 0
            ? 0
            : streams.GetOrCreate("society.inheritance.potential")
                .NextInt(-variance, variance + 1);
        int potential = Math.Max(0, Math.Min(10_000, averagePotential + potentialOffset));

        AgentTraitId[] candidates = mother.Traits
            .Concat(father.Traits)
            .Distinct()
            .OrderBy(trait => trait)
            .ToArray();
        List<AgentTraitId> inherited = new List<AgentTraitId>();
        DeterministicRandomStream traitStream = streams.GetOrCreate("society.inheritance.traits");
        foreach (AgentTraitId trait in candidates)
        {
            TraitInheritanceDefinition? definition = policy.GetTrait(trait);
            if (definition is null)
            {
                continue;
            }

            bool fromMother = mother.Traits.Contains(trait);
            bool fromFather = father.Traits.Contains(trait);
            int chance = fromMother && fromFather
                ? definition.ChanceFromBothParents
                : definition.ChanceFromOneParent;
            if (traitStream.NextInt(10_000) < chance)
            {
                inherited.Add(trait);
            }
        }

        return new ResidentHeritage(potential, inherited);
    }

    private static EntityId CreateId(DeterministicRandomStream stream)
    {
        byte[] bytes = new byte[16];
        Buffer.BlockCopy(BitConverter.GetBytes(stream.NextUInt64()), 0, bytes, 0, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(stream.NextUInt64()), 0, bytes, 8, 8);
        Guid guid = new Guid(bytes);
        if (guid == Guid.Empty)
        {
            bytes[0] = 1;
            guid = new Guid(bytes);
        }

        return new EntityId(guid);
    }

    private static ulong Mix(ulong left, ulong right)
    {
        ulong value = left ^ (right + 0x9E3779B97F4A7C15UL + (left << 6) + (left >> 2));
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
}
