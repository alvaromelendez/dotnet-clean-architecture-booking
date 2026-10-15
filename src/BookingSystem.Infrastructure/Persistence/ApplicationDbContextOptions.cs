using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Persistence;

/// <summary>
/// Single source of truth for provider configuration, shared by runtime DI and design-time tooling.
/// </summary>
internal static class ApplicationDbContextOptions
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention();
}
