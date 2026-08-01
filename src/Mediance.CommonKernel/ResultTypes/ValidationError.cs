using System.Collections.Immutable;

namespace Mediance.CommonKernel.ResultTypes;

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
public sealed record ValidationErrors
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

/// <summary>
/// Represents an immutable failure consisting of one or more distinct validation errors, each carrying
/// a message and an error code.
/// </summary>
/// <typeparam name="TErrorCode">
/// The type used to represent each validation error's error code. Must support equality comparison so that
/// error codes can be reliably distinguished as unique. Every error supplied to a single
/// <see cref="ValidationErrors{TErrorCode}"/> instance must use this same <typeparamref name="TErrorCode"/> type;
/// this is enforced by the compiler since the type parameter is fixed for the entire instance.
/// </typeparam>
public sealed record ValidationErrors<TErrorCode>
    where TErrorCode : notnull, IEquatable<TErrorCode>
{
    private readonly ImmutableArray<ValidationError<TErrorCode>> _errors;
    private readonly ImmutableArray<string> _uniqueMessages;
    private readonly ImmutableArray<TErrorCode> _uniqueErrorCodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors{TErrorCode}"/> class from the specified
    /// validation errors.
    /// </summary>
    /// <param name="errors">
    /// The validation errors that describe why validation failed. Duplicate message-and-code pairs are removed,
    /// preserving the order of first occurrence.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> contains no elements.</exception>
    public ValidationErrors(IEnumerable<ValidationError<TErrorCode>> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        _errors = [.. errors.Distinct()];

        if (_errors.IsEmpty)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        // Computed once here, rather than on every property access, and each preserves the order
        // in which the corresponding message/error code was first encountered in _errors.
        _uniqueMessages = [.. _errors.Select(e => e.Message).Distinct()];
        _uniqueErrorCodes = [.. _errors.Select(e => e.ErrorCode).Distinct()];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors{TErrorCode}"/> class from the specified
    /// message and error code tuples.
    /// </summary>
    /// <param name="errors">
    /// The message and error code pairs that describe why validation failed. Duplicate pairs are removed,
    /// preserving the order of first occurrence. Every tuple must use the same <typeparamref name="TErrorCode"/>
    /// type, which the compiler enforces.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> contains no elements.</exception>
    public ValidationErrors(IEnumerable<(string Message, TErrorCode ErrorCode)> errors)
        : this(
            (errors ?? throw new ArgumentNullException(nameof(errors))).Select(e => new ValidationError<TErrorCode>(
                e.Message,
                e.ErrorCode
            ))
        ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors{TErrorCode}"/> class from a single
    /// message and error code pair.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="errorCode">The error code associated with the validation error.</param>
    public ValidationErrors(string message, TErrorCode errorCode)
        : this([new ValidationError<TErrorCode>(message, errorCode)]) { }

    /// <summary>
    /// Gets the number of distinct validation errors (unique message and error code pairs).
    /// </summary>
    /// <value>The count of distinct <see cref="ValidationError{TErrorCode}"/> values.</value>
    public int Count => _errors.Length;

    /// <summary>
    /// Gets the number of distinct validation error messages, irrespective of error code.
    /// </summary>
    /// <value>The count of distinct values in <see cref="Messages"/>.</value>
    public int UniqueErrorCount => _uniqueMessages.Length;

    /// <summary>
    /// Gets the number of distinct error codes present in the original validation errors.
    /// </summary>
    /// <value>The count of distinct values in <see cref="ErrorCodes"/>.</value>
    public int UniqueErrorCodeCount => _uniqueErrorCodes.Length;

    /// <summary>
    /// Gets the distinct set of validation errors, each with its message and error code, in the order
    /// they were first encountered.
    /// </summary>
    /// <value>An unmodifiable collection of the distinct <see cref="ValidationError{TErrorCode}"/> values provided at creation.</value>
    public IReadOnlyList<ValidationError<TErrorCode>> Errors => _errors;

    /// <summary>
    /// Gets the distinct set of validation error messages, without their error codes, in the order
    /// they were first encountered.
    /// </summary>
    /// <value>An unmodifiable collection of the distinct messages found in <see cref="Errors"/>.</value>
    public IReadOnlyList<string> Messages => _uniqueMessages;

    /// <summary>
    /// Gets the distinct set of error codes present in the original validation errors, in the order
    /// they were first encountered.
    /// </summary>
    /// <value>An unmodifiable collection of the distinct error codes found in <see cref="Errors"/>.</value>
    public IReadOnlyList<TErrorCode> ErrorCodes => _uniqueErrorCodes;
}
