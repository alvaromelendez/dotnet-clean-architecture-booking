namespace BookingSystem.Api.Infrastructure;

/// <summary>
/// Stable error codes produced by the API layer itself (domain and application codes come from <c>Error.Code</c>).
/// </summary>
internal static class ErrorCodes
{
    public const string ConcurrencyConflict = "Concurrency.Conflict";
    public const string InvalidRequest = "Request.Invalid";
    public const string RateLimitExceeded = "RateLimit.Exceeded";
    public const string UnexpectedError = "Server.UnexpectedError";

    /// <summary>
    /// Code for problem responses created by the framework from a bare status code (e.g. 404, 405).
    /// </summary>
    public static string ForStatusCode(int statusCode) =>
        $"Http.{Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode).Replace(" ", string.Empty)}";
}
