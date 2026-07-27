using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Queries;

namespace Mediance.DependencyInjection.Autofac;

public static class MedianceAutofacExtensions
{
    public static ContainerBuilder AddMediance(this ContainerBuilder builder, params Assembly[] assembliesToScan)
    {
        var handlerTypes = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var type in handlerTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType)
                {
                    var def = iface.GetGenericTypeDefinition();
                    if (
                        def == typeof(IQueryHandler<,>)
                        || def == typeof(ICommandHandler<,>)
                        || def == typeof(INotificationHandler<>)
                    )
                    {
                        builder.RegisterType(type).As(iface).InstancePerLifetimeScope();
                    }
                }

                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                {
                    builder.RegisterType(type).As(iface).InstancePerLifetimeScope();
                }
            }
        }

        builder.Register(c => new AutofacServiceProvider(c.Resolve<ILifetimeScope>())).As<IServiceProvider>().ExternallyOwned();

        builder.RegisterType<Mediator>().As<IMediator>().As<ISender>().As<IPublisher>().InstancePerLifetimeScope();

        return builder;
    }

    public static ContainerBuilder AddMediance(this ContainerBuilder builder, Type markerType) =>
        builder.AddMediance(markerType.Assembly);

    public static ContainerBuilder AddMedianceFromEntryAssembly(this ContainerBuilder builder) =>
        builder.AddMediance(Assembly.GetEntryAssembly()!);
}
