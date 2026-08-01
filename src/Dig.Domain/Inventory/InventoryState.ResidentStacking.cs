using System;
using System.Security.Cryptography;
using System.Text;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    private Result<EntityId> CreateResidentUnitId(EntityId sourceStackId, int ordinal)
    {
        for (int salt = 0; salt < 1024; salt++)
        {
            string key = $"{sourceStackId}:resident-unit:{ordinal}:{salt}";
            byte[] hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }

            byte[] guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, guidBytes.Length);
            Guid guid = new Guid(guidBytes);
            if (guid == Guid.Empty)
            {
                continue;
            }

            EntityId candidate = new EntityId(guid);
            if (!_stacks.ContainsKey(candidate))
            {
                return Result<EntityId>.Success(candidate);
            }
        }

        return Result<EntityId>.Failure(InventoryErrors.StackAlreadyExists);
    }

    private sealed class ResidentUnitCandidate
    {
        private ResidentUnitCandidate(
            ItemStackState source,
            EntityId unitId,
            int ordinal,
            bool isOriginal)
        {
            Source = source;
            UnitId = unitId;
            Ordinal = ordinal;
            IsOriginal = isOriginal;
        }

        public ItemStackState Source { get; }
        public EntityId UnitId { get; }
        public int Ordinal { get; }
        public bool IsOriginal { get; }
        public bool HasAssignedSlot { get; private set; }
        public ResidentInventorySlot AssignedSlot { get; private set; }
        public ItemStackState? Materialized { get; private set; }

        public static ResidentUnitCandidate Original(ItemStackState source)
        {
            return new ResidentUnitCandidate(source, source.Id, ordinal: 0, isOriginal: true);
        }

        public static ResidentUnitCandidate Split(
            ItemStackState source,
            EntityId unitId,
            int ordinal)
        {
            return new ResidentUnitCandidate(source, unitId, ordinal, isOriginal: false);
        }

        public void Assign(ResidentInventorySlot slot)
        {
            AssignedSlot = slot;
            HasAssignedSlot = true;
        }

        public void Materialize(ItemStackState stack)
        {
            Materialized = stack;
        }
    }

    private sealed class ResidentPlacementCandidate
    {
        private ResidentPlacementCandidate(
            ItemStackState source,
            ResidentUnitCandidate? unit,
            bool isExpansion)
        {
            Source = source;
            Unit = unit;
            IsExpansion = isExpansion;
        }

        public ItemStackState Source { get; }
        public ResidentUnitCandidate? Unit { get; }
        public bool IsExpansion { get; }
        public EntityId UnitId => Unit?.UnitId ?? Source.Id;
        public int Ordinal => Unit?.Ordinal ?? 0;

        public static ResidentPlacementCandidate Expansion(ItemStackState source)
        {
            return new ResidentPlacementCandidate(
                source,
                unit: null,
                isExpansion: true);
        }

        public static ResidentPlacementCandidate Ordinary(ResidentUnitCandidate unit)
        {
            return new ResidentPlacementCandidate(
                unit.Source,
                unit,
                isExpansion: false);
        }

        public void Assign(ResidentInventorySlot slot)
        {
            if (Unit is not null)
            {
                Unit.Assign(slot);
            }
        }
    }

}

}
