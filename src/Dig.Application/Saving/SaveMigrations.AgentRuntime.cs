using System;

namespace Dig.Application.Saving
{

public sealed class SaveVersionEightAgentRuntimeMigration : ISaveMigration
{
    public string Id => "save.v8_to_v9.agent_runtime";
    public int FromVersion => 8;
    public int ToVersion => 9;

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

        document.AgentRuntime ??= new AgentRuntimeSaveData();
        document.FormatVersion = ToVersion;
    }
}

}
