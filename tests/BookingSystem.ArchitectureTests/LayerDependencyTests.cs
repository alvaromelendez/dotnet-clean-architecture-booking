using System.Xml.Linq;
using ArchUnitNET.xUnit;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static BookingSystem.ArchitectureTests.Layers;

namespace BookingSystem.ArchitectureTests;

/// <summary>
/// Dependency rule of Clean Architecture: source dependencies only point inwards
/// (Api → Infrastructure → Application → Domain). On failure ArchUnitNET lists every
/// offending type together with the dependency that breaks the rule.
/// </summary>
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DoesNotDependOnApplication() =>
        Types().That().Are(DomainLayer).Should().NotDependOnAny(InNamespaceTree(ApplicationNamespace))
            .Because("the domain is the innermost layer and must not know its use cases")
            .Check(ProductionCode);

    [Fact]
    public void Domain_DoesNotDependOnInfrastructure() =>
        Types().That().Are(DomainLayer).Should().NotDependOnAny(InNamespaceTree(InfrastructureNamespace))
            .Because("the domain must stay persistence-ignorant")
            .Check(ProductionCode);

    [Fact]
    public void Domain_DoesNotDependOnApi() =>
        Types().That().Are(DomainLayer).Should().NotDependOnAny(InNamespaceTree(ApiNamespace))
            .Because("the domain must not know how it is exposed")
            .Check(ProductionCode);

    [Fact]
    public void Domain_DoesNotDependOnMicrosoftNamespaces() =>
        Types().That().Are(DomainLayer).Should().NotDependOnAny(InNamespaceTree("Microsoft"))
            .Because("the domain is plain C# without framework packages (no EF Core, ASP.NET Core, logging or DI)")
            .Check(ProductionCode);

    [Fact]
    public void Domain_DoesNotDependOnSystemData() =>
        Types().That().Are(DomainLayer).Should().NotDependOnAny(InNamespaceTree("System.Data"))
            .Because("data access belongs to Infrastructure")
            .Check(ProductionCode);

    [Fact]
    public void Application_DoesNotDependOnInfrastructure() =>
        Types().That().Are(ApplicationLayer).Should().NotDependOnAny(InNamespaceTree(InfrastructureNamespace))
            .Because("use cases depend on ports (interfaces), never on their implementations")
            .Check(ProductionCode);

    [Fact]
    public void Application_DoesNotDependOnApi() =>
        Types().That().Are(ApplicationLayer).Should().NotDependOnAny(InNamespaceTree(ApiNamespace))
            .Because("use cases must not know how they are exposed")
            .Check(ProductionCode);

    [Fact]
    public void Application_DoesNotDependOnEntityFrameworkCore() =>
        Types().That().Are(ApplicationLayer).Should().NotDependOnAny(InNamespaceTree("Microsoft.EntityFrameworkCore"))
            .Because("persistence technology is an Infrastructure detail hidden behind repositories and queries")
            .Check(ProductionCode);

    [Fact]
    public void Application_DoesNotDependOnAspNetCore() =>
        Types().That().Are(ApplicationLayer).Should().NotDependOnAny(InNamespaceTree("Microsoft.AspNetCore"))
            .Because("HTTP concerns belong to the Api layer")
            .Check(ProductionCode);

    [Fact]
    public void Infrastructure_DoesNotDependOnApi() =>
        Types().That().Are(InfrastructureLayer).Should().NotDependOnAny(InNamespaceTree(ApiNamespace))
            .Because("Infrastructure implements Application ports and must not know the host")
            .Check(ProductionCode);

    /// <summary>
    /// IL-level rules only see dependencies that are actually used (the compiler drops unused references),
    /// so the project file is checked too: the domain must not even declare a package or project reference.
    /// </summary>
    [Fact]
    public void DomainProject_DeclaresNoPackageOrProjectReferences()
    {
        var project = XDocument.Load(Path.Combine(RepositoryRoot(), "src", "BookingSystem.Domain", "BookingSystem.Domain.csproj"));

        var references = project.Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference" or "Reference" or "FrameworkReference")
            .Select(element => $"{element.Name.LocalName} {element.Attribute("Include")?.Value}")
            .ToList();

        references.ShouldBeEmpty(
            $"BookingSystem.Domain.csproj must stay dependency-free, but declares: {string.Join(", ", references)}");
    }

    [Fact]
    public void DomainAssembly_ReferencesOnlyTheBaseClassLibrary()
    {
        var references = DomainAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name != "netstandard" && !name.StartsWith("System", StringComparison.Ordinal))
            .ToList();

        references.ShouldBeEmpty(
            $"BookingSystem.Domain must not reference other assemblies, but references: {string.Join(", ", references)}");
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BookingSystem.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"BookingSystem.sln not found above {AppContext.BaseDirectory}.");
    }
}
