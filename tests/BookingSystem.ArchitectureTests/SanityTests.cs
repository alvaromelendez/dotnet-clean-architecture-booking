using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Assembly = System.Reflection.Assembly;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static BookingSystem.ArchitectureTests.Layers;

namespace BookingSystem.ArchitectureTests;

/// <summary>
/// Guards against rules that pass vacuously: scanning the wrong (or an empty) assembly,
/// or a selector that matches nothing, would turn every "should not" rule green.
/// </summary>
public sealed class SanityTests
{
    private static readonly Dictionary<string, Assembly> AssembliesByLayerNamespace = new()
    {
        [DomainNamespace] = DomainAssembly,
        [ApplicationNamespace] = ApplicationAssembly,
        [InfrastructureNamespace] = InfrastructureAssembly,
        [ApiNamespace] = ApiAssembly,
    };

    public static TheoryData<string> LayerNamespaces => [.. AssembliesByLayerNamespace.Keys];

    [Theory]
    [MemberData(nameof(LayerNamespaces))]
    public void ScannedAssembly_ContainsTypesOfItsLayer(string rootNamespace)
    {
        var assembly = AssembliesByLayerNamespace[rootNamespace];

        var types = Types().That().ResideInAssembly(assembly).GetObjects(ProductionCode).ToList();

        types.Count.ShouldBeGreaterThan(5, $"assembly {assembly.GetName().Name} was loaded without its types");
        types.ShouldContain(
            type => type.Namespace.FullName.StartsWith(rootNamespace, StringComparison.Ordinal),
            $"assembly {assembly.GetName().Name} does not contain types of {rootNamespace}");
    }

    [Fact]
    public void LayerSelectors_FindWellKnownTypes()
    {
        FullNamesOf(DomainLayer).ShouldContain(typeof(Domain.Bookings.Booking).FullName);
        FullNamesOf(ApplicationLayer).ShouldContain(typeof(Application.DependencyInjection).FullName);
        FullNamesOf(InfrastructureLayer).ShouldContain(typeof(Infrastructure.Persistence.ApplicationDbContext).FullName);
        FullNamesOf(ApiLayer).ShouldContain(typeof(Api.Endpoints.IEndpoint).FullName);
    }

    /// <summary>
    /// Positive controls: the same kind of rule used in <see cref="LayerDependencyTests"/> must report
    /// dependencies that do exist, including dependencies on external (NuGet/framework) namespaces.
    /// </summary>
    [Fact]
    public void DependencyRules_DetectExistingDependencies()
    {
        ShouldBeViolated(Types().That().Are(InfrastructureLayer).Should().NotDependOnAny(InNamespaceTree("Microsoft.EntityFrameworkCore")));
        ShouldBeViolated(Types().That().Are(ApplicationLayer).Should().NotDependOnAny(InNamespaceTree(DomainNamespace)));
        ShouldBeViolated(Types().That().Are(ApiLayer).Should().NotDependOnAny(InNamespaceTree("Microsoft.AspNetCore")));
    }

    private static List<string> FullNamesOf(IObjectProvider<IType> types) =>
        types.GetObjects(ProductionCode).Select(type => type.FullName).ToList();

    private static void ShouldBeViolated(IArchRule rule) =>
        rule.HasNoViolations(ProductionCode).ShouldBeFalse($"expected a violation of: {rule.Description}");
}
