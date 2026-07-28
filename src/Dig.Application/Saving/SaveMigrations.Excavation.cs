using System;
using System.Collections.Generic;

namespace Dig.Application.Saving
{
public sealed class SaveVersionSevenWorldExcavationProgressMigration : ISaveMigration
{
    public string Id => "save.v7_to_v8.world_excavation_progress";
    public int FromVersion => 7;
    public int ToVersion => 8;

    public void Apply(SaveGameDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException(
                "Migration received the wrong source version.");
        }

        document.World ??= new WorldSaveData();
        document.World.Chunks ??= new List<WorldChunkSaveData>();
        foreach (WorldChunkSaveData chunk in document.World.Chunks)
        {
            if (chunk?.Cells == null)
            {
                continue;
            }

            foreach (WorldCellSaveData cell in chunk.Cells)
            {
                if (cell == null)
                {
                    continue;
                }

                cell.CompletedExcavationQuarters = 0;
                cell.ExcavationCutPattern = 0;
                cell.ExcavationSourceMaterialId = string.Empty;
            }
        }

        document.FormatVersion = ToVersion;
    }
}
}
