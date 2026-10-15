using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingSystem.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without starting the API. Generating migrations does
/// not connect to the database; commands that do (e.g. <c>database update</c>) read the connection
/// string from the <c>BOOKINGSYSTEM_DB_CONNECTION</c> environment variable.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ConnectionStringVariable = "BOOKINGSYSTEM_DB_CONNECTION";
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=booking_system";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? DefaultConnectionString;

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        ApplicationDbContextOptions.Configure(builder, connectionString);

        return new ApplicationDbContext(builder.Options);
    }
}
