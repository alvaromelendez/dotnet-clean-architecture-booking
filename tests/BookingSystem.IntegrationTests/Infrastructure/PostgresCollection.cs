namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Shares a single <see cref="PostgresFixture"/> across all database tests.
/// Tests isolate their data by creating their own rooms and users instead of resetting the database.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgreSQL";
}
