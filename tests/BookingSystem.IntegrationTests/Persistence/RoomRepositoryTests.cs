using BookingSystem.Domain.Shared;
using BookingSystem.Infrastructure.Persistence.Repositories;
using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;

namespace BookingSystem.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class RoomRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ThenSave_PersistsRoomThatCanBeLoadedById()
    {
        var room = TestData.NewRoom(capacity: 6, hourlyRate: 35.50m);
        await using (var dbContext = fixture.CreateDbContext())
        {
            await new RoomRepository(dbContext).AddAsync(room);
            await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var loaded = await new RoomRepository(readContext).GetByIdAsync(room.Id);

        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe(room.Name);
        loaded.Capacity.ShouldBe(6);
        loaded.HourlyRate.ShouldBe(Money.Create(35.50m, "USD").Value);
        loaded.IsActive.ShouldBeTrue();
        loaded.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        await using var dbContext = fixture.CreateDbContext();

        var loaded = await new RoomRepository(dbContext).GetByIdAsync(Domain.Rooms.RoomId.New());

        loaded.ShouldBeNull();
    }

    [Theory]
    [InlineData("Court Central {0}")]
    [InlineData("COURT CENTRAL {0}")]
    [InlineData("  court central {0}  ")]
    public async Task ExistsByNameAsync_WithNameInAnyCase_ReturnsTrue(string candidateFormat)
    {
        var suffix = Guid.NewGuid().ToString("N");
        await SaveAsync(TestData.NewRoom(name: $"Court Central {suffix}"));

        await using var dbContext = fixture.CreateDbContext();
        var exists = await new RoomRepository(dbContext).ExistsByNameAsync(string.Format(candidateFormat, suffix));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithUnknownName_ReturnsFalse()
    {
        await using var dbContext = fixture.CreateDbContext();

        var exists = await new RoomRepository(dbContext).ExistsByNameAsync($"Missing {Guid.NewGuid():N}");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_WithSameNameInDifferentCase_IsRejectedByUniqueIndex()
    {
        var name = $"Board Room {Guid.NewGuid():N}";
        await SaveAsync(TestData.NewRoom(name: name));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(TestData.NewRoom(name: name.ToUpperInvariant())));

        // Unique violations are not concurrency conflicts: the unit of work lets them propagate untranslated.
        exception.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
    }

    private async Task SaveAsync(Domain.Rooms.Room room)
    {
        await using var dbContext = fixture.CreateDbContext();
        await new RoomRepository(dbContext).AddAsync(room);
        await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
    }
}
