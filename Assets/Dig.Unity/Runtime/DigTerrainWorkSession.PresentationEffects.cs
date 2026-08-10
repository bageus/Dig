using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Rendering;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private Action<IReadOnlyList<PresentationEffectFact>>? _presentationEffectSink;

    internal void BindPresentationEffectSink(
        Action<IReadOnlyList<PresentationEffectFact>> sink)
    {
        _presentationEffectSink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    private void PublishTerrainCompletionEffects(
        EntityId jobId,
        CellId target,
        long tick,
        bool revealedDeposit)
    {
        if (_presentationEffectSink == null) return;
        string prefix = "terrain:" + jobId + ":" + tick;
        PresentationEffectFact impact = new PresentationEffectFact(
            prefix + ":impact",
            PresentationEffectKind.ExcavationImpact,
            target.X,
            0d,
            target.Y,
            1d,
            tick);
        if (!revealedDeposit)
        {
            _presentationEffectSink(new[] { impact });
            return;
        }
        PresentationEffectFact reveal = new PresentationEffectFact(
            prefix + ":deposit",
            PresentationEffectKind.DepositReveal,
            target.X,
            0d,
            target.Y,
            1d,
            tick);
        _presentationEffectSink(new[] { impact, reveal });
    }
}
}
