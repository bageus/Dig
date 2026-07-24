using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class MiningOutputSaveWorldValidationCodes
{
    public const string CellOutOfBounds = "mining_output.save.cell_out_of_bounds";
}

public sealed class MiningOutputSaveWorldValidationIssue
{
    public MiningOutputSaveWorldValidationIssue(CellId cell, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A validation issue code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A validation issue message is required.", nameof(message));
        }

        Cell = cell;
        Code = code;
        Message = message;
    }

    public CellId Cell { get; }
    public string Code { get; }
    public string Message { get; }
}

public sealed class MiningOutputSaveWorldValidationReport
{
    internal MiningOutputSaveWorldValidationReport(
        IEnumerable<MiningOutputSaveWorldValidationIssue> issues)
    {
        MiningOutputSaveWorldValidationIssue[] ordered = (issues
            ?? throw new ArgumentNullException(nameof(issues)))
            .OrderBy(value => value.Cell)
            .ThenBy(value => value.Code, StringComparer.Ordinal)
            .ToArray();
        Issues = new ReadOnlyCollection<MiningOutputSaveWorldValidationIssue>(ordered);
    }

    public IReadOnlyList<MiningOutputSaveWorldValidationIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;
}

public sealed class MiningOutputSaveWorldValidator
{
    public MiningOutputSaveWorldValidationReport Validate(
        MiningOutputCommitSaveSnapshot snapshot,
        WorldSize worldSize)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        List<MiningOutputSaveWorldValidationIssue> issues =
            new List<MiningOutputSaveWorldValidationIssue>();
        foreach (MiningOutputCommitSaveEntry entry in snapshot.Commits)
        {
            if (!worldSize.Contains(entry.Cell))
            {
                issues.Add(new MiningOutputSaveWorldValidationIssue(
                    entry.Cell,
                    MiningOutputSaveWorldValidationCodes.CellOutOfBounds,
                    $"Mining output commit cell '{entry.Cell}' is outside the authoritative world bounds."));
            }
        }

        return new MiningOutputSaveWorldValidationReport(issues);
    }

    public MiningOutputSaveWorldValidationReport Validate(
        MiningOutputCommitsSaveData data,
        WorldSize worldSize)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return Validate(MiningOutputSaveDataAdapter.Decode(data), worldSize);
    }
}

}
