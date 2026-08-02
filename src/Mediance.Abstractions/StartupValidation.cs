namespace Mediance.Abstractions;

using System.Reflection;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Queries;

/// <summary>Provides startup validation that all handler types are properly registered in the DI container.</summary>
public static class MedianceStartupValidation
{
    /// <summary>Validates that all handler types referenced by query and command types in the specified assemblies are registered in the DI container.</summary>
    /// <param name="provider">The service provider to validate against.</param>
    /// <param name="assembliesToScan">The assemblies to scan for request types.</param>
    /// <returns>The same <paramref name="provider"/> instance, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when one or more handler types referenced by the scanned request types are not registered.</exception>
    public static IServiceProvider ValidateHandlerRegistrations(
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
            // Resolve IQuery<T> and ICommand<T> interfaces to their corresponding
            // handler types and verify each handler is registered.
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
                {
                    missing.Add($"{requestType.Name} implements {iface.Name} but no {handlerType.Name} is registered.");
                }
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Mediance startup validation failed — missing handler registrations:\n"
                    + string.Join("\n", missing.Select(m => $"  - {m}"))
            );
        }

        return provider;
    }
}
