using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Shared;

public static class MoneyErrors
{
    public static readonly Error NegativeAmount = new(
        "Money.NegativeAmount",
        "The amount cannot be negative.");

    public static readonly Error InvalidCurrency = new(
        "Money.InvalidCurrency",
        "The currency must be a three-letter ISO 4217 code.");
}
