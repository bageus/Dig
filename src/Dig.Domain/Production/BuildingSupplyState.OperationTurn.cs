using System;
using Dig.Domain.Core;

namespace Dig.Domain.Production
{

public enum BuildingOperationTurn
{
    Production = 0,
    Supply = 1,
}

public sealed partial class BuildingSupplyState
{
    public Result SetOperationTurn(
        EntityId buildingId,
        BuildingOperationTurn operationTurn,
        long tick)
    {
        ValidateTick(tick);
        if (!Enum.IsDefined(typeof(BuildingOperationTurn), operationTurn))
        {
            throw new ArgumentOutOfRangeException(nameof(operationTurn));
        }

        WorkstationSupplyEntry? entry = Find(buildingId);
        if (entry is null)
        {
            return Result.Failure(BuildingSupplyErrors.WorkstationNotFound);
        }

        entry.SetOperationTurn(operationTurn);
        return Result.Success();
    }
}

}
