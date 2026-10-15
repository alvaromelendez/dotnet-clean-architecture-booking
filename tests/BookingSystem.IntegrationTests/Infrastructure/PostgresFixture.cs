using BookingSystem.Infrastructure.Persistence;
using BookingSystem.IntegrationTests.Api;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one disposable PostgreSQL container per test run and applies the real migrations,
/// so tests exercise the actual schema, including raw-SQL constraints and indexes.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private BookingApiFactory? _api;

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// In-memory API host shared by the HTTP tests, created on first use.
    /// </summary>
    public BookingApiFactory Api => _api ??= new BookingApiFactory(ConnectionString);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    internal ApplicationDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        ApplicationDbContextOptions.Configure(builder, ConnectionString);

        return new ApplicationDbContext(builder.Options);
    }

    internal static UnitOfWork CreateUnitOfWork(ApplicationDbContext dbContext, IPublisher? publisher = null) =>
        new(dbContext, publisher ?? new RecordingPublisher(), NullLogger<UnitOfWork>.Instance);
}
