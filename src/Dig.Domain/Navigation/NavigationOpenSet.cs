using Dig.Domain.World;

namespace Dig.Domain.Navigation;

internal readonly struct NavigationOpenNode
{
    public NavigationOpenNode(CellId cell, int cost, int heuristic)
    {
        Cell = cell;
        Cost = cost;
        Heuristic = heuristic;
        Total = checked(cost + heuristic);
    }

    public CellId Cell { get; }

    public int Cost { get; }

    public int Heuristic { get; }

    public int Total { get; }
}

internal sealed class NavigationOpenSet
{
    private readonly List<NavigationOpenNode> _heap = new List<NavigationOpenNode>();

    public int Count => _heap.Count;

    public void Push(NavigationOpenNode node)
    {
        _heap.Add(node);
        int index = _heap.Count - 1;
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (Compare(_heap[parent], _heap[index]) <= 0)
            {
                break;
            }

            Swap(parent, index);
            index = parent;
        }
    }

    public NavigationOpenNode Pop()
    {
        if (_heap.Count == 0)
        {
            throw new InvalidOperationException("The navigation open set is empty.");
        }

        NavigationOpenNode result = _heap[0];
        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);

        int index = 0;
        while (true)
        {
            int left = (index * 2) + 1;
            int right = left + 1;
            int smallest = index;

            if (left < _heap.Count
                && Compare(_heap[left], _heap[smallest]) < 0)
            {
                smallest = left;
            }

            if (right < _heap.Count
                && Compare(_heap[right], _heap[smallest]) < 0)
            {
                smallest = right;
            }

            if (smallest == index)
            {
                break;
            }

            Swap(index, smallest);
            index = smallest;
        }

        return result;
    }

    private static int Compare(
        NavigationOpenNode left,
        NavigationOpenNode right)
    {
        int total = left.Total.CompareTo(right.Total);
        if (total != 0)
        {
            return total;
        }

        int heuristic = left.Heuristic.CompareTo(right.Heuristic);
        if (heuristic != 0)
        {
            return heuristic;
        }

        return left.Cell.CompareTo(right.Cell);
    }

    private void Swap(int left, int right)
    {
        NavigationOpenNode value = _heap[left];
        _heap[left] = _heap[right];
        _heap[right] = value;
    }
}
