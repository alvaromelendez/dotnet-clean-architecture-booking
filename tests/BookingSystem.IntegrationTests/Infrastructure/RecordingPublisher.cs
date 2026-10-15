using MediatR;

namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Captures published notifications; optionally fails to simulate a broken event handler.
/// </summary>
internal sealed class RecordingPublisher(bool throwOnPublish = false) : IPublisher
{
    public List<object> Published { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (throwOnPublish)
        {
            throw new InvalidOperationException("Simulated handler failure.");
        }

        Published.Add(notification);

        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification =>
        Publish((object)notification, cancellationToken);
}
