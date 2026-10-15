namespace BookingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Commits every change tracked during a use case as a single atomic operation.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
