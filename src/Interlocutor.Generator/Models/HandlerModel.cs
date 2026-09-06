namespace Interlocutor.Generator.Models;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one discovered handler-to-contract match: a concrete
/// handler type implementing one of the four mediator handler contracts, closed over its request (and, for
/// query/command/bare-command handlers, response) type.
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
[DebuggerDisplay("{Kind} {HandlerTypeName} ({RequestTypeName})")]
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

    /// <inheritdoc/>
    /// <remarks>
    /// Overridden for two reasons (ticket 53). First, the same reason as
    /// <see cref="CompilationInfo.Equals(CompilationInfo?)"/>: <see cref="ImmutableArray{T}"/>'s own
    /// <c>Equals</c> compares its backing array by reference, not by element, so the compiler-generated
    /// positional-record equality this type would otherwise get from <see cref="RunAfter"/>/<see cref="RunBefore"/>
    /// is useless for incremental-pipeline caching. Second, <see cref="Location"/> is deliberately excluded:
    /// <see cref="Microsoft.CodeAnalysis.Location"/>'s own equality compares the declaring
    /// <see cref="Microsoft.CodeAnalysis.SyntaxTree"/> by reference, and a syntax-provider transform re-runs
    /// (producing a new <see cref="Microsoft.CodeAnalysis.SyntaxTree"/> instance) for every candidate in a file
    /// whenever *any* part of that file changes - even a change nowhere near this handler's own declaration.
    /// Two <see cref="HandlerModel"/>s whose registration-relevant fields are otherwise identical describe the
    /// exact same logical registration regardless of which physical <see cref="Microsoft.CodeAnalysis.SyntaxTree"/>
    /// instance produced them, so comparing <see cref="Location"/> would defeat downstream caching for reasons
    /// that have nothing to do with what actually changed. <see cref="Location"/> remains available as data for
    /// anchoring diagnostics within a single run; it is simply not part of this type's notion of equality.
    /// </remarks>
    public bool Equals(HandlerModel? other) =>
        other is not null
        && this.Kind == other.Kind
        && this.HandlerTypeName == other.HandlerTypeName
        && this.RequestTypeName == other.RequestTypeName
        && this.ResponseTypeName == other.ResponseTypeName
        && this.ServiceInterfaceName == other.ServiceInterfaceName
        && this.RunAfterOrEmpty.SequenceEqual(other.RunAfterOrEmpty)
        && this.RunBeforeOrEmpty.SequenceEqual(other.RunBeforeOrEmpty);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method. Location is deliberately excluded - see
        // the remarks on Equals above.
        var hash = 17;
        hash = (hash * 31) + this.Kind.GetHashCode();
        hash = (hash * 31) + this.HandlerTypeName.GetHashCode();
        hash = (hash * 31) + this.RequestTypeName.GetHashCode();
        hash = (hash * 31) + (this.ResponseTypeName?.GetHashCode() ?? 0);
        hash = (hash * 31) + this.ServiceInterfaceName.GetHashCode();
        foreach (var name in this.RunAfterOrEmpty)
        {
            hash = (hash * 31) + name.GetHashCode();
        }

        foreach (var name in this.RunBeforeOrEmpty)
        {
            hash = (hash * 31) + name.GetHashCode();
        }

        return hash;
    }
}
