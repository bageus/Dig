using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
public sealed class AuthoritativeCoordinateMigrationTests
{
    private static readonly EntityId StackId =
        EntityId.Parse("fa000000000000000000000000000001");
    private static readonly EntityId JobId =
        EntityId.Parse("fb000000000000000000000000000002");

    [Fact]
    public void Version_four_normalizes_every_legacy_coordinate_owner_to_z_zero()
    {
        SaveGameDocument document = CreateVersionFourDocument();

        new SaveVersionFourAuthoritativeCoordinatesMigration().Apply(document);

        Assert.Equal(5, document.FormatVersion);
        Assert.Equal(WorldSize.RequiredDepth, document.World.Depth);
        Assert.Equal(0, document.World.Chunks[0].Z);
        Assert.Equal(0, document.World.Chunks[0].Cells[0].Z);
        Assert.Equal(0, document.Inventory.Stacks[0].Location.CellZ);
        Assert.Equal(0, document.Buildings.Buildings[0].OriginZ);
        Assert.Equal(0, document.Buildings.Buildings[0].WorkPositionZ);
        Assert.Equal(0, document.AgentPositions.Agents[0].Z);
        Assert.Equal(0, document.TerrainDeposits.Deposits[0].Z);
        Assert.Equal(
            new[]
            {
                "approach.x",
                "approach.y",
                "approach.z",
                "target.x",
                "target.y",
                "target.z",
            },
            document.Jobs.Jobs[0].Definition.Properties.ConvertAll(value => value.Key));
        Assert.Equal("20,21,0", document.Jobs.Reservations[0].Value);
        Assert.Equal("22,23,0", document.Jobs.Reservations[1].Value);
        Assert.Equal("unchanged", document.Jobs.Reservations[2].Value);
    }

    private static SaveGameDocument CreateVersionFourDocument()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 4,
            Metadata = new SaveMetadataData { SlotId = "v4" },
            World = new WorldSaveData { Depth = 1 },
            Inventory = new InventorySaveData(),
            Jobs = new JobsSaveData(),
            Buildings = new BuildingsSaveData(),
            AgentPositions = new AgentPositionsSaveData(),
            TerrainDeposits = new TerrainDepositsSaveData(),
        };
        document.World.Chunks.Add(new WorldChunkSaveData
        {
            X = 1,
            Y = 2,
            Z = 3,
            Cells =
            {
                new WorldCellSaveData { X = 4, Y = 5, Z = 2 },
            },
        });
        document.Inventory.Stacks.Add(new ItemStackSaveData
        {
            StackId = StackId.ToString(),
            ItemId = "ore.test",
            Quantity = 1,
            Location = new ItemLocationSaveData
            {
                CellX = 6,
                CellY = 7,
                CellZ = 3,
            },
        });
        document.Buildings.Buildings.Add(new BuildingSaveData
        {
            BuildingId = "fd000000000000000000000000000004",
            OriginX = 8,
            OriginY = 9,
            OriginZ = 2,
            WorkPositionX = 10,
            WorkPositionY = 11,
            WorkPositionZ = 3,
        });
        document.AgentPositions.Agents.Add(new AgentPositionSaveData
        {
            AgentId = "fe000000000000000000000000000005",
            X = 12,
            Y = 13,
            Z = 2,
        });
        document.TerrainDeposits.Deposits.Add(new TerrainDepositSaveData
        {
            InstanceId = "legacy-deposit",
            DefinitionId = "deposit.test",
            X = 14,
            Y = 15,
            Z = 3,
        });
        document.Jobs.Jobs.Add(new JobSaveData
        {
            Definition = CreateLegacyJobDefinition(),
        });
        AddReservations(document);
        return document;
    }

    private static JobDefinitionSaveData CreateLegacyJobDefinition()
    {
        return new JobDefinitionSaveData
        {
            TypeId = DigJobDefinitionSaveCodec.StableTypeId,
            JobId = JobId.ToString(),
            Properties =
            {
                new SavePropertyData { Key = "target.y", Value = "17" },
                new SavePropertyData { Key = "approach.x", Value = "18" },
                new SavePropertyData { Key = "target.x", Value = "16" },
                new SavePropertyData { Key = "approach.y", Value = "19" },
            },
        };
    }

    private static void AddReservations(SaveGameDocument document)
    {
        document.Jobs.Reservations.Add(new JobReservationSaveData
        {
            Kind = (int)ReservationKind.Position,
            Value = "20,21",
        });
        document.Jobs.Reservations.Add(new JobReservationSaveData
        {
            Kind = (int)ReservationKind.Designation,
            Value = "22,23,3",
        });
        document.Jobs.Reservations.Add(new JobReservationSaveData
        {
            Kind = (int)ReservationKind.Item,
            Value = "unchanged",
        });
    }
}
}
