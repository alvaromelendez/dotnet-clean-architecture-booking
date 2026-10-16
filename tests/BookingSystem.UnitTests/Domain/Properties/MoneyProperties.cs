using BookingSystem.Domain.Shared;
using FsCheck.Xunit;

namespace BookingSystem.UnitTests.Domain.Properties;

[Properties(Arbitrary = [typeof(DomainArbitraries)], MaxTest = 500)]
public sealed class MoneyProperties
{
    [Property]
    public bool Add_IsCommutative(SameCurrencyAmounts amounts) =>
        amounts.First.Add(amounts.Second) == amounts.Second.Add(amounts.First);

    [Property]
    public bool Add_IsAssociative(SameCurrencyAmounts amounts) =>
        amounts.First.Add(amounts.Second).Add(amounts.Third) == amounts.First.Add(amounts.Second.Add(amounts.Third));

    [Property]
    public bool Zero_IsTheIdentityOfAdd(SameCurrencyAmounts amounts)
    {
        var amount = amounts.First;
        var zero = Money.Zero(amount.Currency);

        return amount.Add(zero) == amount && zero.Add(amount) == amount;
    }
}
