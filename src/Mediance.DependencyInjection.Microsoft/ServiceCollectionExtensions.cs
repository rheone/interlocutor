using System.Reflection;
using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Queries;

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
                    if (def == typeof(IQueryHandler<,>) || def == typeof(ICommandHandler<,>))
                    {
                        services.AddScoped(iface, type);
                    }
                }

                if (iface == typeof(ICommandHandler<>))
                {
                    services.AddScoped(iface, type);
                }
            }
        }

        services.AddScoped<IMediator, Mediator>();

        return services;
    }
}
