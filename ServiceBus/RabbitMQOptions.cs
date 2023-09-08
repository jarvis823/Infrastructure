namespace Nacencom.Infrastructure.ServiceBus
{
    public class RabbitMQOptions
    {
        public string ClientProvidedName { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RetryCount { get; set; }
        public string ExchangeName { get; set; }
    }
}
