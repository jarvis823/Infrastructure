using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Nacencom.Infrastructure.ServiceBus.Internal
{
    internal class MQPublisher : IMqPublisher
    {
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly RabbitMQOptions _options;
        private readonly ILogger<MQPublisher> _logger;
        private IModel _model;
        private static readonly object _lock = new();

        public MQPublisher(
              IRabbitMQPersistentConnection persistentConnection
            , RabbitMQOptions options
            , ILogger<MQPublisher> logger
        )
        {
            _persistentConnection = persistentConnection;
            _options = options;
            _logger = logger;
        }

        public void Publish(string channel, object message)
        {
            Publish(_options.ExchangeName, channel, message);
        }

        public void Publish(string exchange, string channel, object message)
        {
            InitModel(exchange, channel);
            var policy = Policy.Handle<Exception>()
              .WaitAndRetry(_options.RetryCount, Sleep, (ex, time) => OnRetry(ex, time, exchange, channel));
            policy.Execute(() => PublishImpl(exchange, channel, message));
        }

        public async Task PublishAsync(string exchange, string channel, object message, CancellationToken cancellationToken = default)
        {
            InitModel(exchange, channel);
            var policy = Policy.Handle<Exception>()
              .WaitAndRetryAsync(_options.RetryCount, Sleep, (ex, time) => OnRetry(ex, time, exchange, channel));

            await policy.ExecuteAsync(async (c) => await Task.Run(() => PublishImpl(exchange, channel, message), c), cancellationToken);
        }

        public async Task PublishAsync(string channel, object message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(_options.ExchangeName, channel, message, cancellationToken);
        }

        private void PublishImpl(string exchange, string channel, object message)
        {
            var body = Encoding.UTF8.GetBytes(message is string str ? str : JsonSerializer.Serialize(message));
            var properties = _model.CreateBasicProperties();
            properties.DeliveryMode = 1; // none-persistent
            _model.BasicPublish(exchange, $"{exchange}_{channel}", true, properties, body);
        }

        private void InitModel(string exchange, string channel)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect($"{exchange}: publish to channel {channel}");
            }
            if (_model == null)
            {
                lock (_lock)
                {
                    _model = _persistentConnection.CreateModel();
                    _model.ExchangeDeclare(exchange: exchange, ExchangeType.Direct);
                }
            }
        }

        private TimeSpan Sleep(int retryAttempt)
        {
            var retryTime = Math.Pow(2, retryAttempt);
            if (retryTime > 60)
                retryTime = 60;
            return TimeSpan.FromSeconds(retryTime);
        }

        private void OnRetry(Exception ex, TimeSpan t, string exchange, string channel)
        {
            _logger.LogWarning(ex, "{Exchange}: could not publish channel: {channel} after {TotalSeconds:n1}s ({Message})", exchange, channel, t.TotalSeconds, ex.Message);
        }
    }
}
