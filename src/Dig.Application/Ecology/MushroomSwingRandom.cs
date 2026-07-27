using System;
using Dig.Domain.Core;
using Dig.Domain.Runtime;

namespace Dig.Application.Ecology
{

public sealed class MushroomSwingRandom : IMushroomSwingRandom
{
    private const string StreamName = "ecology.mushroom.chop_hits";
    private readonly DeterministicRandomStream _stream;

    public MushroomSwingRandom(RandomStreamCatalog randomStreams)
    {
        if (randomStreams is null)
        {
            throw new ArgumentNullException(nameof(randomStreams));
        }

        _stream = randomStreams.GetOrCreate(StreamName);
    }

    public int SelectRequiredSwings(
        EntityId siteId,
        EntityId workerId,
        int minimum,
        int maximum)
    {
        if (siteId.IsEmpty || workerId.IsEmpty || minimum <= 0 || maximum < minimum)
        {
            throw new ArgumentException("Mushroom swing selection inputs are invalid.");
        }

        return minimum == maximum
            ? minimum
            : _stream.NextInt(minimum, checked(maximum + 1));
    }
}

}
