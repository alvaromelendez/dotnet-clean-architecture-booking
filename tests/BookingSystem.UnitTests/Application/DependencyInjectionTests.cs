using BookingSystem.Application;
using BookingSystem.Application.Behaviors;
using BookingSystem.Application.Rooms.CreateRoom;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace BookingSystem.UnitTests.Application;

public sealed class DependencyInjectionTests
{
    private readonly ServiceProvider _provider;

    public DependencyInjectionTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void AddApplication_RegistersBehaviorsInLoggingThenValidationOrder()
    {
        var behaviors = _provider.GetServices<IPipelineBehavior<CreateRoomCommand, Result<Guid>>>().ToList();

        behaviors.Select(behavior => behavior.GetType().GetGenericTypeDefinition()).ShouldBe(
        [
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
        ]);
    }

    [Fact]
    public void AddApplication_RegistersValidatorsFromApplicationAssembly()
    {
        _provider.GetServices<IValidator<CreateRoomCommand>>()
            .ShouldHaveSingleItem()
            .ShouldBeOfType<CreateRoomCommandValidator>();
    }

    [Fact]
    public void AddApplication_RegistersBookingServiceAndMediator()
    {
        _provider.GetService<BookingService>().ShouldNotBeNull();
        _provider.GetService<IMediator>().ShouldNotBeNull();
    }
}
