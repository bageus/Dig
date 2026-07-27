using System;
using System.Linq;
using Dig.Domain.Ecology;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameBuilder
{
    private static MushroomSaveData BuildMushrooms(MushroomState mushrooms)
    {
        if (mushrooms is null)
        {
            throw new ArgumentNullException(nameof(mushrooms));
        }

        MushroomSaveData data = new MushroomSaveData();
        foreach (MushroomSiteSnapshot site in mushrooms.GetAll()
            .OrderBy(value => value.SiteId.ToString(), StringComparer.Ordinal))
        {
            data.Sites.Add(new MushroomSiteSaveData
            {
                SiteId = site.SiteId.ToString(),
                DefinitionId = site.DefinitionId.ToString(),
                X = site.Cell.X,
                Y = site.Cell.Y,
                Z = site.Cell.Z,
                Stage = (int)site.Stage,
                StageStartedTick = site.StageStartedTick,
                NextStageTick = site.NextStageTick,
                GrowthGeneration = site.GrowthGeneration,
                ActiveChopJobId = site.ActiveChopJobId?.ToString(),
                ActiveWorkerId = site.ActiveWorkerId?.ToString(),
                RequiredSwings = site.RequiredSwings,
                CompletedSwings = site.CompletedSwings,
                GrowthPausedAtTick = site.GrowthPausedAtTick,
                Version = site.Version,
            });
        }

        return data;
    }
}

}
