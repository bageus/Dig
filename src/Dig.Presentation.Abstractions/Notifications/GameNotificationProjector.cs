using System;
using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Society;
using Dig.Domain.Technology;

namespace Dig.Presentation.Notifications
{

public sealed class GameNotificationProjector
{
    public GameNotification? Project(
        IDomainEvent domainEvent,
        ISet<string>? residentIds = null)
    {
        if (domainEvent is null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        return domainEvent switch
        {
            CombatAttackResolved attack => ProjectAttack(attack, residentIds),
            ResidentBorn born => Create(
                GameNotificationKind.ResidentBorn,
                born.EventId,
                born.Tick,
                priority: 60,
                "notification.resident.born",
                born.ResidentId.ToString(),
                new GameNotificationNavigationTarget(
                    GameNotificationNavigationKind.Resident,
                    born.ResidentId.ToString(),
                    born.Position)),
            AgentNeedThresholdCrossed need => ProjectNeed(need),
            ResidentLifeStageChanged stage
                when stage.CurrentStage == ResidentLifeStage.Old => Create(
                    GameNotificationKind.ResidentOld,
                    stage.EventId,
                    stage.Tick,
                    priority: 55,
                    "notification.resident.old",
                    stage.ResidentId.ToString(),
                    Resident(stage.ResidentId)),
            ResidentDied died => CreateDeath(
                died.ResidentId,
                died.Tick,
                died.LastKnownPosition),
            AgentDied died => CreateDeath(
                died.AgentId,
                died.Tick,
                died.LastKnownPosition),
            TechnologyUnlocked technology => Create(
                GameNotificationKind.TechnologyUnlocked,
                $"technology-unlocked:{technology.TechnologyId}",
                technology.Tick,
                priority: 50,
                "notification.technology.unlocked",
                technology.TechnologyId.ToString(),
                new GameNotificationNavigationTarget(
                    GameNotificationNavigationKind.Technology,
                    technology.TechnologyId.ToString())),
            JobStatusChanged job when job.CurrentStatus == JobStatus.Completed => Create(
                GameNotificationKind.JobCompleted,
                $"job-completed:{job.JobId}",
                job.Tick,
                priority: 40,
                "notification.job.completed",
                job.JobId.ToString(),
                new GameNotificationNavigationTarget(
                    GameNotificationNavigationKind.Job,
                    job.JobId.ToString())),
            _ => null,
        };
    }

    private static GameNotification? ProjectAttack(
        CombatAttackResolved attack,
        ISet<string>? residentIds)
    {
        string targetId = attack.Resolution.TargetId.ToString();
        if (attack.Resolution.WasAlreadyProcessed
            || (residentIds != null && !residentIds.Contains(targetId)))
        {
            return null;
        }

        return Create(
            GameNotificationKind.ResidentAttacked,
            attack.EventId,
            attack.Tick,
            priority: 90,
            "notification.resident.attacked",
            targetId,
            Resident(attack.Resolution.TargetId));
    }

    private static GameNotification ProjectNeed(AgentNeedThresholdCrossed need)
    {
        bool hunger = need.Kind == AgentNeedThresholdKind.Hunger;
        return Create(
            hunger
                ? GameNotificationKind.ResidentHungry
                : GameNotificationKind.ResidentMoodCritical,
            need.EventId,
            need.Tick,
            hunger ? 80 : 75,
            hunger
                ? "notification.resident.hungry"
                : "notification.resident.mood_critical",
            need.AgentId.ToString(),
            Resident(need.AgentId),
            new GameNotificationArgument(
                "value",
                need.CurrentValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static GameNotification CreateDeath(
        EntityId residentId,
        long tick,
        Dig.Domain.World.CellId? cell)
    {
        GameNotificationNavigationTarget target = cell.HasValue
            ? new GameNotificationNavigationTarget(
                GameNotificationNavigationKind.Cell,
                cell: cell)
            : Resident(residentId);
        return Create(
            GameNotificationKind.ResidentDied,
            $"resident-died:{residentId}:{tick}",
            tick,
            priority: 100,
            "notification.resident.died",
            residentId.ToString(),
            target);
    }

    private static GameNotification Create(
        GameNotificationKind kind,
        string sourceKey,
        long tick,
        int priority,
        string localizationKey,
        string sourceId,
        GameNotificationNavigationTarget target,
        params GameNotificationArgument[] additionalArguments)
    {
        List<GameNotificationArgument> arguments = new List<GameNotificationArgument>
        {
            new GameNotificationArgument("source_id", sourceId),
        };
        arguments.AddRange(additionalArguments);
        return new GameNotification(
            $"notification:{sourceKey}",
            kind,
            sourceKey,
            tick,
            priority,
            localizationKey,
            arguments,
            target);
    }

    private static GameNotificationNavigationTarget Resident(EntityId id)
    {
        return new GameNotificationNavigationTarget(
            GameNotificationNavigationKind.Resident,
            id.ToString());
    }
}

}
