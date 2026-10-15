namespace BookingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Thrown by <see cref="IUnitOfWork.SaveChangesAsync"/> when the changes conflict with data
/// written concurrently by another operation (optimistic concurrency or a database constraint
/// such as the booking exclusion constraint). Persistence implementations translate their
/// provider-specific exceptions into this one so handlers stay persistence-agnostic.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
