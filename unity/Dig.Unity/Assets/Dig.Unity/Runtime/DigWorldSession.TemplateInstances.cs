using System;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    internal Result<WorldMutationResult> CommitTemplateDesignations(
        ExcavationTemplateInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        for (int index = 0; index < instance.OrderedMask.Count; index++)
        {
            if (IsProtected(instance.OrderedMask[index]))
            {
                return Result<WorldMutationResult>.Failure(ProtectedRock);
            }
        }

        _tick = checked(_tick + 1);
        return _repository.Get().SetDigDesignations(instance.OrderedMask, _tick);
    }
}

}
