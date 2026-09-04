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

    /// <summary>The base notification marker interface, <c>INotification</c>.</summary>
    public const string Notification = "Interlocutor.Abstractions.Notifications.INotification";

    /// <summary>The relative-ordering attribute, <c>NotificationHandlerRunAfterAttribute</c>.</summary>
    public const string NotificationHandlerRunAfter =
        "Interlocutor.Abstractions.Notifications.NotificationHandlerRunAfterAttribute";

    /// <summary>The relative-ordering attribute, <c>NotificationHandlerRunBeforeAttribute</c>.</summary>
    public const string NotificationHandlerRunBefore =
        "Interlocutor.Abstractions.Notifications.NotificationHandlerRunBeforeAttribute";

    /// <summary>
    /// The base outcome marker interface, <c>IOutcome</c> - not to be confused with
    /// <see cref="ConcreteNonGenericOutcome"/>, the concrete non-generic <c>Outcome</c> union that implements it.
    /// </summary>
    public const string Outcome = "Interlocutor.CommonKernel.ResultTypes.IOutcome";

    /// <summary>The open-generic-handler-ambiguity escape hatch, <c>HandlerOverrideAttribute</c>.</summary>
    public const string HandlerOverride = "Interlocutor.Abstractions.Core.HandlerOverrideAttribute";

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

    /// <summary>
    /// FluentValidation's open generic validator base class, <c>AbstractValidator&lt;T&gt;</c>. Resolved by
    /// name only - <c>Interlocutor.Generator</c> takes no compile-time dependency on FluentValidation itself,
    /// so this resolves to <see langword="null"/> in a compilation that doesn't reference it.
    /// </summary>
    public const string AbstractValidator = "FluentValidation.AbstractValidator`1";

    /// <summary>The minimal validation-shaped failure case marker, <c>IValidationFailureCase</c>.</summary>
    public const string ValidationFailureCase = "Interlocutor.CommonKernel.ResultTypes.IValidationFailureCase";

    /// <summary>
    /// The reason-only, single validation failure case type, <c>ValidationError</c>. Unlike
    /// <see cref="ValidationErrors"/>, this type does not itself implement <see cref="ValidationFailureCase"/> -
    /// see <c>documentation/messages-and-result-types.md</c>'s naming convention callout.
    /// </summary>
    public const string ValidationError = "Interlocutor.CommonKernel.ResultTypes.ValidationError";

    /// <summary>The error-code-carrying single validation failure case type, <c>ValidationError&lt;TErrorCode&gt;</c>.</summary>
    public const string ValidationErrorOfErrorCode = "Interlocutor.CommonKernel.ResultTypes.ValidationError`1";

    /// <summary>
    /// The library's own non-generic, fixed-shape <c>Outcome</c> union (<c>Success | Failure</c>) - unlike
    /// every generic arity, its case set can never include a validation-shaped case, so it is recognized by
    /// this exact metadata name rather than by inspecting type arguments it doesn't have.
    /// </summary>
    public const string ConcreteNonGenericOutcome = "Interlocutor.CommonKernel.Outcome";

    /// <summary>
    /// The member-generation marker attribute, <c>GenerateOutcomeMembersAttribute</c> - see
    /// <see cref="OutcomeMemberGenerator"/>.
    /// </summary>
    public const string GenerateOutcomeMembersAttribute = "Interlocutor.CommonKernel.GenerateOutcomeMembersAttribute";

    /// <summary>
    /// The HTTP-result-overload-generation marker attribute, <c>GenerateResultExtensionsAttribute</c> - see
    /// <see cref="ResultExtensionsGenerator"/>.
    /// </summary>
    public const string GenerateResultExtensionsAttribute = "Interlocutor.AspNetCore.GenerateResultExtensionsAttribute";
}
