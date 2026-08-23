namespace Interlocutor.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// Reports the three handler-shape diagnostics <see cref="InterlocutorSourceGenerator.Execute"/> checks once
/// the compilation-wide handler and request lists are known: a missing handler, more than one handler for the
/// same closed query/command, and a notification handler subscribing to the catch-all base marker.
/// </summary>
/// <remarks>
/// Separated from <see cref="InterlocutorSourceGenerator"/> the same way <see cref="GenericUsageAnalyzer"/>,
/// <see cref="OpenGenericHandlerResolver"/>, and <see cref="NotificationHandlerOrderPlanner"/> already are - so
/// the generator's own file stays incremental-pipeline wiring, and each diagnostic algorithm is directly
/// unit-testable without running the full generator pipeline.
/// </remarks>
internal static class HandlerDiagnosticAnalyzer
{
    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.MissingHandler"/> for every request with no matching handler
    /// among <paramref name="handlers"/> or any upstream <paramref name="externalModules"/>'s own handlers. A
    /// query or command request matches a handler with the same request and response type; a void command
    /// matches any void-command handler for the same request type regardless of the handler's chosen result
    /// type (see <see cref="RequestKind.VoidCommand"/>).
    /// </summary>
    /// <param name="handlers">Every handler discovered in this compilation.</param>
    /// <param name="externalModules">Every upstream referenced assembly's own discovered module and handlers.</param>
    /// <param name="requests">Every request declared in this compilation.</param>
    /// <param name="context">The source production context to report diagnostics to.</param>
    public static void ReportMissingHandlers(
        ImmutableArray<HandlerModel> handlers,
        ImmutableArray<ExternalModuleInfo> externalModules,
        ImmutableArray<RequestModel> requests,
        SourceProductionContext context
    )
    {
        var externalHandlers = externalModules.SelectMany(static m => m.Handlers);

        var queryHandlerRequestTypes = new HashSet<(string, string?)>(
            handlers.Where(static h => h.Kind == HandlerKind.Query).Select(static h => (h.RequestTypeName, h.ResponseTypeName))
        );
        var commandHandlerRequestTypes = new HashSet<(string, string?)>(
            handlers
                .Where(static h => h.Kind == HandlerKind.Command)
                .Select(static h => (h.RequestTypeName, h.ResponseTypeName))
        );
        var voidCommandHandlerRequestTypes = new HashSet<string>(
            handlers.Where(static h => h.Kind == HandlerKind.VoidCommand).Select(static h => h.RequestTypeName)
        );

        foreach (var externalHandler in externalHandlers)
        {
            switch (externalHandler.Kind)
            {
                case HandlerKind.Query:
                    queryHandlerRequestTypes.Add((externalHandler.RequestTypeName, externalHandler.ResponseTypeName));
                    break;
                case HandlerKind.Command:
                    commandHandlerRequestTypes.Add((externalHandler.RequestTypeName, externalHandler.ResponseTypeName));
                    break;
                case HandlerKind.VoidCommand:
                    voidCommandHandlerRequestTypes.Add(externalHandler.RequestTypeName);
                    break;
                case HandlerKind.Notification:
                default:
                    // Never emitted for external handlers - see RegistrationModuleEmitter.EmitAssemblyAttributes.
                    break;
            }
        }

        foreach (var request in requests)
        {
            var hasHandler = request.Kind switch
            {
                RequestKind.Query => queryHandlerRequestTypes.Contains((request.RequestTypeName, request.ResponseTypeName)),
                RequestKind.Command => commandHandlerRequestTypes.Contains((request.RequestTypeName, request.ResponseTypeName)),
                RequestKind.VoidCommand => voidCommandHandlerRequestTypes.Contains(request.RequestTypeName),
                _ => true,
            };

            if (!hasHandler)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(InterlocutorDiagnostics.MissingHandler, request.Location, request.RequestTypeName)
                );
            }
        }
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.DuplicateHandler"/> whenever more than one query or command
    /// handler is found for the same closed request/response pair. A notification's zero-or-many arity (see
    /// <see cref="HandlerKind.Notification"/>) and a void command's caller-chosen result type (see
    /// <see cref="RequestKind.VoidCommand"/>) both make "more than one handler" the expected, valid shape for
    /// those kinds, so only <see cref="HandlerKind.Query"/> and <see cref="HandlerKind.Command"/> are checked.
    /// </summary>
    /// <param name="handlers">Every handler discovered in this compilation.</param>
    /// <param name="context">The source production context to report diagnostics to.</param>
    public static void ReportDuplicateHandlers(ImmutableArray<HandlerModel> handlers, SourceProductionContext context)
    {
        var duplicateGroups = handlers
            .Where(static h => h.Kind is HandlerKind.Query or HandlerKind.Command)
            .GroupBy(static h => (h.RequestTypeName, h.ResponseTypeName))
            .Where(static group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var duplicateHandlers = group.ToImmutableArray();
            var handlerNames = string.Join(", ", duplicateHandlers.Select(static h => h.HandlerTypeName));

            foreach (var handler in duplicateHandlers)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InterlocutorDiagnostics.DuplicateHandler,
                        handler.Location,
                        handler.RequestTypeName,
                        handlerNames
                    )
                );
            }
        }
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.CatchAllNotificationHandler"/> for every notification handler
    /// implementing <c>INotificationHandler&lt;INotification&gt;</c> - subscribing to the base marker interface
    /// itself, and therefore to every notification published anywhere in the process.
    /// </summary>
    /// <param name="handlers">Every handler discovered in this compilation.</param>
    /// <param name="context">The source production context to report diagnostics to.</param>
    public static void ReportCatchAllNotificationHandlers(
        ImmutableArray<HandlerModel> handlers,
        SourceProductionContext context
    )
    {
        foreach (var handler in handlers)
        {
            if (
                handler.Kind == HandlerKind.Notification
                && handler.RequestTypeName == "global::" + WellKnownTypeNames.Notification
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InterlocutorDiagnostics.CatchAllNotificationHandler,
                        handler.Location,
                        handler.HandlerTypeName
                    )
                );
            }
        }
    }
}
