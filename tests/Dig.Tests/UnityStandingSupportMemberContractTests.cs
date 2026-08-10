using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Dig.Tests
{

public sealed class UnityStandingSupportMemberContractTests
{
    private static readonly Regex Declaration = new Regex(
        @"\b(?:private|internal)\s+bool\s+HasFullStandingSupport\s*\(\s*CellId\s+cell\s*\)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Terrain_work_session_has_one_authoritative_standing_support_member()
    {
        string runtime = Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime");
        List<string> declarations = Directory
            .GetFiles(runtime, "DigTerrainWorkSession*.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .SelectMany(path => Enumerable.Repeat(
                Path.GetFileName(path),
                Declaration.Matches(File.ReadAllText(path)).Count))
            .ToList();

        string declarationFile = Assert.Single(declarations);
        Assert.Equal(
            "DigTerrainWorkSession.SupportedActionPositions.cs",
            declarationFile);
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
