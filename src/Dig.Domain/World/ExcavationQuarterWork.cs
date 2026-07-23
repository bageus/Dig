using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.World
{
    [Flags]
    public enum ExcavationQuarter
    {
        None = 0,
        UpperLeft = 1,
        LowerLeft = 2,
        UpperRight = 4,
        LowerRight = 8,
        All = UpperLeft | LowerLeft | UpperRight | LowerRight,
    }

    public enum ExcavationApproachSide
    {
        Left = 0,
        Right = 1,
        Above = 2,
        Below = 3,
    }

    public readonly struct ExcavationSwingPlan
    {
        public ExcavationSwingPlan(
            ExcavationQuarter quarters,
            int requiredSwingsPerQuarter)
        {
            if (requiredSwingsPerQuarter < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredSwingsPerQuarter));
            }

            Quarters = quarters;
            RequiredSwingsPerQuarter = requiredSwingsPerQuarter;
        }

        public ExcavationQuarter Quarters { get; }

        public int RequiredSwingsPerQuarter { get; }
    }

    public sealed class ExcavationQuarterState
    {
        private readonly Dictionary<ExcavationQuarter, int> _swingProgress =
            new Dictionary<ExcavationQuarter, int>();

        public ExcavationQuarter Completed { get; private set; }

        public bool IsComplete => Completed == ExcavationQuarter.All;

        public bool IsCompleted(ExcavationQuarter quarter)
        {
            RequireSingleQuarter(quarter);
            return (Completed & quarter) != 0;
        }

        public int GetSwingProgress(ExcavationQuarter quarter)
        {
            RequireSingleQuarter(quarter);
            return _swingProgress.TryGetValue(quarter, out int progress)
                ? progress
                : 0;
        }

        public bool ApplySwing(ExcavationQuarter quarter, int requiredSwings)
        {
            RequireSingleQuarter(quarter);
            if (requiredSwings < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredSwings));
            }

            if (IsCompleted(quarter))
            {
                return false;
            }

            int next = GetSwingProgress(quarter) + 1;
            if (next < requiredSwings)
            {
                _swingProgress[quarter] = next;
                return false;
            }

            _swingProgress.Remove(quarter);
            Completed |= quarter;
            return true;
        }

        public void Complete(ExcavationQuarter quarter)
        {
            RequireSingleQuarter(quarter);
            _swingProgress.Remove(quarter);
            Completed |= quarter;
        }

        private static void RequireSingleQuarter(ExcavationQuarter quarter)
        {
            int value = (int)quarter;
            if (value == 0 || (value & (value - 1)) != 0)
            {
                throw new ArgumentException(
                    "A single excavation quarter is required.",
                    nameof(quarter));
            }
        }
    }

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

            ExcavationQuarter selected = ExcavationQuarter.None;
            int take = Math.Min(quarterCount, preferred.Count + remaining.Count);
            for (int index = 0; index < preferred.Count && index < take; index++)
            {
                selected |= preferred[index];
            }

            int selectedCount = Count(selected);
            for (int index = 0;
                index < remaining.Count && selectedCount < take;
                index++, selectedCount++)
            {
                selected |= remaining[index];
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
