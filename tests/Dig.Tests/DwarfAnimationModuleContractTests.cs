using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class DwarfAnimationModuleContractTests
{
    [Fact]
    public void Animator_bridge_requires_animation_module_in_both_unity_hosts()
    {
        string rootManifest = Read("Packages", "manifest.json");
        string unityManifest = Read(
            "unity", "Dig.Unity", "Packages", "manifest.json");
        string unityLock = Read(
            "unity", "Dig.Unity", "Packages", "packages-lock.json");
        string asmdef = Read(
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime",
            "Dig.Unity.asmdef");
        string bridge = Read(
            "Assets", "DigDwarfs", "Scripts", "Runtime",
            "DwarfAnimatorBridge.cs");
        string quality = Read("tools", "quality", "check_unity_modules.py");

        Assert.Contains("Animator", bridge, StringComparison.Ordinal);
        Assert.Contains("com.unity.modules.animation", rootManifest,
            StringComparison.Ordinal);
        Assert.Contains("com.unity.modules.animation", unityManifest,
            StringComparison.Ordinal);
        Assert.Contains("com.unity.modules.animation", unityLock,
            StringComparison.Ordinal);
        Assert.Contains("UnityEngine.AnimationModule", asmdef,
            StringComparison.Ordinal);
        Assert.Contains("ROOT_MANIFEST_PATH", quality, StringComparison.Ordinal);
        Assert.Contains("UnityEngine.AnimationModule", quality,
            StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        string path = FindRepositoryRoot();
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
        }

        return File.ReadAllText(path);
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
