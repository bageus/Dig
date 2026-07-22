using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Presentation.Navigation
{

public readonly struct RouteCellViewModel
{
    public RouteCellViewModel(int x, int y)
        : this(x, y, 0)
    {
    }

    public RouteCellViewModel(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public int X { get; }
    public int Y { get; }
    public int Z { get; }
}

public sealed class RouteViewModel
{
    public RouteViewModel(
        string jobId,
        string agentId,
        int workX,
        int workY,
        bool succeeded,
        string detail,
        int totalCost,
        long navigationVersion,
        IReadOnlyCollection<RouteCellViewModel> cells)
        : this(
            jobId,
            agentId,
            workX,
            workY,
            0,
            succeeded,
            detail,
            totalCost,
            navigationVersion,
            cells)
    {
    }

    public RouteViewModel(
        string jobId,
        string agentId,
        int workX,
        int workY,
        int workZ,
        bool succeeded,
        string detail,
        int totalCost,
        long navigationVersion,
        IReadOnlyCollection<RouteCellViewModel> cells)
    {
        JobId = jobId;
        AgentId = agentId;
        WorkX = workX;
        WorkY = workY;
        WorkZ = workZ;
        Succeeded = succeeded;
        Detail = detail;
        TotalCost = totalCost;
        NavigationVersion = navigationVersion;
        Cells = new ReadOnlyCollection<RouteCellViewModel>(cells.ToArray());
    }

    public string JobId { get; }
    public string AgentId { get; }
    public int WorkX { get; }
    public int WorkY { get; }
    public int WorkZ { get; }
    public bool Succeeded { get; }
    public string Detail { get; }
    public int TotalCost { get; }
    public long NavigationVersion { get; }
    public IReadOnlyList<RouteCellViewModel> Cells { get; }
}

}
