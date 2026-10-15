using System.Reflection;
using BookingSystem.Application.Abstractions.Validation;
using BookingSystem.Domain.Abstractions;
using FluentValidation;
using MediatR;

namespace BookingSystem.Application.Behaviors;

/// <summary>
/// Runs every <see cref="IValidator{T}"/> registered for the request. When any of them fails,
/// the pipeline is short-circuited with a failed result carrying a <see cref="ValidationError"/>;
/// no exception is thrown and the handler is never invoked.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly Func<Error, TResponse> CreateFailure = BuildFailureFactory();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validatorList = validators.ToList();
        if (validatorList.Count == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        if (errors.Count == 0)
        {
            return await next(cancellationToken);
        }

        return CreateFailure(new ValidationError(errors));
    }

    /// <summary>
    /// Builds, once per closed generic type, a factory that turns an <see cref="Error"/> into
    /// either a failed <see cref="Result"/> or a failed <see cref="Result{TValue}"/>.
    /// </summary>
    private static Func<Error, TResponse> BuildFailureFactory()
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return error => (TResponse)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments().Single();
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), genericParameterCount: 1, types: [typeof(Error)])!
            .MakeGenericMethod(valueType);

        return failureMethod.CreateDelegate<Func<Error, TResponse>>();
    }
}
