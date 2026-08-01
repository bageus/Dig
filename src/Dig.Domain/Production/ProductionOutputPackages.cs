using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public enum ProductionOutputPackageKind
{
    Unfinished = 0,
    Building = 1,
    Food = 2,
    Weapon = 3,
    Tool = 4,
}

public sealed class ProductionOutputPackageSnapshot
{
    public ProductionOutputPackageSnapshot(
        EntityId stackId,
        EntityId orderId,
        ProductionOutputPackageKind kind,
        long version,
        IReadOnlyCollection<ContentItemQuantity> manifest)
    {
        if (stackId.IsEmpty || orderId.IsEmpty)
        {
            throw new ArgumentException("Package and order ids are required.");
        }

        if (!Enum.IsDefined(typeof(ProductionOutputPackageKind), kind) || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        StackId = stackId;
        OrderId = orderId;
        Kind = kind;
        Version = version;
        Manifest = new ReadOnlyCollection<ContentItemQuantity>(
            (manifest ?? throw new ArgumentNullException(nameof(manifest)))
                .OrderBy(value => value.ItemId)
                .ToArray());
    }

    public EntityId StackId { get; }
    public EntityId OrderId { get; }
    public ProductionOutputPackageKind Kind { get; }
    public long Version { get; }
    public IReadOnlyList<ContentItemQuantity> Manifest { get; }
    public bool IsClosed => Kind is ProductionOutputPackageKind.Food
        or ProductionOutputPackageKind.Weapon
        or ProductionOutputPackageKind.Tool;
}

internal sealed class ProductionOutputPackageState
{
    private ContentItemQuantity[] _manifest = Array.Empty<ContentItemQuantity>();

    internal ProductionOutputPackageState(
        EntityId stackId,
        EntityId orderId,
        ProductionOutputPackageKind kind = ProductionOutputPackageKind.Unfinished,
        long version = 0,
        IEnumerable<ContentItemQuantity>? manifest = null)
    {
        StackId = stackId;
        OrderId = orderId;
        Kind = kind;
        Version = version;
        _manifest = (manifest ?? Array.Empty<ContentItemQuantity>())
            .OrderBy(value => value.ItemId)
            .ToArray();
    }

    internal EntityId StackId { get; }
    internal EntityId OrderId { get; }
    internal ProductionOutputPackageKind Kind { get; private set; }
    internal long Version { get; private set; }

    internal void Close(
        ProductionOutputPackageKind kind,
        IEnumerable<ContentItemQuantity> manifest)
    {
        if (Kind != ProductionOutputPackageKind.Unfinished
            || kind is ProductionOutputPackageKind.Unfinished
                or ProductionOutputPackageKind.Building)
        {
            throw new InvalidOperationException("Only an unfinished non-building package can close.");
        }

        ContentItemQuantity[] values = manifest.OrderBy(value => value.ItemId).ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("Closed package manifest cannot be empty.", nameof(manifest));
        }

        Kind = kind;
        _manifest = values;
        Version = checked(Version + 1);
    }

    internal ProductionOutputPackageSnapshot CreateSnapshot()
    {
        return new ProductionOutputPackageSnapshot(
            StackId,
            OrderId,
            Kind,
            Version,
            _manifest);
    }
}

public sealed partial class ProductionState
{
    private readonly Dictionary<EntityId, ProductionOutputPackageState> _packages =
        new Dictionary<EntityId, ProductionOutputPackageState>();

    public Result CreateOutputPackage(
        EntityId orderId,
        EntityId packageStackId,
        long tick)
    {
        ValidateTick(tick);
        ProductionOrderState? order = Find(orderId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        if (packageStackId.IsEmpty
            || order.Status is not (ProductionOrderStatus.InputsReserved
                or ProductionOrderStatus.InProgress)
            || _packages.ContainsKey(packageStackId)
            || _packages.Values.Any(value => value.OrderId == orderId))
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        _packages.Add(
            packageStackId,
            new ProductionOutputPackageState(packageStackId, orderId));
        return Result.Success();
    }

    public Result CloseOutputPackage(
        EntityId packageStackId,
        ProductionOutputPackageKind kind,
        IReadOnlyCollection<ContentItemQuantity> manifest,
        long tick)
    {
        ValidateTick(tick);
        if (!_packages.TryGetValue(packageStackId, out ProductionOutputPackageState? package)
            || manifest is null)
        {
            return Result.Failure(ProductionErrors.OutputPackageNotFound);
        }

        package.Close(kind, manifest);
        return Result.Success();
    }

    public Result RemoveOutputPackage(EntityId packageStackId, long tick)
    {
        ValidateTick(tick);
        return _packages.Remove(packageStackId)
            ? Result.Success()
            : Result.Failure(ProductionErrors.OutputPackageNotFound);
    }

    public ProductionOutputPackageSnapshot? GetOutputPackage(EntityId packageStackId)
    {
        return _packages.TryGetValue(packageStackId, out ProductionOutputPackageState? value)
            ? value.CreateSnapshot()
            : null;
    }

    public ProductionOutputPackageSnapshot? GetOutputPackageForOrder(EntityId orderId)
    {
        return _packages.Values
            .Where(value => value.OrderId == orderId)
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .Select(value => value.CreateSnapshot())
            .FirstOrDefault();
    }

    public IReadOnlyList<ProductionOutputPackageSnapshot> GetOutputPackages()
    {
        return new ReadOnlyCollection<ProductionOutputPackageSnapshot>(_packages.Values
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .Select(value => value.CreateSnapshot())
            .ToArray());
    }

    public Result RestoreOutputPackage(
        EntityId stackId,
        EntityId orderId,
        ProductionOutputPackageKind kind,
        long version,
        IReadOnlyCollection<ContentItemQuantity> manifest)
    {
        if (stackId.IsEmpty
            || orderId.IsEmpty
            || version < 0
            || !Enum.IsDefined(typeof(ProductionOutputPackageKind), kind)
            || _packages.ContainsKey(stackId)
            || Find(orderId) is null)
        {
            return Result.Failure(ProductionErrors.InvalidStatus);
        }

        _packages.Add(
            stackId,
            new ProductionOutputPackageState(stackId, orderId, kind, version, manifest));
        return Result.Success();
    }
}

public static partial class ProductionErrors
{
    public static readonly DomainError OutputPackageNotFound = new DomainError(
        "production.output_package_not_found",
        "The production output package does not exist.");

    public static readonly DomainError OutputPackageNotUsable = new DomainError(
        "production.output_package_not_usable",
        "The production output package is unfinished, stale, or already opened.");
}

}
