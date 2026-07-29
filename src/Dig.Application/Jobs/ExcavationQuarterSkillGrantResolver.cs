using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public sealed class ExcavationQuarterSkillGrantResolver
{
    public IReadOnlyList<SkillGrant> Resolve(
        SkillGrantProfile profile,
        ExcavationQuarter quarter)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        int quarterIndex = ResolveQuarterIndex(quarter);
        List<SkillGrant> grants = new List<SkillGrant>();
        foreach (SkillGrant grant in profile.PerUnit)
        {
            int baseUnits = grant.RequestedUnits / 4;
            int remainder = grant.RequestedUnits % 4;
            int units = baseUnits + (quarterIndex < remainder ? 1 : 0);
            if (units > 0)
            {
                grants.Add(new SkillGrant(grant.SkillId, units));
            }
        }

        return new ReadOnlyCollection<SkillGrant>(grants);
    }

    private static int ResolveQuarterIndex(ExcavationQuarter quarter)
    {
        switch (quarter)
        {
            case ExcavationQuarter.UpperLeft:
                return 0;
            case ExcavationQuarter.LowerLeft:
                return 1;
            case ExcavationQuarter.UpperRight:
                return 2;
            case ExcavationQuarter.LowerRight:
                return 3;
            default:
                throw new ArgumentException(
                    "A single excavation quarter is required.",
                    nameof(quarter));
        }
    }
}

}
