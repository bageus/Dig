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
            int miningSkill,
            ulong deterministicSeed,
            ExcavationQuarter reserved = ExcavationQuarter.None)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (miningSkill < 0 || miningSkill > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(miningSkill));
            }

            int requiredSwings = RequiredSwings(miningSkill, deterministicSeed);
            int quarterCount = QuarterCount(miningSkill, deterministicSeed);
            ExcavationQuarter unfinished = ExcavationQuarter.All
                & ~state.Completed
                & ~reserved;
            if (unfinished == ExcavationQuarter.None)
            {
                return new ExcavationSwingPlan(
                    ExcavationQuarter.None,
                    requiredSwings);
            }

            ExcavationQuarter preferredMask = CandidatesFor(approach) & unfinished;
            List<ExcavationQuarter> preferred = AllQuarters
                .Where(value => (preferredMask & value) != 0)
                .ToList();
            List<ExcavationQuarter> remaining = AllQuarters
                .Where(value => (unfinished & value) != 0
                    && (preferredMask & value) == 0)
                .ToList();
            Shuffle(preferred, deterministicSeed ^ 0x9E3779B97F4A7C15UL);
            Shuffle(remaining, deterministicSeed ^ 0xD1B54A32D192ED03UL);

            // Complete the near-side band before touching the opposite band. This
            // keeps partial progress spatially coherent: digging downward removes
            // the whole upper row before either lower quarter, even for high skill.
            List<ExcavationQuarter> activeBand = preferred.Count > 0
                ? preferred
                : remaining;
            ExcavationQuarter selected = ExcavationQuarter.None;
            int take = Math.Min(quarterCount, activeBand.Count);
            for (int index = 0; index < take; index++)
            {
                selected |= activeBand[index];
            }

            return new ExcavationSwingPlan(selected, requiredSwings);
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

        private static int RequiredSwings(int skill, ulong seed)
        {
            if (skill <= 10)
            {
                return SelectInclusive(2, 3, seed ^ 0xA0761D6478BD642FUL);
            }

            if (skill <= 20)
            {
                return SelectInclusive(1, 2, seed ^ 0xE7037ED1A0B428DBUL);
            }

            return 1;
        }

        private static int QuarterCount(int skill, ulong seed)
        {
            if (skill <= 40)
            {
                return 1;
            }

            if (skill <= 50)
            {
                return SelectInclusive(1, 2, seed ^ 0x8EBC6AF09C88C6E3UL);
            }

            if (skill <= 70)
            {
                return SelectInclusive(2, 3, seed ^ 0x589965CC75374CC3UL);
            }

            return SelectInclusive(3, 4, seed ^ 0x1D8E4E27C47D124FUL);
        }

        private static int SelectInclusive(int minimum, int maximum, ulong seed)
        {
            ulong value = Mix(seed);
            return minimum + (int)(value % (ulong)(maximum - minimum + 1));
        }

        private static void Shuffle(IList<ExcavationQuarter> values, ulong seed)
        {
            ulong state = seed;
            for (int index = values.Count - 1; index > 0; index--)
            {
                state = Mix(state + (ulong)index);
                int selected = (int)(state % (ulong)(index + 1));
                ExcavationQuarter value = values[index];
                values[index] = values[selected];
                values[selected] = value;
            }
        }

        private static int Count(ExcavationQuarter quarters)
        {
            int value = (int)quarters;
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }

        private static ulong Mix(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return value;
        }
    }
}
