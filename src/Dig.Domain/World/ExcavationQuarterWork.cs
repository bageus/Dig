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

    public enum ExcavationCutPattern
    {
        None = 0,
        VerticalColumns = 1,
        HorizontalRows = 2,
        DepthFace = 3,
    }

    public static class ExcavationApproachResolver
    {
        public static ExcavationApproachSide Resolve(
            CellId residentCell,
            CellId targetCell)
        {
            return Resolve(residentCell, targetCell, ExcavationCutPattern.None);
        }

        public static ExcavationApproachSide Resolve(
            CellId residentCell,
            CellId targetCell,
            ExcavationCutPattern cutPattern)
        {
            if (!Enum.IsDefined(typeof(ExcavationCutPattern), cutPattern))
            {
                throw new ArgumentOutOfRangeException(nameof(cutPattern));
            }

            int dx = residentCell.X - targetCell.X;
            int dy = residentCell.Y - targetCell.Y;
            if (cutPattern == ExcavationCutPattern.HorizontalRows)
            {
                // World Y grows downward. Equal Y is resolved to the upper row so a
                // side work cell cannot turn a vertical tunnel into a vertical cut.
                return dy <= 0
                    ? ExcavationApproachSide.Above
                    : ExcavationApproachSide.Below;
            }

            if (cutPattern == ExcavationCutPattern.VerticalColumns)
            {
                return dx <= 0
                    ? ExcavationApproachSide.Left
                    : ExcavationApproachSide.Right;
            }

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                return dx < 0
                    ? ExcavationApproachSide.Left
                    : ExcavationApproachSide.Right;
            }

            return dy < 0
                ? ExcavationApproachSide.Above
                : ExcavationApproachSide.Below;
        }
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

        public void SynchronizeCompleted(ExcavationQuarter completed)
        {
            int value = (int)completed;
            if ((value & ~(int)ExcavationQuarter.All) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completed));
            }

            Completed = completed;
            ExcavationQuarter[] stale = _swingProgress.Keys
                .Where(quarter => (completed & quarter) != 0)
                .ToArray();
            for (int index = 0; index < stale.Length; index++)
            {
                _swingProgress.Remove(stale[index]);
            }
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

}
