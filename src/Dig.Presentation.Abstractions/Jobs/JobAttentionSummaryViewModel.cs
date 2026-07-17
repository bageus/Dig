using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Presentation.Jobs
{

public sealed class JobAttentionItemViewModel
{
    public JobAttentionItemViewModel(
        string jobId,
        string description,
        string readinessLabel,
        string reasonCode,
        string? assignedAgentId)
    {
        if (string.IsNullOrWhiteSpace(jobId)
            || string.IsNullOrWhiteSpace(description)
            || string.IsNullOrWhiteSpace(readinessLabel)
            || string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Job attention presentation text is required.");
        }

        JobId = jobId.Trim();
        Description = description.Trim();
        ReadinessLabel = readinessLabel.Trim();
        ReasonCode = reasonCode.Trim();
        AssignedAgentId = string.IsNullOrWhiteSpace(assignedAgentId)
            ? null
            : assignedAgentId.Trim();
    }

    public string JobId { get; }

    public string Description { get; }

    public string ReadinessLabel { get; }

    public string ReasonCode { get; }

    public string? AssignedAgentId { get; }
}

public sealed class JobAttentionSummaryViewModel
{
    public JobAttentionSummaryViewModel(
        int totalCount,
        IReadOnlyList<JobAttentionItemViewModel> items)
    {
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount));
        }

        JobAttentionItemViewModel[] values =
            (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
        if (values.Any(value => value is null)
            || values.Select(value => value.JobId).Distinct(StringComparer.Ordinal).Count()
                != values.Length
            || values.Length > totalCount)
        {
            throw new ArgumentException(
                "Job attention items must be non-null, unique and bounded by the total count.",
                nameof(items));
        }

        TotalCount = totalCount;
        Items = new ReadOnlyCollection<JobAttentionItemViewModel>(values);
    }

    public int TotalCount { get; }

    public IReadOnlyList<JobAttentionItemViewModel> Items { get; }

    public int HiddenCount => TotalCount - Items.Count;

    public bool HasAttention => TotalCount > 0;
}
}