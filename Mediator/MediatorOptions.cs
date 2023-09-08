using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Nacencom.Infrastructure.Mediator
{
    public class MediatorOptions
    {
        private readonly IServiceCollection _services;

        internal MediatorOptions(IServiceCollection services)
        {
            _services = services;
        }

        public void AddBehavior(Type behaviorType)
        {
            _services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
        }
    }
}
