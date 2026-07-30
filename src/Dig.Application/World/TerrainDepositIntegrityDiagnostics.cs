using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public static class TerrainDepositIntegrityCodes
{
    public const string ActiveHostInvalid =
        "terrain_deposit.integrity.active_host_invalid";
    public const string DepletedCellSolid =
        "terrain_deposit.integrity.depleted_cell_solid";
    public const string DepletedWithoutOutputCommit =
        "terrain_deposit.integrity.depleted_without_output_commit";
    public const string DepositCommitNotDepleted =
        "terrain_deposit.integrity.deposit_commit_not_depleted";
}

public sealed class TerrainDepositIntegrityIssue
{
    public TerrainDepositIntegrityIssue(
        CellId cell,
        string instanceId,
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException(
                "Deposit instance id is required.",
                nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Integrity issue code is required.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Integrity issue message is required.",
                nameof(message));
        }

        Cell = cell;
        InstanceId = instanceId;
        Code = code;
        Message = message;
    }

    public CellId Cell { get; }

    public string InstanceId { get; }

    public string Code { get; }

    public string Message { get; }
}

public sealed class TerrainDepositIntegrityReport
{
    internal TerrainDepositIntegrityReport(
        int generatorVersion,
        int hiddenCount,
        int revealedCount,
        int depletedCount,
        IEnumerable<TerrainDepositIntegrityIssue> issues)
    {
        GeneratorVersion = generatorVersion;
        HiddenCount = hiddenCount;
        RevealedCount = revealedCount;
        DepletedCount = depletedCount;
        Issues = new ReadOnlyCollection<TerrainDepositIntegrityIssue>(
            (issues ?? throw new ArgumentNullException(nameof(issues)))
                .OrderBy(value => value.Cell)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .ToArray());
    }

    public int GeneratorVersion { get; }

    public int HiddenCount { get; }

    public int RevealedCount { get; }

    public int DepletedCount { get; }

    public IReadOnlyList<TerrainDepositIntegrityIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;
}

public sealed class TerrainDepositIntegrityDiagnostics
{
    public TerrainDepositIntegrityReport Inspect(
        WorldState world,
        MiningOutputCommitState? outputCommits = null)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        List<TerrainDepositIntegrityIssue> issues =
            new List<TerrainDepositIntegrityIssue>();
        TerrainDepositInstance[] deposits = world.TerrainDeposits.Snapshot().ToArray();
        Dictionary<CellId, MiningOutputCommit> depositCommits =
            CreateDepositCommitIndex(outputCommits);
        int hiddenCount = 0;
        int revealedCount = 0;
        int depletedCount = 0;
        foreach (TerrainDepositInstance deposit in deposits)
        {
            CellSnapshot cell = world.GetCell(deposit.Cell).Value;
            MaterialDefinition material = world.Materials.Get(cell.State.MaterialId)!;
            if (deposit.IsDepleted)
            {
                depletedCount++;
                if (cell.IsSolid)
                {
                    issues.Add(Issue(
                        deposit,
                        TerrainDepositIntegrityCodes.DepletedCellSolid,
                        "A depleted deposit still occupies solid terrain."));
                }

                if (outputCommits != null
                    && !depositCommits.ContainsKey(deposit.Cell))
                {
                    issues.Add(Issue(
                        deposit,
                        TerrainDepositIntegrityCodes.DepletedWithoutOutputCommit,
                        "A depleted deposit has no exactly-once output commit."));
                }
            }
            else
            {
                if (deposit.IsRevealed)
                {
                    revealedCount++;
                }
                else
                {
                    hiddenCount++;
                }

                if (!deposit.Definition.CanOccupy(material))
                {
                    issues.Add(Issue(
                        deposit,
                        TerrainDepositIntegrityCodes.ActiveHostInvalid,
                        $"Active deposit host '{material.Id}' is invalid."));
                }
            }
        }

        foreach (KeyValuePair<CellId, MiningOutputCommit> pair in depositCommits)
        {
            if (!world.TerrainDeposits.TryGet(
                    pair.Key,
                    out TerrainDepositInstance deposit)
                || !deposit.IsDepleted)
            {
                string instanceId = deposit?.InstanceId ?? "missing-deposit";
                issues.Add(new TerrainDepositIntegrityIssue(
                    pair.Key,
                    instanceId,
                    TerrainDepositIntegrityCodes.DepositCommitNotDepleted,
                    "A deposit output commit does not reference a depleted deposit."));
            }
        }

        return new TerrainDepositIntegrityReport(
            world.TerrainDeposits.GeneratorVersion,
            hiddenCount,
            revealedCount,
            depletedCount,
            issues);
    }

    private static Dictionary<CellId, MiningOutputCommit> CreateDepositCommitIndex(
        MiningOutputCommitState? commits)
    {
        return commits is null
            ? new Dictionary<CellId, MiningOutputCommit>()
            : commits.Snapshot()
                .Where(value => value.SourceKind == MiningOutputSourceKind.Deposit)
                .ToDictionary(value => value.Cell);
    }

    private static TerrainDepositIntegrityIssue Issue(
        TerrainDepositInstance deposit,
        string code,
        string message)
    {
        return new TerrainDepositIntegrityIssue(
            deposit.Cell,
            deposit.InstanceId,
            code,
            message);
    }
}

}
