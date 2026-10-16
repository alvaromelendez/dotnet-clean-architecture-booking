using BookingSystem.Domain.Bookings;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace BookingSystem.UnitTests.Domain.Properties;

[Properties(Arbitrary = [typeof(DomainArbitraries)], MaxTest = 500)]
public sealed class CancellationPolicyProperties
{
    /// <summary>
    /// Whatever the status, price and notice, a refund is never negative, never exceeds the price
    /// and is in the price's currency. Once the slot has started there is no refund at all.
    /// </summary>
    [Property]
    public Property Refund_IsBetweenZeroAndTotalPriceInTheSameCurrency(CancellationScenario scenario)
    {
        var result = CancellationPolicy.CalculateRefund(scenario.Status, scenario.TotalPrice, scenario.Slot, scenario.UtcNow);

        if (scenario.UtcNow >= scenario.Slot.Start)
        {
            return (result.IsFailure && result.Error == BookingErrors.AlreadyStarted)
                .Label("cancelling after the start must fail with AlreadyStarted");
        }

        var refund = result.Value;

        return (refund.Amount >= 0m).Label("refund is not negative")
            .And(refund.Amount <= scenario.TotalPrice.Amount).Label("refund does not exceed the total price")
            .And(refund.Currency == scenario.TotalPrice.Currency).Label("refund is in the price currency");
    }
}
