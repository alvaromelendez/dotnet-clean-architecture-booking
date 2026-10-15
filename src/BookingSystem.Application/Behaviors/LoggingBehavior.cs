using System.Diagnostics;
using BookingSystem.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Behaviors;

/// <summary>
/// Logs the start, outcome and duration of every request. Only the request type name is
/// logged, never its content, because requests may carry personal or sensitive data.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startTimestamp = Stopwatch.GetTimestamp();

        LoggingBehaviorLog.Handling(logger, requestName);

        try
        {
            var response = await next(cancellationToken);
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

            if (response.IsFailure)
            {
                LoggingBehaviorLog.HandledWithFailure(logger, requestName, response.Error.Code, elapsedMilliseconds);
            }
            else
            {
                LoggingBehaviorLog.Handled(logger, requestName, elapsedMilliseconds);
            }

            return response;
        }
        catch (Exception exception)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            LoggingBehaviorLog.HandlingFailedWithException(logger, exception, requestName, elapsedMilliseconds);
            throw;
        }
    }
}
