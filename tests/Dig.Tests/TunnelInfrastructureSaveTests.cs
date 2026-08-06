using System;
using System.IO;
using System.Linq;
using Dig.Application.Saving;
using Dig.Application.Tunnels;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelInfrastructureSaveTests
{
    private static readonly EntityId SupportSegmentId = Id(1);
    private static readonly EntityId CompletedJunctionSegmentId = Id(2);
    private static readonly EntityId PendingJunctionSegmentId = Id(3);

    [Fact]
    public void Adapter_round_trip_preserves_anchors_trim_targets_and_sequence()
    {
        JobSystem jobs = new JobSystem();
        TunnelInfrastructureRuntimeSnapshot runtime = CreateRuntime(sequence: 8);

        TunnelInfrastructureSaveData saved =
            TunnelInfrastructureSaveAdapter.Encode(runtime, jobs);
        Result<TunnelInfrastructureRuntimeSnapshot> restored =
            TunnelInfrastructureSaveAdapter.Decode(saved, jobs);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        Assert.Equal((ulong)8, restored.Value.NextAutomaticJobSequence);
        AssertSnapshotsEqual(
            runtime.Infrastructure,
            restored.Value.Infrastructure);
        Assert.Single(saved.CompletedJunctionStoneTrimCells);
        Assert.Single(saved.PendingJunctionStoneTrimTargets);
        Assert.Equal(
            new CellId(15, 1, 0),
            restored.Value.Infrastructure.Segments
                .Single(value => value.SegmentId == SupportSegmentId)
                .NextAutomaticSupportTarget!.Value.TargetCell);
    }

    [Fact]
    public void Decode_rejects_obsolete_saved_target_instead_of_restoring_it()
    {
        JobSystem jobs = new JobSystem();
        TunnelInfrastructureSaveData saved =
            TunnelInfrastructureSaveAdapter.Encode(CreateRuntime(8), jobs);
        TunnelSegmentSaveData support = saved.Segments.Single(
            value => value.SegmentId == SupportSegmentId.ToString());
        support.NextAutomaticSupportTarget!.TargetX = 10;

        Result<TunnelInfrastructureRuntimeSnapshot> restored =
            TunnelInfrastructureSaveAdapter.Decode(saved, jobs);

        Assert.True(restored.IsFailure);
        Assert.Equal(TunnelInfrastructureSaveErrors.InvalidSnapshot, restored.Error);
    }

    [Fact]
    public void Adapter_rejects_sequence_that_can_reuse_an_existing_job_id()
    {
        JobSystem jobs = new JobSystem();
        EntityId jobId = AutomaticJobId(8);
        RequireSuccess(jobs.Add(new TunnelAutomaticWorkJobDefinition(
            jobId,
            SupportSegmentId,
            TunnelAutomaticWorkKind.WoodenSupport,
            new CellId(15, 1, 0),
            createdTick: 1,
            JobRetryPolicy.Default)));

        Assert.Throws<InvalidOperationException>(() =>
            TunnelInfrastructureSaveAdapter.Encode(CreateRuntime(8), jobs));
    }

    [Fact]
    public void Version_fourteen_migration_adds_no_anchors_and_advances_sequence()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 14,
            TunnelInfrastructure = null!,
            Jobs = new JobsSaveData
            {
                Jobs =
                {
                    new JobSaveData
                    {
                        Definition = new JobDefinitionSaveData
                        {
                            TypeId = new TunnelAutomaticWorkJobSaveCodec().TypeId,
                            JobId = AutomaticJobId(5).ToString(),
                        },
                    },
                },
            },
        };

        new SaveVersionFourteenTunnelInfrastructureMigration().Apply(document);

        Assert.Equal(15, document.FormatVersion);
        Assert.NotNull(document.TunnelInfrastructure);
        Assert.Equal((ulong)6, document.TunnelInfrastructure.NextAutomaticJobSequence);
        Assert.Empty(document.TunnelInfrastructure.Segments);
        Assert.Empty(document.TunnelInfrastructure.CompletedJunctionStoneTrimCells);
        Assert.Empty(document.TunnelInfrastructure.PendingJunctionStoneTrimTargets);
    }

    [Fact]
    public void Unity_runtime_captures_and_restores_state_and_sequence()
    {
        string runtime = Path.Combine(
            FindRepositoryRoot(),
            "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
        string source = Normalize(File.ReadAllText(Path.Combine(
            runtime,
            "DigTerrainTunnelInfrastructure.Saving.cs")));

        Assert.Contains("CaptureTunnelInfrastructureRuntimeState()", source);
        Assert.Contains("_tunnelInfrastructure!.Get().CaptureSnapshot()", source);
        Assert.Contains("RestoreTunnelInfrastructureRuntimeState(", source);
        Assert.Contains(
            "TunnelInfrastructureState.Restore(runtime.Infrastructure)",
            source);
        Assert.Contains(
            "_tunnelAutomaticJobSequence=runtime.NextAutomaticJobSequence",
            source);
        Assert.Contains("PublishTunnelInfrastructureVisuals()", source);
    }

    [Fact]
    public void Save_document_round_trip_exposes_validated_tunnel_runtime()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(new MaterialId("terrain.rock"), true, 100),
        });
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(
                new ItemId("material.test"),
                "Test material",
                maximumStackSize: 10,
                isTool: false),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(64, 4),
            chunkSize: 4,
            materials,
            new MaterialId("terrain.rock"),
            explored: true).Value;
        InventoryState inventory = new InventoryState(items);
        JobSystem jobs = new JobSystem();
        TunnelInfrastructureRuntimeSnapshot runtime = CreateRuntime(12);
        JobDefinitionSaveRegistry registry = new JobDefinitionSaveRegistry(new[]
        {
            new TunnelAutomaticWorkJobSaveCodec(),
        });
        SaveGameBuilder builder = new SaveGameBuilder(registry);
        SaveGameContext context = new SaveGameContext(
            Metadata(),
            world,
            inventory,
            jobs,
            new BuildingsState(),
            Array.Empty<AgentState>(),
            tunnelInfrastructure: runtime);

        SaveGameDocument document = builder.Build(context);
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        byte[] first = codec.Serialize(document);
        Result<LoadedGameState> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            registry).Load(codec.Deserialize(first), materials, items);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        AssertSnapshotsEqual(
            runtime.Infrastructure,
            loaded.Value.TunnelInfrastructure.Infrastructure);
        Assert.Equal(
            runtime.NextAutomaticJobSequence,
            loaded.Value.TunnelInfrastructure.NextAutomaticJobSequence);
        SaveGameDocument rebuilt = builder.Build(new SaveGameContext(
            loaded.Value.Metadata,
            loaded.Value.World,
            loaded.Value.Inventory,
            loaded.Value.Jobs,
            loaded.Value.Buildings,
            Array.Empty<AgentState>(),
            tunnelInfrastructure: loaded.Value.TunnelInfrastructure));
        Assert.Equal(first, codec.Serialize(rebuilt));
    }

    private static TunnelInfrastructureRuntimeSnapshot CreateRuntime(ulong sequence)
    {
        TunnelInfrastructureState state = new TunnelInfrastructureState();
        CellId supportOrigin = new CellId(0, 1, 0);
        RequireSuccess(state.RegisterSegment(
            SupportSegmentId,
            TunnelSegmentOriginKind.RoomExit,
            supportOrigin,
            Cells(supportOrigin, direction: 1, count: 25),
            tick: 1));
        RequireSuccess(state.RegisterCompletedWoodenSupport(
            SupportSegmentId,
            new CellId(5, 1, 0),
            tick: 2));

        CellId completedJunction = new CellId(30, 1, 0);
        RequireSuccess(state.RegisterSegment(
            CompletedJunctionSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            completedJunction,
            Cells(completedJunction, direction: 1, count: 2),
            tick: 3));
        RequireSuccess(state.RegisterCompletedJunctionStoneTrim(
            completedJunction,
            tick: 4));

        CellId pendingJunction = new CellId(40, 1, 0);
        RequireSuccess(state.RegisterSegment(
            PendingJunctionSegmentId,
            TunnelSegmentOriginKind.VerticalJunction,
            pendingJunction,
            Cells(pendingJunction, direction: -1, count: 2),
            tick: 5));
        return new TunnelInfrastructureRuntimeSnapshot(
            state.CaptureSnapshot(),
            sequence);
    }

    private static CellId[] Cells(CellId origin, int direction, int count)
    {
        return Enumerable.Range(1, count)
            .Select(value => new CellId(
                origin.X + (direction * value),
                origin.Y,
                origin.Z))
            .ToArray();
    }

    private static void AssertSnapshotsEqual(
        TunnelInfrastructureSnapshot expected,
        TunnelInfrastructureSnapshot actual)
    {
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.CompletedJunctionStoneTrimCells,
            actual.CompletedJunctionStoneTrimCells);
        Assert.Equal(expected.PendingJunctionStoneTrimTargets,
            actual.PendingJunctionStoneTrimTargets);
        Assert.Equal(expected.Segments.Count, actual.Segments.Count);
        for (int index = 0; index < expected.Segments.Count; index++)
        {
            HorizontalTunnelSegmentSnapshot left = expected.Segments[index];
            HorizontalTunnelSegmentSnapshot right = actual.Segments[index];
            Assert.Equal(left.SegmentId, right.SegmentId);
            Assert.Equal(left.OriginKind, right.OriginKind);
            Assert.Equal(left.OriginCell, right.OriginCell);
            Assert.Equal(left.OrderedHorizontalCells, right.OrderedHorizontalCells);
            Assert.Equal(left.StructuralAnchors, right.StructuralAnchors);
            Assert.Equal(
                left.NextAutomaticSupportTarget,
                right.NextAutomaticSupportTarget);
            Assert.Equal(left.Version, right.Version);
        }
    }

    private static SaveMetadataData Metadata()
    {
        return new SaveMetadataData
        {
            SlotId = "tunnel-save",
            DisplayName = "Tunnel save",
            SavedAtUtc = "2026-08-03T10:00:00Z",
            SimulationTick = 20,
            WorldSeed = 42,
            GeneratorVersion = 1,
        };
    }

    private static EntityId AutomaticJobId(ulong sequence) =>
        EntityId.Parse("a" + sequence.ToString("x31"));

    private static string Normalize(string source)
    {
        return source
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
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

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));

    private static void RequireSuccess(Result result) =>
        Assert.True(result.IsSuccess, result.Error?.ToString());
}

}
