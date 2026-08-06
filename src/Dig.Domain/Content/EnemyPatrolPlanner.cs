using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Content
{

public readonly struct EnemyPatrolDecision
{
    public EnemyPatrolDecision(bool shouldMove, CellId target, string reasonCode)
    {
        ShouldMove = shouldMove;
        Target = target;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode)
            ? "unspecified"
            : reasonCode.Trim();
    }

    public bool ShouldMove { get; }
    public CellId Target { get; }
    public string ReasonCode { get; }
}

public sealed class EnemyPatrolPlanner
{
    public EnemyPatrolDecision Plan(
        EnemyCombatDefinition definition,
        EntityId enemyId,
        CellId anchor,
        CellId current,
        TunnelNavigationVolume volume,
        ulong worldSeed,
        long tick)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (volume == null)
        {
            throw new ArgumentNullException(nameof(volume));
        }

        if (enemyId.IsEmpty || tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(enemyId));
        }

        if (!definition.HasPatrol)
        {
            return new EnemyPatrolDecision(false, current, "patrol_disabled");
        }

        if (tick == 0 || tick % definition.PatrolIntervalTicks != 0)
        {
            return new EnemyPatrolDecision(false, current, "patrol_not_due");
        }

        CellId[] candidates = BuildCandidates(
            definition,
            current,
            anchor,
            definition.PatrolWanderRadius,
            volume);
        if (candidates.Length == 0)
        {
            return new EnemyPatrolDecision(false, current, "patrol_blocked");
        }

        long sequence = tick / definition.PatrolIntervalTicks;
        RandomStreamCatalog streams = new RandomStreamCatalog(worldSeed);
        DeterministicRandomStream stream = streams.GetOrCreate(
            "enemy.patrol:" + enemyId + ":" + sequence);
        return new EnemyPatrolDecision(
            true,
            candidates[stream.NextInt(candidates.Length)],
            "patrol_step");
    }

    private static CellId[] BuildCandidates(
        EnemyCombatDefinition definition,
        CellId current,
        CellId anchor,
        int wanderRadius,
        TunnelNavigationVolume volume)
    {
        HashSet<CellId> candidates = new HashSet<CellId>();
        CellId[] cardinal =
        {
            new CellId(current.X - 1, current.Y, current.Z),
            new CellId(current.X + 1, current.Y, current.Z),
            new CellId(current.X, current.Y, current.Z - 1),
            new CellId(current.X, current.Y, current.Z + 1),
        };
        for (int index = 0; index < cardinal.Length; index++)
        {
            if (IsAllowedStep(definition, current, cardinal[index], volume))
            {
                candidates.Add(cardinal[index]);
            }
        }

        if (definition.Traversal.HasFlag(EnemyTraversalCapability.VerticalClimb))
        {
            foreach (int deltaY in new[] { -1, 1 })
            {
                CellId target = new CellId(current.X, current.Y + deltaY, current.Z);
                if (IsAllowedStep(definition, current, target, volume))
                {
                    candidates.Add(target);
                }
            }
        }

        foreach (int deltaX in new[] { -1, 1 })
        {
            foreach (int deltaZ in new[] { -1, 1 })
            {
                CellId target = new CellId(
                    current.X + deltaX,
                    current.Y,
                    current.Z + deltaZ);
                if (IsLegalDiagonal(current, target, volume))
                {
                    candidates.Add(target);
                }
            }
        }

        return candidates
            .Where(candidate => ChebyshevDistance(anchor, candidate)
                <= wanderRadius)
            .OrderBy(candidate => candidate)
            .ToArray();
    }

    private static bool IsLegalDiagonal(
        CellId current,
        CellId target,
        TunnelNavigationVolume volume)
    {
        if (!volume.Contains(target)
            || target.Y != current.Y
            || Math.Abs(target.X - current.X) != 1
            || Math.Abs(target.Z - current.Z) != 1
            || !volume.HasFullActorSupport(target))
        {
            return false;
        }

        CellId sideX = new CellId(target.X, current.Y, current.Z);
        CellId sideZ = new CellId(current.X, current.Y, target.Z);
        return IsFlatSupportedStep(current, sideX, volume)
            && IsFlatSupportedStep(current, sideZ, volume)
            && IsFlatSupportedStep(sideX, target, volume)
            && IsFlatSupportedStep(sideZ, target, volume);
    }

    private static bool IsFlatSupportedStep(
        CellId from,
        CellId to,
        TunnelNavigationVolume volume)
    {
        if (!volume.Contains(to)
            || from.Y != to.Y
            || !volume.HasFullActorSupport(to))
        {
            return false;
        }

        TunnelTraversalKind traversal = volume.ClassifyTraversal(from, to);
        return traversal == TunnelTraversalKind.SupportedWalk
            || traversal == TunnelTraversalKind.DepthTraverse;
    }

    private static bool IsAllowedStep(
        EnemyCombatDefinition definition,
        CellId from,
        CellId to,
        TunnelNavigationVolume volume)
    {
        if (!volume.Contains(to))
        {
            return false;
        }

        TunnelTraversalKind traversal = volume.ClassifyTraversal(from, to);
        if (traversal == TunnelTraversalKind.VerticalClimb)
        {
            return definition.Traversal.HasFlag(
                EnemyTraversalCapability.VerticalClimb);
        }

        return IsFlatSupportedStep(from, to, volume);
    }

    private static int ChebyshevDistance(CellId first, CellId second)
    {
        return Math.Max(
            Math.Max(
                Math.Abs(first.X - second.X),
                Math.Abs(first.Y - second.Y)),
            Math.Abs(first.Z - second.Z));
    }
}

}
