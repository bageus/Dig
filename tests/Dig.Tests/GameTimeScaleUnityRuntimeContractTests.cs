using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class GameTimeScaleUnityRuntimeContractTests
{
    [Fact]
    public void Clock_has_no_selection_dependent_day_length()
    {
        string clock = ReadRuntime("DigGameHudCanvas.Clock.cs");

        Assert.Contains("GameTimeCadence.Project(tick)", clock);
        Assert.Contains("GameTimeCadence.GameSecondsPerDay", clock);
        Assert.DoesNotContain("int ticksPerDay = 24", clock);
        Assert.DoesNotContain("tick % ticksPerDay", clock);
    }

    [Fact]
    public void Runtime_exposes_one_effective_real_to_game_coefficient()
    {
        string driver = ReadRuntime("DigAgentSimulationDriverBase.Hud.cs");

        Assert.Contains("EffectiveGameSecondsPerRealSecond", driver);
        Assert.Contains("GameTimeCadence.EffectiveGameSecondsPerRealSecond", driver);
        Assert.Contains("GameTimeCadence.Project(CurrentTick)", driver);
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity/Dig.Unity/Assets/Dig.Unity/Runtime",
            file));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
