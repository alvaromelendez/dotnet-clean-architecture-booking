using System.Text.Json;
using BookingSystem.Application.Abstractions.Validation;
using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Api.Infrastructure;

/// <summary>
/// Translates application results into HTTP results. This is the single place where
/// <see cref="ErrorType"/> is mapped to a status code.
/// </summary>
internal static class ResultExtensions
{
    private const string CodeExtension = "code";

    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Result, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result);

    public static TOut Match<TValue, TOut>(
        this Result<TValue> result,
        Func<TValue, TOut> onSuccess,
        Func<Result, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result);

    /// <summary>
    /// Converts a failed result into an RFC 9457 problem response that carries <c>code</c>.
    /// The <c>traceId</c> is added for every problem response in <see cref="ProblemDetailsSetup"/>.
    /// </summary>
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be converted into a problem.");
        }

        var error = result.Error;
        var extensions = new Dictionary<string, object?> { [CodeExtension] = error.Code };

        return error switch
        {
            ValidationError validationError => Results.ValidationProblem(
                ToCamelCaseKeys(validationError.Errors),
                detail: validationError.Description,
                extensions: extensions),

            { Type: ErrorType.Validation } => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: error.Description,
                extensions: extensions),

            { Type: ErrorType.NotFound } => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: error.Description,
                extensions: extensions),

            { Type: ErrorType.Conflict } => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: error.Description,
                extensions: extensions),

            // Failure means something unexpected: never leak its description to clients.
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred.",
                extensions: extensions),
        };
    }

    /// <summary>
    /// Validation keys come from C# property names; clients send camelCase JSON.
    /// </summary>
    private static Dictionary<string, string[]> ToCamelCaseKeys(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key), pair => pair.Value);
}
