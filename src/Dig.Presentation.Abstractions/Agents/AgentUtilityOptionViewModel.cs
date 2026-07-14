using System;

namespace Dig.Presentation.Agents
{

public readonly struct AgentUtilityOptionViewModel
{
    public AgentUtilityOptionViewModel(
        string intent,
        int score,
        bool available,
        bool critical,
        bool selected,
        string reasonCode,
        string detail)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            throw new ArgumentException("Intent is required.", nameof(intent));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Reason code is required.", nameof(reasonCode));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Detail is required.", nameof(detail));
        }

        Intent = intent.Trim();
        Score = score;
        Available = available;
        Critical = critical;
        Selected = selected;
        ReasonCode = reasonCode.Trim();
        Detail = detail.Trim();
    }

    public string Intent { get; }

    public int Score { get; }

    public bool Available { get; }

    public bool Critical { get; }

    public bool Selected { get; }

    public string ReasonCode { get; }

    public string Detail { get; }
}
}