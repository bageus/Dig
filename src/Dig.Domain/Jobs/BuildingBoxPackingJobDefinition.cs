using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class BuildingBoxPackingJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] PackingStages =
    {
        JobStageKind.TravelToDestination,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public BuildingBoxPackingJobDefinition(
        EntityId id,
        EntityId buildingId,
        EntityId outputStackId,
        CellId workPosition,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null,
        SkillGrantProfile? skillGrantProfile = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            PackingStages,
            dependencies)
    {
        if (buildingId.IsEmpty || outputStackId.IsEmpty)
        {
            throw new ArgumentException("Building and output stack ids are required.");
        }

        BuildingId = buildingId;
        OutputStackId = outputStackId;
        WorkPosition = workPosition;
        SkillGrantProfile = skillGrantProfile
            ?? DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.Logistics);
    }

    public EntityId BuildingId { get; }

    public EntityId OutputStackId { get; }

    public CellId WorkPosition { get; }

    public SkillGrantProfile SkillGrantProfile { get; }

    public override string Description => $"Pack building {BuildingId} into a box";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForDestination(BuildingId),
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}
}
