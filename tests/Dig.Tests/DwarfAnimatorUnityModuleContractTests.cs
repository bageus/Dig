using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class DwarfAnimatorUnityModuleContractTests
{
    [Fact]
    public void Dwarf_animator_bridge_requires_checked_in_animation_module()
    {
        string root = FindRepositoryRoot();
        string bridge = File.ReadAllText(Path.Combine(
            root,
            "unity", "Dig.Unity", "Assets", "DigDwarfs", "Scripts", "Runtime",
            "DwarfAnimatorBridge.cs"));
        string manifest = File.ReadAllText(Path.Combine(
            root,
            "unity", "Dig.Unity", "Packages", "manifest.json"));
        string lockFile = File.ReadAllText(Path.Combine(
            root,
            "unity", "Dig.Unity", "Packages", "packages-lock.json"));

        Assert.Contains("Animator.StringToHash", bridge);
        Assert.Contains("[SerializeField] private Animator animator", bridge);
        Assert.Contains(
            "\"com.unity.modules.animation\": \"1.0.0\"",
            manifest);
        Assert.Contains(
            "\"com.unity.modules.animation\"",
            lockFile);
        Assert.Contains(
            "\"com.unity.cloud.gltfast\": \"6.19.0\"",
            manifest);
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
