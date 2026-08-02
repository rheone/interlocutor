namespace Mediance.CommonKernel.ResultTypes;

using System.Collections.Immutable;

/// <summary>
/// Represents a single validation error consisting of a message only, with no associated error code.
/// </summary>
/// <param name="Message">The validation error message.</param>
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
public readonly record struct ValidationError<TErrorCode>(string Message, TErrorCode ErrorCode)
    where TErrorCode : notnull, IEquatable<TErrorCode>;

/// <summary>
/// Represents an immutable failure consisting of one or more distinct validation error messages,
/// with no associated error codes.
/// </summary>
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
    public IReadOnlyList<string> Messages => [.. _errors.Select(e => e.Message).Distinct()];
}
