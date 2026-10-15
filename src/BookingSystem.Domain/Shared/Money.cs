using System.Globalization;
using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Shared;

/// <summary>
/// A non-negative monetary amount in a specific currency.
/// </summary>
/// <remarks>
/// Arithmetic between different currencies is treated as a programming error and throws
/// <see cref="InvalidOperationException"/>: the domain has no exchange rates, so every amount
/// combined in a single calculation must already share a currency. Invalid user input
/// (negative amounts, malformed currency codes) is reported through <see cref="Create"/>.
/// </remarks>
public sealed record Money
{
    public const string DefaultCurrency = "USD";

    private const int CurrencyCodeLength = 3;
    private const int DecimalPlaces = 2;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>
    /// Upper-case ISO 4217 currency code.
    /// </summary>
    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            return MoneyErrors.NegativeAmount;
        }

        if (!IsValidCurrency(currency))
        {
            return MoneyErrors.InvalidCurrency;
        }

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <exception cref="ArgumentException"><paramref name="currency"/> is not a valid currency code.</exception>
    public static Money Zero(string currency = DefaultCurrency)
    {
        if (!IsValidCurrency(currency))
        {
            throw new ArgumentException($"'{currency}' is not a valid ISO 4217 currency code.", nameof(currency));
        }

        return new Money(0m, currency.ToUpperInvariant());
    }

    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies the amount by <paramref name="factor"/>, rounding to two decimal places
    /// (midpoint away from zero).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> is negative.</exception>
    public Money Multiply(decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factor);

        var amount = Math.Round(Amount * factor, DecimalPlaces, MidpointRounding.AwayFromZero);

        return new Money(amount, Currency);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in different currencies ({Currency} and {other.Currency}).");
        }
    }

    private static bool IsValidCurrency(string? currency) =>
        currency is { Length: CurrencyCodeLength } && currency.All(char.IsAsciiLetter);
}
