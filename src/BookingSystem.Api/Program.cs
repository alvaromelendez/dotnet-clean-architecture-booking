using BookingSystem.Api.Infrastructure;
using BookingSystem.Application;
using BookingSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiProblemDetails()
    .AddApiJson()
    .AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

await app.ApplyMigrationsIfEnabledAsync();

app.MapGroup("api/v1").MapEndpoints(app.Services);

await app.RunAsync();

/// <summary>
/// Entry point, made public so integration tests can host the API with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
