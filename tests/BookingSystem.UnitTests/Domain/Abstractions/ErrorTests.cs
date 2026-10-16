using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.UnitTests.Domain.Abstractions;

public sealed class ErrorTests
{
    [Fact]
    public void Failure_WithCodeAndDescription_CreatesFailureError()
    {
        var error = Error.Failure("Sample.Code", "Description");

        Assert.Equal(new Error("Sample.Code", "Description", ErrorType.Failure), error);
    }

    [Fact]
    public void Validation_WithCodeAndDescription_CreatesValidationError()
    {
        Assert.Equal(ErrorType.Validation, Error.Validation("Sample.Code", "Description").Type);
    }

    [Fact]
    public void NotFound_WithCodeAndDescription_CreatesNotFoundError()
    {
        Assert.Equal(ErrorType.NotFound, Error.NotFound("Sample.Code", "Description").Type);
    }

    [Fact]
    public void Conflict_WithCodeAndDescription_CreatesConflictError()
    {
        Assert.Equal(ErrorType.Conflict, Error.Conflict("Sample.Code", "Description").Type);
    }

    [Fact]
    public void None_HasNoCodeAndNoDescription()
    {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Description);
    }

    [Fact]
    public void Equals_SameCodeAndDescriptionButDifferentType_ReturnsFalse()
    {
        Assert.NotEqual(Error.Validation("Sample.Code", "Description"), Error.Conflict("Sample.Code", "Description"));
    }

    [Theory]
    [MemberData(nameof(DomainErrorTypes))]
    public void DomainErrors_AreClassifiedWithExpectedType(Error error, ErrorType expectedType)
    {
        Assert.Equal(expectedType, error.Type);
    }

    public static TheoryData<Error, ErrorType> DomainErrorTypes => new()
    {
        { MoneyErrors.NegativeAmount, ErrorType.Validation },
        { TimeSlotErrors.EndNotAfterStart, ErrorType.Validation },
        { RoomErrors.InvalidCapacity, ErrorType.Validation },
        { RoomErrors.AlreadyInactive, ErrorType.Conflict },
        { BookingErrors.StartNotInFuture, ErrorType.Validation },
        { BookingErrors.RoomInactive, ErrorType.Conflict },
        { BookingErrors.Overlapping, ErrorType.Conflict },
        { BookingErrors.NotPending, ErrorType.Conflict },
    };
}
