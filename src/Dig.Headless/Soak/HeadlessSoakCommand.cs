using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Dig.Headless.Soak
{

internal static class HeadlessSoakCommand
{
    public static int Run(string[] args)
    {
        HeadlessSoakConfiguration configuration = HeadlessSoakConfiguration.Parse(args);
        try
        {
            HeadlessSoakReport first = HeadlessSoakScenario.Execute(configuration);
            HeadlessSoakReport replay = HeadlessSoakScenario.Execute(configuration);
            first.DeterministicReplayMatched = string.Equals(
                first.StateHash,
                replay.StateHash,
                StringComparison.Ordinal);
            first.Succeeded = first.Succeeded
                && replay.Succeeded
                && first.DeterministicReplayMatched;
            WriteReport(configuration.ReportPath, first);
            PrintSummary(first);
            return first.Succeeded ? 0 : 1;
        }
        catch (Exception exception)
        {
            WriteFailure(configuration.ReportPath, configuration, exception);
            Console.Error.WriteLine($"Soak failed: {exception}");
            return 1;
        }
    }

    private static void PrintSummary(HeadlessSoakReport report)
    {
        Console.WriteLine(
            $"Soak profile={report.Profile}, residents={report.ResidentCount}, "
            + $"workers={report.HaulingWorkerCount}, "
            + $"ticks={report.RequestedTicks} (+drain to {report.FinalTick}) "
            + $"completed in {report.ElapsedMilliseconds:F1} ms; "
            + $"hash {report.StateHash}; replay={report.DeterministicReplayMatched}; "
            + $"ore {report.StoredOre}/{report.SpawnedOre}; "
            + $"events retained/dropped {report.RetainedEventCount}/{report.DroppedEventCount}.");
        foreach (HeadlessSoakSystemReport system in report.Systems.Take(5))
        {
            Console.WriteLine(
                $"  {system.Name}: total={system.TotalMilliseconds:F2} ms, "
                + $"avg={system.AverageMicroseconds:F2} us, "
                + $"max={system.MaximumMilliseconds:F2} ms, "
                + $"avg allocations={system.AverageAllocatedBytes} bytes");
        }

        foreach (string violation in report.InvariantViolations)
        {
            Console.Error.WriteLine($"Invariant violation: {violation}");
        }

        foreach (string violation in report.BudgetViolations)
        {
            Console.Error.WriteLine($"Budget violation: {violation}");
        }

        if (!report.DeterministicReplayMatched)
        {
            Console.Error.WriteLine("Deterministic replay produced a different state hash.");
        }
    }

    private static void WriteReport(string path, HeadlessSoakReport report)
    {
        EnsureDirectory(path);
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void WriteFailure(
        string path,
        HeadlessSoakConfiguration configuration,
        Exception exception)
    {
        EnsureDirectory(path);
        object failure = new
        {
            Profile = configuration.Profile.Name,
            configuration.Seed,
            RequestedTicks = configuration.TickCount,
            configuration.ResidentCount,
            configuration.HaulingWorkerCount,
            configuration.InitialFoodQuantity,
            Succeeded = false,
            ExceptionType = exception.GetType().FullName,
            exception.Message,
            exception.StackTrace,
        };
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                failure,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void EnsureDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
}
