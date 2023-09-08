namespace Nacencom.Infrastructure.ServiceBus
{
    public interface IMqSubscriber
    {
        string Exchange { get; }
        string Channel { get; }
        bool WorkQueues { get; }
    }

    public interface IMqSubscriber<T> : IMqSubscriber
    {
        Task Handle(T message, CancellationToken token = default);
    }
}
