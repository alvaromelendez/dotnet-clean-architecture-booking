using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Domain.Bookings;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

public sealed class ContractTests
{
    /// <summary>
    /// The API maps statuses by numeric value, so both enums must stay identical.
    /// </summary>
    [Fact]
    public void BookingStatusContract_MirrorsDomainBookingStatus()
    {
        Enum.GetValues<BookingStatusContract>()
            .Select(status => (Name: status.ToString(), Value: (int)status))
            .ShouldBe(Enum.GetValues<BookingStatus>().Select(status => (Name: status.ToString(), Value: (int)status)));
    }
}
