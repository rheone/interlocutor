namespace Mediance.CommonKernel.ResultTypes;

using System.Collections.Immutable;

/// <summary>
/// Represents a single validation error consisting of a message only, with no associated error code.
/// </summary>
/// <param name="message">The validation error message.</param>
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
public readonly record struct ValidationError(string message);

/// <summary>
/// Represents a single validation error consisting of a message and an associated error code.
/// </summary>
/// <typeparam name="TErrorCode">
/// The type used to represent the error code. Must support equality comparison so that error codes
/// can be reliably distinguished as unique.
/// </typeparam>
/// <param name="message">The validation error message.</param>
/// <param name="errorCode">The error code associated with the validation error.</param>
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
public readonly record struct ValidationError<TErrorCode>(string message, TErrorCode errorCode)
    where TErrorCode : notnull, IEquatable<TErrorCode>;

/// <summary>
/// Represents an immutable failure consisting of one or more distinct validation error messages,
/// with no associated error codes.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ValidationErrors"/> as a failure case in operation outcome unions when you need to return
/// multiple validation failures from a request. It aggregates <see cref="ValidationError"/> instances, removes
/// duplicates, and preserves order.
/// </para>
/// <para>
/// Typically raised by the <c>ValidationBehavior</c> pipeline when a request implements FluentValidation validators
/// and one or more validators report failures. Example outcome type: <c>OperationOutcome&lt;UserDto, ValidationErrors&gt;</c>.
/// </para>
/// <para>
/// For validation errors with codes, use property-based collections instead (e.g., <c>Dictionary&lt;string, List&lt;(code, message)&gt;&gt;</c>).
/// </para>
/// </remarks>
public readonly record struct ValidationErrors
{
    private readonly ImmutableArray<ValidationError> _errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors"/> class from the specified validation errors.
    /// </summary>
    /// <param name="errors">
    /// The validation errors that describe why validation failed. Duplicate errors are removed, preserving
    /// the order of first occurrence.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> contains no elements.</exception>
    public ValidationErrors(IEnumerable<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        _errors = [.. errors.Distinct()];

        if (_errors.IsEmpty)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors"/> class from the specified messages.
    /// </summary>
    /// <param name="messages">
    /// The validation error messages that describe why validation failed. Duplicate messages are removed,
    /// preserving the order of first occurrence.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="messages"/> contains no elements.</exception>
    public ValidationErrors(IEnumerable<string> messages)
        : this((messages ?? throw new ArgumentNullException(nameof(messages))).Select(m => new ValidationError(m))) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors"/> class from a single validation error message.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ValidationErrors(string message)
        : this([new ValidationError(message)]) { }

    /// <summary>
    /// Gets the number of distinct validation error messages.
    /// </summary>
    /// <value>The count of distinct validation error messages.</value>
    public int Count => _errors.Length;

    /// <summary>
    /// Gets the distinct set of validation errors, in the order they were first encountered.
    /// </summary>
    /// <value>An unmodifiable collection of the distinct <see cref="ValidationError"/> values provided at creation.</value>
    public IReadOnlyList<ValidationError> Errors => _errors;

    /// <summary>
    /// Gets the distinct set of validation error messages, in the order they were first encountered.
    /// </summary>
    /// <value>An unmodifiable collection of the distinct messages found in <see cref="Errors"/>.</value>
    public IReadOnlyList<string> Messages => [.. _errors.Select(e => e.message).Distinct()];
}
