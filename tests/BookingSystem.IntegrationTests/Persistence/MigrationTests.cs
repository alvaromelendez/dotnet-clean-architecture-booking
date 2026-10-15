using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BookingSystem.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class MigrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrations_AfterApplying_LeaveNoPendingMigrations()
    {
        await using var dbContext = fixture.CreateDbContext();

        (await dbContext.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public void Model_ComparedWithLatestMigration_HasNoPendingChanges()
    {
        using var dbContext = fixture.CreateDbContext();

        dbContext.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task Schema_AfterMigration_ContainsExclusionConstraintAndLowerNameIndex()
    {
        await using var dbContext = fixture.CreateDbContext();

        var constraintCount = await dbContext.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_constraint WHERE conname = 'ex_bookings_room_id_time_slot' AND contype = 'x'")
            .SingleAsync();
        var indexDefinition = await dbContext.Database
            .SqlQuery<string>($"SELECT indexdef AS \"Value\" FROM pg_indexes WHERE indexname = 'ux_rooms_lower_name'")
            .SingleAsync();

        constraintCount.ShouldBe(1);
        indexDefinition.ShouldContain("UNIQUE INDEX");
        indexDefinition.ShouldContain("lower");
    }
}
