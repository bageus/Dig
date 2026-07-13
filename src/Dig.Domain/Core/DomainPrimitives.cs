using System.Collections.ObjectModel;

namespace Dig.Domain.Core;

public readonly struct EntityId : IEquatable<EntityId>
{
    private readonly Guid _value;

    public EntityId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Entity id cannot be empty.", nameof(value));
        }

        _value = value;
    }

    public bool IsEmpty => _value == Guid.Empty;

    public static EntityId New()
    {
        return new EntityId(Guid.NewGuid());
    }

    public static EntityId Parse(string value)
    {
        if (!TryParse(value, out EntityId entityId))
        {
            throw new FormatException($"'{value}' is not a valid entity id.");
        }

        return entityId;
    }

    public static bool TryParse(string? value, out EntityId entityId)
    {
        entityId = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        bool parsed = Guid.TryParseExact(value, "N", out Guid guid)
            || Guid.TryParse(value, out guid);

        if (!parsed || guid == Guid.Empty)
        {
            return false;
        }

        entityId = new EntityId(guid);
        return true;
    }

    public bool Equals(EntityId other)
    {
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return _value.ToString("N");
    }

    public static bool operator ==(EntityId left, EntityId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EntityId left, EntityId right)
    {
        return !left.Equals(right);
    }
}

public sealed class DomainError : IEquatable<DomainError>
{
    public DomainError(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Error code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required.", nameof(message));
        }

        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }

    public bool Equals(DomainError? other)
    {
        return other is not null
            && string.Equals(Code, other.Code, StringComparison.Ordinal)
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DomainError);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code, Message);
    }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}

public class Result
{
    protected Result(bool isSuccess, DomainError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public DomainError? Error { get; }

    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Failure(DomainError error)
    {
        return new Result(false, error ?? throw new ArgumentNullException(nameof(error)));
    }
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, DomainError? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("A failed result does not contain a value.");
            }

            return _value!;
        }
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null);
    }

    public static new Result<T> Failure(DomainError error)
    {
        return new Result<T>(false, default, error ?? throw new ArgumentNullException(nameof(error)));
    }
}

public interface IDomainEvent
{
    long Tick { get; }
}

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = new List<IDomainEvent>();

    protected void Raise(IDomainEvent domainEvent)
    {
        _uncommittedEvents.Add(domainEvent ?? throw new ArgumentNullException(nameof(domainEvent)));
    }

    public IReadOnlyList<IDomainEvent> PeekUncommittedEvents()
    {
        return new ReadOnlyCollection<IDomainEvent>(_uncommittedEvents);
    }

    public IReadOnlyList<IDomainEvent> DequeueUncommittedEvents()
    {
        if (_uncommittedEvents.Count == 0)
        {
            return Array.Empty<IDomainEvent>();
        }

        IDomainEvent[] events = _uncommittedEvents.ToArray();
        _uncommittedEvents.Clear();
        return events;
    }
}
