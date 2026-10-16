using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.xUnit;
using BookingSystem.Api.Endpoints;
using BookingSystem.Infrastructure.Persistence;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static BookingSystem.ArchitectureTests.Layers;

namespace BookingSystem.ArchitectureTests;

public sealed class ApiConventionTests
{
    /// <summary>
    /// The composition root wires the application together and is the only part of the Api allowed
    /// to know Infrastructure internals: <c>Program</c> plus the setup code in <c>BookingSystem.Api.Infrastructure</c>
    /// (health checks and development-only migrations need the DbContext).
    /// </summary>
    private const string CompositionRootNamespace = "BookingSystem.Api.Infrastructure";

    [Fact]
    public void Endpoints_LiveOnlyInApi() =>
        Classes().That().ImplementInterface(typeof(IEndpoint))
            .Should().ResideInAssembly(ApiAssembly)
            .AndShould().ResideInNamespaceMatching(@"^BookingSystem\.Api\.Endpoints(\..+)?$")
            .Check(ProductionCode);

    [Fact]
    public void Endpoints_HaveNameEndingWithEndpoint() =>
        Classes().That().ImplementInterface(typeof(IEndpoint))
            .Should().HaveNameEndingWith("Endpoint")
            .Check(ProductionCode);

    [Fact]
    public void ApiTypesOutsideCompositionRoot_DoNotReferenceDbContext() =>
        TypesOutsideCompositionRoot().Should().NotDependOnAny(typeof(ApplicationDbContext))
            .Because("endpoints talk to the Application through MediatR; only the composition root knows persistence")
            .Check(ProductionCode);

    /// <summary>
    /// Positive control: the composition root does reference the DbContext (health checks, migrations),
    /// so the same rule applied to the whole Api must fail. This proves the exclusion above is what makes it pass.
    /// </summary>
    [Fact]
    public void DbContextRule_DetectsCompositionRootUsage()
    {
        Types().That().Are(ApiLayer).Should().NotDependOnAny(typeof(ApplicationDbContext))
            .HasNoViolations(ProductionCode)
            .ShouldBeFalse();

        TypesOutsideCompositionRoot().GetObjects(ProductionCode)
            .ShouldContain(type => type.FullName == "BookingSystem.Api.Endpoints.Bookings.CreateBookingEndpoint");
    }

    private static GivenTypesConjunction TypesOutsideCompositionRoot() =>
        Types().That().Are(ApiLayer)
            .And().DoNotResideInNamespace(CompositionRootNamespace)
            .And().DoNotHaveFullName(typeof(Program).FullName!);
}
