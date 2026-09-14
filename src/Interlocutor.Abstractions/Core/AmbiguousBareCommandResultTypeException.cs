namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Thrown by runtime-typed dispatch (<see cref="ISender.SendAsync(object, CancellationToken)"/>) when a bare
/// command's result type cannot be resolved unambiguously - more than one <c>IBareCommandHandler&lt;TCommand,
/// TResult&gt;</c> closing, over more than one distinct <c>TResult</c>, was found in loaded assemblies for the
/// same command type.
/// </summary>
/// <remarks>
/// <para>
/// A bare command (<c>ICommand</c>) declares no <c>TResult</c> of its own - the strongly-typed
/// <c>SendAsync&lt;TResult&gt;</c> overload's caller-supplied type argument picks it. Runtime-typed dispatch has
/// no caller-supplied type argument, so it discovers <c>TResult</c> by scanning loaded assemblies for a
/// <c>IBareCommandHandler&lt;TCommand, TResult&gt;</c> implementation closing over the concrete command type.
/// When exactly one distinct <c>TResult</c> is found this way, that closing is unambiguous and dispatch
/// proceeds exactly as it always has. When more than one distinct <c>TResult</c> is found - typically two
/// handlers accidentally registered against the same command with different result shapes - there is no
/// principled way to pick a winner, so this is thrown instead of silently returning whichever closing the
/// assembly-enumeration order happened to find first.
/// </para>
/// <para>
/// This is distinct from <see cref="HandlerNotRegisteredException"/>, which is thrown when no closing at all
/// is found (or found but not registered in the service provider); this exception means too many were found,
/// not too few.
/// </para>
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's CommandType/AmbiguousResultTypes are load-bearing - the whole point of catching it is to read
// which result types were ambiguous - so a parameterless or message-only constructor would produce a useless
// exception missing the one thing callers need. Suppressed here rather than adding constructors that would
// only confuse callers into using them, mirroring HandlerNotRegisteredException's identical rationale.
#pragma warning disable RCS1194
public sealed class AmbiguousBareCommandResultTypeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="AmbiguousBareCommandResultTypeException"/> class.</summary>
    /// <param name="commandType">The bare command type more than one distinct result type was found for.</param>
    /// <param name="ambiguousResultTypes">Every distinct <c>TResult</c> a loaded closing was found for.</param>
    public AmbiguousBareCommandResultTypeException(Type commandType, IReadOnlyList<Type> ambiguousResultTypes)
        : base(BuildMessage(commandType, ambiguousResultTypes))
    {
        this.CommandType = commandType;
        this.AmbiguousResultTypes = ambiguousResultTypes;
    }

    /// <summary>Gets the bare command type more than one distinct result type was found for.</summary>
    /// <value>The bare command type more than one distinct result type was found for.</value>
    public Type CommandType { get; }

    /// <summary>Gets every distinct <c>TResult</c> a loaded <c>IBareCommandHandler&lt;,&gt;</c> closing was found for.</summary>
    /// <value>Every distinct <c>TResult</c> a loaded <c>IBareCommandHandler&lt;,&gt;</c> closing was found for.</value>
    public IReadOnlyList<Type> AmbiguousResultTypes { get; }

    private static string BuildMessage(Type commandType, IReadOnlyList<Type> ambiguousResultTypes) =>
        $"Runtime-typed dispatch cannot resolve a single result type for bare command '{commandType.FullName}': "
        + $"found {ambiguousResultTypes.Count} distinct IBareCommandHandler<{commandType.Name}, TResult> "
        + "closings over result types "
        + $"{string.Join(", ", ambiguousResultTypes.Select(resultType => $"'{resultType.FullName}'"))}. "
        + "Register only one IBareCommandHandler<,> closing per bare command type, or dispatch this command "
        + "through the strongly-typed SendAsync<TResult> overload instead, which picks TResult explicitly "
        + "and never reaches this scan.";
}
#pragma warning restore RCS1194
