using BookingSystem.Domain.Abstractions;

namespace BookingSystem.UnitTests.Domain.Abstractions;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.Failure("Sample.Failure", "Something went wrong.");

    [Fact]
    public void Success_WhenCreated_IsSuccessWithNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_WithError_IsFailureCarryingTheError()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void Failure_WithNoneError_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Constructor_SuccessCarryingAnError_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SampleResult(isSuccess: true, SampleError));
    }

    [Fact]
    public void SuccessOfT_WithValue_ExposesValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<int>(SampleError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessfulResult()
    {
        Result<string> result = "booked";

        Assert.True(result.IsSuccess);
        Assert.Equal("booked", result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailedResult()
    {
        Result<string> result = SampleError;

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    /// <summary>
    /// Reaches the protected constructor, which outer layers use to derive specialized results.
    /// </summary>
    private sealed class SampleResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
