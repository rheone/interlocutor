namespace Mediance.Abstractions.Commands;

/// <summary>
/// Marks a command that produces a typed response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of data returned when the command is handled.</typeparam>
public interface ICommand<TResponse>;

/// <summary>
/// Marks a command that produces no response data.
/// </summary>
/// <remarks>
/// <para>
/// Use the non-generic <see cref="ICommand"/> for fire-and-forget or side-effect-only
/// operations. The handler returns a <c>Result</c> (success or failure) without carrying
/// a payload.
/// </para>
/// </remarks>
public interface ICommand;
