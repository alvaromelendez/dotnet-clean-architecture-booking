using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Shared;
using BookingSystem.TestUtilities;
using FsCheck;
using FsCheck.Fluent;

namespace BookingSystem.UnitTests.Domain.Properties;

/// <summary>Two slots that are often related: independent, sharing a boundary, or identical.</summary>
public sealed record SlotPair(TimeSlot First, TimeSlot Second);

/// <summary>Two slots where <see cref="First"/> ends exactly when <see cref="Second"/> starts.</summary>
public sealed record AdjacentSlots(TimeSlot First, TimeSlot Second);

/// <summary>Three amounts that share a currency, so they can be combined.</summary>
public sealed record SameCurrencyAmounts(Money First, Money Second, Money Third);

/// <summary>A cancellable booking state observed at <see cref="UtcNow"/>, before or after the slot starts.</summary>
public sealed record CancellationScenario(BookingStatus Status, Money TotalPrice, TimeSlot Slot, DateTime UtcNow);

/// <summary>
/// Generators that only produce valid domain values, registered with
/// <c>[Property(Arbitrary = [typeof(DomainArbitraries)])]</c>. All instants are UTC and relative to
/// <see cref="TestClock.UtcNow"/>, never to the system clock.
/// </summary>
public static class DomainArbitraries
{
    // Starts within ±3 days at minute precision: wide enough for disjoint slots, narrow enough that
    // overlapping, identical and adjacent pairs come up often.
    private const int StartWindowMinutes = 3 * 24 * 60;
    private static readonly int MinDurationMinutes = (int)TimeSlot.MinimumDuration.TotalMinutes;
    private static readonly int MaxDurationMinutes = (int)TimeSlot.MaximumDuration.TotalMinutes;

    private const int MaxAmountInCents = 100_000_000; // 1,000,000.00

    private static readonly string[] Currencies = ["USD", "EUR", "GBP", "JPY"];

    public static Arbitrary<TimeSlot> TimeSlots() => TimeSlotGen().ToArbitrary();

    /// <summary>
    /// Independent slots rarely share an instant at minute precision, so two thirds of the pairs are built
    /// from the first slot's boundaries: those are exactly the cases where an off-by-one comparison
    /// (<c>&lt;</c> vs <c>&lt;=</c>) changes the answer.
    /// </summary>
    public static Arbitrary<SlotPair> SlotPairs() =>
        (from first in TimeSlotGen()
         from second in Gen.Frequency(
             (2, TimeSlotGen()),
             (1, DurationGen().Select(duration => SlotAt(first.End, duration))),
             (1, DurationGen().Select(duration => SlotAt(first.Start - duration, duration))),
             (1, DurationGen().Select(duration => SlotAt(first.Start, duration))),
             (1, Gen.Constant(first)))
         select new SlotPair(first, second))
        .ToArbitrary();

    public static Arbitrary<AdjacentSlots> AdjacentSlots() =>
        (from first in TimeSlotGen()
         from secondDuration in DurationGen()
         select new AdjacentSlots(first, SlotAt(first.End, secondDuration)))
        .ToArbitrary();

    public static Arbitrary<SameCurrencyAmounts> SameCurrencyAmounts() =>
        (from currency in Gen.Elements(Currencies)
         from first in MoneyGen(currency)
         from second in MoneyGen(currency)
         from third in MoneyGen(currency)
         select new SameCurrencyAmounts(first, second, third))
        .ToArbitrary();

    public static Arbitrary<CancellationScenario> CancellationScenarios() =>
        (from status in Gen.Elements(BookingStatus.Pending, BookingStatus.Confirmed)
         from currency in Gen.Elements(Currencies)
         from totalPrice in MoneyGen(currency)
         from slot in TimeSlotGen()
         // From 2 days before the start (every refund tier) to 1 hour after it (already started).
         from minutesBeforeStart in Gen.Choose(-60, 2 * 24 * 60)
         select new CancellationScenario(status, totalPrice, slot, slot.Start.AddMinutes(-minutesBeforeStart)))
        .ToArbitrary();

    private static Gen<TimeSlot> TimeSlotGen() =>
        from startOffset in Gen.Choose(-StartWindowMinutes, StartWindowMinutes)
        from duration in DurationGen()
        select SlotAt(TestClock.UtcNow.AddMinutes(startOffset), duration);

    private static Gen<TimeSpan> DurationGen() =>
        Gen.Choose(MinDurationMinutes, MaxDurationMinutes).Select(minutes => TimeSpan.FromMinutes(minutes));

    private static Gen<Money> MoneyGen(string currency) =>
        Gen.Choose(0, MaxAmountInCents).Select(cents => Money.Create(cents / 100m, currency).Value);

    private static TimeSlot SlotAt(DateTime start, TimeSpan duration) =>
        new TimeSlotBuilder().Starting(start).Lasting(duration).Build();
}
