using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal bool HasActiveResidentInventoryPlacement(string stackId)
        {
            if (string.IsNullOrWhiteSpace(stackId))
            {
                return false;
            }

            EntityId stack = EntityId.Parse(stackId);
            return _jobRepository.Get().GetAll().Any(value =>
                !value.IsTerminal
                && value.Definition is ResidentInventoryPlacementJobDefinition placement
                && placement.StackId == stack);
        }
    }
}