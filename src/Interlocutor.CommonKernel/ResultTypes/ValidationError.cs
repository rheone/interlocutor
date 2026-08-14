namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Represents a single validation error consisting of a message only, with no associated error code.
/// </summary>
/// <param name="Message">The validation error message.</param>
/// <remarks>
/// <para>
/// Use <see cref="ValidationError"/> to represent a single validation failure. Messages typically describe what
/// constraint was violated (e.g., <c>"Email is required"</c>, <c>"Password must be at least 8 characters"</c>).
/// </para>
/// <para>
/// For error codes, use <see cref="ValidationError{TErrorCode}"/> instead. Aggregate multiple
/// <see cref="ValidationError"/> instances with <see cref="ValidationErrors"/>.
/// </para>
/// </remarks>
public readonly record struct ValidationError(string Message);

/// <summary>
/// Represents a single validation error consisting of a message and an associated error code.
/// </summary>
/// <typeparam name="TErrorCode">
/// The type used to represent the error code. Must support equality comparison so that error codes
/// can be reliably distinguished as unique.
/// </typeparam>
/// <param name="Message">The validation error message.</param>
/// <param name="ErrorCode">The error code associated with the validation error.</param>
/// <remarks>
/// <para>
/// Use <see cref="ValidationError{TErrorCode}"/> when you need both a human-readable message and a programmatic
/// error code (e.g., for localization, retry logic, or form field binding). The error code type is generic, so
/// you can use enums, strings, or composite types.
/// </para>
/// <para>
/// Example: <c>ValidationError&lt;string&gt;("Email is invalid", "email_format")</c>.
/// </para>
/// </remarks>
public readonly record struct ValidationError<TErrorCode>(string Message, TErrorCode ErrorCode)
    where TErrorCode : notnull, IEquatable<TErrorCode>;
