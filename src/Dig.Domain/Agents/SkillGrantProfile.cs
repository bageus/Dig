using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.Agents
{

public sealed class SkillGrantProfile
{
    private readonly SkillGrant[] _perUnit;

    public SkillGrantProfile(IEnumerable<SkillGrant> perUnit)
    {
        if (perUnit is null)
        {
            throw new ArgumentNullException(nameof(perUnit));
        }

        _perUnit = perUnit
            .GroupBy(value => value.SkillId)
            .Select(group => new SkillGrant(
                group.Key,
                checked(group.Sum(value => value.RequestedUnits))))
            .OrderBy(value => value.SkillId)
            .ToArray();
        if (_perUnit.Length == 0)
        {
            throw new ArgumentException(
                "A skill grant profile needs at least one entry.",
                nameof(perUnit));
        }
    }

    public IReadOnlyList<SkillGrant> PerUnit =>
        new ReadOnlyCollection<SkillGrant>(_perUnit);

    public IReadOnlyList<SkillGrant> Multiply(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return new ReadOnlyCollection<SkillGrant>(_perUnit
            .Select(value => new SkillGrant(
                value.SkillId,
                checked(value.RequestedUnits * quantity)))
            .ToArray());
    }

    public static SkillGrantProfile Single(AgentSkillId skillId, int units)
    {
        return new SkillGrantProfile(new[] { new SkillGrant(skillId, units) });
    }
}

}
