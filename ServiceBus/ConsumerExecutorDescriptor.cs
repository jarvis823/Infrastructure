using System.Reflection;

namespace Nacencom.Infrastructure.ServiceBus
{
    public class ConsumerExecutorDescriptor
    {
        public string Exchange { get; set; }
        public string Channel { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; } = default!;
        public bool WorkQueues { get; set; }
        public IList<ParameterDescriptor> Parameters { get; set; } = new List<ParameterDescriptor>();
    }

    public class ParameterDescriptor
    {
        public string Name { get; set; } = default!;

        public Type ParameterType { get; set; } = default!;
    }
}
