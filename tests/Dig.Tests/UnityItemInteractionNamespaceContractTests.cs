using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class UnityItemInteractionNamespaceContractTests
{
    [Fact]
    public void Unity_files_using_domain_item_action_enums_import_inventory_namespace()
    {
        foreach (string path in Directory.GetFiles(
            RuntimeRoot(),
            "*.cs",
            SearchOption.TopDirectoryOnly))
        {
            string source = File.ReadAllText(path);
            bool usesInventoryAction = source.Contains(
                "ItemInventoryInteractionAction",
                StringComparison.Ordinal);
            bool usesWorldAction = source.Contains(
                "ItemWorldInteractionAction",
                StringComparison.Ordinal);
            if (!usesInventoryAction && !usesWorldAction)
            {
                continue;
            }

            bool importsNamespace = source.Contains(
                "using Dig.Domain.Inventory;",
                StringComparison.Ordinal);
            bool fullyQualifiesInventoryAction = source.Contains(
                "Dig.Domain.Inventory.ItemInventoryInteractionAction",
                StringComparison.Ordinal);
            bool fullyQualifiesWorldAction = source.Contains(
                "Dig.Domain.Inventory.ItemWorldInteractionAction",
                StringComparison.Ordinal);

            Assert.True(
                importsNamespace
                    || fullyQualifiesInventoryAction
                    || fullyQualifiesWorldAction,
                $"{Path.GetFileName(path)} uses an item interaction action without "
                    + "importing Dig.Domain.Inventory or fully qualifying the type.");
        }
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "unity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
