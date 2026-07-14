using System;
namespace Dig.Domain.Storage
{

public interface IStorageInspectionVisitor
{
    void VisitStorageReservation(StorageReservationSnapshot reservation);
}

public sealed partial class StorageState
{
    public void VisitInspection(IStorageInspectionVisitor visitor)
    {
        if (visitor is null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        foreach (StorageReservationSnapshot reservation in _reservations.Values)
        {
            visitor.VisitStorageReservation(reservation);
        }
    }
}
}
