using BookingSystem.Api.Infrastructure;
using BookingSystem.Application;
using BookingSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiProblemDetails()
    .AddApiJson()
    .AddApiVersioningAndOpenApi()
    .AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiDocumentation();
}

await app.ApplyMigrationsIfEnabledAsync();

app.MapVersionedEndpoints();

await app.RunAsync();

/// <summary>
/// Entry point, made public so integration tests can host the API with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
