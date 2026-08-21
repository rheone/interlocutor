namespace Interlocutor.Generator;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one discovered handler-to-contract match: a concrete
/// handler type implementing one of the four mediator handler contracts, closed over its request (and, for
/// query/command/void-command handlers, response) type.
/// </summary>
/// <remarks>
/// One handler type implementing more than one handler contract (or the same contract closed over more than
/// one request) produces one <see cref="HandlerModel"/> per closed interface - mirroring
/// <c>Interlocutor.DependencyInjection.Abstractions.HandlerDiscovery</c>'s "one registration per matching
/// handler interface" rule, replicated here at compile time.
/// </remarks>
/// <param name="Kind">Which of the four handler contracts <see cref="HandlerTypeName"/> implements here.</param>
/// <param name="HandlerTypeName">The handler's fully-qualified (<c>global::</c>-prefixed) type name.</param>
/// <param name="RequestTypeName">The request type's fully-qualified (<c>global::</c>-prefixed) type name.</param>
/// <param name="ResponseTypeName">
/// The response type's fully-qualified (<c>global::</c>-prefixed) type name - the handler's own <c>TResponse</c>/
/// <c>TResult</c> type argument. Never <see langword="null"/>: even <see cref="HandlerKind.Notification"/> handlers
/// have no response type in the pipeline sense, but this field is only read for the three kinds that do.
/// </param>
/// <param name="ServiceInterfaceName">
/// The fully-qualified closed handler-contract interface name (for example
/// <c>global::Interlocutor.Abstractions.Queries.IQueryHandler&lt;global::MyApp.FooQuery, global::MyApp.FooResult&gt;</c>)
/// this handler satisfies - used verbatim as the emitted registration's service type.
/// </param>
/// <param name="Location">
/// The handler type declaration's identifier location, used to anchor the duplicate-handler diagnostic at
/// the handler type itself rather than at the compilation as a whole.
/// </param>
/// <param name="RunAfter">
/// Fully-qualified handler type names this <see cref="HandlerKind.Notification"/> handler must run after
/// (from <c>NotificationHandlerRunAfterAttribute</c>), empty for every other <see cref="HandlerKind"/> and
/// for a notification handler with no such attribute. See ADR-0007's relative-ordering escape hatch.
/// </param>
/// <param name="RunBefore">
/// The mirror image of <see cref="RunAfter"/>, from <c>NotificationHandlerRunBeforeAttribute</c>.
/// </param>
internal sealed record HandlerModel(
    HandlerKind Kind,
    string HandlerTypeName,
    string RequestTypeName,
    string? ResponseTypeName,
    string ServiceInterfaceName,
    Location Location,
    ImmutableArray<string> RunAfter = default,
    ImmutableArray<string> RunBefore = default
)
{
    /// <summary>Gets <see cref="RunAfter"/>.</summary>
    /// <value>Defaulted to empty rather than a default (uninitialized) <c>ImmutableArray</c>.</value>
    public ImmutableArray<string> RunAfterOrEmpty => this.RunAfter.IsDefault ? [] : this.RunAfter;

    /// <summary>Gets <see cref="RunBefore"/>.</summary>
    /// <value>Defaulted to empty rather than a default (uninitialized) <c>ImmutableArray</c>.</value>
    public ImmutableArray<string> RunBeforeOrEmpty => this.RunBefore.IsDefault ? [] : this.RunBefore;
}
