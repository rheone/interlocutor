namespace Interlocutor.Generator;

/// <summary>
/// Fully-qualified metadata names for the Interlocutor types the generator reasons about via Roslyn's
/// semantic model - never by string-matching source text, only by resolving these names to real symbols
/// through <see cref="Microsoft.CodeAnalysis.Compilation.GetTypeByMetadataName(string)"/>.
/// </summary>
internal static class WellKnownTypeNames
{
    /// <summary>The open generic query-handler contract, <c>IQueryHandler&lt;TQuery, TResponse&gt;</c>.</summary>
    public const string QueryHandler = "Interlocutor.Abstractions.Queries.IQueryHandler`2";

    /// <summary>The open generic query marker, <c>IQuery&lt;TResponse&gt;</c>.</summary>
    public const string Query = "Interlocutor.Abstractions.Queries.IQuery`1";

    /// <summary>The open generic command-handler contract, <c>ICommandHandler&lt;TCommand, TResponse&gt;</c>.</summary>
    public const string CommandHandler = "Interlocutor.Abstractions.Commands.ICommandHandler`2";

    /// <summary>The open generic command marker, <c>ICommand&lt;TResponse&gt;</c>.</summary>
    public const string Command = "Interlocutor.Abstractions.Commands.ICommand`1";

    /// <summary>The non-generic unit-command marker, <c>ICommand</c>.</summary>
    public const string VoidCommand = "Interlocutor.Abstractions.Commands.ICommand";

    /// <summary>The open generic unit-command-handler contract, <c>IVoidCommandHandler&lt;TCommand, TResult&gt;</c>.</summary>
    public const string VoidCommandHandler = "Interlocutor.Abstractions.Commands.IVoidCommandHandler`2";

    /// <summary>The open generic notification-handler contract, <c>INotificationHandler&lt;TNotification&gt;</c>.</summary>
    public const string NotificationHandler = "Interlocutor.Abstractions.Notifications.INotificationHandler`1";

    /// <summary>The open generic pipeline-behavior contract, <c>IPipelineBehavior&lt;TRequest, TResponse&gt;</c>.</summary>
    public const string PipelineBehavior = "Interlocutor.Abstractions.Pipeline.IPipelineBehavior`2";

    /// <summary>The base outcome marker interface, <c>IOperationOutcome</c>.</summary>
    public const string OperationOutcome = "Interlocutor.CommonKernel.IOperationOutcome";

    /// <summary>
    /// The assembly-level attribute identifying a referenced assembly's generated registration module
    /// (ticket 12), <c>InterlocutorGeneratedModuleAttribute</c>.
    /// </summary>
    public const string GeneratedModuleAttribute =
        "Interlocutor.DependencyInjection.Abstractions.InterlocutorGeneratedModuleAttribute";

    /// <summary>
    /// The assembly-level attribute restating one referenced assembly's handler shape for the missing-handler
    /// diagnostic (ticket 12), <c>InterlocutorGeneratedHandlerAttribute</c>.
    /// </summary>
    public const string GeneratedHandlerAttribute =
        "Interlocutor.DependencyInjection.Abstractions.InterlocutorGeneratedHandlerAttribute";
}
