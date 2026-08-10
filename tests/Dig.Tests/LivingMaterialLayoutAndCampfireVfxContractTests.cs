using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialLayoutAndCampfireVfxContractTests
{
    [Fact]
    public void Hidden_living_material_proxies_do_not_consume_item_layout_slots()
    {
        string policy = ReadRuntime("DigWorldItemVisualPolicy.cs");
        string renderer = ReadRuntime("DigWorldItemRenderer.cs");
        string visual = ReadRuntime("DigWorldItemVisual.cs");
        string playMode = ReadPlayMode(
            "LivingMaterialWorldItemLayoutPlayModeTests.cs");

        Assert.Contains("IsLivingMaterial(string itemId)", policy);
        Assert.Contains("creature.hamster", policy);
        Assert.Contains("creature.grub", policy);
        Assert.Contains("creature.larva", policy);
        Assert.Contains("ConsumesCellLayoutSlot", policy);
        Assert.Contains(
            "DigWorldItemVisualPolicy.ConsumesCellLayoutSlot(item.ItemId)",
            renderer);
        Assert.Contains("if (consumesCellSlot)", renderer);
        Assert.Contains("!consumesCellSlot", renderer);
        Assert.Contains(
            "DigWorldItemVisualPolicy.IsLivingMaterial(Model.ItemId)",
            visual);
        Assert.DoesNotContain(
            "private static bool IsLivingMaterial",
            visual);
        Assert.Contains(
            "Hidden_living_material_proxy_does_not_shift_unfinished_package",
            playMode);
        Assert.Contains("livingInPackageCell, package", playMode);
        Assert.Contains("AssertStable", playMode);
    }

    [Fact]
    public void Campfire_and_sky_particles_are_not_emitted_by_runtime()
    {
        string runtime = ReadRuntime("DigPresentationEffectRuntime.cs");
        string presenter = ReadPresentation("PresentationEffectPresenter.cs");
        string presenterTests = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Dig.Tests",
            "PresentationEffectPresenterTests.cs"));

        Assert.Contains("FilterCampfireProductionParticleEvents", runtime);
        Assert.Contains("_campfireBuildingIds", runtime);
        Assert.DoesNotContain(
            "private const int AmbientDustIntervalTicks",
            runtime);
        Assert.DoesNotContain(
            "new PresentationEffectFact(\n                \"ambient-dust:",
            runtime);
        Assert.DoesNotContain("vfx.production.campfire", presenter);
        Assert.Contains(
            "Campfire_glow_creates_light_without_particle_effect",
            presenterTests);
        Assert.Contains("Assert.Empty(frame.Effects)", presenterTests);
    }

    private static string ReadRuntime(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime",
            file));
    }

    private static string ReadPlayMode(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Tests",
            "PlayMode",
            file));
    }

    private static string ReadPresentation(string file)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Dig.Presentation.Abstractions",
            "Rendering",
            file));
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
