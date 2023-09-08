using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nacencom.Infrastructure.ServiceBus.Internal
{
    internal class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
        private readonly int _retryCount;
        private IConnection _connection;
        private bool _disposed;
        private static readonly object _sync_root = new();

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<DefaultRabbitMQPersistentConnection> logger, RabbitMQOptions options)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _retryCount = options.RetryCount;
        }

        public bool IsConnected => _connection?.IsOpen == true && !_disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public bool TryConnect(string source = null)
        {
            lock (_sync_root)
            {
                if (IsConnected) return true;
                _logger.LogInformation("RabbitMQ Client is trying to connect. Source: {source}", source);

                var policy = Policy.Handle<Exception>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message)
                    );

                policy.Execute(() =>
                {
                    if (IsConnected) return;
                    _connection = _connectionFactory.CreateConnection();
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                });

                if (IsConnected)
                {
                    _logger.LogInformation("RabbitMQ Client connected");
                    return true;
                }
                else
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                    return false;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, ex.Message);
            }
        }

        #region private function

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is blocked: {Reason}.", e.Reason);
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "A RabbitMQ connection throw exception.");
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is on shutdown. Cause: {Cause}. ReplyText: {ReplyText}.", reason.Cause, reason.ReplyText);
        }

        #endregion
    }
}
