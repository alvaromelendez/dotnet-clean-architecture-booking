using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Behaviors;

/// <summary>
/// High-performance log messages for <see cref="LoggingBehavior{TRequest, TResponse}"/>.
/// </summary>
internal static partial class LoggingBehaviorLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    public static partial void Handling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds:0.00} ms")]
    public static partial void Handled(ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Handled {RequestName} with error {ErrorCode} in {ElapsedMilliseconds:0.00} ms")]
    public static partial void HandledWithFailure(
        ILogger logger, string requestName, string errorCode, double elapsedMilliseconds);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception while handling {RequestName} after {ElapsedMilliseconds:0.00} ms")]
    public static partial void HandlingFailedWithException(
        ILogger logger, Exception exception, string requestName, double elapsedMilliseconds);
}
