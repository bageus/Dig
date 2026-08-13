using System;
using System.IO;
using Dig.Presentation.Input;
using Xunit;

namespace Dig.Tests
{
public sealed class UnityInputParserMergeRegressionTests
{
    [Fact]
    public void Cursor_and_pointer_partials_keep_compile_safe_method_boundaries()
    {
        string runtime = RuntimeRoot();
        string cursor = Read(runtime, "DigWorldInteraction.DirectCommandCursor.cs");
        string itemCursor = Read(runtime, "DigWorldInteraction.ItemInteractionCursor.cs");
        string pointerHits = Read(runtime, "DigWorldInteraction.PointerHits.cs");
        string normalizedCursor = Normalize(cursor);
        string normalizedItemCursor = Normalize(itemCursor);
        string normalizedPointerHits = Normalize(pointerHits);

        Assert.Equal(Count(cursor, '{'), Count(cursor, '}'));
        Assert.Equal(Count(itemCursor, '{'), Count(itemCursor, '}'));
        Assert.Equal(Count(pointerHits, '{'), Count(pointerHits, '}'));
        Assert.Contains("TryResolveWorldItemPointerTarget", normalizedCursor);
        Assert.Contains(
            "privateboolTryResolveWorldItemPointerTarget",
            normalizedItemCursor);
        Assert.Contains(
            "privatevoidSetInteractionHighlightedItem",
            normalizedItemCursor);
        Assert.Contains(
            "best=candidate;}}agent=best!;returnbest!=null;}privateboolTryProjectResidentBounds",
            normalizedPointerHits);
        Assert.Contains("Eat=6", normalizedCursor);
        Assert.Contains("Sword=5", normalizedCursor);
        Assert.Contains("Drop=7", normalizedCursor);
    }

    [Fact]
    public void Food_and_barrel_input_identities_are_distinct()
    {
        Assert.NotEqual(
            (int)ContextWorldTargetKind.FoodItem,
            (int)ContextWorldTargetKind.Barrel);
        Assert.NotEqual(
            (int)ApplicationInputCommandKind.EatWorldItem,
            (int)ApplicationInputCommandKind.AttackBarrel);
        Assert.Equal(8, (int)ContextWorldTargetKind.Barrel);
        Assert.Equal(10, (int)ApplicationInputCommandKind.AttackBarrel);
    }

    [Fact]
    public void Bootstrap_binds_each_world_interaction_dependency_once()
    {
        string runtime = RuntimeRoot();
        string bootstrap = Normalize(Read(runtime, "DigUnityBootstrap.cs"));
        string interaction = Normalize(Read(runtime, "DigWorldInteraction.cs"));
        const string expectedCall =
            "interaction.Initialize(targetCamera,cameraController,worldSession,worldRenderer,"
            + "agentRenderer,creatureRenderer,mushroomRenderer,barrelRenderer,jobRenderer,"
            + "buildingRenderer,buildingInternalStockRenderer,itemRenderer,ghostRenderer,"
            + "terrainSession,agentSession,simulation,hud);";

        Assert.Equal(1, Count(bootstrap, "interaction.Initialize("));
        Assert.Contains(expectedCall, bootstrap);
        Assert.Contains("DigBarrelRendererbarrelRenderer", interaction);
        Assert.Contains("_barrelRenderer=barrelRenderer;", interaction);
        Assert.Contains("&&_barrelRenderer!=null", interaction);
    }

    [Fact]
    public void World_consumable_commands_use_the_overlay_HUD_contract()
    {
        string runtime = RuntimeRoot();
        string interaction = Normalize(Read(runtime, "DigWorldInteraction.cs"));
        string worldFood = Normalize(Read(runtime, "DigWorldInteraction.WorldFood.cs"));

        Assert.Contains("privateDigHudOverlay?_hud;", interaction);
        Assert.Contains("DigHudOverlayhud=_hud!;", worldFood);
        Assert.DoesNotContain("DigGameHudCanvashud=_hud!;", worldFood);
        Assert.Contains("hud.SetCommandResult(effectOwner);", worldFood);
        Assert.Contains("hud.SetJobs(jobs);", worldFood);
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

    private static int Count(string source, string value)
    {
        int count = 0;
        int start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);

    private static string Read(string root, string file)
    {
        return File.ReadAllText(Path.Combine(root, file));
    }

    private static string RuntimeRoot()
    {
        return Path.Combine(
            FindRepositoryRoot(),
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
