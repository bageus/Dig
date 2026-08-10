using System.Collections.Generic;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal IReadOnlyList<JournalEventEntry> ReadEventsAfter(
        long sequenceExclusive,
        int maximumCount)
    {
        return _journal.ReadEventsAfter(sequenceExclusive, maximumCount);
    }
}

}
