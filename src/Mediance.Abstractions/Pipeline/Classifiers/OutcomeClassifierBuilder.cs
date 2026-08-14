namespace Mediance.Abstractions.Pipeline.Classifiers;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Builds an immutable <see cref="IOutcomeClassifier"/> that classifies operation outcomes
/// as success or failure. Seeded with all kernel types via open-generic matching; consumers
/// may register additional custom types.
/// </summary>
/// <remarks>
/// <para>
/// The builder collects type registrations and predicates, then produces an immutable classifier.
/// It is pre-seeded with all Mediance.CommonKernel result types so new arities cannot silently
/// regress classification behavior.
/// </para>
/// <para>
/// Unregistered types follow the fail-closed policy: unknown outcomes are classified as failures,
/// so consumers who forget to register a custom type see immediate loud rollbacks rather than
/// silently committed writes.
/// </para>
/// </remarks>
public sealed class OutcomeClassifierBuilder
{
    private static IOutcomeClassifier? _defaultInstance;
    private static readonly object _lockObject = new object();

    private readonly Dictionary<Type, OutcomeClassification> _classifications = new();
    private readonly List<(Func<object?, bool> Predicate, string Label, bool IsFailure)> _predicates = new();

    /// <summary>
    /// Gets the shared default classifier instance, pre-seeded with all kernel types.
    /// </summary>
    /// <value>
    /// The shared default classifier instance, pre-seeded with all kernel types.
    /// </value>
    public static IOutcomeClassifier DefaultClassifier
    {
        get
        {
            if (_defaultInstance is null)
            {
                lock (_lockObject)
                {
                    _defaultInstance ??= new OutcomeClassifierBuilder().Build();
                }
            }

            return _defaultInstance;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutcomeClassifierBuilder"/> class.
    /// Initializes a new builder, pre-seeded with all Mediance.CommonKernel case types.
    /// </summary>
    public OutcomeClassifierBuilder()
    {
        // Seed with kernel types using open-generic matching where applicable.
        // This ensures new union arities cannot silently regress.
        this.RegisterSuccessType<Success>("Success");
        this.RegisterFailureType<NotFound>("NotFound");
        this.RegisterFailureType<Forbidden>("Forbidden");
        this.RegisterFailureType<Conflict>("Conflict");
        this.RegisterFailureType<ValidationErrors>("ValidationError");
        this.RegisterFailureType<Failures>("Failure");
        this.RegisterFailureType<Conflict>("Conflict");
        this.RegisterFailureType<Conflicts>("Conflict");
        this.RegisterFailureType<Failure>("Failure");

        // Register generic variants via predicate to handle open-ended shapes.
        // This covers ValidationError<TErrorCode>, ValidationErrors<TErrorCode>, and
        // any future generic result types.
        this.RegisterPredicate(
            response => response?.GetType().IsGenericType == true && response.GetType().Name.StartsWith("ValidationError"),
            "ValidationError",
            isFailure: true
        );
    }

    /// <summary>
    /// Registers a type as a success outcome.
    /// </summary>
    /// <typeparam name="TSuccess">The success type to register.</typeparam>
    /// <param name="label">The human-readable label for this outcome.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterSuccessType<TSuccess>(string label)
        where TSuccess : notnull
    {
        this._classifications[typeof(TSuccess)] = new OutcomeClassification(label, false);
        return this;
    }

    /// <summary>
    /// Registers a type as a failure outcome.
    /// </summary>
    /// <typeparam name="TFailure">The failure type to register.</typeparam>
    /// <param name="label">The human-readable label for this outcome.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterFailureType<TFailure>(string label)
        where TFailure : notnull
    {
        this._classifications[typeof(TFailure)] = new OutcomeClassification(label, true);
        return this;
    }

    /// <summary>
    /// Registers a type (by runtime Type) as a failure outcome.
    /// </summary>
    /// <param name="type">The failure type to register.</param>
    /// <param name="label">The human-readable label for this outcome.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterFailureType(Type type, string label)
    {
        ArgumentNullException.ThrowIfNull(type);
        this._classifications[type] = new OutcomeClassification(label, true);
        return this;
    }

    /// <summary>
    /// Registers a predicate-based classifier for outcome types that cannot be expressed by type alone.
    /// </summary>
    /// <remarks>
    /// Predicates are evaluated in registration order when a response's type is not found in the
    /// type registration table. The first predicate to return true wins; if none match, the
    /// fail-closed policy applies (unknown = failure).
    /// </remarks>
    /// <param name="predicate">A function that returns true if the response matches this classification.</param>
    /// <param name="label">The human-readable label for matching outcomes.</param>
    /// <param name="isFailure">Whether matching outcomes are failures.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterPredicate(Func<object?, bool> predicate, string label, bool isFailure)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(label);
        this._predicates.Add((predicate, label, isFailure));
        return this;
    }

    /// <summary>
    /// Builds an immutable classifier from the registrations.
    /// </summary>
    /// <returns>An immutable classifier that applies all registered classifications.</returns>
    public IOutcomeClassifier Build() =>
        new DefaultOutcomeClassifier(this._classifications.ToImmutableDictionary(), this._predicates.ToImmutableList());

    /// <summary>
    /// Internal immutable classifier implementation produced by the builder.
    /// </summary>
    private sealed class DefaultOutcomeClassifier : IOutcomeClassifier
    {
        private readonly ImmutableDictionary<Type, OutcomeClassification> _classifications;
        private readonly ImmutableList<(Func<object?, bool> Predicate, string Label, bool IsFailure)> _predicates;

        internal DefaultOutcomeClassifier(
            ImmutableDictionary<Type, OutcomeClassification> classifications,
            ImmutableList<(Func<object?, bool>, string, bool)> predicates
        )
        {
            this._classifications = classifications;
            this._predicates = predicates;
        }

        public bool IsFailure(object? response)
        {
            var unwrapped = Unwrap(response);
            var type = unwrapped?.GetType();

            // Check type registrations first.
            if (type is not null && this._classifications.TryGetValue(type, out var classification))
            {
                return classification.IsFailure;
            }

            // Check predicates in order.
            foreach (var (predicate, _, isFailure) in this._predicates)
            {
                if (predicate(unwrapped))
                {
                    return isFailure;
                }
            }

            // Fail-closed policy: unregistered types are failures, so consumers
            // see immediate loud rollbacks.
            return true;
        }

        public string GetLabel(object? response)
        {
            var unwrapped = Unwrap(response);
            var type = unwrapped?.GetType();

            // Null/None outcome.
            if (unwrapped is null)
            {
                return "None";
            }

            // Check type registrations first.
            if (type is not null && this._classifications.TryGetValue(type, out var classification))
            {
                return classification.Label;
            }

            // Check predicates in order.
            foreach (var (predicate, label, _) in this._predicates)
            {
                if (predicate(unwrapped))
                {
                    return label;
                }
            }

            // Fallback: use runtime type name for unregistered types.
            return type?.Name ?? "Unknown";
        }

        // Unwrap compiler-generated union types to access their underlying Value.
        private static object? Unwrap(object? response) => response is IUnion union ? union.Value : response;
    }
}

/// <summary>
/// Describes how an outcome type is classified (success vs. failure, with human-readable label).
/// </summary>
internal sealed record class OutcomeClassification(string Label, bool IsFailure);
