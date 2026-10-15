using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BookingSystem.Api.Infrastructure;

internal static class RateLimitingSetup
{
    /// <summary>
    /// Global fixed-window limiter partitioned by client IP. Rejections are 429 problem details
    /// with a <c>Retry-After</c> header when the limiter can compute one.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = limits.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
            });

            options.OnRejected = WriteProblemAsync;
        });

        return services;
    }

    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "The request limit was exceeded. Retry later.",
                Extensions = { ["code"] = ErrorCodes.RateLimitExceeded },
            },
        });
    }
}
