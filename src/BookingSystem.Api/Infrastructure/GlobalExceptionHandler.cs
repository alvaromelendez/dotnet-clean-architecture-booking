using System.Diagnostics;
using BookingSystem.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Api.Infrastructure;

/// <summary>
/// Last line of defense for exceptions. Responses never contain exception messages or stack traces,
/// in any environment, because they may reveal internals such as SQL or connection details.
/// The full exception is logged together with the trace id returned to the client.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var problemDetails = CreateProblemDetails(exception, traceId);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }

    private ProblemDetails CreateProblemDetails(Exception exception, string traceId)
    {
        switch (exception)
        {
            case ConcurrencyConflictException:
                LogConcurrencyConflict(logger, exception, traceId);
                return Problem(
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    "The resource was modified by another request. Reload it and try again.",
                    ErrorCodes.ConcurrencyConflict);

            // Malformed JSON or unbindable parameters. Thrown (instead of returning 400) in Development.
            case BadHttpRequestException badRequest:
                LogBadRequest(logger, exception, traceId);
                return Problem(
                    badRequest.StatusCode,
                    "Bad Request",
                    "The request body or parameters could not be read.",
                    ErrorCodes.InvalidRequest);

            default:
                LogUnhandledException(logger, exception, traceId);
                return Problem(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    detail: null,
                    ErrorCodes.UnexpectedError);
        }
    }

    private static ProblemDetails Problem(int status, string title, string? detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception (traceId {TraceId})")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrency conflict (traceId {TraceId})")]
    private static partial void LogConcurrencyConflict(ILogger logger, Exception exception, string traceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Malformed request (traceId {TraceId})")]
    private static partial void LogBadRequest(ILogger logger, Exception exception, string traceId);
}
