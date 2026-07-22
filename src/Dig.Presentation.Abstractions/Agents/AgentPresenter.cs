using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Application.Agents;
using Dig.Application.Messaging;
using Dig.Domain.Agents;

namespace Dig.Presentation.Agents
{

public sealed class AgentPresenter
{
    private readonly IQueryHandler<GetAgentSnapshotsQuery, IReadOnlyList<AgentSnapshot>> _queryHandler;

    public AgentPresenter(
        IQueryHandler<GetAgentSnapshotsQuery, IReadOnlyList<AgentSnapshot>> queryHandler)
    {
        _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
    }

    public IReadOnlyList<AgentViewModel> Load(long tick)
    {
        IReadOnlyList<AgentSnapshot> snapshots = _queryHandler.Handle(
            new GetAgentSnapshotsQuery(tick));
        AgentViewModel[] models = new AgentViewModel[snapshots.Count];
        for (int index = 0; index < snapshots.Count; index++)
        {
            models[index] = CreateViewModel(snapshots[index]);
        }

        return new ReadOnlyCollection<AgentViewModel>(models);
    }

    private static AgentViewModel CreateViewModel(AgentSnapshot snapshot)
    {
        AgentDecision? decision = snapshot.LastDecision;
        AgentActionSnapshot? action = snapshot.ActiveAction;
        IReadOnlyList<UtilityOptionDiagnostic> diagnostics = decision?.Options
            ?? Array.Empty<UtilityOptionDiagnostic>();
        AgentUtilityOptionViewModel[] options =
            new AgentUtilityOptionViewModel[diagnostics.Count];
        for (int index = 0; index < diagnostics.Count; index++)
        {
            UtilityOptionDiagnostic option = diagnostics[index];
            options[index] = new AgentUtilityOptionViewModel(
                option.IntentKind.ToString(),
                option.FinalScore,
                option.Available,
                option.Critical,
                option.Selected,
                option.ReasonCode,
                option.Detail);
        }

        return new AgentViewModel(
            snapshot.Id.ToString(),
            snapshot.Name,
            snapshot.Version,
            snapshot.IsAlive,
            snapshot.Position.X,
            snapshot.Position.Y,
            snapshot.Needs.Nutrition.Points,
            snapshot.Needs.Alertness.Points,
            snapshot.Needs.Mood.Points,
            snapshot.Needs.Health.Points,
            snapshot.ScheduledActivity.ToString(),
            action?.IntentKind.ToString() ?? decision?.SelectedIntent.ToString() ?? "Idle",
            action?.ElapsedTicks ?? 0,
            action?.RequiredTicks ?? 0,
            decision?.ReasonCode ?? "agents.decision.pending",
            decision?.Explanation ?? "No utility decision has been recorded yet.",
            options,
            snapshot.PositionZ,
            snapshot.AutomaticPlanningEnabled);
    }
}

}
