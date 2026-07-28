using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class PointerHitUnitySyntaxContractTests
{
    [Fact]
    public void Resident_screen_pick_loop_closes_before_following_methods()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigWorldInteraction.PointerHits.cs"));
        string normalized = Normalize(source);

        Assert.Equal(Count(source, '{'), Count(source, '}'));
        Assert.Contains(
            "best=candidate;}}agent=best!;returnbest!=null;}privateboolTryProjectResidentBounds",
            normalized);
    }

    private static int Count(string source, char value)
    {
        int count = 0;
        for (int index = 0; index < source.Length; index++)
        {
            if (source[index] == value)
            {
                count++;
            }
        }

        return count;
    }

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);

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
