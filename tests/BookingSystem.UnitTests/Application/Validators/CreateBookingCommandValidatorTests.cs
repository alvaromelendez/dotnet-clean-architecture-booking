using BookingSystem.Application.Bookings.CreateBooking;
using FluentValidation.TestHelper;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Validators;

public sealed class CreateBookingCommandValidatorTests
{
    private static readonly CreateBookingCommand ValidCommand =
        new(Guid.NewGuid(), Guid.NewGuid(), UtcNow.AddDays(1), UtcNow.AddDays(1).AddHours(2), Attendees: 2);

    private readonly CreateBookingCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyRoomId_HasRoomIdError()
    {
        _validator.TestValidate(ValidCommand with { RoomId = Guid.Empty })
            .ShouldHaveValidationErrorFor(command => command.RoomId);
    }

    [Fact]
    public void Validate_WithEmptyUserId_HasUserIdError()
    {
        _validator.TestValidate(ValidCommand with { UserId = Guid.Empty })
            .ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Fact]
    public void Validate_WithDefaultStart_HasStartUtcError()
    {
        _validator.TestValidate(ValidCommand with { StartUtc = default })
            .ShouldHaveValidationErrorFor(command => command.StartUtc);
    }

    [Fact]
    public void Validate_WithDefaultEnd_HasEndUtcError()
    {
        _validator.TestValidate(ValidCommand with { EndUtc = default })
            .ShouldHaveValidationErrorFor(command => command.EndUtc);
    }

    [Fact]
    public void Validate_WithBusinessRuleViolation_LeavesItToTheDomain()
    {
        // Zero attendees and an inverted slot are business rules, enforced by the domain.
        var command = ValidCommand with { Attendees = 0, EndUtc = ValidCommand.StartUtc.AddHours(-1) };

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
