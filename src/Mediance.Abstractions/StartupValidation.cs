namespace Mediance.Abstractions;

using System.Reflection;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Queries;

/// <summary>Provides startup validation that all handler types are properly registered in the DI container.</summary>
/// <remarks>
/// <para>
/// Call <see cref="ValidateHandlerRegistrations"/> in your Program.cs (after DI registration) to catch missing
/// handler registrations at startup instead of at runtime. This is especially useful if you use manual handler
/// registration instead of assembly scanning — it validates that every query/command type has a corresponding handler.
/// </para>
/// </remarks>
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

        // Scan for query and command types (those implementing IQuery<T> or ICommand<T>).
        var requestTypes = assembliesToScan.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var requestType in requestTypes)
        {
            // For each query/command type, derive the expected handler interface and verify it's registered.
            // For example, if MyQuery implements IQuery<UserDto>, we look for a registered IQueryHandler<MyQuery, UserDto>.
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

                // Try to resolve the handler from the service provider. If it's not registered, track the missing registration.
                if (handlerType is not null && sp.GetService(handlerType) is null)
                {
                    missing.Add($"{requestType.Name} implements {iface.Name} but no {handlerType.Name} is registered.");
                }
            }
        }

        // If any handlers are missing, throw with a detailed list so the developer can fix registrations immediately,
        // rather than discovering missing handlers at runtime when a request tries to execute.
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
