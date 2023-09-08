namespace Nacencom.Infrastructure.ServiceBus
{
    public interface IConsumerServiceSelector
    {
        IReadOnlyList<ConsumerExecutorDescriptor> FindConsumers();
    }
}
