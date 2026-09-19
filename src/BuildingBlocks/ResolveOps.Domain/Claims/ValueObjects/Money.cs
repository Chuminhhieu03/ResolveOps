using System;

namespace ResolveOps.Domain.Claims.ValueObjects;

public record Money : IComparable<Money>
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), "Cannot multiply money by a negative factor.");
        }
        return new Money(Amount * factor, Currency);
    }

    public bool IsZero() => Amount == 0m;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException($"Cannot perform arithmetic operations on different currencies: {Currency} and {other.Currency}.");
        }
    }

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public static Money Zero(string currency) => new Money(0, currency);
}
