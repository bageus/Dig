using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Dig.Tests
{
public sealed class UnityItemInteractionNamespaceContractTests
{
    private static readonly string[] InventoryTypeNames =
    {
        "ItemFoodUseDefinition",
        "ItemInteractionCategoryIds",
        "ItemInteractionFeedbackKind",
        "ItemInteractionProfile",
        "ItemInteractionProfiles",
        "ItemInventoryInteractionAction",
        "ItemWorldInteractionAction",
    };

    [Fact]
    public void Unity_source_files_using_domain_item_interactions_import_inventory_namespace()
    {
        foreach (string path in Directory.GetFiles(
            UnitySourceRoot(),
            "*.cs",
            SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            if (source.Contains(
                    "using Dig.Domain.Inventory;",
                    StringComparison.Ordinal))
            {
                continue;
            }

            foreach (string typeName in InventoryTypeNames)
            {
                bool usesUnqualifiedType = Regex.IsMatch(
                    source,
                    $@"(?<![\w.]){Regex.Escape(typeName)}\b",
                    RegexOptions.CultureInvariant);
                Assert.False(
                    usesUnqualifiedType,
                    $"{RelativePath(path)} uses {typeName} without importing "
                        + "Dig.Domain.Inventory or fully qualifying the type.");
            }
        }
    }

    private static string UnitySourceRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity");
    }

    private static string RelativePath(string path)
    {
        return Path.GetRelativePath(FindRepositoryRoot(), path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
