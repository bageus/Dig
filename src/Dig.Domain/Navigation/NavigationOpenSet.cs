using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

internal readonly struct NavigationSearchCost
    : IComparable<NavigationSearchCost>, IEquatable<NavigationSearchCost>
{
    public NavigationSearchCost(
        int shaftGapCount,
        int verticalClimbCount,
        int movementCost)
    {
        if (shaftGapCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shaftGapCount));
        }

        if (verticalClimbCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(verticalClimbCount));
        }

        if (movementCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(movementCost));
        }

        ShaftGapCount = shaftGapCount;
        VerticalClimbCount = verticalClimbCount;
        MovementCost = movementCost;
    }

    public int ShaftGapCount { get; }

    public int VerticalClimbCount { get; }

    public int MovementCost { get; }

    public NavigationSearchCost Advance(NavigationTransition transition)
    {
        return new NavigationSearchCost(
            checked(ShaftGapCount
                + (transition.TraversalKind == TunnelTraversalKind.ShaftGapTraverse
                    ? 1
                    : 0)),
            checked(VerticalClimbCount
                + (transition.TraversalKind == TunnelTraversalKind.VerticalClimb
                    ? 1
                    : 0)),
            checked(MovementCost + transition.Cost));
    }

    public int CompareTo(NavigationSearchCost other)
    {
        int gap = ShaftGapCount.CompareTo(other.ShaftGapCount);
        if (gap != 0)
        {
            return gap;
        }

        int climb = VerticalClimbCount.CompareTo(other.VerticalClimbCount);
        return climb != 0 ? climb : MovementCost.CompareTo(other.MovementCost);
    }

    public bool Equals(NavigationSearchCost other)
    {
        return ShaftGapCount == other.ShaftGapCount
            && VerticalClimbCount == other.VerticalClimbCount
            && MovementCost == other.MovementCost;
    }

    public override bool Equals(object? obj)
    {
        return obj is NavigationSearchCost other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ShaftGapCount, VerticalClimbCount, MovementCost);
    }
}

internal readonly struct NavigationOpenNode
{
    public NavigationOpenNode(
        CellId cell,
        NavigationSearchCost cost,
        int heuristic)
    {
        if (heuristic < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heuristic));
        }

        Cell = cell;
        Cost = cost;
        Heuristic = heuristic;
    }

    public CellId Cell { get; }

    public NavigationSearchCost Cost { get; }

    public int Heuristic { get; }
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
        int cost = left.Cost.CompareTo(right.Cost);
        if (cost != 0)
        {
            return cost;
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
}
