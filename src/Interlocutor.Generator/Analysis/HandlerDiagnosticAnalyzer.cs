namespace Interlocutor.Generator.Analysis;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Interlocutor.Generator.Diagnostics;
using Interlocutor.Generator.Models;
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
        var sets = BuildHandlerRequestTypeSets(handlers, externalModules);

        // ImmutableArray<T>.Where resolves to System.Linq.ImmutableArrayExtensions' specialized overload (not
        // the boxing IEnumerable<T> one), so this stays allocation-free - safe for this generator's hot path.
        foreach (
            var request in requests.Where(request =>
                !sets.HasHandler(request.Kind, request.RequestTypeName, request.ResponseTypeName)
            )
        )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(InterlocutorDiagnostics.MissingHandler, request.Location, request.RequestTypeName)
            );
        }
    }

    /// <summary>
    /// Reports <see cref="InterlocutorDiagnostics.UnresolvedGenericRequestUsage"/> for every closed generic
    /// query/command request usage (ticket 15) that resolves to no handler at all: not a closed handler in
    /// this compilation, not an open generic handler closed over it here (already merged into
    /// <paramref name="handlers"/> by <see cref="OpenGenericHandlerResolver"/> before this runs), and not a
    /// closed handler from an upstream referenced assembly's generated module. A usage
    /// <see cref="OpenGenericHandlerResolver.Result.AttemptedUsageDisplayNames"/> already covers - because at
    /// least one local open generic handler's request definition key matched it, whether that resolution
    /// succeeded, hit a constraint mismatch, or was left as an <see cref="InterlocutorDiagnostics.OpenGenericHandlerAmbiguity"/>
    /// - is skipped here to avoid reporting the same usage twice under two different diagnostics.
    /// </summary>
    /// <param name="handlers">Every handler discovered in this compilation, including locally-closed open generic handlers.</param>
    /// <param name="externalModules">Every upstream referenced assembly's own discovered module and handlers.</param>
    /// <param name="genericUsages">Every closed generic type usage found anywhere in the compilation.</param>
    /// <param name="attemptedUsageDisplayNames">
    /// Every usage display name <see cref="OpenGenericHandlerResolver.Resolve"/> already attempted to resolve
    /// locally - excluded here so its own diagnostics (success, ambiguity, or a silent constraint mismatch)
    /// are the only ones reported for that usage.
    /// </param>
    /// <param name="context">The source production context to report diagnostics to.</param>
    public static void ReportUnresolvedGenericRequestUsages(
        ImmutableArray<HandlerModel> handlers,
        ImmutableArray<ExternalModuleInfo> externalModules,
        ImmutableArray<ClosedGenericRequestUsageModel> genericUsages,
        ImmutableArray<string> attemptedUsageDisplayNames,
        SourceProductionContext context
    )
    {
        var sets = BuildHandlerRequestTypeSets(handlers, externalModules);
        var attempted = new HashSet<string>(attemptedUsageDisplayNames, System.StringComparer.Ordinal);

        foreach (var usage in genericUsages)
        {
            if (usage.RequestKind is null || attempted.Contains(usage.ClosedRequestDisplayName))
            {
                continue;
            }

            if (!sets.HasHandler(usage.RequestKind.Value, usage.ClosedRequestDisplayName, usage.ResponseDisplayName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InterlocutorDiagnostics.UnresolvedGenericRequestUsage,
                        usage.Location,
                        usage.ClosedRequestDisplayName
                    )
                );
            }
        }
    }

    /// <summary>
    /// Builds the (request type, response type) lookup sets <see cref="ReportMissingHandlers"/> and
    /// <see cref="ReportUnresolvedGenericRequestUsages"/> both check against - every query, command, and
    /// void-command request type a handler exists for, whether declared in this compilation or an upstream
    /// referenced assembly's generated module.
    /// </summary>
    /// <param name="handlers">Every handler discovered in this compilation.</param>
    /// <param name="externalModules">Every upstream referenced assembly's own discovered module and handlers.</param>
    /// <returns>The three lookup sets, keyed by (request type, response type) for query/command, request type alone for void command.</returns>
    private static HandlerRequestTypeSets BuildHandlerRequestTypeSets(
        ImmutableArray<HandlerModel> handlers,
        ImmutableArray<ExternalModuleInfo> externalModules
    )
    {
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

        foreach (var externalHandler in externalModules.SelectMany(static m => m.Handlers))
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

        return new HandlerRequestTypeSets(queryHandlerRequestTypes, commandHandlerRequestTypes, voidCommandHandlerRequestTypes);
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

    /// <summary>
    /// The three (request type, response type) lookup sets <see cref="BuildHandlerRequestTypeSets"/> builds
    /// once and both <see cref="ReportMissingHandlers"/> and <see cref="ReportUnresolvedGenericRequestUsages"/>
    /// query via <see cref="HasHandler"/> - bundled into one type instead of three fields travelling together,
    /// and carrying the query/command/void-command dispatch each caller previously repeated its own copy of.
    /// </summary>
    /// <param name="QueryHandlerRequestTypes">Every (request type, response type) pair with a query handler.</param>
    /// <param name="CommandHandlerRequestTypes">Every (request type, response type) pair with a command handler.</param>
    /// <param name="VoidCommandHandlerRequestTypes">Every request type with at least one void-command handler, for any result type.</param>
    private sealed record HandlerRequestTypeSets(
        HashSet<(string, string?)> QueryHandlerRequestTypes,
        HashSet<(string, string?)> CommandHandlerRequestTypes,
        HashSet<string> VoidCommandHandlerRequestTypes
    )
    {
        /// <summary>Whether a handler exists for <paramref name="requestTypeName"/> (and, for query/command, <paramref name="responseTypeName"/>) at the given <paramref name="kind"/>.</summary>
        /// <param name="kind">The request's kind - determines which lookup set and key shape is checked.</param>
        /// <param name="requestTypeName">The request type to check.</param>
        /// <param name="responseTypeName">The response type to check, for <see cref="RequestKind.Query"/>/<see cref="RequestKind.Command"/>; ignored for <see cref="RequestKind.VoidCommand"/>.</param>
        /// <returns><see langword="true"/> if a matching handler exists.</returns>
        public bool HasHandler(RequestKind kind, string requestTypeName, string? responseTypeName) =>
            kind switch
            {
                RequestKind.Query => this.QueryHandlerRequestTypes.Contains((requestTypeName, responseTypeName)),
                RequestKind.Command => this.CommandHandlerRequestTypes.Contains((requestTypeName, responseTypeName)),
                RequestKind.VoidCommand => this.VoidCommandHandlerRequestTypes.Contains(requestTypeName),
                _ => true,
            };
    }
}
