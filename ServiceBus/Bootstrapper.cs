using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Nacencom.Infrastructure.ServiceBus
{
    internal class Bootstrapper : BackgroundService
    {
        private readonly IConsumerServiceSelector _consumerServiceSelector;
        private readonly IRabbitMQPersistentConnection _connection;
        private readonly RabbitMQOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public Bootstrapper(
              IConsumerServiceSelector consumerServiceSelector
            , IRabbitMQPersistentConnection connection
            , RabbitMQOptions options
            , IServiceProvider serviceProvider)
        {
            _consumerServiceSelector = consumerServiceSelector;
            _connection = connection;
            _options = options;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_connection.IsConnected)
            {
                _connection.TryConnect();
            }

            var consumers = _consumerServiceSelector.FindConsumers();
            foreach (var consumer in consumers)
            {
                if (consumer.WorkQueues)
                {
                    SubscribeWorkQueue(consumer, stoppingToken);
                }
                else
                {
                    Subscribe(consumer, stoppingToken);
                }
            }
            await Task.CompletedTask;
        }

        private string Subscribe(ConsumerExecutorDescriptor consumerExecutor, CancellationToken cancellationToken)
        {
            var exchange = string.IsNullOrWhiteSpace(consumerExecutor.Exchange) ? _options.ExchangeName : consumerExecutor.Exchange;
            var routingKey = GetRoutingKey(exchange, consumerExecutor.Channel);
            var model = CreateModel(exchange);
            var queue = model.QueueDeclare($"{routingKey}_{Guid.NewGuid():N}", exclusive: true, autoDelete: true).QueueName;
            model.QueueBind(queue, exchange, routingKey);
            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += async (_, args) =>
            {
                await HandleMessage(consumerExecutor, args, cancellationToken);
            };
            return model.BasicConsume(queue, autoAck: true, consumer: consumer);
        }

        private string SubscribeWorkQueue(ConsumerExecutorDescriptor consumerExecutor, CancellationToken cancellationToken)
        {
            var exchange = string.IsNullOrWhiteSpace(consumerExecutor.Exchange) ? _options.ExchangeName : consumerExecutor.Exchange;
            var model = CreateModel(exchange);
            var routingKey = GetRoutingKey(exchange, consumerExecutor.Channel);
            var queue = model.QueueDeclare(routingKey, exclusive: false, autoDelete: false).QueueName;
            model.QueueBind(queue, exchange, routingKey);
            model.BasicQos(0, 1, true);
            var consumer = new AsyncEventingBasicConsumer(model);

            consumer.Received += async (_, args) =>
            {
                try
                {
                    await HandleMessage(consumerExecutor, args, cancellationToken);
                }
                finally
                {
                    model.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
                }
            };

            return model.BasicConsume(queue, autoAck: false, consumer: consumer);
        }

        private async Task HandleMessage(ConsumerExecutorDescriptor consumerExecutor, BasicDeliverEventArgs args, CancellationToken cancellationToken)
        {
            var message = Encoding.UTF8.GetString(args.Body.ToArray());
            using var scope = _serviceProvider.CreateScope();
            var scopeProvider = scope.ServiceProvider;
            var consumerService = scopeProvider.GetService(consumerExecutor.Type);
            if (consumerService != null)
            {
                var msg = Convert(message, consumerExecutor.Parameters[0].ParameterType);
                var task = (Task)consumerExecutor.MethodInfo.Invoke(consumerService, new object[] { msg, cancellationToken });
                await task;
            }
        }

        static object Convert(string message, Type type)
        {
            if (type == typeof(string))
                return message;
            return JsonSerializer.Deserialize(message, type);
        }

        private IModel CreateModel(string exchange)
        {
            var model = _connection.CreateModel();
            model.ExchangeDeclare(exchange: exchange, ExchangeType.Direct);
            return model;
        }

        private static string GetRoutingKey(string exchange, string channel)
        {
            return $"{exchange}_{channel}";
        }
    }
}
