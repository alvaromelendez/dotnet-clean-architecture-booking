using BookingSystem.Application.Abstractions.Validation;
using BookingSystem.Domain.Abstractions;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Abstractions;

public sealed class ValidationErrorTests
{
    [Fact]
    public void Constructor_WithErrors_CreatesValidationErrorWithStableCodeAndMessage()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = ["'Name' must not be empty."] };

        var error = new ValidationError(errors);

        error.Code.ShouldBe(ValidationError.ErrorCode);
        error.Type.ShouldBe(ErrorType.Validation);
        error.Description.ShouldNotBeNullOrWhiteSpace();
        error.Errors.ShouldBeSameAs(errors);
    }

    [Fact]
    public void Constructor_WithNullErrors_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ValidationError(null!));
    }
}
