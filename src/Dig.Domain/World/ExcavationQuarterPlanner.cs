using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.World
{
    public sealed class ExcavationQuarterPlanner
    {
        private static readonly ExcavationQuarter[] AllQuarters =
        {
            ExcavationQuarter.UpperLeft,
            ExcavationQuarter.LowerLeft,
            ExcavationQuarter.UpperRight,
            ExcavationQuarter.LowerRight,
        };

        public ExcavationSwingPlan Plan(
            ExcavationQuarterState state,
            ExcavationApproachSide approach,
            ExcavationQuarter reserved = ExcavationQuarter.None)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ExcavationQuarter unfinished = ExcavationQuarter.All
                & ~state.Completed
                & ~reserved;
            if (unfinished == ExcavationQuarter.None)
            {
                return new ExcavationSwingPlan(
                    ExcavationQuarter.None,
                    requiredSwingsPerQuarter: 1);
            }

            ExcavationQuarter preferredMask = CandidatesFor(approach) & unfinished;
            ExcavationQuarter selected = FirstQuarter(preferredMask);
            if (selected == ExcavationQuarter.None)
            {
                selected = FirstQuarter(unfinished);
            }

            return new ExcavationSwingPlan(selected, requiredSwingsPerQuarter: 1);
        }

        public ExcavationSwingPlan Plan(
            ExcavationQuarterState state,
            ExcavationApproachSide approach,
            int miningSkill,
            ulong deterministicSeed,
            ExcavationQuarter reserved = ExcavationQuarter.None)
        {
            if (miningSkill < 0 || miningSkill > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(miningSkill));
            }

            _ = deterministicSeed;
            return Plan(state, approach, reserved);
        }

        public static ExcavationQuarter CandidatesFor(
            ExcavationApproachSide approach)
        {
            switch (approach)
            {
                case ExcavationApproachSide.Left:
                    return ExcavationQuarter.UpperLeft
                        | ExcavationQuarter.LowerLeft;
                case ExcavationApproachSide.Right:
                    return ExcavationQuarter.UpperRight
                        | ExcavationQuarter.LowerRight;
                case ExcavationApproachSide.Above:
                    return ExcavationQuarter.UpperLeft
                        | ExcavationQuarter.UpperRight;
                case ExcavationApproachSide.Below:
                    return ExcavationQuarter.LowerLeft
                        | ExcavationQuarter.LowerRight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(approach));
            }
        }

        private static ExcavationQuarter FirstQuarter(ExcavationQuarter mask)
        {
            return AllQuarters.FirstOrDefault(value => (mask & value) != 0);
        }
    }
}
