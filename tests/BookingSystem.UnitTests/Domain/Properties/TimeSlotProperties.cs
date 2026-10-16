using BookingSystem.Domain.Shared;
using FsCheck.Xunit;

namespace BookingSystem.UnitTests.Domain.Properties;

/// <summary>
/// Properties of <see cref="TimeSlot.OverlapsWith"/> over randomly generated valid slots.
/// On failure FsCheck prints the shrunk counterexample and the seed to replay it.
/// </summary>
[Properties(Arbitrary = [typeof(DomainArbitraries)], MaxTest = 500)]
public sealed class TimeSlotProperties
{
    [Property]
    public bool OverlapsWith_IsSymmetric(SlotPair slots) =>
        slots.First.OverlapsWith(slots.Second) == slots.Second.OverlapsWith(slots.First);

    [Property]
    public bool OverlapsWith_ItselfIsAlwaysTrue(TimeSlot slot) =>
        slot.OverlapsWith(slot);

    [Property]
    public bool OverlapsWith_AdjacentSlot_IsAlwaysFalse(AdjacentSlots slots) =>
        !slots.First.OverlapsWith(slots.Second) && !slots.Second.OverlapsWith(slots.First);

    /// <summary>
    /// Slots are half-open [Start, End): they overlap exactly when each one starts before the other ends.
    /// </summary>
    [Property]
    public bool OverlapsWith_MatchesHalfOpenIntervalDefinition(SlotPair slots) =>
        slots.First.OverlapsWith(slots.Second)
        == (slots.First.Start < slots.Second.End && slots.Second.Start < slots.First.End);
}
