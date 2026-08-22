using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class Stage1AuthoritativeContractsTests
{
    [Fact]
    public void Unity_sessions_share_one_simulation_state_and_only_agent_session_advances_clock()
    {
        string runtime = RuntimeRoot();
        string agent = Normalize(Read(runtime, "DigAgentSession.cs"));
        string world = Normalize(Read(runtime, "DigWorldSession.cs"));
        string bootstrap = Normalize(Read(runtime, "DigUnityBootstrap.cs"));

        Assert.Contains("publiclongTick=>_simulationState.Clock.TickIndex", agent);
        Assert.Contains("internalSimulationStateSimulationState=>_simulationState", agent);
        Assert.Contains("internalSimulationStateSimulationState=>_simulationState", world);
        Assert.Contains("simulationState:simulationState", bootstrap);
        Assert.Equal(1, Count(agent, "_simulationState.Clock.AdvanceOneTick()"));
        Assert.DoesNotContain("private long _tick", agent);
        Assert.DoesNotContain("private long _tick", world);
        Assert.DoesNotContain("_tick=checked(_tick+1)", agent);
        Assert.DoesNotContain("_tick=checked(_tick+1)", world);
        Assert.DoesNotContain("_tick++", world);
    }

    [Fact]
    public void Exploration_implementation_documents_the_26_neighbor_corner_cutting_contract()
    {
        string design = ReadRepository("docs/design/exploration-fog-of-war.md");
        string implementation = ReadRepository("docs/implementation/exploration-fog-of-war.md");

        Assert.Contains("26-связному графу", design);
        Assert.Contains("Диагональный corner cutting разрешён", design);
        Assert.Contains("все 26 соседей", implementation);
        Assert.Contains("corner cutting разрешён", implementation);
    }

    private static int Count(string source, string fragment)
    {
        int count = 0;
        int start = 0;
        while ((start = source.IndexOf(fragment, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += fragment.Length;
        }

        return count;
    }

    private static string Read(string runtime, string file)
    {
        return File.ReadAllText(Path.Combine(runtime, file));
    }

    private static string ReadRepository(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(FindRepositoryRoot(), "Assets", "Dig.Unity", "Runtime");
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

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}

}
