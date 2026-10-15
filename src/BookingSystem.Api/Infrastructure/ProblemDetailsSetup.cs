using System.Diagnostics;

namespace BookingSystem.Api.Infrastructure;

internal static class ProblemDetailsSetup
{
    /// <summary>
    /// Every problem response (results, exceptions, status code pages, rate limiting, versioning)
    /// gets a <c>traceId</c> to correlate it with the logs, and a stable <c>code</c> when the
    /// producer did not set one.
    /// </summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var httpContext = context.HttpContext;

            context.ProblemDetails.Instance ??= $"{httpContext.Request.Method} {httpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? httpContext.TraceIdentifier);
            context.ProblemDetails.Extensions.TryAdd(
                "code",
                ErrorCodes.ForStatusCode(context.ProblemDetails.Status ?? httpContext.Response.StatusCode));
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
