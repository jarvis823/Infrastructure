using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nacencom.Infrastructure.Mapper;
using Nacencom.Infrastructure.Mediator;
using Nacencom.Infrastructure.ServiceBus;
using Nacencom.Infrastructure.ServiceBus.Internal;
using Nacencom.Infrastructure.UnitOfWork;
using RabbitMQ.Client;
using Serilog;
using Serilog.Settings.Configuration;
using System.Reflection;

namespace Nacencom.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Type _consumerType = typeof(IMqSubscriber);

        public static IServiceCollection AddMediator(this IServiceCollection services, Action<MediatorOptions> options = null, params Assembly[] assemblies)
        {
            assemblies = GetAssemblies(assemblies);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
            options?.Invoke(new MediatorOptions(services));
            return services;
        }

        public static IServiceCollection AddRabbitMQServiceBus(this IServiceCollection services, Action<RabbitMQOptions> setupAction, Func<Type, bool> consumerFilter = null, params Assembly[] assemblies)
        {
            services.AddSingleton(sp =>
            {
                var options = new RabbitMQOptions();
                setupAction?.Invoke(options);
                return options;
            });
            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
                var options = sp.GetService<RabbitMQOptions>();
                var factory = new ConnectionFactory()
                {
                    ClientProvidedName = options.ClientProvidedName,
                    HostName = options.HostName,
                    Port = options.Port,
                    DispatchConsumersAsync = true,
                    UserName = options.UserName,
                    Password = options.Password,
                };
                if (!string.IsNullOrEmpty(options.UserName))
                {
                    factory.UserName = options.UserName;
                    factory.Password = options.Password;
                }
                return new DefaultRabbitMQPersistentConnection(factory, logger, options);
            });

            services.TryAddSingleton<IMqPublisher, MQPublisher>();
            services.AddConsumers(consumerFilter, GetAssemblies(assemblies));
            services.TryAddSingleton<IConsumerServiceSelector, ConsumerServiceSelector>();
            services.AddHostedService<Bootstrapper>();
            return services;
        }

        public static IServiceCollection AddSqlServerUnitOfWork<TContext>(this IServiceCollection services, Action<UnitOfWorkOptions<TContext>> options = null)
           where TContext : UnitOfWorkBase
        {
            return services.AddSqlServerUnitOfWork<TContext, TContext>(options);
        }

        public static IServiceCollection AddSqlServerUnitOfWork<TService, TContext>(this IServiceCollection services, Action<UnitOfWorkOptions<TContext>> options = null)
            where TService : class, IUnitOfWork
            where TContext : UnitOfWorkBase, TService
        {
            var uowOptions = new UnitOfWorkOptions<TContext>
            {
                DbProviderFactory = SqlClientFactory.Instance
            };

            if (options != null)
            {
                options(uowOptions);
                uowOptions.IsConfigured = true;
            }

            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<TContext>>();
                uowOptions.RetryPolicy = new Retry<SqlServerTransientExceptionDetector>(logger, typeof(TContext), uowOptions.RetryCount);
                return uowOptions;
            });
            services.AddScoped<TService, TContext>();
            return services;
        }

        public static IServiceCollection AddMapper(this IServiceCollection services, params Assembly[] assemblies)
        {
            assemblies = GetAssemblies(assemblies);
            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile(assemblies));
                cfg.AddMaps(assemblies);
            }).CreateMapper();

            return services
                .AddSingleton(mapper)
                .AddMapperInstance(mapper);
        }

        public static void AddSerilog(this IHostBuilder builder)
        {
            builder.UseSerilog((ctx, cfg) =>
            {
                var appName = ctx.Configuration.GetValue<string>("App:Name");
                if (string.IsNullOrEmpty(appName))
                {
                    var envName = ctx.HostingEnvironment.EnvironmentName;
                    appName = ctx.HostingEnvironment.ApplicationName + envName switch
                    {
                        "Production" => "",
                        _ => "." + envName
                    };
                }
                cfg
                    .ReadFrom.Configuration(ctx.Configuration, new ConfigurationReaderOptions
                    {
                        SectionName = "Serilog"
                    })
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", appName)
                    .Enrich.WithDemystifiedStackTraces();
            });
        }

        #region private

        private static IServiceCollection AddConsumers(this IServiceCollection services, Func<Type, bool> consumerFilter = null, params Assembly[] assemblies)
        {
            foreach (var type in assemblies.Distinct().SelectMany(x => x.GetTypes().Where(type =>
            {
                var cond = consumerFilter == null || consumerFilter(type);
                return cond && FilterConsumers(type);
            })).Distinct())
            {
                services.AddScoped(_consumerType, type);
                services.AddScoped(type);
            }
            return services;
        }

        private static Assembly[] GetAssemblies(params Assembly[] assemblies)
        {
            return assemblies?.Any() == true ? assemblies : new[] { Assembly.GetEntryAssembly()! };
        }

        private static bool FilterConsumers(Type t)
        {
            return _consumerType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract;
        }

        #endregion private
    }
}
