using System;
using System.IO;
using System.Text.Json;
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
            "Packages", "manifest.json");
        string unityLock = Read(
            "Packages", "packages-lock.json");
        string asmdef = Read(
            "Assets", "Dig.Unity", "Runtime",
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

    [Fact]
    public void Dig_unity_package_graph_is_valid_json_and_uses_only_gltfast()
    {
        string unityManifest = Read(
            "Packages", "manifest.json");
        string unityLock = Read(
            "Packages", "packages-lock.json");

        using JsonDocument manifestDocument = JsonDocument.Parse(unityManifest);
        using JsonDocument lockDocument = JsonDocument.Parse(unityLock);

        JsonElement manifestDependencies = manifestDocument.RootElement
            .GetProperty("dependencies");
        JsonElement lockDependencies = lockDocument.RootElement
            .GetProperty("dependencies");

        Assert.Equal(
            "6.19.0",
            manifestDependencies.GetProperty("com.unity.cloud.gltfast").GetString());
        Assert.Equal(
            "6.19.0",
            lockDependencies.GetProperty("com.unity.cloud.gltfast")
                .GetProperty("version")
                .GetString());
        Assert.False(manifestDependencies.TryGetProperty(
            "org.khronos.unitygltf",
            out _));
        Assert.False(lockDependencies.TryGetProperty(
            "org.khronos.unitygltf",
            out _));

        Assert.DoesNotContain("<<<<<<<", unityManifest, StringComparison.Ordinal);
        Assert.DoesNotContain("=======", unityManifest, StringComparison.Ordinal);
        Assert.DoesNotContain(">>>>>>>", unityManifest, StringComparison.Ordinal);
        Assert.DoesNotContain("<<<<<<<", unityLock, StringComparison.Ordinal);
        Assert.DoesNotContain("=======", unityLock, StringComparison.Ordinal);
        Assert.DoesNotContain(">>>>>>>", unityLock, StringComparison.Ordinal);
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
