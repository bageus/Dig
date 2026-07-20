using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private static readonly DomainError HudScheduleNotInitialized = new DomainError(
        "unity.resident_schedule.not_initialized",
        "Resident schedule editing is not initialized.");
    private readonly ResidentRosterPresenter _rosterPresenter =
        new ResidentRosterPresenter();
    private SetAgentWorkRestWindowCommandHandler? _workScheduleHandler;

    internal void InitializeHudSchedule(InMemoryExecutionJournal journal)
    {
        _workScheduleHandler = new SetAgentWorkRestWindowCommandHandler(
            _repository,
            journal ?? throw new ArgumentNullException(nameof(journal)));
    }

    internal ResidentRosterViewModel LoadResidentRoster(
        IReadOnlyList<JobSnapshot> jobs,
        string? selectedResidentId)
    {
        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        EntityId? selected = string.IsNullOrWhiteSpace(selectedResidentId)
            ? null
            : EntityId.Parse(selectedResidentId);
        AgentSnapshot[] snapshots = _repository.GetAll()
            .Select(agent => agent.CreateSnapshot(_tick))
            .ToArray();
        return _rosterPresenter.Present(
            snapshots,
            society: null,
            jobs,
            selected);
    }

    internal bool TryGetWorkWindow(
        string residentId,
        out int ticksPerDay,
        out int startTickInclusive,
        out int endTickExclusive)
    {
        AgentState? agent = GetResident(residentId);
        if (agent is null)
        {
            ticksPerDay = 24;
            startTickInclusive = 0;
            endTickExclusive = 12;
            return false;
        }

        ticksPerDay = agent.Schedule.TicksPerDay;
        return agent.Schedule.TryGetWorkWindow(
            out startTickInclusive,
            out endTickExclusive);
    }

    internal Result SetWorkRestWindow(
        string residentId,
        int startTickInclusive,
        int endTickExclusive)
    {
        if (_workScheduleHandler is null)
        {
            return Result.Failure(HudScheduleNotInitialized);
        }

        return _workScheduleHandler.Handle(new SetAgentWorkRestWindowCommand(
            EntityId.Parse(residentId),
            startTickInclusive,
            endTickExclusive,
            _tick));
    }

    private AgentState? GetResident(string residentId)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            return null;
        }

        return _repository.Get(EntityId.Parse(residentId));
    }
}

}