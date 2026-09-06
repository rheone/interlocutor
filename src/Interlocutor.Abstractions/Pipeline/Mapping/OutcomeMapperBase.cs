namespace Interlocutor.Abstractions.Pipeline.Mapping;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Recognizes the built-in <c>Interlocutor.CommonKernel.ResultTypes</c> failure cases and their aggregate
/// forms, dispatching each to its own protected abstract method - a concrete <typeparamref name="TResult"/>-
/// specific mapper (e.g. <c>Interlocutor.AspNetCore.DefaultHttpOutcomeStatusMapper</c>) implements one method
/// per case instead of writing the dispatch itself. Anything else - including a consumer's own custom
/// failure type - is looked up in the optional, constructor-supplied
/// <see cref="ICustomFailureCaseRegistry{TResult}"/> before falling back to <see cref="MapUnrecognized{TFailure}"/>.
/// </summary>
/// <typeparam name="TResult">The transport- or format-specific representation a failure case maps to.</typeparam>
/// <remarks>
/// Each case method is a separate <see langword="protected"/> member, so a consumer who wants to change how
/// one case maps while keeping the rest can derive one level further and override just that method, rather
/// than reimplementing <see cref="IOutcomeMapper{TResult}"/> from scratch.
/// </remarks>
public abstract class OutcomeMapperBase<TResult> : IOutcomeMapper<TResult>
{
    private readonly ICustomFailureCaseRegistry<TResult>? _customCaseRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutcomeMapperBase{TResult}"/> class.
    /// </summary>
    /// <param name="customCaseRegistry">
    /// An optional, built <see cref="ICustomFailureCaseRegistry{TResult}"/> (from
    /// <see cref="CustomFailureCaseRegistryBuilder{TResult}"/>) consulted for a consumer-defined custom
    /// failure case type after every built-in case - including the reflective <c>NotFound&lt;TId&gt;</c>
    /// check - has been exhausted, and before falling into <see cref="MapUnrecognized{TFailure}"/>. Omit this
    /// (or pass <see langword="null"/>) to preserve today's exact behavior: any type not recognized as a
    /// built-in case reaches <see cref="MapUnrecognized{TFailure}"/> directly.
    /// </param>
    protected OutcomeMapperBase(ICustomFailureCaseRegistry<TResult>? customCaseRegistry = null)
    {
        this._customCaseRegistry = customCaseRegistry;
    }

    /// <inheritdoc/>
    public TResult Map<TFailure>(TFailure failure) =>
        failure switch
        {
            ValidationErrors validationErrors => this.MapValidationErrors(validationErrors),
            IValidationFailureCase validationFailureCase => this.MapValidationFailureCase(validationFailureCase),
            Forbidden forbidden => this.MapForbidden(forbidden),
            IForbiddenFailureCase forbiddenFailureCase => this.MapForbiddenFailureCase(forbiddenFailureCase),
            Conflict conflict => this.MapConflict(conflict),
            Failures failures => this.MapFailures(failures),
            Failure singleFailure => this.MapFailure(singleFailure),
            NotFound notFound => this.MapNotFound(notFound),
            Cancelled cancelled => this.MapCancelled(cancelled),
            Timeout timeout => this.MapTimeout(timeout),
            ITimeoutFailureCase timeoutFailureCase => this.MapTimeoutFailureCase(timeoutFailureCase),
            RateLimited rateLimited => this.MapRateLimited(rateLimited),
            IRateLimitedFailureCase rateLimitedFailureCase => this.MapRateLimitedFailureCase(rateLimitedFailureCase),
            _ => this.MapNotFoundGenericOrUnrecognized(failure),
        };

    /// <summary>Maps a <see cref="ValidationErrors"/> aggregate.</summary>
    /// <param name="validationErrors">The validation failures to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapValidationErrors(ValidationErrors validationErrors);

    /// <summary>
    /// Maps any <see cref="IValidationFailureCase"/> implementer other than <see cref="ValidationErrors"/>
    /// itself - notably <c>Interlocutor.FluentValidation.FluentValidationErrors</c>, and any future
    /// implementer this mapper was never taught about directly.
    /// </summary>
    /// <param name="validationFailureCase">The validation failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// Ordered after the exact <see cref="ValidationErrors"/> branch in <see cref="Map{TFailure}"/>, so a
    /// plain <see cref="ValidationErrors"/> failure still resolves to <see cref="MapValidationErrors"/>
    /// unchanged - this branch only ever runs for some other <see cref="IValidationFailureCase"/> implementer.
    /// </remarks>
    protected abstract TResult MapValidationFailureCase(IValidationFailureCase validationFailureCase);

    /// <summary>Maps a <see cref="NotFound"/> singular case.</summary>
    /// <param name="notFound">The not-found failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapNotFound(NotFound notFound);

    /// <summary>Maps a <see cref="Forbidden"/> case.</summary>
    /// <param name="forbidden">The forbidden failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapForbidden(Forbidden forbidden);

    /// <summary>
    /// Maps any <see cref="IForbiddenFailureCase"/> implementer other than <see cref="Forbidden"/> itself -
    /// notably a future evaluator-specific case type this mapper was never taught about directly.
    /// </summary>
    /// <param name="forbiddenFailureCase">The forbidden failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// Ordered after the exact <see cref="Forbidden"/> branch in <see cref="Map{TFailure}"/>, so a plain
    /// <see cref="Forbidden"/> failure still resolves to <see cref="MapForbidden"/> unchanged - this branch
    /// only ever runs for some other <see cref="IForbiddenFailureCase"/> implementer.
    /// </remarks>
    protected abstract TResult MapForbiddenFailureCase(IForbiddenFailureCase forbiddenFailureCase);

    /// <summary>Maps a <see cref="Conflict"/> case.</summary>
    /// <param name="conflict">The conflict failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapConflict(Conflict conflict);

    /// <summary>Maps a <see cref="Cancelled"/> case.</summary>
    /// <param name="cancelled">The cancellation failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapCancelled(Cancelled cancelled);

    /// <summary>Maps a <see cref="Timeout"/> case.</summary>
    /// <param name="timeout">The timeout failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapTimeout(Timeout timeout);

    /// <summary>
    /// Maps any <see cref="ITimeoutFailureCase"/> implementer other than <see cref="Timeout"/> itself -
    /// notably a future policy-specific case type this mapper was never taught about directly.
    /// </summary>
    /// <param name="timeoutFailureCase">The timeout failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// Ordered after the exact <see cref="Timeout"/> branch in <see cref="Map{TFailure}"/>, so a plain
    /// <see cref="Timeout"/> failure still resolves to <see cref="MapTimeout"/> unchanged - this branch only
    /// ever runs for some other <see cref="ITimeoutFailureCase"/> implementer. Unlike <see cref="MapTimeout"/>,
    /// there is no <c>Budget</c>/<c>Elapsed</c> fallback available here - <see cref="ITimeoutFailureCase"/>
    /// only guarantees <see cref="ITimeoutFailureCase.Message"/>.
    /// </remarks>
    protected abstract TResult MapTimeoutFailureCase(ITimeoutFailureCase timeoutFailureCase);

    /// <summary>Maps a <see cref="RateLimited"/> case.</summary>
    /// <param name="rateLimited">The rate-limit failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapRateLimited(RateLimited rateLimited);

    /// <summary>
    /// Maps any <see cref="IRateLimitedFailureCase"/> implementer other than <see cref="RateLimited"/> itself -
    /// notably a future limiter-specific case type this mapper was never taught about directly.
    /// </summary>
    /// <param name="rateLimitedFailureCase">The rate-limit failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// Ordered after the exact <see cref="RateLimited"/> branch in <see cref="Map{TFailure}"/>, so a plain
    /// <see cref="RateLimited"/> failure still resolves to <see cref="MapRateLimited"/> unchanged - this
    /// branch only ever runs for some other <see cref="IRateLimitedFailureCase"/> implementer. Unlike
    /// <see cref="MapRateLimited"/>, there is no <c>LimitKey</c> fallback available here -
    /// <see cref="IRateLimitedFailureCase"/> only guarantees <see cref="IRateLimitedFailureCase.Message"/>.
    /// </remarks>
    protected abstract TResult MapRateLimitedFailureCase(IRateLimitedFailureCase rateLimitedFailureCase);

    /// <summary>Maps a <see cref="Failures"/> aggregate.</summary>
    /// <param name="failures">The failures to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapFailures(Failures failures);

    /// <summary>Maps a <see cref="Failure"/> case.</summary>
    /// <param name="failure">The failure to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    protected abstract TResult MapFailure(Failure failure);

    /// <summary>Maps the generic <c>NotFound&lt;TId&gt;</c> case.</summary>
    /// <param name="entityType">The <see cref="Type"/> of entity that was not found.</param>
    /// <param name="id">The identifier that was searched for, boxed - its static type varies per closed <c>NotFound&lt;TId&gt;</c>.</param>
    /// <param name="message">The optional human-readable message carried by the <c>NotFound&lt;TId&gt;</c> instance.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// A separate override point from <see cref="MapNotFound"/> because pattern matching can't target the open
    /// generic <c>NotFound&lt;&gt;</c> case type directly - <see cref="MapNotFoundGenericOrUnrecognized{TFailure}"/>
    /// reflects the closed instance's fields and forwards them here, so a consumer who wants to change only this
    /// case's mapping still overrides a plain method rather than reimplementing the reflection.
    /// </remarks>
    protected abstract TResult MapNotFoundGeneric(Type entityType, object? id, string? message);

    /// <summary>
    /// Maps a failure case neither a built-in case nor the supplied <see cref="ICustomFailureCaseRegistry{TResult}"/>
    /// (if any) recognized.
    /// </summary>
    /// <typeparam name="TFailure">The static failure case type.</typeparam>
    /// <param name="failure">The failure value to map.</param>
    /// <returns>The mapped <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// Reached only after the constructor-supplied <see cref="ICustomFailureCaseRegistry{TResult}"/> (when
    /// one was supplied) has also missed <typeparamref name="TFailure"/> - see
    /// <see cref="MapNotFoundGenericOrUnrecognized{TFailure}"/> for the exact ordering.
    /// </remarks>
    protected abstract TResult MapUnrecognized<TFailure>(TFailure failure);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2090:RequiresUnreferencedCode",
        Justification = "The only closed generic ever tested here is Interlocutor.CommonKernel.ResultTypes."
            + "NotFound<TId> - a library-owned type whose EntityType/Id/Message properties are always kept by "
            + "the trimmer independent of this reflective read, since every consumer that produces a NotFound<> "
            + "failure also constructs or pattern-matches it directly elsewhere. See "
            + "documentation/trim-and-aot-support.md."
    )]
    private TResult MapNotFoundGenericOrUnrecognized<TFailure>(TFailure failure)
    {
        // Pattern matching doesn't work against the open generic NotFound<> case type, so fall back to
        // reflection: check whether TFailure closes NotFound<TId> for any TId, then read its fields and
        // forward to the overridable MapNotFoundGeneric rather than building the mapping here directly.
        var failureType = typeof(TFailure);
        if (failureType.IsGenericType && failureType.GetGenericTypeDefinition() == typeof(NotFound<>))
        {
            var entityType = (Type)failureType.GetProperty("EntityType")!.GetValue(failure)!;
            var id = failureType.GetProperty("Id")?.GetValue(failure);
            var message = (string?)failureType.GetProperty("Message")?.GetValue(failure);
            return this.MapNotFoundGeneric(entityType, id, message);
        }

        // Every built-in case (and NotFound<TId> above) has now been exhausted. Consult the consumer-supplied
        // custom-case registry, if any, before falling into the subclass's MapUnrecognized catch-all - a
        // registry hit maps TFailure distinctly instead of landing in that single generic bucket.
        if (this._customCaseRegistry is not null && this._customCaseRegistry.TryMap(failure, out var customResult))
        {
            return customResult;
        }

        return this.MapUnrecognized(failure);
    }
}
