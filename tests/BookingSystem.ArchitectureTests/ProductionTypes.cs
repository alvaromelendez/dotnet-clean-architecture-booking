using System.Reflection;

namespace BookingSystem.ArchitectureTests;

/// <summary>
/// Reflection helpers for selections that ArchUnitNET cannot express precisely,
/// such as "implements any closed version of this open generic interface".
/// </summary>
internal static class ProductionTypes
{
    public static IReadOnlyList<Type> ConcreteClassesIn(Assembly assembly) =>
        assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && !IsCompilerGenerated(type))
            .ToList();

    public static IReadOnlyList<Type> ImplementingOpenGeneric(Assembly assembly, params Type[] openGenericInterfaces) =>
        ConcreteClassesIn(assembly)
            .Where(type => type.GetInterfaces().Any(implemented =>
                implemented.IsGenericType && openGenericInterfaces.Contains(implemented.GetGenericTypeDefinition())))
            .ToList();

    public static IReadOnlyList<Type> DerivingFromOpenGeneric(Assembly assembly, Type openGenericBaseClass, bool includeAbstract = false) =>
        assembly.GetTypes()
            .Where(type => type.IsClass && (includeAbstract || !type.IsAbstract) && !IsCompilerGenerated(type))
            .Where(type => InheritsFrom(type, openGenericBaseClass))
            .ToList();

    public static string Describe(IEnumerable<string> violations) =>
        string.Join(Environment.NewLine, violations.Select(violation => $"  - {violation}"));

    private static bool InheritsFrom(Type type, Type openGenericBaseClass)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBaseClass)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false);
}
