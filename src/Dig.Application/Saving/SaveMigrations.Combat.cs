using System;

namespace Dig.Application.Saving
{

public sealed class SaveVersionNineCombatSpatialMigration : ISaveMigration
{
    public string Id => "save.v9_to_v10.combat_spatial";
    public int FromVersion => 9;
    public int ToVersion => 10;

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

        document.Combat ??= new CombatSaveData();
        document.FormatVersion = ToVersion;
    }
}
}
