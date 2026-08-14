namespace Interlocutor.CommonKernel.ResultTypes;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
/// For validation errors that carry error codes, use <see cref="ValidationErrors{TErrorCode}"/>.
/// </para>
/// </remarks>
public readonly record struct ValidationErrors
{
    private readonly ImmutableArray<ValidationError> _errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors"/> struct from the specified validation errors.
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

        this._errors = [.. errors.Distinct()];

        if (this._errors.IsEmpty)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors"/> struct from the specified messages.
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
    /// Initializes a new instance of the <see cref="ValidationErrors"/> struct from a single validation error message.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ValidationErrors(string message)
        : this([new ValidationError(message)]) { }

    /// <summary>
    /// Gets the number of distinct validation error messages.
    /// </summary>
    /// <value>The count of distinct validation error messages, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors"/> (zero-initialized) is inert and reports a count of zero,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int Count => this._errors.IsDefault ? 0 : this._errors.Length;

    /// <summary>
    /// Gets the distinct set of validation errors, in the order they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="ValidationError"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors"/> (zero-initialized) is inert and returns an empty collection,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<ValidationError> Items => this._errors.IsDefault ? [] : this._errors;

    /// <summary>
    /// Gets the distinct set of validation errors, in the order they were first encountered.
    /// Semantic projection of <see cref="Items"/>.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="ValidationError"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    public IReadOnlyList<ValidationError> Errors => this.Items;

    /// <summary>
    /// Gets the distinct set of validation error messages, in the order they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct messages found in <see cref="Items"/>, or an empty collection
    /// if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors"/> (zero-initialized) is inert and returns an empty collection,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<string> Messages => this._errors.IsDefault ? [] : [.. this._errors.Select(e => e.Message).Distinct()];
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
public readonly record struct ValidationErrors<TErrorCode>
    where TErrorCode : notnull, IEquatable<TErrorCode>
{
    private readonly ImmutableArray<ValidationError<TErrorCode>> _errors;
    private readonly ImmutableArray<string> _uniqueMessages;
    private readonly ImmutableArray<TErrorCode> _uniqueErrorCodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors{TErrorCode}"/> struct.
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

        this._errors = [.. errors.Distinct()];

        if (this._errors.IsEmpty)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        // Computed once here, rather than on every property access, and each preserves the order
        // in which the corresponding message/error code was first encountered in _errors.
        this._uniqueMessages = [.. this._errors.Select(e => e.Message).Distinct()];
        this._uniqueErrorCodes = [.. this._errors.Select(e => e.ErrorCode).Distinct()];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrors{TErrorCode}"/> struct.
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
    /// Initializes a new instance of the <see cref="ValidationErrors{TErrorCode}"/> struct.
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
    /// <value>The count of distinct <see cref="ValidationError{TErrorCode}"/> values, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors{TErrorCode}"/> (zero-initialized) is inert and reports counts of zero,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int Count => this._errors.IsDefault ? 0 : this._errors.Length;

    /// <summary>
    /// Gets the number of distinct validation error messages, irrespective of error code.
    /// </summary>
    /// <value>The count of distinct values in <see cref="Messages"/>, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors{TErrorCode}"/> (zero-initialized) is inert and reports counts of zero,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int UniqueErrorCount => this._uniqueMessages.IsDefault ? 0 : this._uniqueMessages.Length;

    /// <summary>
    /// Gets the number of distinct error codes present in the original validation errors.
    /// </summary>
    /// <value>The count of distinct values in <see cref="ErrorCodes"/>, or zero if this instance is the default value.</value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors{TErrorCode}"/> (zero-initialized) is inert and reports counts of zero,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public int UniqueErrorCodeCount => this._uniqueErrorCodes.IsDefault ? 0 : this._uniqueErrorCodes.Length;

    /// <summary>
    /// Gets the distinct set of validation errors, each with its message and error code, in the order
    /// they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="ValidationError{TErrorCode}"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors{TErrorCode}"/> (zero-initialized) is inert and returns empty collections,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<ValidationError<TErrorCode>> Items => this._errors.IsDefault ? [] : this._errors;

    /// <summary>
    /// Gets the distinct set of validation errors, each with its message and error code, in the order
    /// they were first encountered.
    /// Semantic projection of <see cref="Items"/>.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct <see cref="ValidationError{TErrorCode}"/> values provided at creation,
    /// or an empty collection if this instance is the default value.
    /// </value>
    public IReadOnlyList<ValidationError<TErrorCode>> Errors => this.Items;

    /// <summary>
    /// Gets the distinct set of validation error messages, without their error codes, in the order
    /// they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct messages found in <see cref="Items"/>, or an empty collection
    /// if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors{TErrorCode}"/> (zero-initialized) is inert and returns empty collections,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<string> Messages => this._uniqueMessages.IsDefault ? [] : this._uniqueMessages;

    /// <summary>
    /// Gets the distinct set of error codes present in the original validation errors, in the order
    /// they were first encountered.
    /// </summary>
    /// <value>
    /// An unmodifiable collection of the distinct error codes found in <see cref="Items"/>, or an empty collection
    /// if this instance is the default value.
    /// </value>
    /// <remarks>
    /// The default value of <see cref="ValidationErrors{TErrorCode}"/> (zero-initialized) is inert and returns empty collections,
    /// rather than throwing. This permits the pipeline to pass a default response value when a handler has not yet
    /// produced one. Explicitly constructing this type with an empty sequence still throws.
    /// </remarks>
    public IReadOnlyList<TErrorCode> ErrorCodes => this._uniqueErrorCodes.IsDefault ? [] : this._uniqueErrorCodes;
}
