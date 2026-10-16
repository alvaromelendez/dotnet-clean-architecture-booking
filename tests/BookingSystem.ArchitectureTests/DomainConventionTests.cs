using System.Reflection;
using ArchUnitNET.Domain;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.xUnit;
using BookingSystem.Domain.Abstractions;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static BookingSystem.ArchitectureTests.Layers;

namespace BookingSystem.ArchitectureTests;

public sealed class DomainConventionTests
{
    private static readonly IReadOnlyList<Type> Entities =
        ProductionTypes.DerivingFromOpenGeneric(DomainAssembly, typeof(Entity<>), includeAbstract: true);

    private static readonly IReadOnlyList<Type> AggregateRoots =
        ProductionTypes.DerivingFromOpenGeneric(DomainAssembly, typeof(AggregateRoot<>));

    [Fact]
    public void Selections_AreNotEmpty()
    {
        Entities.ShouldContain(typeof(Entity<>));
        AggregateRoots.ShouldContain(typeof(Domain.Bookings.Booking));
        AggregateRoots.ShouldContain(typeof(Domain.Rooms.Room));
    }

    /// <summary>
    /// State changes go through intention-revealing methods that enforce invariants.
    /// <c>init</c> accessors count as setters too: they would allow <c>with</c>-style bypasses.
    /// </summary>
    [Fact]
    public void EntitiesAndAggregates_HaveNoPublicSetters()
    {
        var violations = Entities
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        violations.ShouldBeEmpty(
            $"Entities must expose state changes through methods, not public setters:{Environment.NewLine}{ProductionTypes.Describe(violations)}");
    }

    /// <summary>
    /// Aggregates are created through factories (<c>Room.Create</c>, <c>BookingService.Reserve</c>)
    /// that validate invariants; a public constructor would bypass them.
    /// </summary>
    [Fact]
    public void AggregateRoots_HaveNoPublicConstructors()
    {
        var violations = AggregateRoots
            .SelectMany(type => type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            .Select(constructor => $"{constructor.DeclaringType!.Name}({string.Join(", ", constructor.GetParameters().Select(p => p.ParameterType.Name))})")
            .ToList();

        violations.ShouldBeEmpty(
            $"Aggregate roots must be created through factory methods:{Environment.NewLine}{ProductionTypes.Describe(violations)}");
    }

    [Fact]
    public void DomainEvents_AreSealed() =>
        Classes().That().ResideInAssembly(DomainAssembly).And().ImplementInterface(typeof(IDomainEvent))
            .Should().BeSealed()
            .Because("events are immutable facts and are matched by exact type when dispatched")
            .Check(ProductionCode);

    [Fact]
    public void DomainEvents_HaveNameEndingWithDomainEvent() =>
        Types().That().ResideInAssembly(DomainAssembly).And().ImplementInterface(typeof(IDomainEvent))
            .Should().HaveNameEndingWith("DomainEvent")
            .Check(ProductionCode);

    [Fact]
    public void TypesNamedDomainEvent_ImplementIDomainEvent() =>
        Classes().That().ResideInAssembly(DomainAssembly).And().HaveNameEndingWith("DomainEvent")
            .Should().ImplementInterface(typeof(IDomainEvent))
            .Check(ProductionCode);

    [Fact]
    public void DomainEvents_AreDeclaredOnlyInDomain() =>
        Types().That().ImplementInterface(typeof(IDomainEvent))
            .Should().ResideInAssembly(DomainAssembly)
            .Because("only aggregates raise domain events; outer layers react to them")
            .Check(ProductionCode);

    /// <summary>
    /// The domain receives "now" as a parameter. This is verified on the compiled IL: however it is
    /// written in source (<c>DateTime.UtcNow</c>, <c>using static</c>, inside a lambda...), reading the
    /// clock compiles to a call to the property getter, which ArchUnitNET records as a method call dependency.
    /// </summary>
    /// <remarks>
    /// <c>NotCallAny(MethodMembers()...)</c> cannot be used: <c>MethodMembers()</c> only enumerates members of
    /// the loaded assemblies, so a framework getter never matches and the rule would pass vacuously
    /// (see <see cref="SystemClockRule_DetectsTheInfrastructureClock"/>).
    /// </remarks>
    [Fact]
    public void Domain_DoesNotReadTheSystemClock() =>
        Types().That().Are(DomainLayer).Should().FollowCustomCondition(NotReadTheSystemClock, NotReadTheSystemClockDescription)
            .Because("the current time must be passed in by the caller (IDateTimeProvider in Application)")
            .Check(ProductionCode);

    [Fact]
    public void Application_DoesNotReadTheSystemClock() =>
        Types().That().Are(ApplicationLayer).Should().FollowCustomCondition(NotReadTheSystemClock, NotReadTheSystemClockDescription)
            .Because("handlers read the time through IDateTimeProvider")
            .Check(ProductionCode);

    /// <summary>
    /// Positive control for the IL rule: <c>SystemDateTimeProvider</c>, the single legitimate
    /// reader of the clock, must be reported. Otherwise the rules above could pass vacuously.
    /// </summary>
    [Fact]
    public void SystemClockRule_DetectsTheInfrastructureClock()
    {
        var violations = Types().That().Are(InfrastructureLayer).GetObjects(ProductionCode)
            .Where(type => !NotReadTheSystemClock(type).Pass)
            .Select(type => type.Name);

        violations.ShouldBe(["SystemDateTimeProvider"]);
    }

    private const string NotReadTheSystemClockDescription = "not read DateTime/DateTimeOffset.UtcNow, Now or Today";

    private static readonly Regex SystemClockGetter = new(
        @"^System\.(DateTime|DateTimeOffset) System\.(DateTime|DateTimeOffset)::get_(UtcNow|Now|Today)\(\)$",
        RegexOptions.Compiled);

    private static ConditionResult NotReadTheSystemClock(IType type)
    {
        var clockReads = type.Dependencies
            .OfType<MethodCallDependency>()
            .Where(call => SystemClockGetter.IsMatch(call.TargetMember.FullName))
            .Select(call => $"{call.OriginMember.Name} calls {call.TargetMember.FullName}")
            .Distinct()
            .ToList();

        return new ConditionResult(type, clockReads.Count == 0, string.Join("; ", clockReads));
    }
}
