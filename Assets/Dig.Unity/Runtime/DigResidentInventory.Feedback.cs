using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Presentation.Inventory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly ResidentInventoryExpansionFeedbackPresenter
        _residentExpansionFeedbackPresenter =
            new ResidentInventoryExpansionFeedbackPresenter();
    private readonly ResidentInventoryAttachmentPresenter
        _residentInventoryAttachmentPresenter =
            new ResidentInventoryAttachmentPresenter();

    internal ResidentInventoryExpansionFeedbackViewModel?
        LoadResidentExpansionFeedback(string residentId, string stackId)
    {
        if (string.IsNullOrWhiteSpace(residentId)
            || string.IsNullOrWhiteSpace(stackId))
        {
            return null;
        }

        return _residentExpansionFeedbackPresenter.Present(
            _inventoryRepository.Get(),
            EntityId.Parse(residentId),
            EntityId.Parse(stackId));
    }

    internal IReadOnlyList<ResidentInventoryAttachmentViewModel>
        LoadResidentInventoryAttachments()
    {
        return _residentInventoryAttachmentPresenter.Present(
            _inventoryRepository.Get());
    }
}

}
