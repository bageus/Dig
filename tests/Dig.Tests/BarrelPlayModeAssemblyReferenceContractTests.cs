using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace Dig.Tests
{
public sealed class BarrelPlayModeAssemblyReferenceContractTests
{
    [Fact]
    public void Barrel_integration_fixture_has_required_assembly_references()
    {
        string root = FindRepositoryRoot();
        string fixturePath = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "BarrelDestructionPlayModeTests.cs");
        string asmdefPath = Path.Combine(
            root,
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "Dig.Unity.PlayModeTests.asmdef");

        string fixture = File.ReadAllText(fixturePath);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(asmdefPath));
        JsonElement references = document.RootElement.GetProperty("references");

        Assert.Contains("using Dig.Application.WorldObjects;", fixture);
        Assert.Contains("using Dig.Infrastructure.InMemory;", fixture);
        Assert.Contains(
            references.EnumerateArray(),
            value => value.GetString() == "Dig.Application");
        Assert.Contains(
            references.EnumerateArray(),
            value => value.GetString() == "Dig.Infrastructure");
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
