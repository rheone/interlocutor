using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Interlocutor.Mediation;

/// <summary>
/// The remaining risk after removing <c>dynamic</c> from <see cref="Mediator"/>
/// is that a missing handler registration is still only discovered the
/// first time a given request type is dispatched — <c>GetRequiredService</c>
/// throws at that point, not at startup, no matter how the lookup is
/// written. This does not make the locator call itself unsafe (a missing
/// registration is a genuine configuration bug, and DI containers
/// universally defer resolution to first use), but it does mean the
/// failure surfaces later than ideal. Call
/// <see cref="ValidateInterlocutorHandlerRegistrations"/> once at
/// application startup to convert "first request in production throws"
/// into "the application fails to start."
/// </summary>
public static class InterlocutorStartupValidation
{
    public static IServiceProvider ValidateInterlocutorHandlerRegistrations(
        this IServiceProvider provider,
        params Assembly[] assembliesToScan
    )
    {
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var missing = new List<string>();

        var requestTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var requestType in requestTypes)
        {
            foreach (var iface in requestType.GetInterfaces().Where(i => i.IsGenericType))
            {
                var handlerType = iface.GetGenericTypeDefinition() switch
                {
                    var def when def == typeof(IQuery<>) => typeof(IQueryHandler<,>).MakeGenericType(
                        requestType,
                        iface.GetGenericArguments()[0]
                    ),
                    var def when def == typeof(ICommand<>) => typeof(ICommandHandler<,>).MakeGenericType(
                        requestType,
                        iface.GetGenericArguments()[0]
                    ),
                    _ => null,
                };

                if (handlerType is not null && sp.GetService(handlerType) is null)
                    missing.Add($"{requestType.Name} implements {iface.Name} but no {handlerType.Name} is registered.");
            }

            if (typeof(ICommand).IsAssignableFrom(requestType) && requestType.GetInterface(nameof(ICommand)) is not null)
            {
                var handlerType = typeof(ICommandHandler<>).MakeGenericType(requestType);
                if (sp.GetService(handlerType) is null)
                    missing.Add($"{requestType.Name} implements ICommand but no {handlerType.Name} is registered.");
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Interlocutor startup validation failed — missing handler registrations:\n"
                    + string.Join("\n", missing.Select(m => $"  - {m}"))
            );
        }

        return provider;
    }
}
