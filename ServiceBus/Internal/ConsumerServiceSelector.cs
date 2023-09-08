using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nacencom.Infrastructure.ServiceBus.Internal
{
    public class ConsumerServiceSelector : IConsumerServiceSelector
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly Type _consumerType = typeof(IMqSubscriber);

        public ConsumerServiceSelector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IReadOnlyList<ConsumerExecutorDescriptor> FindConsumers()
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();
            var subscribeTypeInfo = typeof(IMqSubscriber).GetTypeInfo();

            using var scope = _serviceProvider.CreateScope();
            var scopeProvider = scope.ServiceProvider;

            //Find consumers from that implement ISubscriber interface
            var consumerServices = scopeProvider.GetServices<IMqSubscriber>();

            var genericConsumers = consumerServices
                .Where(t => t.GetType().GetInterfaces().Any(x => x.IsGenericType &&
                      x.GetGenericTypeDefinition() == typeof(IMqSubscriber<>)
                ));

            executorDescriptorList.AddRange(GetGenericDescriptors(genericConsumers));
            return executorDescriptorList;
        }

        public IEnumerable<Type> FindConsumerTypes(params Assembly[] assemblies)
        {
            foreach (var type in GetAssemblies(assemblies).Distinct().SelectMany(x => x.GetTypes().Where(FilterConsumers)))
            {
                yield return type;
            }
        }

        private static IEnumerable<ConsumerExecutorDescriptor> GetGenericDescriptors(IEnumerable<IMqSubscriber> consumerServices)
        {
            foreach (var service in consumerServices)
            {
                var type = service.GetType();
                var typeInfo = type.GetTypeInfo();
                var method = typeInfo.DeclaredMethods.FirstOrDefault(x => x.Name == "Handle");
                if (method == null) continue;
                var parameters = method.GetParameters().Select(p => new ParameterDescriptor
                {
                    Name = p.Name!,
                    ParameterType = p.ParameterType,
                }).ToList();

                yield return new ConsumerExecutorDescriptor
                {
                    Exchange = service.Exchange,
                    Channel = service.Channel,
                    WorkQueues = service.WorkQueues,
                    Parameters = parameters,
                    MethodInfo = method,
                    Type = type,
                };
            }
        }

        private static Assembly[] GetAssemblies(params Assembly[] assemblies)
        {
            var asm = assemblies?.Any() == true ? assemblies : new[] { Assembly.GetEntryAssembly() };
            return asm!;
        }

        private static bool FilterConsumers(Type t)
        {
            return _consumerType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract;
        }
    }
}
