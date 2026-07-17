using System;
using Dig.Application.Jobs;

namespace Dig.Presentation.Jobs
{

public sealed class JobAssignmentDiagnosticViewModel
{
    public JobAssignmentDiagnosticViewModel(
        long tick,
        long? score,
        JobToolPreparationOutcome? toolPreparation,
        string? toolStackId,
        string? failureCode,
        string? failureMessage)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        bool assigned = score.HasValue && toolPreparation.HasValue;
        bool failed = !string.IsNullOrWhiteSpace(failureCode);
        if (assigned == failed)
        {
            throw new ArgumentException(
                "A job assignment diagnostic must represent exactly one outcome.");
        }

        if (assigned)
        {
            JobToolPreparationOutcome preparation = toolPreparation!.Value;
            if (!Enum.IsDefined(typeof(JobToolPreparationOutcome), preparation))
            {
                throw new ArgumentOutOfRangeException(nameof(toolPreparation));
            }

            bool needsTool = preparation != JobToolPreparationOutcome.None;
            if (needsTool == string.IsNullOrWhiteSpace(toolStackId))
            {
                throw new ArgumentException(
                    "Tool preparation requires a tool stack id.",
                    nameof(toolStackId));
            }

            if (failureMessage != null)
            {
                throw new ArgumentException(
                    "Successful assignments cannot contain a failure message.",
                    nameof(failureMessage));
            }
        }
        else
        {
            if (score.HasValue
                || toolPreparation.HasValue
                || !string.IsNullOrWhiteSpace(toolStackId))
            {
                throw new ArgumentException(
                    "Failed assignments cannot contain successful assignment data.");
            }

            if (string.IsNullOrWhiteSpace(failureMessage))
            {
                throw new ArgumentException(
                    "Failed assignments require a failure message.",
                    nameof(failureMessage));
            }
        }

        Tick = tick;
        Score = score;
        ToolPreparation = toolPreparation;
        ToolStackId = string.IsNullOrWhiteSpace(toolStackId) ? null : toolStackId.Trim();
        FailureCode = failed ? failureCode!.Trim() : null;
        FailureMessage = failed ? failureMessage!.Trim() : null;
    }

    public long Tick { get; }

    public long? Score { get; }

    public JobToolPreparationOutcome? ToolPreparation { get; }

    public string? ToolStackId { get; }

    public string? FailureCode { get; }

    public string? FailureMessage { get; }

    public bool IsFailure => FailureCode != null;
}
}
