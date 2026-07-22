using System;
using Dig.Domain.Core;

namespace Dig.Domain.Agents
{

public sealed partial class AgentState
{
    public Result SetSkillLevel(AgentSkillId skillId, int level)
    {
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        _skills.SetLevel(skillId, level);
        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result<SkillRedistributionReport> ApplySkillGrant(
        SkillGrantBundle bundle)
    {
        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }

        if (!IsAlive)
        {
            return Result<SkillRedistributionReport>.Failure(AgentErrors.AgentDead);
        }

        if (bundle.AgentId != Id)
        {
            return Result<SkillRedistributionReport>.Failure(
                AgentErrors.SkillBundleAgentMismatch);
        }

        SkillRedistributionReport report = _skills.Apply(bundle);
        if (!report.WasAlreadyApplied)
        {
            Version = checked(Version + 1);
            Raise(new AgentSkillGrantApplied(bundle.Tick, Id, report));
        }

        return Result<SkillRedistributionReport>.Success(report);
    }

    public Result ExpandTotalSkillCapacity(int capacityUnits, long tick)
    {
        ValidateTick(tick);
        if (!IsAlive)
        {
            return Result.Failure(AgentErrors.AgentDead);
        }

        int previous = _skills.TotalCapacityUnits;
        _skills.SetTotalCapacity(capacityUnits);
        if (previous != _skills.TotalCapacityUnits)
        {
            Version = checked(Version + 1);
            Raise(new AgentSkillCapacityChanged(
                tick, Id, previous, _skills.TotalCapacityUnits));
        }

        return Result.Success();
    }

    public Result RestoreSkillProgression(AgentSkillProgressionSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        _skills.Restore(snapshot);
        Version = checked(Version + 1);
        return Result.Success();
    }

    public AgentSkillProgressionSnapshot CreateSkillProgressionSnapshot()
    {
        return _skills.CreateProgressionSnapshot();
    }
}

}
