using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;

namespace Dig.Presentation.World
{

public sealed class CaveTemplateTrimPresenter
{
    public CaveTemplateTrimVolumeViewModel Present(
        IReadOnlyCollection<CaveRoomPlan> completedPlans)
    {
        if (completedPlans == null)
        {
            throw new ArgumentNullException(nameof(completedPlans));
        }

        Dictionary<string, CaveTemplateTrimInstanceViewModel> instances =
            new Dictionary<string, CaveTemplateTrimInstanceViewModel>(
                StringComparer.Ordinal);
        foreach (CaveRoomPlan plan in completedPlans)
        {
            if (plan == null)
            {
                throw new ArgumentException(
                    "Completed cave room plans cannot contain null entries.",
                    nameof(completedPlans));
            }

            CaveTemplateTrimInstanceViewModel instance = Present(plan);
            if (!instances.TryAdd(instance.InstanceId, instance))
            {
                throw new ArgumentException(
                    $"Duplicate template cave trim instance '{instance.InstanceId}'.",
                    nameof(completedPlans));
            }
        }

        CaveTemplateTrimInstanceViewModel[] ordered = instances.Values
            .OrderBy(instance => instance.Entrance.Y)
            .ThenBy(instance => instance.Entrance.X)
            .ThenBy(instance => instance.TemplateId, StringComparer.Ordinal)
            .ToArray();
        return new CaveTemplateTrimVolumeViewModel(
            CalculateVersion(ordered),
            ordered);
    }

    private static CaveTemplateTrimInstanceViewModel Present(CaveRoomPlan plan)
    {
        CaveRoomPreset preset = plan.Preset;
        CaveTemplateTrimRowViewModel[] rows = new CaveTemplateTrimRowViewModel[
            preset.Height];
        for (int level = 0; level < preset.Height; level++)
        {
            int width = CaveRoomPlanner.InterpolateWidth(preset, level);
            int minX = plan.Entrance.X - ((width - 1) / 2);
            rows[level] = new CaveTemplateTrimRowViewModel(
                level,
                minX,
                plan.Entrance.Y - level,
                width);
        }

        int[] arches = new int[Math.Max(0, preset.Depth - 1)];
        for (int index = 0; index < arches.Length; index++)
        {
            arches[index] = index + 1;
        }

        string templateId = ResolveTemplateId(preset.Kind);
        string instanceId = $"{templateId}:{plan.Entrance.X}:{plan.Entrance.Y}";
        return new CaveTemplateTrimInstanceViewModel(
            instanceId,
            templateId,
            preset.Kind,
            plan.Entrance,
            preset.Depth,
            ResolveVariant(instanceId),
            hasBackWall: true,
            rows,
            arches);
    }

    private static string ResolveTemplateId(CaveRoomPresetKind kind)
    {
        switch (kind)
        {
            case CaveRoomPresetKind.Small:
                return "cave.template.small";
            case CaveRoomPresetKind.Medium:
                return "cave.template.medium";
            case CaveRoomPresetKind.Large:
                return "cave.template.large";
            case CaveRoomPresetKind.Tall:
                return "cave.template.tall";
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static byte ResolveVariant(string instanceId)
    {
        unchecked
        {
            uint hash = 2166136261u;
            for (int index = 0; index < instanceId.Length; index++)
            {
                hash ^= instanceId[index];
                hash *= 16777619u;
            }

            return (byte)(hash & 3u);
        }
    }

    private static long CalculateVersion(
        IReadOnlyList<CaveTemplateTrimInstanceViewModel> instances)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        for (int index = 0; index < instances.Count; index++)
        {
            CaveTemplateTrimInstanceViewModel instance = instances[index];
            MixString(ref hash, instance.InstanceId, prime);
            MixString(ref hash, instance.TemplateId, prime);
            Mix(ref hash, (ulong)(uint)instance.Entrance.X, prime);
            Mix(ref hash, (ulong)(uint)instance.Entrance.Y, prime);
            Mix(ref hash, (ulong)(uint)instance.Depth, prime);
            Mix(ref hash, instance.Variant, prime);
            Mix(ref hash, instance.HasBackWall ? 1UL : 0UL, prime);
            for (int rowIndex = 0; rowIndex < instance.Rows.Count; rowIndex++)
            {
                CaveTemplateTrimRowViewModel row = instance.Rows[rowIndex];
                Mix(ref hash, (ulong)(uint)row.Level, prime);
                Mix(ref hash, (ulong)(uint)row.MinX, prime);
                Mix(ref hash, (ulong)(uint)row.Y, prime);
                Mix(ref hash, (ulong)(uint)row.Width, prime);
            }

            for (int archIndex = 0; archIndex < instance.ArchDepths.Count; archIndex++)
            {
                Mix(ref hash, (ulong)(uint)instance.ArchDepths[archIndex], prime);
            }
        }

        return unchecked((long)(hash & (ulong)long.MaxValue));
    }

    private static void MixString(ref ulong hash, string value, ulong prime)
    {
        for (int index = 0; index < value.Length; index++)
        {
            Mix(ref hash, value[index], prime);
        }
    }

    private static void Mix(ref ulong hash, ulong value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }
}

}
