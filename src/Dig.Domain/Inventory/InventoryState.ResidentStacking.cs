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
        }

        public void Materialize(ItemStackState stack)
        {
            Materialized = stack;
        }
    }
}

}
