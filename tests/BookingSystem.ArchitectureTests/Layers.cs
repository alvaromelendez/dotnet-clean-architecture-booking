using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace BookingSystem.ArchitectureTests;

/// <summary>
/// The production assemblies under test, loaded once. Every assembly is located through a type
/// that lives in it, so a rename or a moved type breaks compilation instead of silently making
/// the rules scan the wrong assembly.
/// </summary>
internal static class Layers
{
    public const string DomainNamespace = "BookingSystem.Domain";
    public const string ApplicationNamespace = "BookingSystem.Application";
    public const string InfrastructureNamespace = "BookingSystem.Infrastructure";
    public const string ApiNamespace = "BookingSystem.Api";

    public static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.IDomainEvent).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    public static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    public static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    public static readonly Architecture ProductionCode = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly)
        .Build();

    public static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain layer");

    public static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application layer");

    public static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Infrastructure layer");

    public static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(ApiAssembly).As("Api layer");

    /// <summary>
    /// Types of any loaded or referenced assembly (including the framework and NuGet packages)
    /// whose namespace is <paramref name="rootNamespace"/> or one of its children.
    /// </summary>
    public static IObjectProvider<IType> InNamespaceTree(string rootNamespace) =>
        Types(includeReferenced: true)
            .That()
            .ResideInNamespaceMatching($"^{System.Text.RegularExpressions.Regex.Escape(rootNamespace)}(\\..+)?$")
            .As($"types in {rootNamespace}.*");
}
