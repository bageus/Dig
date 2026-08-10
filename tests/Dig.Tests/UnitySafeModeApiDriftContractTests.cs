using System;
using System.IO;
using Dig.Application.Ecology;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Xunit;

namespace Dig.Tests
{
public sealed class UnitySafeModeApiDriftContractTests
{
    [Fact]
    public void Barrel_navigation_imports_the_authoritative_route_plan_namespace()
    {
        string source = ReadRuntime("DigTerrainWorkSession.BarrelNavigation.cs");

        Assert.Equal("Dig.Application.Jobs", typeof(TerrainWorkRoutePlan).Namespace);
        Assert.Contains("usingDig.Application.Jobs;", source);
        Assert.Contains("newTerrainWorkRoutePlan(", source);
    }

    [Fact]
    public void Mushroom_runtime_uses_current_application_contracts()
    {
        string source = ReadRuntime("DigTerrainWorkSession.Mushrooms.cs");

        Assert.NotNull(typeof(CompleteMushroomChopCommand).GetConstructor(new[]
        {
            typeof(EntityId),
            typeof(EntityId),
            typeof(long),
        }));
        Assert.Contains("MushroomErrors.NotFound", source);
        Assert.Contains("Result<bool>swing", source);
        Assert.Contains("if(!swing.Value)", source);
        Assert.Contains("job=_jobRepository.Get().Get(job.Id)", source);
        Assert.Contains("if(job.Stage!=JobStageKind.Finalize)", source);
        Assert.Contains("Result<MushroomChopCompletionResult>completed", source);
        int finalSwing = source.IndexOf("if(!swing.Value)", StringComparison.Ordinal);
        int reload = source.IndexOf("job=_jobRepository.Get().Get(job.Id)", StringComparison.Ordinal);
        Assert.True(finalSwing >= 0 && reload > finalSwing);
        Assert.Contains("newCompleteMushroomChopCommand(job.Id,DemoId('7',sequence),tick)", source);
        string playMode = Normalize(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "MushroomFinalSwingPlayModeTests.cs")));
        Assert.Contains(
            "Final_runtime_swing_commits_absent_site_completed_job_and_exact_drops",
            playMode);
        Assert.Contains("AdvanceMushroomJob", playMode);
        Assert.DoesNotContain("MushroomErrors.SiteNotFound", source);
        Assert.DoesNotContain("MushroomSwingCompletedResult", source);
        Assert.DoesNotContain("MushroomChopCompletedResult", source);
    }

    [Fact]
    public void Pickup_runtime_uses_one_sequence_and_exact_stack_identity_contract()
    {
        string source = ReadRuntime("DigWorldItemPickupSession.cs");

        Assert.NotNull(typeof(ItemStackSnapshot).GetProperty("StackId"));
        Assert.Null(typeof(ItemStackSnapshot).GetProperty("Id"));
        Assert.Equal(1, Count(source, "longsequence=checked(_nextWorldItemPickupSequence+1)"));
        Assert.Contains("GetStack(EntityId.Parse(stackId))", source);
        Assert.Contains("GetStack(stackId)", source);
        Assert.Contains("newCreateWorldItemPickupCommand(jobId,stack,resident", source);
        Assert.DoesNotContain("Where(value=>value.ItemId==", source);
        Assert.DoesNotContain("value.Id.ToString()", source);
        Assert.DoesNotContain("stackId=stack.Id.ToString()", source);
    }

    [Fact]
    public void Food_cursor_allocates_pixels_through_the_shared_cursor_size()
    {
        string source = ReadRuntime("DigWorldInteraction.FoodCursorTextures.cs");

        Assert.Contains("newColor32[CommandCursorSize*CommandCursorSize]", source);
        Assert.DoesNotContain("NewCursorPixels", source);
    }

    [Fact]
    public void Cave_room_partial_completion_uses_current_runtime_contracts()
    {
        string quarters = ReadRuntime("DigTerrainWorkExcavationQuarters.cs");
        string partial = ReadRuntime("DigTerrainWorkSession.PartialCompletion.cs");
        string owner = ReadRuntime("DigTerrainWorkSession.cs");

        Assert.Equal("Dig.Application.World", typeof(CaveRoomExcavationTarget).Namespace);
        Assert.Contains("usingDig.Application.World;", quarters);
        Assert.Contains("outCaveRoomExcavationTargetroomTarget", quarters);
        Assert.Contains(
            "PublishTerrainCompletionEffects(job.Id,target.Cell,tick,false);",
            partial);
        Assert.DoesNotContain("producedOutput:", partial);
        Assert.Contains(
            "PublishTerrainCompletionEffects(job.Id,targetCell,tick,!output.IsEmpty);",
            owner);
    }

    [Fact]
    public void Terrain_output_play_mode_uses_collection_constraints_for_typed_item_ids()
    {
        string source = Normalize(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            "TerrainOutputCatalogPlayModeTests.cs")));

        Assert.Contains("Has.Member(newItemId(\"material.stone\"))", source);
        Assert.Contains("Has.No.Member(newItemId(\"material.metal\"))", source);
        Assert.DoesNotContain("Does.Not.Contain(newItemId(", source);
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string ReadRuntime(string fileName)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(
            root,
            "Assets",
            "Dig.Unity",
            "Runtime",
            fileName);
        return Normalize(File.ReadAllText(path));
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

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);
}
}