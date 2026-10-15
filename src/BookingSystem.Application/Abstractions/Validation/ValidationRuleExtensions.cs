using FluentValidation;

namespace BookingSystem.Application.Abstractions.Validation;

/// <summary>
/// Reusable input-shape rules. Business rules are not duplicated here: they live in the domain.
/// </summary>
internal static class ValidationRuleExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeCurrencyCode<T>(this IRuleBuilderInitial<T, string> ruleBuilder) =>
        ruleBuilder
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("'{PropertyName}' must be a three-letter ISO 4217 currency code.");
}
