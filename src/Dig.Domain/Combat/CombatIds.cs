using System;

namespace Dig.Domain.Combat
{

public readonly struct CombatActionId : IEquatable<CombatActionId>, IComparable<CombatActionId>
{
    private readonly string? _value;

    public CombatActionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Combat action id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(CombatActionId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(CombatActionId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is CombatActionId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(CombatActionId left, CombatActionId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CombatActionId left, CombatActionId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct WeaponProfileId : IEquatable<WeaponProfileId>, IComparable<WeaponProfileId>
{
    private readonly string? _value;

    public WeaponProfileId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Weapon profile id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(WeaponProfileId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(WeaponProfileId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is WeaponProfileId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(WeaponProfileId left, WeaponProfileId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WeaponProfileId left, WeaponProfileId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct CombatStatusId : IEquatable<CombatStatusId>, IComparable<CombatStatusId>
{
    private readonly string? _value;

    public CombatStatusId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Combat status id is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public int CompareTo(CombatStatusId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public bool Equals(CombatStatusId other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is CombatStatusId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    public static bool operator ==(CombatStatusId left, CombatStatusId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CombatStatusId left, CombatStatusId right)
    {
        return !left.Equals(right);
    }
}
}
