using ArchUnitNET.xUnit;
using BookingSystem.Application.Abstractions.Messaging;
using FluentValidation;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static BookingSystem.ArchitectureTests.Layers;

namespace BookingSystem.ArchitectureTests;

/// <remarks>
/// Handlers and validators must not be public. They are only ever resolved by MediatR and
/// FluentValidation through assembly scanning (<c>RegisterServicesFromAssembly</c> and
/// <c>AddValidatorsFromAssembly(includeInternalTypes: true)</c>), so outer layers have no reason to
/// reference them: the Api only sees commands, queries and their results. Keeping them internal
/// keeps the Application's public surface limited to its use-case contracts; unit tests reach them
/// through <c>InternalsVisibleTo</c>.
/// </remarks>
public sealed class ApplicationConventionTests
{
    private static readonly IReadOnlyList<Type> Handlers = ProductionTypes.ImplementingOpenGeneric(
        ApplicationAssembly,
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>));

    private static readonly IReadOnlyList<Type> Validators =
        ProductionTypes.DerivingFromOpenGeneric(ApplicationAssembly, typeof(AbstractValidator<>));

    [Fact]
    public void Selections_AreNotEmpty()
    {
        Handlers.ShouldNotBeEmpty();
        Validators.ShouldNotBeEmpty();
    }

    [Fact]
    public void Handlers_AreSealed() =>
        Classes().That().Are(Handlers).Should().BeSealed()
            .Because("handlers are leaf implementations; inheritance would hide behavior from the pipeline")
            .Check(ProductionCode);

    [Fact]
    public void Handlers_AreNotPublic() =>
        Classes().That().Are(Handlers).Should().NotBePublic()
            .Because("handlers are resolved by MediatR, never referenced by outer layers")
            .Check(ProductionCode);

    [Fact]
    public void Handlers_HaveNameEndingWithHandler() =>
        Classes().That().Are(Handlers).Should().HaveNameEndingWith("Handler")
            .Check(ProductionCode);

    [Fact]
    public void Validators_AreSealed() =>
        Classes().That().Are(Validators).Should().BeSealed()
            .Check(ProductionCode);

    [Fact]
    public void Validators_AreNotPublic() =>
        Classes().That().Are(Validators).Should().NotBePublic()
            .Because("validators are resolved by the validation pipeline behavior, never referenced by outer layers")
            .Check(ProductionCode);

    [Fact]
    public void Validators_HaveNameEndingWithValidator() =>
        Classes().That().Are(Validators).Should().HaveNameEndingWith("Validator")
            .Check(ProductionCode);
}
