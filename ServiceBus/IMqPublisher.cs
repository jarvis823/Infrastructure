namespace Nacencom.Infrastructure.ServiceBus
{
    public interface IMqPublisher
    {
        void Publish(string channel, object message);
        Task PublishAsync(string channel, object message, CancellationToken cancellationToken = default);

        void Publish(string exchange, string channel, object message);
        Task PublishAsync(string exchange, string channel, object message, CancellationToken cancellationToken = default);
    }
}
