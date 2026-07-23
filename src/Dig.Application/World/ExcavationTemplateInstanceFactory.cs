using System;

namespace Dig.Application.World
{

public sealed class ExcavationTemplateInstanceFactory
{
    public ExcavationTemplateInstance Create(
        string instanceId,
        CaveRoomPlan plan,
        CaveRoomTemplatePlacementUnlock unlock,
        string styleId)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return new ExcavationTemplateInstance(
            instanceId,
            plan,
            unlock,
            styleId);
    }
}

}
