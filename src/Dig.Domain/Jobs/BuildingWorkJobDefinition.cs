using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public enum BuildingWorkKind
{
    Construction = 0,
    Repair = 1,
    Demolition = 2,
}

public sealed class BuildingWorkJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public BuildingWorkJobDefinition(
        EntityId id,
        EntityId buildingId,
        BuildingWorkKind kind,
        CellId workPosition,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null,
        SkillGrantProfile? skillGrantProfile = null)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies)
    {
        if (buildingId.IsEmpty)
        {
            throw new ArgumentException("Building id cannot be empty.", nameof(buildingId));
        }

        BuildingId = buildingId;
        Kind = kind;
        WorkPosition = workPosition;
        SkillGrantProfile = skillGrantProfile ?? CreateSkillProfile(kind);
    }

    public EntityId BuildingId { get; }

    public BuildingWorkKind Kind { get; }

    public CellId WorkPosition { get; }

    public SkillGrantProfile SkillGrantProfile { get; }

    public override string Description =>
        $"{Kind}:{BuildingId}@{WorkPosition}";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(WorkPosition),
        });
    }

    private static SkillGrantProfile CreateSkillProfile(BuildingWorkKind kind)
    {
        return kind == BuildingWorkKind.Construction
            ? DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Construction)
            : DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Logistics);
    }
}
}
