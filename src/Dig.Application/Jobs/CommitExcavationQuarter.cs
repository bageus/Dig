using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public sealed class CommitExcavationQuarterCommand
{
    public CommitExcavationQuarterCommand(
        CellId target,
        ExcavationQuarter quarter,
        ExcavationCutPattern cutPattern,
        MaterialId emptyMaterialId,
        EntityId workerId,
        SkillGrantProfile skillGrantProfile,
        long tick)
    {
        if (workerId.IsEmpty)
        {
            throw new ArgumentException("Worker id is required.", nameof(workerId));
        }

        Target = target;
        Quarter = quarter;
        CutPattern = cutPattern;
        EmptyMaterialId = emptyMaterialId;
        WorkerId = workerId;
        SkillGrantProfile = skillGrantProfile
            ?? throw new ArgumentNullException(nameof(skillGrantProfile));
        Tick = tick;
    }

    public CellId Target { get; }
    public ExcavationQuarter Quarter { get; }
    public ExcavationCutPattern CutPattern { get; }
    public MaterialId EmptyMaterialId { get; }
    public EntityId WorkerId { get; }
    public SkillGrantProfile SkillGrantProfile { get; }
    public long Tick { get; }
}

public sealed class CommitExcavationQuarterCommandHandler
{
    private readonly IWorldRepository _world;
    private readonly IAgentSkillGrantService _skills;
    private readonly IEventSink _events;
    private readonly ExcavationQuarterSkillGrantResolver _grantResolver =
        new ExcavationQuarterSkillGrantResolver();

    public CommitExcavationQuarterCommandHandler(
        IWorldRepository world,
        IAgentSkillGrantService skills,
        IEventSink events)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<WorldMutationResult> Handle(CommitExcavationQuarterCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.Tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.Tick));
        }

        IReadOnlyList<SkillGrant> grants = _grantResolver.Resolve(
            command.SkillGrantProfile,
            command.Quarter);
        SkillGrantBundle? bundle = grants.Count == 0
            ? null
            : new SkillGrantBundle(
                command.WorkerId,
                SkillGrantSourceKind.ExcavationQuarterCommitted,
                BuildSourceId(command.Target, command.Quarter),
                command.Tick,
                grants);
        if (bundle != null)
        {
            Result skillValidation = _skills.Validate(bundle);
            if (skillValidation.IsFailure)
            {
                return Result<WorldMutationResult>.Failure(skillValidation.Error!);
            }
        }

        WorldState world = _world.Get();
        Result<WorldMutationResult> committed = world.CommitExcavationQuarter(
            command.Target,
            command.Quarter,
            command.CutPattern,
            command.EmptyMaterialId,
            command.Tick);
        if (committed.IsFailure)
        {
            return committed;
        }

        if (bundle != null && committed.Value.ChangedCellCount > 0)
        {
            Result<SkillRedistributionReport> applied = _skills.ApplyConfirmed(bundle);
            if (applied.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Committed excavation quarter skill grant failed: {applied.Error}");
            }
        }

        _world.Save(world);
        _events.Append(world.DequeueUncommittedEvents());
        return committed;
    }

    private static string BuildSourceId(CellId target, ExcavationQuarter quarter)
    {
        return $"excavation:{target.X}:{target.Y}:{target.Z}:{(int)quarter}";
    }
}

}
