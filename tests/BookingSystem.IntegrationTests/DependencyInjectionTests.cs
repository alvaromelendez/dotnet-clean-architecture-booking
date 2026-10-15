using BookingSystem.Application;
using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.Application.Abstractions.Notifications;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Infrastructure;
using BookingSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BookingSystem.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WithConnectionString_ResolvesEveryApplicationPort()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=localhost;Database=booking_system",
            })
            .Build();
        var services = new ServiceCollection().AddLogging().AddApplication().AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IRoomRepository>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IBookingRepository>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IRoomQueries>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IBookingQueries>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<INotificationSender>().ShouldNotBeNull();
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder().Build();

        Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));
    }
}
