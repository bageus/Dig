using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result<MushroomState> BuildMushroomState(
        MushroomSaveData data,
        MushroomCatalog? catalog,
        JobSystem jobs)
    {
        if (data is null || data.Sites is null)
        {
            throw new InvalidOperationException("Mushroom save data is missing.");
        }

        if (data.Sites.Count > 0 && catalog is null)
        {
            return Result<MushroomState>.Failure(SaveErrors.UnknownMushroomDefinition);
        }

        MushroomCatalog resolvedCatalog = catalog ?? new MushroomCatalog(
            Array.Empty<MushroomDefinition>());
        List<MushroomSiteSnapshot> snapshots = new List<MushroomSiteSnapshot>();
        foreach (MushroomSiteSaveData site in data.Sites
            .OrderBy(value => value.SiteId, StringComparer.Ordinal))
        {
            if (site is null
                || !Enum.IsDefined(typeof(MushroomStage), site.Stage)
                || !resolvedCatalog.Contains(new MushroomDefinitionId(site.DefinitionId)))
            {
                return Result<MushroomState>.Failure(SaveErrors.UnknownMushroomDefinition);
            }

            EntityId? activeJob = ParseOptionalId(site.ActiveChopJobId);
            EntityId? activeWorker = ParseOptionalId(site.ActiveWorkerId);
            if (activeJob.HasValue)
            {
                JobSnapshot? job = jobs.Get(activeJob.Value);
                if (job?.Definition is not MushroomChopJobDefinition definition
                    || definition.SiteId != EntityId.Parse(site.SiteId)
                    || job.IsTerminal
                    || job.AssignedAgentId != activeWorker)
                {
                    return Result<MushroomState>.Failure(SaveErrors.InvalidDocument);
                }
            }

            snapshots.Add(new MushroomSiteSnapshot(
                EntityId.Parse(site.SiteId),
                new MushroomDefinitionId(site.DefinitionId),
                new CellId(site.X, site.Y, site.Z),
                (MushroomStage)site.Stage,
                site.StageStartedTick,
                site.NextStageTick,
                site.GrowthGeneration,
                activeJob,
                activeWorker,
                site.RequiredSwings,
                site.CompletedSwings,
                site.GrowthPausedAtTick,
                site.Version));
        }

        return MushroomState.Restore(resolvedCatalog, snapshots);
    }

    private static EntityId? ParseOptionalId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : EntityId.Parse(value);
}

}
