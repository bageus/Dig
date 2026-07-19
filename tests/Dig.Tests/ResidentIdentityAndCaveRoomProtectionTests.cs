using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentIdentityAndCaveRoomProtectionTests
{
    [Fact]
    public void Resident_identity_generation_is_deterministic_and_unique()
    {
        ResidentIdentityGenerator generator = new ResidentIdentityGenerator();

        ResidentIdentity[] first = generator.Generate(80).ToArray();
        ResidentIdentity[] second = generator.Generate(80).ToArray();

        Assert.Equal(80, first.Select(identity => identity.Id).Distinct().Count());
        Assert.Equal(80, first.Select(identity => identity.Name).Distinct().Count());
        Assert.Equal(
            first.Select(identity => identity.Id),
            second.Select(identity => identity.Id));
        Assert.Equal(
            first.Select(identity => identity.Name),
            second.Select(identity => identity.Name));
    }

    [Fact]
    public void Completed_room_shell_protects_roof_sides_and_floor_support()
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Small);
        CellId entrance = new CellId(10, 9);
        CaveRoomPlan plan = new CaveRoomPlan(
            preset,
            entrance,
            Array.Empty<CellId>(),
            Array.Empty<SpatialCellId>(),
            new[]
            {
                new CellId(9, 6),
                new CellId(10, 6),
                new CellId(11, 6),
            });

        CellId[] shell = new CaveRoomShellProtectionPolicy()
            .Resolve(plan, new WorldSize(20, 14))
            .ToArray();

        Assert.Contains(new CellId(9, 6), shell);
        Assert.Contains(new CellId(7, 9), shell);
        Assert.Contains(new CellId(13, 9), shell);
        Assert.Contains(new CellId(8, 10), shell);
        Assert.Contains(new CellId(12, 10), shell);
        Assert.DoesNotContain(entrance, shell);
    }
}

}