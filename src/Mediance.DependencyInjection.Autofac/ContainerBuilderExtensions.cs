using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mediance.Abstractions;

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
                    if (def == typeof(IQueryHandler<,>) || def == typeof(ICommandHandler<,>))
                    {
                        builder.RegisterType(type).As(iface).InstancePerLifetimeScope();
                    }
                }

                if (iface == typeof(ICommandHandler<>))
                {
                    builder.RegisterType(type).As(iface).InstancePerLifetimeScope();
                }
            }
        }

        builder.Register(c => new AutofacServiceProvider(c.Resolve<ILifetimeScope>())).As<IServiceProvider>().ExternallyOwned();

        builder.RegisterType<Mediator>().As<IMediator>().InstancePerLifetimeScope();

        return builder;
    }
}
