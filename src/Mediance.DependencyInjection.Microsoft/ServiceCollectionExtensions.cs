using System.Reflection;
using Mediance.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

public static class MedianceServiceCollectionExtensions
{
    public static IServiceCollection AddMediance(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        var handlerTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

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
                        services.AddScoped(iface, type);
                    }
                }

                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                {
                    services.AddScoped(iface, type);
                }
            }
        }

        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        return services;
    }

    public static IServiceCollection AddMediance(this IServiceCollection services, Type markerType) =>
        services.AddMediance(markerType.Assembly);

    public static IServiceCollection AddMedianceFromEntryAssembly(this IServiceCollection services) =>
        services.AddMediance(Assembly.GetEntryAssembly()!);
}
