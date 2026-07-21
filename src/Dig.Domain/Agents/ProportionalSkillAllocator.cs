using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.Agents
{

internal readonly struct SkillAllocationWeight
{
    public SkillAllocationWeight(AgentSkillId skillId, int weight)
    {
        SkillId = skillId;
        Weight = weight;
    }

    public AgentSkillId SkillId { get; }
    public int Weight { get; }
}

internal readonly struct SkillAllocation
{
    public SkillAllocation(
        AgentSkillId skillId,
        int units,
        long fractionalRemainder,
        bool receivedRoundingUnit)
    {
        SkillId = skillId;
        Units = units;
        FractionalRemainder = fractionalRemainder;
        ReceivedRoundingUnit = receivedRoundingUnit;
    }

    public AgentSkillId SkillId { get; }
    public int Units { get; }
    public long FractionalRemainder { get; }
    public bool ReceivedRoundingUnit { get; }
}

internal static class ProportionalSkillAllocator
{
    public static IReadOnlyDictionary<AgentSkillId, SkillAllocation> Allocate(
        int totalUnits,
        IEnumerable<SkillAllocationWeight> source)
    {
        SkillAllocationWeight[] weights = (source
            ?? throw new ArgumentNullException(nameof(source)))
            .Where(value => value.Weight > 0)
            .OrderBy(value => value.SkillId)
            .ToArray();
        long weightSum = weights.Sum(value => (long)value.Weight);
        if (totalUnits < 0 || totalUnits > weightSum)
        {
            throw new ArgumentOutOfRangeException(nameof(totalUnits));
        }

        Dictionary<AgentSkillId, SkillAllocation> result =
            new Dictionary<AgentSkillId, SkillAllocation>();
        if (totalUnits == 0 || weights.Length == 0)
        {
            foreach (SkillAllocationWeight weight in weights)
            {
                result.Add(weight.SkillId, new SkillAllocation(
                    weight.SkillId, 0, 0, receivedRoundingUnit: false));
            }

            return result;
        }

        int floorSum = 0;
        List<SkillAllocation> floors = new List<SkillAllocation>(weights.Length);
        foreach (SkillAllocationWeight weight in weights)
        {
            long numerator = checked((long)totalUnits * weight.Weight);
            int floor = checked((int)(numerator / weightSum));
            long remainder = numerator % weightSum;
            floorSum = checked(floorSum + floor);
            floors.Add(new SkillAllocation(
                weight.SkillId, floor, remainder, receivedRoundingUnit: false));
        }

        HashSet<AgentSkillId> rounded = new HashSet<AgentSkillId>(floors
            .OrderByDescending(value => value.FractionalRemainder)
            .ThenBy(value => value.SkillId)
            .Take(totalUnits - floorSum)
            .Select(value => value.SkillId));
        foreach (SkillAllocation floor in floors)
        {
            bool receivesUnit = rounded.Contains(floor.SkillId);
            result.Add(floor.SkillId, new SkillAllocation(
                floor.SkillId,
                floor.Units + (receivesUnit ? 1 : 0),
                floor.FractionalRemainder,
                receivesUnit));
        }

        return result;
    }
}

}
