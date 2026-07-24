using System;
using Dig.Application.World;
using Dig.Domain.Core;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameBuilder
{
    private readonly MiningOutputSaveDocumentSection _miningOutputSection =
        new MiningOutputSaveDocumentSection();

    public Result<SaveGameDocument> Build(
        SaveGameContext context,
        MiningOutputCommitState miningOutputCommits)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (miningOutputCommits == null)
        {
            throw new ArgumentNullException(nameof(miningOutputCommits));
        }

        Result<MiningOutputCommitsSaveData> miningOutput =
            _miningOutputSection.Capture(
                miningOutputCommits,
                context.Inventory,
                context.World.Size);
        if (miningOutput.IsFailure)
        {
            return Result<SaveGameDocument>.Failure(
                miningOutput.Error ?? MiningOutputSaveErrors.InvalidSnapshot);
        }

        SaveGameDocument document = Build(context);
        document.MiningOutput = miningOutput.Value;
        return Result<SaveGameDocument>.Success(document);
    }
}

}
