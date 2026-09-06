namespace Interlocutor.Generator.Models;

/// <summary>
/// Discriminates which request marker interface a discovered request type implements, for the
/// missing-handler diagnostic's matching rule.
/// </summary>
internal enum RequestKind
{
    /// <summary>Implements <c>IQuery&lt;TResponse&gt;</c> - needs a matching <c>IQueryHandler&lt;,&gt;</c>.</summary>
    Query,

    /// <summary>Implements <c>ICommand&lt;TResponse&gt;</c> - needs a matching <c>ICommandHandler&lt;,&gt;</c>.</summary>
    Command,

    /// <summary>
    /// Implements the non-generic <c>ICommand</c> - needs at least one matching
    /// <c>IBareCommandHandler&lt;TCommand, *&gt;</c> for any result type.
    /// </summary>
    BareCommand,
}
