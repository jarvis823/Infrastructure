using RabbitMQ.Client;

namespace Nacencom.Infrastructure.ServiceBus
{
    public interface IRabbitMQPersistentConnection
    {
        bool IsConnected { get; }

        bool TryConnect(string source = null);

        IModel CreateModel();
    }
}
