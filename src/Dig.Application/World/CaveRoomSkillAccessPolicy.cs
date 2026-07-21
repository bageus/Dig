using System;
using Dig.Domain.Agents;

namespace Dig.Application.World
{

public readonly struct CaveRoomSkillAccessResult
{
    public CaveRoomSkillAccessResult(bool allowed, int requiredUnits)
    {
        Allowed = allowed;
        RequiredUnits = requiredUnits;
    }

    public bool Allowed { get; }
    public int RequiredUnits { get; }
}

public sealed class CaveRoomSkillAccessPolicy
{
    public CaveRoomSkillAccessResult Evaluate(
        CaveRoomPresetKind kind,
        int maximumStoneworkUnits)
    {
        if (maximumStoneworkUnits < 0
            || maximumStoneworkUnits > AgentSkillCatalog.IndividualMaximumUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStoneworkUnits));
        }

        int required = RequiredUnits(kind);
        return new CaveRoomSkillAccessResult(
            maximumStoneworkUnits >= required,
            required);
    }

    public static int RequiredUnits(CaveRoomPresetKind kind)
    {
        return kind switch
        {
            CaveRoomPresetKind.Small => 0,
            CaveRoomPresetKind.Medium => AgentSkillCatalog.StoneworkThresholdUnits(1),
            CaveRoomPresetKind.Large => AgentSkillCatalog.StoneworkThresholdUnits(2),
            CaveRoomPresetKind.Tall => AgentSkillCatalog.StoneworkThresholdUnits(3),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}

}
