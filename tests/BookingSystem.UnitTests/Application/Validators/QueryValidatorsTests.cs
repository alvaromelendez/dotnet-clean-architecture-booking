using BookingSystem.Application.Bookings.GetUserBookings;
using BookingSystem.Application.Rooms.GetAvailableRooms;
using BookingSystem.Domain.Bookings;
using FluentValidation.TestHelper;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Validators;

public sealed class QueryValidatorsTests
{
    private static readonly GetUserBookingsQuery ValidUserBookings = new(Guid.NewGuid());
    private static readonly GetAvailableRoomsQuery ValidAvailableRooms = new(UtcNow, UtcNow.AddHours(1), 2);

    private readonly GetUserBookingsQueryValidator _userBookingsValidator = new();
    private readonly GetAvailableRoomsQueryValidator _availableRoomsValidator = new();

    [Fact]
    public void GetUserBookings_WithDefaults_HasNoErrors()
    {
        _userBookingsValidator.TestValidate(ValidUserBookings).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetUserBookings_WithEmptyUserId_HasUserIdError()
    {
        _userBookingsValidator.TestValidate(ValidUserBookings with { UserId = Guid.Empty })
            .ShouldHaveValidationErrorFor(query => query.UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetUserBookings_WithPageBelowOne_HasPageError(int page)
    {
        _userBookingsValidator.TestValidate(ValidUserBookings with { Page = page })
            .ShouldHaveValidationErrorFor(query => query.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetUserBookingsQuery.MaxPageSize + 1)]
    public void GetUserBookings_WithPageSizeOutOfRange_HasPageSizeError(int pageSize)
    {
        _userBookingsValidator.TestValidate(ValidUserBookings with { PageSize = pageSize })
            .ShouldHaveValidationErrorFor(query => query.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(GetUserBookingsQuery.MaxPageSize)]
    public void GetUserBookings_WithPageSizeAtBoundary_HasNoErrors(int pageSize)
    {
        _userBookingsValidator.TestValidate(ValidUserBookings with { PageSize = pageSize })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetUserBookings_WithUndefinedStatus_HasStatusError()
    {
        _userBookingsValidator.TestValidate(ValidUserBookings with { Status = (BookingStatus)99 })
            .ShouldHaveValidationErrorFor(query => query.Status);
    }

    [Fact]
    public void GetAvailableRooms_WithValidQuery_HasNoErrors()
    {
        _availableRoomsValidator.TestValidate(ValidAvailableRooms).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetAvailableRooms_WithDefaultDates_HasStartAndEndErrors()
    {
        var result = _availableRoomsValidator.TestValidate(ValidAvailableRooms with { StartUtc = default, EndUtc = default });

        result.ShouldHaveValidationErrorFor(query => query.StartUtc);
        result.ShouldHaveValidationErrorFor(query => query.EndUtc);
    }

    [Fact]
    public void GetAvailableRooms_WithMinCapacityBelowOne_HasMinCapacityError()
    {
        _availableRoomsValidator.TestValidate(ValidAvailableRooms with { MinCapacity = 0 })
            .ShouldHaveValidationErrorFor(query => query.MinCapacity);
    }
}
