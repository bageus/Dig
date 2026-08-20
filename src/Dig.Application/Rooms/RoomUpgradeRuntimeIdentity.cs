using System;
using System.Globalization;
using Dig.Domain.Core;

namespace Dig.Application.Rooms
{
    public static class RoomUpgradeRuntimeIdentity
    {
        public const char JobPrefix = 'd';
        public const char TransitStackPrefix = 'c';

        public static EntityId CreateJobId(ulong sequence)
        {
            return Create(JobPrefix, sequence);
        }

        public static EntityId CreateTransitStackId(ulong sequence)
        {
            return Create(TransitStackPrefix, sequence);
        }

        public static bool TryParseSequence(EntityId id, out ulong sequence)
        {
            return TryParseSequence(id.ToString(), out sequence);
        }

        public static bool TryParseSequence(string? value, out ulong sequence)
        {
            sequence = 0UL;
            if (string.IsNullOrWhiteSpace(value)
                || value.Length != 32
                || (value[0] != JobPrefix && value[0] != TransitStackPrefix))
            {
                return false;
            }

            return ulong.TryParse(
                    value.Substring(1),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out sequence)
                && sequence > 0UL;
        }

        private static EntityId Create(char prefix, ulong sequence)
        {
            if (sequence == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            return EntityId.Parse(
                prefix + sequence.ToString("x31", CultureInfo.InvariantCulture));
        }
    }
}
